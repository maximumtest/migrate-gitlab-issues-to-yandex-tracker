using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.Extensions.Configuration;
using RestSharp;
using System.Text.Json;

namespace ConsoleApp
{
    public class Program
    {
        static void Main(string[] args)
        {
            var commands = new Dictionary<string, string>();
            commands.Add("/help", "посмотреть список доступных команд");
            commands.Add("/readcsv", "Считать данные из файла CSV");
            commands.Add("/reviewdata", "Проверка корректности считанной информации из файла CSV");
            commands.Add("/post", "Создание задач на Yandex Tracker");
            commands.Add("/setconfig", "Считать файл конфигурации");
            commands.Add("/deltags", "Удалить тэги из Yandex Tracker");
            commands.Add("/getconfig", "Вывести данные текущей настройки конфигурации");
            commands.Add("/m", "Вернуться к выбору команды");

            var settings = GetSettings();
            List<RecivedData> data = ReadDataFromFile(settings);

            while (true)
            {
                Console.WriteLine("\nВведите команду для исполнения. Посмотреть список команд: /help");
                var comand = Console.ReadLine().ToLower();
                if (comand == "/help")
                    PrintComands(commands);
                else if (comand == "/readcsv")
                    data = ReadDataFromFile(settings);
                else if (comand == "/reviewdata")
                    ViewRecivedData(data);
                else if (comand == "/post")
                    Execute(settings, data);
                else if (comand == "/deltags")
                    DeleteTags(settings);
                else if (comand == "/setconfig")
                    settings = GetSettings();
                else if (comand == "/getconfig")
                    PrintSettings(settings);
            }
        }

        private static void PrintSettings(Settings settings)
        {
            if (settings == null)
                Console.WriteLine("Данные конфигурации отсутствуют.");
            else
                Console.WriteLine(settings);
        }

        private static Settings GetSettings()
        {
            try
            {
                IConfiguration config = new ConfigurationBuilder()
                    .AddJsonFile("appsettings.json")
                    .AddEnvironmentVariables()
                    .Build();
                var settings = config.GetRequiredSection("Settings").Get<Settings>();

                if (!File.Exists(settings.FileName))
                    settings.FileName = Directory.GetCurrentDirectory() + "\\" + settings.FileName;
                return settings;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Произошла ошибка при чтении файла конфигурации. ({ex.Message})" +
                    $"\nПроверьте фaйл и повторите попытку при помощи соманды /setconfig.");
                return null;
            }
        }

        private static void PrintComands(Dictionary<string, string> commands)
        {
            Console.WriteLine("Команды для работы:");
            foreach (var item in commands)
                Console.WriteLine($"{item.Key} - {item.Value}.");
        }

        private static List<RecivedData> ReadDataFromFile(Settings settings)
        {
            try
            {
                var data = ExcelHelper.Read(settings.FileName);

                if (!ValidsteData(data))
                {
                    Console.WriteLine("Некоррекно считан файл .csv. " +
                        "Скорректируйте файл и выполните команду /readcsv");
                    return null;
                }

                Console.WriteLine($"Успешно считано {data.Count} записей. Для проверки корректности " +
                    "воспользуйтесь командой: /reviewdata");

                return data;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Произошла ошибка: ({ex.Message}), попробуйте снова.");
                return null;
            }
        }

        private static bool ValidsteData(List<RecivedData> data)
        {
            var ids = new List<string>();
            foreach (var item in data)
            {
                if (!int.TryParse(item.Weight, out var weigth) && !string.IsNullOrEmpty(item.Weight))
                    ids.Add(item.IssueID);
            }
            if (!ids.Any())
                return true;
            else
            {
                Console.WriteLine("Некорректно считано поле weigth из файла для записей с IssueId:");

                foreach (var item in ids)
                    Console.Write($"{item}; ");

                return false;
            }
        }

        private static void ViewRecivedData(List<RecivedData> data)
        {
            if (data == null)
            {
                Console.WriteLine("Данные отсутствуют. Для инициализации данных " +
                    "воспользуйтесь командой: /readcsv");
                return;
            }

            while (true)
            {
                var input = ReadInput("Введите колличество записей для вывода на консоль:");
                if (string.IsNullOrEmpty(input))
                    return;
                if (!int.TryParse(input, out var count))
                {
                    Console.WriteLine("Введено некорректное число.");
                    continue;
                }
                count = count <= data.Count ? count : data.Count;
                for (int i = 0; i < count; i++)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Запись №{i + 1}");
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine(data[i].ToString());
                    Console.WriteLine();
                }
                Console.WriteLine("Если данные не верны, скорректируйте csv файл и повторите" +
                    "считывание при помощи команды: /readcsv");
                return;
            }
        }

        private static RestResponse Post(RestRequest request, PostData data)
        {
            var client = new RestClient("https://api.tracker.yandex.net/v2/issues");

            request.AddJsonBody(data);

            return client.Post(request);
        }

        private static RestRequest CreateRequest(Settings settings)
        {
            var request = new RestRequest();
            request.AddHeader("Authorization", settings.Token);
            request.AddHeader("X-Org-ID", settings.OrgNumber);
            return request;
        }

        private static void Execute(Settings settings, IEnumerable<RecivedData> data)
        {
            var dataToWrite = new List<DataToWrite>();

            var counter = 0;
            var count = data.Count();
            foreach (var item in data)
            {
                counter++;

                Console.WriteLine($"Создаем задачу №{counter} из {count}.");
                Console.WriteLine($"Id импортироемой задачи: {item.IssueID}");

                RestResponse response = null;
                try
                {
                    var request = CreateRequest(settings);
                    response = Post(request, new PostData(settings.Queue).Map(item));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Произошла непредвиденная ошибка при переносе задчи с Id {item.IssueID}." +
                        $"\n({ex.Message})\n" +
                        $"Задача не была создана. Для продолжения нажмите любую клавишу.");
                    Console.ReadKey();
                    continue;
                }

                Console.WriteLine($"Статус ответа: {response.StatusCode}");
                var content = response.Content;
                var key = $"\"key\":\"{settings.Queue}-";

                var dataAbout = new DataToWrite()
                {
                    GitLabTaskId = item.IssueID,
                    YandexTaskId = content.Substring(content.IndexOf(key) + 13, 6)
                        .Replace("\"", "").Replace(",", "").Replace("\\", "").Replace("\"", "")
                        .Replace("v", "").Replace("e", "").Replace("r", "").Replace("s", ""),
                    HasImage = item.Description.Contains("[image]") ? "да" : "",
                };
                Console.WriteLine("создана задача с id: " + dataAbout.YandexTaskId);
                dataToWrite.Add(dataAbout);

                Console.WriteLine("Успешно");
                Console.WriteLine();
            }

            ExcelHelper.Write(Directory.GetCurrentDirectory() + "\\result.csv", dataToWrite);
        }


        private static void DeleteTags(Settings settings)
        {
            var tags = ReadInput("Введите теги для удаления через ','.")?.Split(",");
            if (!tags.Any())
                return;

            var client = new RestClient($"https://api.tracker.yandex.net/v2/queues/{settings.Queue}/tags/_remove");

            foreach (var tag in tags)
            {
                Console.WriteLine($"Удаление Тега: \"{tag}\"");

                try
                {
                    var request = CreateRequest(settings);
                    request.AddJsonBody(new TagToDel() { Tag = tag });
                    var response = client.Post(request);
                    Console.WriteLine("Успешно");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Произошла ошибка при удалении тега {tag}. ({ex.Message}).\n" +
                        $"Возможно, этот тэг используется. Для продолжения нажмите любую клавишу.");
                    Console.ReadKey();
                }
                Console.WriteLine();
            }
        }

        private static string ReadInput(string text)
        {
            Console.WriteLine(text);
            var input = Console.ReadLine();
            if (input == "/m")
                return null;
            else
                return input;
        }
    }
}