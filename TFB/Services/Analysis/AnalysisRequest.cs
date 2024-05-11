using TFB.Models;
using Personality = TFB.DTOs.Personality;

namespace TFB.Services.Analysis;

public class AnalysisRequest
{
    public Personality Personality { get; set; }
    /// <summary>
    /// Optional summary parameter, if provided given to personality as summary of conversation
    /// </summary>
    public string? Summary { get; set; }
    public ChatTypes ChatTypes { get; set; } = ChatTypes.OpenRouter;
}