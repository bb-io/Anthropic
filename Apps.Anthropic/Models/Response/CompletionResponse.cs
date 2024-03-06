using Blackbird.Applications.Sdk.Common;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Apps.Anthropic.Models.Response
{
    public class CompletionResponse
    {
        [JsonProperty("content")]
        public List<Content> Content { get; set; }

        [JsonProperty("stop_reason")]
        public string StopReason { get; set; }

        [JsonProperty("model")]
        public string Model { get; set; }
    }
}
