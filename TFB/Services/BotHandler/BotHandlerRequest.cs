namespace TFB.Services.BotHandler;

public class BotHandlerRequest
{
    public long ChatId { get; set; }
    public string Command { get; set; }
    public string MessageText { get; set; }
    public DTOs.Message Message { get; set; }
    public CancellationToken CancellationToken { get; set; }
    public Telegram.Bot.Types.Message TelegramMessage { get; set; }
    
    public BotHandlerRequest()
    {
        Message = new DTOs.Message();
        CancellationToken = new CancellationToken();
        ChatId = 0;
        TelegramMessage = new Telegram.Bot.Types.Message();
        Command = "";
        MessageText = "";
    }
}