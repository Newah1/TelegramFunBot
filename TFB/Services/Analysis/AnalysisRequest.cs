using TFB.Models;
using Personality = TFB.DTOs.Personality;

namespace TFB.Services.Analysis;

public class AnalysisRequest
{
    public Personality Personality { get; set; }
    public ChatTypes ChatTypes { get; set; } = ChatTypes.OpenRouter;
}