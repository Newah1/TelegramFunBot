using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using TFB.DTOs.Settings;
using TFB.Services.BotHandler;
using Message = Telegram.Bot.Types.Message;
using MessageType = TFB.DTOs.MessageType;

namespace TFB.Services;

public class TelegramService
{
    private readonly TelegramBotClient _botClient;
    private readonly ReceiverOptions _options;

    public List<Action<BotHandlerRequest, CancellationToken>> OnUpdateActions { get; set; } =
        new List<Action<BotHandlerRequest, CancellationToken>>();

    public List<Action<BotHandlerRequest, CancellationToken>> CallBackQueryActions { get; set; } =
        new List<Action<BotHandlerRequest, CancellationToken>>();

    private TelegramService(TelegramBotClient botClient, ReceiverOptions options)
    {
        _botClient = botClient;
        _options = options;
    }

    public async Task StartReceiving()
    {
        await _botClient.ReceiveAsync(
            updateHandler:HandleUpdates,
            pollingErrorHandler: (client, exception, arg3) =>
            {
                Console.WriteLine($"Telegram error... {exception.Message}");
                return Task.CompletedTask;
            },
            _options
        );

        
    }

    private Task HandleUpdates(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        if (update.Message is not { } message)
            return Task.CompletedTask;
        
        var messageText = update.Message?.Text ?? "";
        var command = "";
        
        if (update.Message?.Entities?.Length > 0 
            && update.Message.Entities.FirstOrDefault(type => type.Type == MessageEntityType.BotCommand) != null)
        {
            command = update.Message?.EntityValues?.FirstOrDefault() ?? "";
        }

        var chatId = message?.Chat.Id ?? 0;

        var author = message?.From?.Username ?? message?.From?.FirstName ?? "Default*User";

        var dtoMessage = new TFB.DTOs.Message()
        {
            Author = author,
            DatePosted = message?.Date ?? DateTime.Now,
            Value = messageText,
            MessageType = MessageType.User,
            ConversationWith = author
        };

        var botHandlerRequest = new BotHandlerRequest()
        {
            CancellationToken = cancellationToken,
            ChatId = chatId,
            Command = command,
            Message = dtoMessage,
            MessageText = $"{messageText} From: {update.Message.From.FirstName}",
            TelegramMessage = update.Message ?? new Message()
        };
        
        if (update.Type == UpdateType.CallbackQuery)
        {
            foreach (var callBackQueryAction in CallBackQueryActions)
            {
                callBackQueryAction.Invoke(botHandlerRequest, cancellationToken);
            }
        }
        else
        {
            foreach (var onUpdateAction in OnUpdateActions)
            {
                onUpdateAction.Invoke(botHandlerRequest, cancellationToken);
            }
        }

        return Task.CompletedTask;
    }

    public async Task<Telegram.Bot.Types.Message?> Send(long chatId, string message, int? replyToId, InlineKeyboardMarkup? inlineKeyboardMarkup = null)
    {

        if (message.Length > 4096)
        {
            message = message.Substring(0, 4094); // max telegram len
        }
        
        try
        {
            var sendTextMessageAsync = await _botClient.SendTextMessageAsync(
                chatId: chatId,
                text: message,
                replyMarkup: inlineKeyboardMarkup,
                cancellationToken: new CancellationToken(), replyToMessageId: replyToId ?? 0);
            

            return sendTextMessageAsync;
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }

        return new Telegram.Bot.Types.Message();
    }

    public static TelegramService SetupClient(TelegramSettings telegramSettings)
    {
        var botClient = new TelegramBotClient(telegramSettings.TelegramToken);
        ReceiverOptions receiverOptions = new ()
        {
            AllowedUpdates = Array.Empty<UpdateType>() // receive all update types except ChatMember related updates
        };

        return new TelegramService(botClient, receiverOptions);
    }
}