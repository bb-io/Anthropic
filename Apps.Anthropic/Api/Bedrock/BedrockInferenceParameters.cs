using Apps.Anthropic.Models.Request;
using Apps.Anthropic.Utils;

namespace Apps.Anthropic.Api.Bedrock;

internal sealed record BedrockInferenceParameters(
    int? MaxTokens,
    float? Temperature,
    float? TopP,
    int? TopK)
{
    public static BedrockInferenceParameters From(MessageRequest request)
    {
        var supportsSamplingParameters = ModelCatalog.SupportsSamplingParameters(request.Model);

        return new BedrockInferenceParameters(
            request.MaxTokens,
            supportsSamplingParameters ? request.Temperature : null,
            supportsSamplingParameters ? request.TopP : null,
            supportsSamplingParameters ? request.TopK : null);
    }
}
