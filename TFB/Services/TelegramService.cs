using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using TFB.Models;

namespace TFB.Services;

public class TelegramService
{
    private readonly TelegramBotClient _botClient;
    private readonly ReceiverOptions _options;

    public List<Action<ITelegramBotClient, Update, CancellationToken>> OnUpdateActions { get; set; } =
        new List<Action<ITelegramBotClient, Update, CancellationToken>>();

    public List<Action<ITelegramBotClient, Update, CancellationToken>> CallBackQueryActions { get; set; } =
        new List<Action<ITelegramBotClient, Update, CancellationToken>>();

    private TelegramService(TelegramBotClient botClient,ReceiverOptions options)
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
        if (update.Type == UpdateType.CallbackQuery)
        {
            foreach (var callBackQueryAction in CallBackQueryActions)
            {
                callBackQueryAction.Invoke(botClient, update, cancellationToken);
            }
        }
        else
        {
            foreach (var onUpdateAction in OnUpdateActions)
            {
                onUpdateAction.Invoke(botClient, update, cancellationToken);
            }
        }

        return Task.CompletedTask;
    }

    public async Task<Telegram.Bot.Types.Message?> Send(long chatId, string message, int? replyToId, InlineKeyboardMarkup? inlineKeyboardMarkup = null)
    {
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