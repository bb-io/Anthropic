using Apps.Anthropic.Constants;
using Apps.Anthropic.Models.Request;
using Apps.Anthropic.Models.Response;
using Apps.Anthropic.Utils;
using Blackbird.Applications.Sdk.Common.Authentication;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Utils.Extensions.Http;
using Blackbird.Applications.Sdk.Utils.RestSharp;
using Newtonsoft.Json;
using RestSharp;

namespace Apps.Anthropic.Api.Anthropic;

public class BaseAnthropicClient : BlackBirdRestClient
{
    protected override JsonSerializerSettings JsonSettings => new() 
    { 
        MissingMemberHandling = MissingMemberHandling.Ignore
    };

    public BaseAnthropicClient(IEnumerable<AuthenticationCredentialsProvider> creds, Uri baseUrl) :
        base(new RestClientOptions
        {
            ThrowOnAnyError = false,
            BaseUrl = baseUrl,
            MaxTimeout = (int)TimeSpan.FromMinutes(10).TotalMilliseconds,
            
        })
    {
        this.AddDefaultHeader("x-api-key", creds.First(x => x.KeyName == "apiKey").Value);
        this.AddDefaultHeader("anthropic-version", "2023-06-01");
    }

    protected override Exception ConfigureErrorException(RestResponse response)
    {
        if (response.Content == null)
            throw new PluginApplicationException(response.ErrorMessage);

        var error = JsonConvert.DeserializeObject<ErrorResponse>(response.Content, JsonSettings);

        if (error?.Error == null || string.IsNullOrWhiteSpace(error.Error.Type))
            throw new PluginApplicationException(error?.Error?.Message ?? response.ErrorException?.Message);

        var errorType = error.Error.Type;

        if (KnownErrors.AnthropicErrors.TryGetValue(errorType, out var message))
        {
            return new PluginApplicationException(error?.Error?.Message ?? message);
        }

        // We should explicitly throw errors here to be notified of invalid request errors that we can fix
        return new Exception(error?.Error?.Message ?? response.ErrorException.Message);
    }

    public virtual async Task<ResponseMessage> ExecuteChat(MessageRequest message)
    {
        var formattedMessages = new List<object>();

        foreach (var msg in message.Messages)
        {
            if (msg.Role == "user" && message.FileData != null)
            {
                var contentList = new List<object>();

                string base64Data = Convert.ToBase64String(message.FileData.FileBytes);
                string ext = message.FileData.FileExtension;

                if (ext == ".pdf")
                {
                    contentList.Add(new
                    {
                        type = "document",
                        source = new
                        {
                            data = base64Data,
                            media_type = "application/pdf",
                            type = "base64",
                        }
                    });
                }
                else if (FileFormatHelper.IsImage(ext))
                {
                    contentList.Add(new
                    {
                        type = "image",
                        source = new
                        {
                            type = "base64",
                            media_type = FileFormatHelper.GetAnthropicImageMediaType(ext),
                            data = base64Data
                        }
                    });
                }
                else
                {
                    throw new PluginMisconfigurationException(
                        $"The file format '{ext}' is not supported. Only .pdf and image files are currently allowed"
                    );
                }

                if (!string.IsNullOrEmpty(msg.Content))
                    contentList.Add(new { type = "text", text = msg.Content });

                formattedMessages.Add(new { role = "user", content = contentList });

                message.FileData = null;
            }
            else
                formattedMessages.Add(new { role = msg.Role, content = msg.Content });
        }

        var payload = new
        {
            model = message.Model,
            max_tokens = ModelTokenService.GetMaxTokensForModel(message.Model),
            system = message.System,
            messages = formattedMessages,
            stop_sequences = message.StopSequences,
            temperature = message.Temperature,
            top_p = message.TopP,
            top_k = message.TopK
        };

        var request = new RestRequest("/messages", Method.Post).WithJsonBody(payload, JsonOptions.JsonSettings);

        var response = await ExecuteWithErrorHandling<CompletionResponse>(request);
        return new ResponseMessage
        {
            Text = response.Content.FirstOrDefault()?.Text ?? "",
            Usage = response.Usage
        };
    }
}
