using HtmlAgilityPack;
using System.Globalization;
using System.Text.Json.Serialization;
using System.Text.Json;

class Program
{
    public class ApocalypticEvent
    {
        [JsonPropertyName("date")]
        public Object Date { get; set; }
        [JsonPropertyName("claimants")]
        public string Claimants { get; set; }
        [JsonPropertyName("description")]
        public string Description { get; set; }
    }

    static async Task Main(string[] args)
    {
        string url = "https://en.wikipedia.org/wiki/List_of_dates_predicted_for_apocalyptic_events";
        string html = await GetHtmlAsync(url);
        List<string> allRecrds = new List<string>();
        string fileName = "apocalypses.csv";
        List<ApocalypticEvent> allEvents = new List<ApocalypticEvent>();

        HtmlDocument doc = new HtmlDocument();
        doc.LoadHtml(html);

        var tables = doc.DocumentNode.SelectNodes("//table[contains(@class, 'wikitable')]");

        foreach (var table in tables)
        {
            if (HasClaimantHeader(table))
            {
                //List<string> tableRows= ProcessTable(table);
                //allRecrds.AddRange(tableRows);
                allEvents.AddRange(ProcessTable(table));
            }
        }
        WriteToJsonFile(allEvents, "apocalyptic_events.json");

        Console.ReadKey();
    }

    static async Task<string> GetHtmlAsync(string url)
    {
        using (HttpClient client = new HttpClient())
        {
            return await client.GetStringAsync(url);
        }
    }

    static bool HasClaimantHeader(HtmlNode table)
    {
        var headers = table.SelectNodes(".//th");
        return headers != null && headers.Any(h => h.InnerText.Contains("Claimant(s)"));
    }

    static void WriteToJsonFile(List<ApocalypticEvent> events, string fileName)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        string jsonString = JsonSerializer.Serialize(events, options);
        File.WriteAllText(fileName, jsonString);
        Console.WriteLine($"JSON file '{fileName}' has been created.");
    }

    static List<ApocalypticEvent> ProcessTable(HtmlNode table)
    {
        var rows = table.SelectNodes(".//tr");
        var events = new List<ApocalypticEvent>();
        object currentDate = null;

        foreach (var row in rows.Skip(1)) // Skip header row
        {
            if (row.InnerHtml.Contains("Hon-Ming"))
            {
                int x = 1;
            }
            var cells = row.SelectNodes(".//td");
            if (cells != null)
            {
                if (cells.Count >= 4)
                {
                    currentDate = ParseDate(cells[0].InnerText.Trim());
                    var evt = new ApocalypticEvent
                    {
                        Date = currentDate,
                        Claimants = cells[1].InnerText.Trim(),
                        Description = cells[2].InnerText.Trim()
                    };
                    events.Add(evt);
                }
                else if (cells.Count == 3 && currentDate != null)
                {
                    var evt = new ApocalypticEvent
                    {
                        Date = currentDate,
                        Claimants = cells[0].InnerText.Trim(),
                        Description = cells[1].InnerText.Trim()
                    };
                    events.Add(evt);
                }
            }
        }

        return events;
    }
    static object ParseDate(string input)
    {
        // Check for date range
        if (input.Contains("–"))
        {
            input = input.Split('–')[1].Trim();
        }
        if (input.Contains(","))
        {
            string input2 = input.Split(",")[1].Trim();
            if (int.TryParse(input2, out int value) && value == 0) // for stuff like "13,000"
            {
                input2 = input.Replace(",", "");
            }
            input = input2;
        }

        // Try parsing as a full date
        if (DateTime.TryParseExact(input, new[] { "d MMM yyyy", "MMM yyyy", "yyyy" },
            CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime date))
        {
            return date;
        }

        // Try parsing as a year
        if (int.TryParse(input, out int year))
        {
            try
            {
                return new DateTime(year, 1, 1);
            }
            catch
            {
                return input;
            }
        }

        // If it's a month and year without a day
        string[] parts = input.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 2)
        {
            if (DateTime.TryParseExact(input, "MMM yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime monthYearDate))
            {
                return monthYearDate;
            }
        }

        // If it's just a month and day without a year
        if (parts.Length == 2 && int.TryParse(parts[0], out int day))
        {
            if (DateTime.TryParseExact(input, "d MMM", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dayMonthDate))
            {
                return dayMonthDate.AddYears(DateTime.Now.Year - dayMonthDate.Year); // Set to current year
            }
        }

        // Return original string if parsing fails
        return input;
    }


}
