using Amazon;
using Amazon.Bedrock;
using Amazon.Bedrock.Model;
using Amazon.BedrockRuntime;
using Amazon.BedrockRuntime.Model;
using Amazon.Runtime.Documents;
using Apps.Anthropic.Constants;
using Apps.Anthropic.Models.Request;
using Apps.Anthropic.Models.Response;
using Apps.Anthropic.Utils;
using Blackbird.Applications.Sdk.Common.Authentication;
using Blackbird.Applications.Sdk.Common.Connections;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Utils.Extensions.Sdk;
using Message = Amazon.BedrockRuntime.Model.Message;

namespace Apps.Anthropic.Api;

public class AmazonBedrockSdkClient : IAnthropicClient
{
    public readonly AmazonBedrockClient ManagementClient;
    public readonly AmazonBedrockRuntimeClient ChatClient;
    private readonly IEnumerable<AuthenticationCredentialsProvider> _creds;

    public AmazonBedrockSdkClient(IEnumerable<AuthenticationCredentialsProvider> authProviders)
    {
        _creds = authProviders;
        var accessKey = _creds.Get(CredNames.AccessKey).Value;
        var secretKey = _creds.Get(CredNames.SecretKey).Value;
        var region = RegionEndpoint.GetBySystemName(_creds.Get(CredNames.Region).Value);

        ManagementClient = new AmazonBedrockClient(accessKey, secretKey, new AmazonBedrockConfig { RegionEndpoint = region });
        ChatClient = new AmazonBedrockRuntimeClient(accessKey, secretKey, new AmazonBedrockRuntimeConfig { RegionEndpoint = region });
    }

    public async Task<ConnectionValidationResponse> ValidateConnection()
    {
        try
        {
            await ManagementClient.ListFoundationModelsAsync(new ListFoundationModelsRequest());
            return new ConnectionValidationResponse { IsValid = true };
        }
        catch (Exception ex)
        {
            return new ConnectionValidationResponse
            {
                IsValid = false,
                Message = ex.Message
            };
        }
    }

    public async Task<List<ModelResponse>> ListModels()
    {
        var models = await ExecuteWithErrorHandling(async () => 
            await ManagementClient.ListFoundationModelsAsync(
                new ListFoundationModelsRequest 
                { 
                    ByProvider = "anthropic",
                    ByInferenceType = "ON_DEMAND"
                }
            )
        );
        return models.ModelSummaries.Select(x => new ModelResponse(x.ModelId, x.ModelName)).ToList();
    }

    public async Task<ResponseMessage> ExecuteChat(MessageRequest request)
    {
        var messages = new List<Message>();

        foreach (var m in request.Messages)
        {
            if (m.Role == "user" && request.FileData != null)
            {
                var contentBlocks = new List<ContentBlock>();

                string format = request.FileData.FileExtension.TrimStart('.').ToLowerInvariant();
                string name = Path.GetFileNameWithoutExtension(request.FileData.FileName);
                var fileStream = new MemoryStream(request.FileData.FileBytes);

                if (format == "pdf")
                {
                    contentBlocks.Add(new ContentBlock
                    {
                        Document = new DocumentBlock
                        {
                            Format = "pdf",
                            Name = name,
                            Source = new DocumentSource { Bytes = fileStream }
                        }
                    });
                }
                else if (FileFormatHelper.IsImage(format))
                {
                    ImageFormat imageFormat = format switch
                    {
                        "png" => ImageFormat.Png,
                        "gif" => ImageFormat.Gif,
                        "webp" => ImageFormat.Webp,
                        _ => ImageFormat.Jpeg
                    };

                    contentBlocks.Add(new ContentBlock
                    {
                        Image = new ImageBlock
                        {
                            Format = imageFormat,
                            Source = new ImageSource { Bytes = fileStream }
                        }
                    });
                }
                else
                {
                    throw new PluginMisconfigurationException(
                        $"The file format '{format}' is not supported. Only .pdf and image files are currently allowed"
                    );
                }

                if (!string.IsNullOrEmpty(m.Content))
                    contentBlocks.Add(new ContentBlock { Text = m.Content });

                messages.Add(new Message
                {
                    Role = ConversationRole.User,
                    Content = contentBlocks
                });

                request.FileData = null;
            }
            else
            {
                messages.Add(new Message
                {
                    Role = m.Role == "user" ? ConversationRole.User : ConversationRole.Assistant,
                    Content = [new ContentBlock { Text = m.Content }]
                });
            }
        }

        var system = !string.IsNullOrEmpty(request?.System)
            ? [new SystemContentBlock { Text = request.System }]
            : new List<SystemContentBlock>();

        var bedrockRequest = new ConverseRequest
        {
            ModelId = request?.Model,
            Messages = messages,
            System = system,
            InferenceConfig = new InferenceConfiguration
            {
                MaxTokens = request?.MaxTokens,
                Temperature = request?.Temperature,
                TopP = request?.TopP
            }
        }; 
        
        if (request.TopK.HasValue)
            bedrockRequest.AdditionalModelRequestFields = new Document { { "top_k", request.TopK.Value } };

        var response = await ExecuteWithErrorHandling(async () => await ChatClient.ConverseAsync(bedrockRequest));

        return new ResponseMessage
        {
            Text = response.Output.Message.Content.FirstOrDefault()?.Text ?? "",
            Usage = new UsageResponse
            {
                InputTokens = response.Usage.InputTokens ?? 0,
                OutputTokens = response.Usage.OutputTokens ?? 0,
                CacheCreationInputTokens = response.Usage.CacheWriteInputTokens ?? 0,
                CacheReadInputTokens = response.Usage.CacheReadInputTokens ?? 0,
            }
        };
    }

    public static async Task<T> ExecuteWithErrorHandling<T>(Func<Task<T>> func)
    {
        try
        {
            return await func();
        }
        catch (Exception ex)
        {
            throw new PluginApplicationException(ex.Message);
        }
    }
}
