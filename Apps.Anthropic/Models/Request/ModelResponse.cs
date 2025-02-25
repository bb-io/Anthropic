using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Apps.Anthropic.Models.Request;
public class ModelResponse
{
    public string Type { get; set; }
    public string Id { get; set; }

    [JsonProperty("display_name")]
    public string DisplayName { get; set; }
}
