using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Xml;
using System.Xml.Linq;

namespace Assignment.Controllers
{
    public class Statistics
        {
            public int MaleCount { get; set; }
            public int FemaleCount { get; set; }
            public int FirstNameAMCount { get; set; }
            public int FirstNameNZCount { get; set; }
            public int LastNameAMCount { get; set; }
            public int LastNameNZCount { get; set; }
            public Dictionary<string, int> StateCounts { get; set; } = new Dictionary<string, int>();
            public int Age0To20Count { get; set; }
            public int Age21To40Count { get; set; }
            public int Age41To60Count { get; set; }
            public int Age61To80Count { get; set; }
            public int Age81To100Count { get; set; }
            public int Age100PlusCount { get; set; }
        }

    public class RandomUserResponse
        {
            public List<RandomUser> Results { get; set; }
            public Info Info { get; set; }
        }

    public class RandomUser
    {
        public string Gender { get; set; }
        public Name Name { get; set; }
        public Location Location { get; set; }
        public Dob Dob { get; set; }
        public string Nat { get; set; }
    }

    public class Name
    {
        public string Title { get; set; }
        public string First { get; set; }
        public string Last { get; set; }
    }

    public class Location
    {
        public Street Street { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string Country { get; set; }
        public int Postcode { get; set; }
        public Coordinates Coordinates { get; set; }
        public Timezone Timezone { get; set; }
    }

    public class Street
    {
        public int Number { get; set; }
        public string Name { get; set; }
    }

    public class Coordinates
    {
        public string Latitude { get; set; }
        public string Longitude { get; set; }
    }

    public class Timezone
    {
        public string Offset { get; set; }
        public string Description { get; set; }
    }

    public class Dob
    {
        public DateTime Date { get; set; }
        public int Age { get; set; }
    }

    public class Info
    {
        public string Seed { get; set; }
        public int Results { get; set; }
        public int Page { get; set; }
        public string Version { get; set; }
    }


    [ApiController]
    [Route("[controller]")]
    public class AssigmentController : ControllerBase
    {
        private readonly HttpClient _httpClient;

        public AssigmentController(ILogger<AssigmentController> logger, IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient();
        }

        [HttpPost]
        public async Task<IActionResult> GenerateStatistics([FromHeader(Name = "Accept")] string acceptHeader = null)
        {
            try
            {
                // Call the Random User Generator API to get random user data
                HttpResponseMessage response = await _httpClient.GetAsync("https://randomuser.me/api/?results=5&nat=us&inc=gender,name,state,dob,nat");

                if (!response.IsSuccessStatusCode)
                    return StatusCode((int)response.StatusCode);

                string json = await response.Content.ReadAsStringAsync();
                var randomUserResponse = JsonConvert.DeserializeObject<RandomUserResponse>(json);

                if (randomUserResponse == null || randomUserResponse.Results == null)
                    return BadRequest("No random user data retrieved.");

                // Proceed to calculate statistics
                var statistics = await CalculateStatistics(randomUserResponse.Results);

                // Return statistics based on the requested format
                if(acceptHeader != null && acceptHeader != "*/*")
                switch (acceptHeader?.ToLower())
                {
                    case "application/json":
                        return Ok(statistics);
                    case "text/plain":
                        return Content(GeneratePlainTextStatistics(statistics), "text/plain");
                    case "application/xml":
                        return Content(GenerateXmlStatistics(statistics).ToString(), "application/xml");
                    default:
                        return BadRequest("Unsupported media type.(use e.g: application/json");
                }
                else
                return Ok(statistics);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }
        private async Task<Statistics> CalculateStatistics(List<RandomUser> response)
    {
        var statistics = new Statistics();

       foreach (var user in response)
            {
                // Gender statistics
                if (user.Gender != null)
                {
                    if (user.Gender.Equals("male", StringComparison.OrdinalIgnoreCase))
                        statistics.MaleCount++;
                    else if (user.Gender.Equals("female", StringComparison.OrdinalIgnoreCase))
                        statistics.FemaleCount++;
                }

                // First name statistics
                if (!string.IsNullOrEmpty(user.Name?.First))
                {
                    char firstLetter = user.Name.First.ToLower()[0];
                    if (firstLetter >= 'a' && firstLetter <= 'm')
                        statistics.FirstNameAMCount++;
                    else
                        statistics.FirstNameNZCount++;
                }

                // Last name statistics
                if (!string.IsNullOrEmpty(user.Name?.Last))
                {
                    char firstLetter = user.Name.Last.ToLower()[0];
                    if (firstLetter >= 'a' && firstLetter <= 'm')
                        statistics.LastNameAMCount++;
                    else
                        statistics.LastNameNZCount++;
                }

                // State statistics
                if (!string.IsNullOrEmpty(user.Location?.State))
                {
                    statistics.StateCounts[user.Location.State] = statistics.StateCounts.ContainsKey(user.Location.State) ?
                        statistics.StateCounts[user.Location.State] + 1 : 1;
                }

                // Age range statistics
                if (user.Dob != null)
                {
                    int age = user.Dob.Age;
                    if (age >= 0 && age <= 20)
                        statistics.Age0To20Count++;
                    else if (age >= 21 && age <= 40)
                        statistics.Age21To40Count++;
                    else if (age >= 41 && age <= 60)
                        statistics.Age41To60Count++;
                    else if (age >= 61 && age <= 80)
                        statistics.Age61To80Count++;
                    else if (age >= 81 && age <= 100)
                        statistics.Age81To100Count++;
                    else
                        statistics.Age100PlusCount++;
                }
            }
       

        return statistics;
    }

        private string GeneratePlainTextStatistics(Statistics statistics)
        {
            double totalGenderCount = statistics.MaleCount + statistics.FemaleCount;
            double totalFirstNameCount = statistics.FirstNameAMCount + statistics.FirstNameNZCount;
            double totalLastNameCount = statistics.LastNameAMCount + statistics.LastNameNZCount;
            double totalStateCount = statistics.StateCounts.Sum(x => x.Value);
            double totalAgeCount = statistics.Age0To20Count + statistics.Age21To40Count +
                                   statistics.Age41To60Count + statistics.Age61To80Count +
                                   statistics.Age81To100Count + statistics.Age100PlusCount;

            return $"Percentage female versus male: {statistics.FemaleCount / totalGenderCount * 100}%\n" +
                   $"Percentage first names A-M versus N-Z: {statistics.FirstNameAMCount / totalFirstNameCount * 100}%\n" +
                   $"Percentage last names A-M versus N-Z: {statistics.LastNameAMCount / totalLastNameCount * 100}%\n" +
                   $"Percentage of people in each state:\n" +
                   string.Join('\n', statistics.StateCounts.Select(x => $"{x.Key}: {x.Value / totalStateCount * 100}%")) + '\n' +
                   $"Percentage of people in each age range:\n" +
                   $"0-20: {statistics.Age0To20Count / totalAgeCount * 100}%\n" +
                   $"21-40: {statistics.Age21To40Count / totalAgeCount * 100}%\n" +
                   $"41-60: {statistics.Age41To60Count / totalAgeCount * 100}%\n" +
                   $"61-80: {statistics.Age61To80Count / totalAgeCount * 100}%\n" +
                   $"81-100: {statistics.Age81To100Count / totalAgeCount * 100}%\n" +
                   $"100+: {statistics.Age100PlusCount / totalAgeCount * 100}%";
        }

        private XElement GenerateXmlStatistics(Statistics statistics)
        {
            double totalGenderCount = statistics.MaleCount + statistics.FemaleCount;
            double totalFirstNameCount = statistics.FirstNameAMCount + statistics.FirstNameNZCount;
            double totalLastNameCount = statistics.LastNameAMCount + statistics.LastNameNZCount;
            double totalStateCount = statistics.StateCounts.Sum(x => x.Value);
            double totalAgeCount = statistics.Age0To20Count + statistics.Age21To40Count +
                                   statistics.Age41To60Count + statistics.Age61To80Count +
                                   statistics.Age81To100Count + statistics.Age100PlusCount;

            var root = new XElement("Statistics",
                new XElement("GenderPercentage",
                    new XElement("Female", statistics.FemaleCount / totalGenderCount * 100),
                    new XElement("Male", statistics.MaleCount / totalGenderCount * 100)
                ),
                new XElement("FirstNamesPercentage",
                    new XElement("FirstNameAtoM", statistics.FirstNameAMCount / totalFirstNameCount * 100),
                    new XElement("FirstNameNtoZ", statistics.FirstNameNZCount / totalFirstNameCount * 100)
                ),
                new XElement("LastNamesPercentage",
                    new XElement("LastNameAtoM", statistics.LastNameAMCount / totalLastNameCount * 100),
                    new XElement("LastNameNtoZ", statistics.LastNameNZCount / totalLastNameCount * 100)
                ),
                new XElement("StatePercentage",
                    statistics.StateCounts.Select(x => new XElement(XmlConvert.EncodeName(x.Key), x.Value / totalStateCount * 100))
                ),
                new XElement("AgeRangePercentage",
                    new XElement("Age0To20", statistics.Age0To20Count / totalAgeCount * 100),
                    new XElement("Age21To40", statistics.Age21To40Count / totalAgeCount * 100),
                    new XElement("Age41To60", statistics.Age41To60Count / totalAgeCount * 100),
                    new XElement("Age61To80", statistics.Age61To80Count / totalAgeCount * 100),
                    new XElement("Age81To100", statistics.Age81To100Count / totalAgeCount * 100),
                    new XElement("Age100Plus", statistics.Age100PlusCount / totalAgeCount * 100)
                )
            );

            return root;
        }

    }
}