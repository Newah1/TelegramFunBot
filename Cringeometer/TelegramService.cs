using Cringeometer.Models;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Cringeometer;

public class TelegramService
{
    private readonly TelegramBotClient _botClient;
    private readonly ReceiverOptions _options;

    public List<Action<ITelegramBotClient, Update, CancellationToken>> OnUpdateActions { get; set; } =
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
                Console.WriteLine($"Test {exception.Message}");
                return Task.CompletedTask;
            },
            _options
        );
    }

    private Task HandleUpdates(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        foreach (var onUpdateAction in OnUpdateActions)
        {
            onUpdateAction.Invoke(botClient, update, cancellationToken);
        }

        return Task.CompletedTask;
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