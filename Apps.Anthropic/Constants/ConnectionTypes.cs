namespace Apps.Anthropic.Constants;

public static class ConnectionTypes
{
    public const string AnthropicNative = "API Token";
    public const string BedrockCreds = "Amazon Bedrock (AWS Credentials)";
    public const string BedrockApiKey = "Amazon Bedrock (API Key)";
    public const string MicrosoftFoundryApiKey = "MicrosoftFoundryApiKey";
    public const string GoogleVertex = "Google Vertex AI";

    public static readonly IEnumerable<string> SupportedConnectionTypes = 
    [
        AnthropicNative, 
        BedrockCreds, 
        BedrockApiKey, 
        MicrosoftFoundryApiKey,
        GoogleVertex
    ];
}
