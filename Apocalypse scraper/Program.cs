using HtmlAgilityPack;
using System.Globalization;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Text.RegularExpressions;

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
        //string url = "https://en.wikipedia.org/wiki/List_of_dates_predicted_for_apocalyptic_events/index.html";
        string url = "C:\\Users\\toyma\\source\\repos\\Apocalypse scraper\\Apocalypse scraper\\apocalypses.html";
        string html = await GetHtmlAsync(url);
        List<string> allRecrds = new List<string>();
        string fileName = "apocalypses.csv";
        List<ApocalypticEvent> allEvents = new List<ApocalypticEvent>();

        HtmlDocument doc = new HtmlDocument();
        doc.LoadHtml(html);

        var tables = doc.DocumentNode.SelectNodes("//table[contains(@class, 'wikitable')]");

        foreach (var table in tables)
        {
            if (HasClaimantHeader(table) && !table.InnerHtml.Contains("300,000"))
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
        //using (HttpClient client = new HttpClient())
        //{
        //    return await client.GetStringAsync(url);
        //}
        string text=File.ReadAllText(url);

        return text;
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
            if (row.InnerHtml.Contains("Whisenant"))
            {
                int x = 1;
            }
            var cells = row.SelectNodes(".//td");
            if (cells != null)
            {
                if (cells.Count >= 4)
                {
                    // If comma-delimited list of dates, do one record for each.
                    string dateString = cells[0].InnerText.Trim();
                    int commas = dateString.Count(f => f == ',');
                    bool multipleDates = false;
                    if (commas == 1)
                    {
                        bool isBigNum = int.TryParse(dateString.Split(',')[1], out int value) && value == 0; // e.g. 123,456
                        multipleDates = !isBigNum;
                    }
                    if (multipleDates)
                    {
                        string[] dateStrings = dateString.Split(",");
                        foreach (string parseMe in dateStrings)
                        {
                            currentDate = ParseDate(parseMe);
                            var evt = new ApocalypticEvent
                            {
                                Date = currentDate,
                                Claimants = cells[1].InnerText.Trim(),
                                Description = cells[2].InnerText.Trim()
                            };
                            events.Add(evt);
                        }
                    }
                    else
                    {
                        currentDate = ParseDate(dateString);
                        var evt = new ApocalypticEvent
                        {
                            Date = currentDate,
                            Claimants = cells[1].InnerText.Trim(),
                            Description = cells[2].InnerText.Trim()
                        };
                        events.Add(evt);
                    }
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
        // if god loves us, then why are dates such a fucking headache?
        input = Regex.Unescape(input).Replace("&#8211;", "-");
        input = Regex.Replace(input, @"&#\d+;", string.Empty);

        input = Regex.Replace(input, @"(\d+)-(\d+)", "$2");
        // Check for date range
        if (input.Contains("-"))
        {
            input = input.Split('-')[1].Trim();
        }
        if (input.Contains("–")) // not the same chracter, I assure you
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
        if (DateTime.TryParse(input, out DateTime date2))
        {
            return date2;
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
