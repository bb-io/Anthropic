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
        [Display("Completion")]
        [JsonProperty("completion")]
        public string Completion { get; set; }

        [Display("Stop reason")]
        [JsonProperty("stop_reason")]
        public string StopReason { get; set; }

        [Display("Model")]
        [JsonProperty("model")]
        public string Model { get; set; }
    }
}
