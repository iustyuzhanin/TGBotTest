using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace TGBotTest
{
    internal class Program
    {
        private static Random random = new Random();

        static async Task Main(string[] args)
        {
            Console.WriteLine("=== Запуск Telegram бота ===");

            // Получаем токен из переменных окружения Railway
            var botToken = Environment.GetEnvironmentVariable("BOT_TOKEN");
            //var botToken = "8410244251:AAGnQ1TI8SB5PYyvfDDFp3FtR4j2ov1VN_o";

            if (string.IsNullOrEmpty(botToken))
            {
                Console.WriteLine("❌ ОШИБКА: Переменная BOT_TOKEN не установлена!");
                Console.WriteLine("ℹ️ На Railway добавьте переменную окружения BOT_TOKEN");
                return;
            }

            var botClient = new TelegramBotClient(botToken);
            using var cts = new CancellationTokenSource();

            try
            {
                // Проверяем подключение к Telegram API
                var me = await botClient.GetMeAsync();
                Console.WriteLine($"✅ Бот @{me.Username} подключен к Telegram");
                Console.WriteLine($"🆔 ID бота: {me.Id}");
                Console.WriteLine($"⏰ Время запуска: {DateTime.Now}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка подключения: {ex.Message}");
                return;
            }

            // Настройки получения обновлений
            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = Array.Empty<UpdateType>(), // Получаем все типы сообщений
                ThrowPendingUpdates = true, // Игнорируем старые сообщения при запуске
            };

            var updateHandler = new DefaultUpdateHandler(
                HandleUpdateAsync,
                HandlePollingErrorAsync
            );

            // Запускаем прослушивание сообщений (новый синтаксис)
            botClient.StartReceiving(
                updateHandler: updateHandler,
                receiverOptions: receiverOptions,
                cancellationToken: cts.Token
            );

            Console.WriteLine("🤖 Бот запущен и слушает сообщения...");
            Console.WriteLine("📍 Хостинг: Railway.app");
            Console.WriteLine("🔗 Статус: 24/7");

            // Бот работает пока Railway не остановит контейнер
            // Ожидаем бесконечно (Railway управляет жизненным циклом)
            await Task.Delay(Timeout.Infinite, cts.Token);

            Console.ReadKey();
        }

        // Обработчик входящих сообщений
        static async Task HandleUpdateAsync(
            ITelegramBotClient botClient,
            Update update,
            CancellationToken cancellationToken)
        {
            try
            {
                // Обрабатываем только текстовые сообщения
                if (update.Message is not { Text: { } messageText } message)
                    return;

                var chatId = message.Chat.Id;
                var userId = message.From?.Id;
                var userName = message.From?.FirstName ?? "Аноним";

                // Логируем в консоль Railway
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] 👤 {userName} ({userId}): {messageText}");

                // --- ОБРАБОТКА КОМАНД ---

                // Команда /start
                if (messageText.Equals("/start", StringComparison.OrdinalIgnoreCase))
                {
                    var welcomeText = $"👋 Привет, {userName}!\n\n" +
                                     "Я телеграм-бот, который отвечает на вопросы.\n" +
                                     "Просто напиши мне что-нибудь, и я отвечу:\n";

                    await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: welcomeText,
                        cancellationToken: cancellationToken
                    );

                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] 🤖 Отправлено приветственное сообщение");
                    return; // Завершаем обработку
                }

                // Команда /help
                if (messageText.Equals("/help", StringComparison.OrdinalIgnoreCase))
                {
                    var helpText = "📋 Доступные команды:\n\n" +
                                  "/start - Начать работу с ботом\n" +
                                  "/help - Получить справку\n" +
                                  "/info - Информация о боте\n\n" +
                                  "Просто напиши любое сообщение, и я отвечу!";

                    await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: helpText,
                        cancellationToken: cancellationToken
                    );

                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] 🤖 Отправлена справка");
                    return;
                }

                // Команда /info
                if (messageText.Equals("/info", StringComparison.OrdinalIgnoreCase))
                {
                    var infoText = "ℹ️ Информация о боте:\n\n" +
                                  "🤖 Тип: Бот ответов\n" +
                                  "📍 Хостинг: Railway.app\n" +
                                  "⏰ Режим: 24/7\n" +
                                  "📅 Создан: 2024\n" +
                                  "💻 Технологии: .NET 8, Telegram.Bot API";

                    await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: infoText,
                        cancellationToken: cancellationToken
                    );

                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] 🤖 Отправлена информация");
                    return;
                }

                // --- ОБЫЧНЫЙ ОТВЕТ (не команда) ---

                // Если это не команда, отвечаем случайным образом
                string[] answers = {
                "✅ Да",
                "❌ Нет",
                "🤔 Возможно",
                "🎯 Конечно!",
                "🙅‍♂️ Вряд ли",
                "🔮 Спроси позже",
                "⚡ Определенно да!",
                "🚫 Точно нет!",
                "🤷‍♀️ Не уверен...",
            };

                string response = answers[random.Next(answers.Length)];

                await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: response,
                    cancellationToken: cancellationToken
                );

                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] 🤖 Ответил: {response}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ОШИБКА обработки] {ex.Message}");
            }
        }

        // Обработчик ошибок
        static Task HandlePollingErrorAsync(
            ITelegramBotClient botClient,
            Exception exception,
            CancellationToken cancellationToken)
        {
            Console.WriteLine($"[⛔ ОШИБКА API] {exception.Message}");
            return Task.CompletedTask;
        }
    }
}
