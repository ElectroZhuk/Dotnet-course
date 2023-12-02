using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Net;
using System.Text.Json.Serialization;
using System.IO;
using System.Net.Http.Json;
using System.Text.Json;

namespace ConsoleApp
{
    class ProgramOnePointTwo
    {
        public static void Start()
        {
            string token = "c3bb78bfd6b210eab4d08b15d88f30da7fdf3478";

            Console.Write("Enter the company's TIN: ");
            string inn = Console.ReadLine();

            HttpClient client = new();
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Token", token);

            Company company = GetCompanyAsync(client, inn).Result;
            Console.WriteLine(company?.Name ?? "No companies were found by the specified TIN");
        }

        static async Task<Company> GetCompanyAsync(HttpClient client, string inn)
        {
            var query = new Dictionary<string, string>() { { "query", inn } };
            var content = new StringContent(JsonSerializer.Serialize(query), Encoding.UTF8, "application/json");
            HttpResponseMessage response = await client.PostAsync("https://suggestions.dadata.ru/suggestions/api/4_1/rs/findById/party", content);
            Stream stream = await response.Content.ReadAsStreamAsync();
            var responseData = await JsonSerializer.DeserializeAsync<Dictionary<string, List<Company>>>(stream);

            return responseData.GetValueOrDefault("suggestions").FirstOrDefault();
        }
    }

    public record Company([property: JsonPropertyName("value")] string Name);
}
