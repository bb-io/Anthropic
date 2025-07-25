using Apps.Anthropic.Constants;
using Apps.Anthropic.Invocable;
using Apps.Anthropic.Models.Entities;
using Apps.Anthropic.Models.Request;
using Apps.Anthropic.Models.Response;
using Apps.Anthropic.Utils;
using Blackbird.Applications.SDK.Blueprints;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;
using Blackbird.Filters.Constants;
using Blackbird.Filters.Enums;
using Blackbird.Filters.Extensions;
using Blackbird.Filters.Transformations;
using Newtonsoft.Json;

namespace Apps.Anthropic.Actions;

[ActionList("Translation")]
public class TranslationActions(InvocationContext invocationContext, IFileManagementClient fileManagementClient) : AnthropicInvocable(invocationContext, fileManagementClient)
{
    private readonly AiUtilities _aiUtilities = new(invocationContext, fileManagementClient);
    
    [BlueprintActionDefinition(BlueprintAction.TranslateFile)]
    [Action("Translate", Description = "Translate file content retrieved from a CMS or file storage. The output can be used in compatible actions.")]
    public async Task<TranslateResponse> Translate([ActionParameter] TranslateRequest input)
    {
        var result = new TranslateResponse();
            
        var stream = await fileManagementClient.DownloadAsync(input.File);
        var content = await ErrorHandler.ExecuteWithErrorHandlingAsync(() => Transformation.Parse(stream, input.File.Name));
        content.SourceLanguage ??= input.SourceLanguage;
        content.TargetLanguage ??= input.TargetLanguage;    
        if (content.TargetLanguage == null)
        {
            throw new PluginMisconfigurationException("The target language is not defined yet. Please assign the target language in this action.");
        }
        
        if (content.SourceLanguage == null)
        {
            content.SourceLanguage = await _aiUtilities.IdentifySourceLanguageAsync(input.Model, content.Source().GetPlaintext());
        }

        var segments = content.GetSegments().ToList();
        result.TotalSegmentsCount = segments.Count;
        segments = segments.Where(x => !x.IsIgnorbale && x.IsInitial).ToList();
        
        async Task<IEnumerable<TranslationEntity>> TranslateBatch(IEnumerable<Segment> batch)
        {
            var batchForJson = batch.Select(x => new { id = x.Id, source_text = x.GetSource() }).ToList();
            var batchJson = JsonConvert.SerializeObject(batchForJson);
            var response = await _aiUtilities.SendMessageAsync(new()
            {
                Prompt = PromptBuilder.BuildTranslateUserPrompt(input.AdditionalInstructions, content, batchJson),
                SystemPrompt = input.SystemPrompt ?? PromptBuilder.BuildTranslateSystemPrompt()
            }, new()
            {
                Glossary = input.Glossary
            });
            
            List<TranslationEntity> translationEntities = new();
            try
            {
                var deserializeResponse = ResponseDeserializationHelper.DeserializeResponse(response.Text);
                if (deserializeResponse.Success)
                {
                    translationEntities = deserializeResponse.Translations;
                }
                else if(input.IgnoreDeserializationErrors == true)
                {
                    throw new PluginApplicationException("Anthropic returned an unexpected response that we cannot deserialize: " + response.Text);
                }
            } 
            catch (Exception e) when (!e.Message.Contains("Anthropic returned an unexpected response"))
            {
                if (e.Message.Contains("Unterminated string. Expected delimiter:")) 
                {
                    throw new PluginApplicationException("Anthropic returned an unexpected response. Try adjusting the model or a lower bucket size or add retries to this action. Original error: " + e.Message);
                }
                else
                {
                    throw new PluginApplicationException(e.Message);
                }
            }

            result.Usage += response.Usage;
            return translationEntities;
        }
        
        var processedBatches = await segments.Batch(input.BucketSize ?? XliffConstants.DefaultBucketSize).Process(TranslateBatch);
        var updatedCount = 0;
        foreach (var (segment, translation) in processedBatches)
        {
            var shouldTranslateFromState = segment.State == null || segment.State == SegmentState.Initial;
            if (!shouldTranslateFromState || string.IsNullOrEmpty(translation.TranslatedText))
            {
                continue;
            }

            if (segment.GetTarget() != translation.TranslatedText)
            {
                updatedCount++;
                segment.SetTarget(translation.TranslatedText);
                segment.State = SegmentState.Translated;
            }
        }

        if (input.OutputFileHandling == "original")
        {
            var targetContent = content.Target();
            result.File = await fileManagementClient.UploadAsync(targetContent.Serialize().ToStream(), targetContent.OriginalMediaType, targetContent.OriginalName);
        } 
        else
        {
            result.File = await fileManagementClient.UploadAsync(content.Serialize().ToStream(), MediaTypes.Xliff, content.XliffFileName);
        }       

        return result;
    }
}