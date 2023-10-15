using Standard.AI.OpenAI.Models.Services.Foundations.ChatCompletions;

namespace TFB.Services.Analysis;

public class AnalysisResponse
{
    public AnalysisResponse()
    {
        Message = string.Empty;
        ChatCompletionChoices = new List<ChatCompletionMessage>();
    }
    
    public bool Success { get; set; }
    public List<ChatCompletionMessage> ChatCompletionChoices { get; set; }
    public string Message { get; set; }
}