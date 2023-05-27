using Newtonsoft.Json;

namespace OobaboogaChatUI.Models;

internal class WebSocketResponse
{
    [JsonProperty("event")] public string Event { get; set; }

    [JsonProperty("message_num")] public int MessageNum { get; set; }

    [JsonProperty("text")] public string Text { get; set; }
}