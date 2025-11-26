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
    public async Task<TranslateContentResponse> Translate([ActionParameter] TranslateContentRequest input)
    {
        var result = new TranslateContentResponse();
            
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
            var batchForJson = batch.Select((x, i) => new { id = i, source_text = x.GetSource() }).ToList();
            var batchJson = JsonConvert.SerializeObject(batchForJson);
            var completionRequest = new CompletionRequest
            {
                Prompt = PromptBuilder.BuildTranslateUserPrompt(input.AdditionalInstructions, content, batchJson),
                SystemPrompt = input.SystemPrompt ?? PromptBuilder.BuildTranslateSystemPrompt(),
                Temperature = input.Temperature,
                TopP = input.TopP,
                TopK = input.TopK,
                Model = input.Model,
                MaxTokensToSample = input.MaxTokensToSample,
                StopSequences = input.StopSequences
            };
            
            var response = await _aiUtilities.SendMessageAsync(completionRequest, new()
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

        result.UpdatedSegmentsCount = updatedCount;

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

    [BlueprintActionDefinition(BlueprintAction.TranslateText)]
    [Action("Translate text", Description = "Localize the text provided.")]
    public async Task<TranslateTextResponse> TranslateText([ActionParameter] TranslateTextRequest input)
    {
        var systemPrompt = "You are a text localizer. Localize the provided text for the specified locale while " +
                          "preserving the original text structure. Respond with localized text. Do not give any other answer but the translation. No explanation or other headers.";

        var sourceLanguage = input.SourceLanguage;
        if (string.IsNullOrEmpty(sourceLanguage))
        {
            sourceLanguage = await _aiUtilities.IdentifySourceLanguageAsync(input.Model, input.Text);
        }

        var userPrompt = $@"
Original text: {input.Text}
Source language: {sourceLanguage}
Target language: {input.TargetLanguage}
";

        if (!string.IsNullOrWhiteSpace(input.AdditionalInstructions))
        {
            userPrompt += $"\nAdditional instructions: {input.AdditionalInstructions}\n";
        }

        if (input.Glossary != null)
        {
            var glossaryPromptPart = await _aiUtilities.GetGlossaryPromptPart(input.Glossary, input.Text, true);
            if (!string.IsNullOrEmpty(glossaryPromptPart))
            {
                userPrompt +=
                    "\nEnhance the localized text by incorporating relevant terms from our glossary where applicable. " +
                    "If you encounter terms from the glossary in the text, ensure that the localized text aligns " +
                    "with the glossary entries for the respective languages. If a term has variations or synonyms, " +
                    "consider them and choose the most appropriate translation from the glossary to maintain " +
                    $"consistency and precision. {glossaryPromptPart}";
            }
        }

        userPrompt += "\nTranslated text: ";

        var completionRequest = new CompletionRequest
        {
            Prompt = userPrompt,
            SystemPrompt = systemPrompt,
            Temperature = input.Temperature,
            TopP = input.TopP,
            TopK = input.TopK,
            Model = input.Model,
            MaxTokensToSample = input.MaxTokensToSample
        };

        var response = await _aiUtilities.SendMessageAsync(completionRequest, new()
        {
            Glossary = input.Glossary
        });

        return new TranslateTextResponse
        {
            SystemPrompt = systemPrompt,
            UserPrompt = userPrompt,
            TranslatedText = response.Text,
            Usage = response.Usage
        };
    }
}