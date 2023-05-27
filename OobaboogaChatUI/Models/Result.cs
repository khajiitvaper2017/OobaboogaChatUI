using Newtonsoft.Json;

namespace OobaboogaChatUI.Models;

public class Result
{
    [JsonProperty("text")] public string Text { get; set; }
}