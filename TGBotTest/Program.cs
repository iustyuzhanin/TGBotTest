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
                var me = await botClient.GetMe();
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
                DropPendingUpdates = true, // Игнорируем старые сообщения при запуске
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

                // Генерируем случайный ответ Да/Нет
                string[] answers = { "✅ Да", "❌ Нет", "🤔 Возможно", "🎯 Конечно!", "🙅‍♂️ Вряд ли" };
                string response = answers[random.Next(answers.Length)];

                // Отправляем ответ
                await botClient.SendMessage(
                    chatId: chatId,
                    text: $"{response}",
                    cancellationToken: cancellationToken
                );

                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] 🤖 Бот ответил: {response}");
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
