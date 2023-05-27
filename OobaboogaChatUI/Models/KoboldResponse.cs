using System.Collections.Generic;
using Newtonsoft.Json;

namespace OobaboogaChatUI.Models;

public class KoboldResponse
{
    [JsonProperty("results")] public IList<Result> Results { get; set; }
}