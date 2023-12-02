using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrganizationAPI.Models;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.Json;
using NuGet.Common;
using System.Net.Http.Headers;

namespace OrganizationAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrganizationsController : ControllerBase
    {
        private HttpClient _client;

        public OrganizationsController()
        {
            _client =  new();
            _client.DefaultRequestHeaders.Accept.Clear();
            _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Token", "c3bb78bfd6b210eab4d08b15d88f30da7fdf3478");
        }

        [HttpGet("GetName/{inn}")]
        public async Task<ActionResult<Company>> GetOrganization(string inn)
        {
            var company = await GetCompanyAsync(inn);
            
            if (company == null)
            {
                return NotFound();
            }

            return company;
        }

        public record Company([property: JsonPropertyName("value")] string Name);

        private async Task<Company> GetCompanyAsync(string inn)
        {
            var query = new Dictionary<string, string>() { { "query", inn } };
            var content = new StringContent(JsonSerializer.Serialize(query), Encoding.UTF8, "application/json");
            HttpResponseMessage response = await _client.PostAsync("https://suggestions.dadata.ru/suggestions/api/4_1/rs/findById/party", content);
            Stream stream = await response.Content.ReadAsStreamAsync();
            var responseData = await JsonSerializer.DeserializeAsync<Dictionary<string, List<Company>>>(stream);

            return responseData.GetValueOrDefault("suggestions").FirstOrDefault();
        }
    }
}
