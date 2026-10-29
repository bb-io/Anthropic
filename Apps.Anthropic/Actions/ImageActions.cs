using Apps.Anthropic.Extensions;
using Apps.Anthropic.Invocable;
using Apps.Anthropic.Models.Dto;
using Apps.Anthropic.Models.Identifiers;
using Apps.Anthropic.Models.Request;
using Apps.Anthropic.Models.Response;
using Apps.Anthropic.Utils;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.Sdk.Utils.Extensions.Files;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;
using Blackbird.Filters.Constants;
using Blackbird.Filters.Transformations;
using Newtonsoft.Json;

namespace Apps.Anthropic.Actions;

[ActionList("Images")]
public class ImageActions(InvocationContext invocationContext, IFileManagementClient fileManagementClient)
    : AnthropicInvocable(invocationContext)
{
    [Action("Extract localizable content from image", Description = "Extracts localizable text from an image into an XLIFF file.")]
    public async Task<ExtractLocalizableContentFromImageResponse> ExtractLocalizableContentFromImage(
        [ActionParameter] ModelIdentifier modelIdentifier,
        [ActionParameter] ExtractLocalizableContentFromImageRequest input)
    {
        modelIdentifier.Validate(InvocationContext.AuthenticationCredentialsProviders);

        var extension = Path.GetExtension(input.Image.Name).ToLowerInvariant();
        if (!FileFormatHelper.IsImage(extension))
        {
            throw new PluginMisconfigurationException(
                "The input file must be a JPEG, PNG, GIF, or WebP image.");
        }

        byte[] imageBytes;
        await using (var imageStream = await fileManagementClient.DownloadAsync(input.Image))
        {
            imageBytes = await imageStream.GetByteData();
        }

        Transformation transformation;
        await using (var transformationStream = new MemoryStream(imageBytes))
        {
            var loadResult = Transformation.Load(
                transformationStream,
                input.Image.Name,
                input.Image.ContentType);

            if (!loadResult.Success)
            {
                throw new PluginMisconfigurationException(
                    $"The original image could not be embedded in the XLIFF file: {loadResult.Error}");
            }

            transformation = loadResult.Value!;
        }

        if (string.IsNullOrWhiteSpace(transformation.Original) ||
            string.IsNullOrWhiteSpace(transformation.OriginalMediaType))
        {
            throw new PluginMisconfigurationException(
                "The original image could not be embedded in the XLIFF file.");
        }

        var sourceLanguageInstruction = string.IsNullOrWhiteSpace(input.SourceLanguage)
            ? "Detect the dominant language of the extracted text and return its BCP-47 language code."
            : $"The source language is '{input.SourceLanguage}'. Return this exact value in source_language.";

        var prompt = "Perform OCR on the supplied image and extract all visible text that should be localized. " +
                     "Return each complete text item as a separate segment in natural reading order. " +
                     "Preserve the exact source spelling, punctuation, capitalization, numbers, and meaningful line breaks. " +
                     "Do not translate, summarize, correct, merge unrelated text, or describe visual elements. " +
                     "If the image contains no localizable text, return an empty segments array and use 'und' as the source language unless a source language was supplied. " +
                     sourceLanguageInstruction + " " +
                     "Return only one valid JSON object without markdown fences or commentary. " +
                     "Use exactly this structure: {\"source_language\":\"BCP-47 code\",\"segments\":[{\"text\":\"exact text\"}]}";

        var response = await Client.ExecuteChat(new MessageRequest
        {
            System = string.Empty,
            Model = modelIdentifier.Model,
            Messages = [new Message { Role = "user", Content = prompt }],
            MaxTokens = Math.Min(8192, ModelCatalog.GetMaxOutputTokens(modelIdentifier.Model)),
            StopSequences = [],
            Temperature = 0,
            FileData = new InputFileData(imageBytes, input.Image.Name, extension)
        });

        var extraction = DeserializeExtraction(response.Text);
        var segments = (extraction.Segments ?? [])
            .Select(segment => segment.Text?.Trim())
            .Where(text => !string.IsNullOrWhiteSpace(text))
            .Select(text => text!)
            .ToList();

        var sourceLanguage = !string.IsNullOrWhiteSpace(input.SourceLanguage)
            ? input.SourceLanguage.Trim()
            : extraction.SourceLanguage?.Trim();

        if (segments.Count == 0 && string.IsNullOrWhiteSpace(sourceLanguage))
        {
            sourceLanguage = "und";
        }

        if (segments.Count > 0 && string.IsNullOrWhiteSpace(sourceLanguage))
        {
            throw new PluginApplicationException(
                "Claude extracted text but did not return its source language.");
        }

        var outputName = BuildOutputName(input.OutputFileName, input.Image.Name);
        transformation.SourceLanguage = sourceLanguage;
        transformation.BilingualFileName = outputName;

        foreach (var text in segments)
        {
            transformation.AddTranslationUnit(text);
        }

        using var outputStream = transformation.ToStream();
        var file = await fileManagementClient.UploadAsync(outputStream, MediaTypes.Xliff2, outputName);

        return new ExtractLocalizableContentFromImageResponse
        {
            File = file,
            SourceLanguage = sourceLanguage,
            SegmentsCount = segments.Count,
            Usage = response.Usage
        };
    }

    internal static ImageLocalizationExtractionDto DeserializeExtraction(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return new ImageLocalizationExtractionDto();
        }

        var json = ExtractJsonObject(content);
        try
        {
            return JsonConvert.DeserializeObject<ImageLocalizationExtractionDto>(json)
                   ?? throw new JsonSerializationException("The response was empty.");
        }
        catch (JsonException exception)
        {
            throw new PluginApplicationException(
                $"Could not parse localizable image content returned by Claude: {exception.Message}",
                exception);
        }
    }

    private static string ExtractJsonObject(string content)
    {
        var trimmed = content.Trim();
        var firstBrace = trimmed.IndexOf('{');
        var lastBrace = trimmed.LastIndexOf('}');

        return firstBrace >= 0 && lastBrace >= firstBrace
            ? trimmed[firstBrace..(lastBrace + 1)]
            : trimmed;
    }

    private static string BuildOutputName(string? requestedName, string inputName)
    {
        var baseName = string.IsNullOrWhiteSpace(requestedName)
            ? Path.GetFileNameWithoutExtension(inputName)
            : Path.GetFileNameWithoutExtension(requestedName.Trim());

        return $"{baseName}.xlf";
    }
}
