using Apps.Anthropic.Invocable;
using Apps.Anthropic.Models.Request;
using Apps.Anthropic.Models.Response;
using Apps.Anthropic.Utils;
using Apps.Anthropic.Models.Entities;
using Apps.Anthropic.Constants;
using Blackbird.Applications.SDK.Blueprints;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Filters.Constants;
using Blackbird.Filters.Enums;
using Blackbird.Filters.Extensions;
using Blackbird.Filters.Transformations;
using Newtonsoft.Json;

namespace Apps.Anthropic.Actions;

[ActionList("Editing")]
public class EditActions(InvocationContext invocationContext, IFileManagementClient fileManagementClient) : AnthropicInvocable(invocationContext, fileManagementClient)
{
    private readonly AiUtilities _aiUtilities = new(invocationContext, fileManagementClient);
    
    [BlueprintActionDefinition(BlueprintAction.EditFile)]
    [Action("Edit", Description = "Edit a translation. This action assumes you have previously translated content in Blackbird through any translation action.")]
    public async Task<EditContentResponse> EditContent([ActionParameter] EditContentRequest input)
    {
        var result = new EditContentResponse();
        
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
        
        var segments = content.GetSegments();
        result.TotalSegmentsReviewed = segments.Count();
        segments = segments.Where(x => !x.IsIgnorbale && x.State == SegmentState.Translated).ToList();
        
        async Task<IEnumerable<TranslationEntity>> EditBatch(IEnumerable<Segment> batch)
        {
            var batchForJson = batch.Select((x, i) => new { 
                id = i, 
                source_text = x.GetSource(), 
                target_text = x.GetTarget() 
            }).ToList();
            var batchJson = JsonConvert.SerializeObject(batchForJson);
            
            var completionRequest = new CompletionRequest
            {
                Prompt = PromptBuilder.BuildEditUserPrompt(input.AdditionalInstructions, content, batchJson),
                SystemPrompt = input.SystemPrompt ?? PromptBuilder.BuildEditSystemPrompt(),
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
        
        var processedBatches = await segments.Batch(input.BucketSize ?? XliffConstants.DefaultBucketSize).Process(EditBatch);
        var updatedCount = 0;
        
        foreach (var (segment, translation) in processedBatches)
        {
            if (!string.IsNullOrEmpty(translation.TranslatedText) && segment.GetTarget() != translation.TranslatedText)
            {
                updatedCount++;
                segment.SetTarget(translation.TranslatedText);
                segment.State = SegmentState.Reviewed;
            }
        }

        result.TotalSegmentsUpdated = updatedCount;

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
    
    [BlueprintActionDefinition(BlueprintAction.EditText)]
    [Action("Edit text", Description = "Review translated text and generate an edited version")]
    public async Task<EditTextResponse> EditText([ActionParameter] EditTextRequest input)
    {
        var systemPrompt =
            $"You are receiving a source text{(input.SourceLanguage != null ? $" written in {input.SourceLanguage} " : "")}" +
            $"that was translated into target text{(input.TargetLanguage != null ? $" written in {input.TargetLanguage}" : "")}. " +
            "Review the target text and respond ONLY with the edited version of the target text. If no edits are required, respond with the original target text unchanged. " +
            "Do not include any explanations, comments, or additional text in your response. " +
            $"{(input.TargetAudience != null ? $"The target audience is {input.TargetAudience}" : string.Empty)}";

        if (input.Glossary != null)
            systemPrompt +=
            " Use relevant terms from the glossary where applicable, ensuring the translation aligns with the glossary entries for the respective languages.";

        if (input.AdditionalInstructions != null)
            systemPrompt = $"{systemPrompt} {input.AdditionalInstructions}";

        var userPrompt = @$"
    Source text: 
    {input.SourceText}

    Target text: 
    {input.TargetText}

    Important: Your response must contain ONLY the edited text, with no explanations or comments.
    ";

        if (input.Glossary != null)
        {
            var glossaryPromptPart = await _aiUtilities.GetGlossaryPromptPart(input.Glossary, input.SourceText, true);
            if (!string.IsNullOrEmpty(glossaryPromptPart))
            {
                userPrompt += glossaryPromptPart;
            }
        }

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

        return new EditTextResponse
        {
            SystemPrompt = systemPrompt,
            UserPrompt = userPrompt,
            EditedText = response.Text,
            Usage = response.Usage
        };
    }
}