using CsvHelper;
using CsvHelper.Configuration;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace ConsoleApp
{
    public static class ExcelHelper
    {
        public static List<RecivedData> Read(string path)
        {
            using var streamReader = new StreamReader(path);

            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                Delimiter = ";",
                BadDataFound = null
            };

            using var reader = new CsvReader(streamReader, config);

            return reader.GetRecords<RecivedData>().ToList();
        }

        public static void Write(string path, List<DataToWrite> data)
        {
            var counter = 1;
            while (true)
            {
                if (File.Exists(path))
                {
                    if (counter == 1)
                        path = path.Replace(".csv", "") + $"({counter}).csv";
                    else
                        path = path.Replace($"({counter - 1})", $"({counter})");
                    counter++;
                }
                else
                    break;
            }


            using var streamReader = new StreamWriter(path);

            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                Delimiter = ";",
            };

            using var writer = new CsvWriter(streamReader, config);

            writer.WriteRecords(data);
        }
    }

    public class RecivedData
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string Weight { get; set; }
        public string Labels { get; set; }
        public string IssueID { get; set; }

        public override string ToString()
        {
            return $"IssueID:\n{IssueID};\nTitle:\n{Title};\nDescription:\n{Description};\n" +
                $"Weight:\n{Weight};\nLabels:\n{Labels}.";
        }
    }

    public class DataToWrite
    {
        public string YandexTaskId { get; set; }
        public string GitLabTaskId { get; set; }
        public string HasImage { get; set; }
    }
}