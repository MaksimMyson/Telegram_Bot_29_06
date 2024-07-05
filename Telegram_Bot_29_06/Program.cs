using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using RestSharp;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

class Program
{
    private static readonly string TelegramToken = "7374337564:AAHCScQY3EsF_42zqdu-uAytZ8cTyxKM9U0";
    private static readonly string OpenWeatherApiKey = "ae212df81558c08950609aac9d64ff41";
    private static readonly TelegramBotClient Bot = new TelegramBotClient(TelegramToken);
    private static string? selectedCountry;
    private static string? selectedCity;
    private static string? selectedDate;

    static async Task Main(string[] args)
    {
        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = Array.Empty<UpdateType>()
        };

        Bot.StartReceiving(HandleUpdateAsync, HandleErrorAsync, receiverOptions, CancellationToken.None);

        Console.WriteLine("Bot is running...");
        Console.ReadLine();
    }

    private static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        if (update.Type == UpdateType.Message && update.Message?.Text != null)
        {
            await Bot_OnMessage(botClient, update.Message);
        }
        else if (update.Type == UpdateType.CallbackQuery && update.CallbackQuery?.Data != null)
        {
            await Bot_OnCallbackQuery(botClient, update.CallbackQuery);
        }
    }

    private static Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        Console.WriteLine($"Error: {exception.Message}");
        return Task.CompletedTask;
    }

    private static async Task Bot_OnMessage(ITelegramBotClient botClient, Message message)
    {
        if (message.Text == "/start")
        {
            selectedCountry = null;
            selectedCity = null;
            selectedDate = null;
            await botClient.SendTextMessageAsync(message.Chat.Id, "Привіт! Введіть країну:");
        }
        else if (selectedCountry == null)
        {
            selectedCountry = message.Text;
            await botClient.SendTextMessageAsync(message.Chat.Id, "Введіть місто:");
        }
        else if (selectedCity == null)
        {
            selectedCity = message.Text;
            var replyKeyboard = new InlineKeyboardMarkup(new[]
            {
                new[] { InlineKeyboardButton.WithCallbackData("Сьогодні", "date_today") },
                new[] { InlineKeyboardButton.WithCallbackData("Завтра", "date_tomorrow") },
                new[] { InlineKeyboardButton.WithCallbackData("Через 3 дні", "date_3days") }
            });
            await botClient.SendTextMessageAsync(message.Chat.Id, "Оберіть день:", replyMarkup: replyKeyboard);
        }
    }

    private static async Task Bot_OnCallbackQuery(ITelegramBotClient botClient, CallbackQuery callbackQuery)
    {
        if (callbackQuery.Data.StartsWith("date_"))
        {
            selectedDate = callbackQuery.Data.Substring(5);
            var weather = GetWeather(selectedCity, selectedCountry, selectedDate);
            await botClient.SendTextMessageAsync(callbackQuery.Message.Chat.Id, weather);
        }
    }

    private static string GetWeather(string city, string country, string date)
    {
        var client = new RestClient($"http://api.openweathermap.org/data/2.5/weather?q={city},{country}&appid={OpenWeatherApiKey}&units=metric&lang=uk");
        var request = new RestRequest("", Method.Get); 
        var response = client.Execute(request);

        if (response.StatusCode != HttpStatusCode.OK)
        {
            return "Не вдалося отримати дані про погоду. Спробуйте пізніше.";
        }

        var data = JObject.Parse(response.Content);
        var weatherDescription = data["weather"]?[0]?["description"]?.ToString();
        var temp = data["main"]?["temp"]?.ToString();
        return $"Погода в {city}:\n\nОпис: {weatherDescription}\nТемпература: {temp}°C";
    }
}
