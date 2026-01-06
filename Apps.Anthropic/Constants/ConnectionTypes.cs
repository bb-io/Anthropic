namespace Apps.Anthropic.Constants;

public static class ConnectionTypes
{
    public const string AnthropicNative = "API Token";
    public const string Bedrock = "Amazon Bedrock";

    public static readonly IEnumerable<string> SupportedConnectionTypes = [AnthropicNative, Bedrock];
}
