using System.Collections.Generic;
using Newtonsoft.Json;

namespace OobaboogaChatUI.Models;

public class KoboldRequest
{
    public KoboldRequest(string prompt)
    {
        Prompt = prompt;

        //Default settings
        UseStory = false;
        UseMemory = false;
        UseAuthorsNote = false;
        UseWorldInfo = false;
        MaxContextLength = 2048;
        MaxLength = 256;
        RepPen = 1.1;
        RepPenRange = 256;
        RepPenSlope = 0.9;
        Temperature = 0.72;
        Tfs = 0.9;
        TopA = 0;
        TopK = 0;
        TopP = 0.73;
        Typical = 1;
        SamplerOrder = new List<int> { 6, 0, 1, 2, 3, 4, 5 };
    }

    [JsonProperty("prompt")] public string Prompt { get; set; }
    [JsonProperty("use_story")] public bool UseStory { get; set; }
    [JsonProperty("use_memory")] public bool UseMemory { get; set; }
    [JsonProperty("use_authors_note")] public bool UseAuthorsNote { get; set; }
    [JsonProperty("use_world_info")] public bool UseWorldInfo { get; set; }
    [JsonProperty("max_context_length")] public int MaxContextLength { get; set; }
    [JsonProperty("max_length")] public int MaxLength { get; set; }
    [JsonProperty("rep_pen")] public double RepPen { get; set; }
    [JsonProperty("rep_pen_range")] public int RepPenRange { get; set; }
    [JsonProperty("rep_pen_slope")] public double RepPenSlope { get; set; }
    [JsonProperty("temperature")] public double Temperature { get; set; }
    [JsonProperty("tfs")] public double Tfs { get; set; }
    [JsonProperty("top_a")] public int TopA { get; set; }
    [JsonProperty("top_k")] public int TopK { get; set; }
    [JsonProperty("top_p")] public double TopP { get; set; }
    [JsonProperty("typical")] public int Typical { get; set; }
    [JsonProperty("sampler_order")] public IList<int> SamplerOrder { get; set; }
}