using Newtonsoft.Json;

namespace TFB.Services.OpenRouter;

public class Choice
{
    [JsonProperty("finish_reason")]
    public string FinishReason { get; set; }
    [JsonProperty("message")]
    public Message Message { get; set; }
}