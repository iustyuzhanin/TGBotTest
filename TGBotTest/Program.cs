using System.Net.Http;
using System.Text;
using System.Text.Json;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace TGBotTest
{
    internal class Program
    {
        private static Random random = new Random();
        private static readonly HttpClient httpClient = new HttpClient();

        static async Task Main(string[] args)
        {
            Console.WriteLine("=== Запуск Telegram бота ===");

            // Получаем токен из переменных окружения Railway
            var botToken = Environment.GetEnvironmentVariable("BOT_TOKEN");
            //var botToken = "8410244251:AAGnQ1TI8SB5PYyvfDDFp3FtR4j2ov1VN_o";
            var yandexApiKey = Environment.GetEnvironmentVariable("YANDEX_API_KEY");
            var yandexFolderId = Environment.GetEnvironmentVariable("YANDEX_FOLDER_ID");

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
            //botClient.StartReceiving(
            //    updateHandler: updateHandler,
            //    receiverOptions: receiverOptions,
            //    cancellationToken: cts.Token
            //);

            botClient.StartReceiving(
                updateHandler: async (client, update, token) =>
                {
                    if (update.Message?.Text is string text)
                    {
                        var chatId = update.Message.Chat.Id;
                        var name = update.Message.From?.FirstName ?? "друг";

                        Console.WriteLine($"{name}: {text}");

                        // Команда /start
                        if (text == "/start")
                        {
                            await client.SendTextMessageAsync(
                                chatId: chatId,
                                text: $"Привет, {name}! 👋\nЯ бот с YandexGPT. Задавай вопросы!",
                                cancellationToken: token
                            );
                            return;
                        }

                        string response;

                        // Если есть ключи Yandex, используем ИИ
                        if (!string.IsNullOrEmpty(yandexApiKey) && !string.IsNullOrEmpty(yandexFolderId))
                        {
                            response = await GetYandexGPTResponse(text, yandexApiKey, yandexFolderId);
                        }
                        else
                        {
                            response = GetRandomResponse();
                        }

                        await client.SendTextMessageAsync(
                            chatId: chatId,
                            text: response,
                            cancellationToken: token
                        );
                    }
                },
                pollingErrorHandler: (client, error, token) =>
                {
                    Console.WriteLine($"Error: {error.Message}");
                    return Task.CompletedTask;
                },
                receiverOptions: new ReceiverOptions(),
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

        static async Task<string> GetYandexGPTResponse(string message, string apiKey, string folderId)
        {
            try
            {
                var request = new
                {
                    modelUri = $"gpt://{folderId}/yandexgpt-lite",
                    completionOptions = new
                    {
                        stream = false,
                        temperature = 0.6,
                        maxTokens = 200
                    },
                    messages = new[]
                    {
                        new
                        {
                            role = "system",
                            text = "Ты дружелюбный телеграм-бот. Отвечай кратко (1-2 предложения). Будь позитивным и иногда используй эмодзи. Если не знаешь ответа, скажи что-то ободряющее."
                        },
                        new
                        {
                            role = "user",
                            text = message
                        }
                    }
                };

                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Api-Key", apiKey);

                var response = await httpClient.PostAsync(
                    "https://llm.api.cloud.yandex.net/foundationModels/v1/completion",
                    content
                );

                if (response.IsSuccessStatusCode)
                {
                    var responseJson = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(responseJson);
                    return doc.RootElement
                        .GetProperty("result")
                        .GetProperty("alternatives")[0]
                        .GetProperty("message")
                        .GetProperty("text")
                        .GetString() ?? "Не могу ответить";
                }

                return $"Ошибка API: {response.StatusCode}";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"YandexGPT error: {ex.Message}");
                return "Извини, возникла ошибка. Попробуй еще раз!";
            }
        }

        static string GetRandomResponse()
        {
            string[] answers = {
                "✅ Да",
                "❌ Нет",
                "🤔 Возможно",
                "🎯 Конечно!",
                "🙅‍♂️ Вряд ли",
                "🔮 Спроси позже",
                "⚡ Определенно да!",
                "🚫 Точно нет!"
            };

            return answers[random.Next(answers.Length)];
        }
    }
}
