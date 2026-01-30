namespace Apps.Anthropic.Models.Response
{
    public class GetMQMResponse
    {
        public string Report { get; set; }
        public UsageResponse Usage { get; set; }
        public string SystemPrompt { get; set; }
    }

}
