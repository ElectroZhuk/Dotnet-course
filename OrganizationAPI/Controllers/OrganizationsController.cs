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
        private readonly OrganizationContext _context;
        private HttpClient _client;

        public OrganizationsController(OrganizationContext context)
        {
            _context = context;
            _client =  new();
            _client.DefaultRequestHeaders.Accept.Clear();
            _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Token", "c3bb78bfd6b210eab4d08b15d88f30da7fdf3478");
        }

        // GET: api/Organizations
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Organization>>> GetOrganizations()
        {
          if (_context.Organizations == null)
          {
              return NotFound();
          }
            return await _context.Organizations.ToListAsync();
        }

        // GET: api/Organizations/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Organization>> GetOrganization(long id)
        {
          if (_context.Organizations == null)
          {
              return NotFound();
          }
            var organization = await _context.Organizations.FindAsync(id);

            if (organization == null)
            {
                return NotFound();
            }

            return organization;
        }

        [Route("api/[controller]/DaData")]
        [HttpGet("{inn}")]
        public async Task<ActionResult<Company>> GetCompany(string inn)
        {
            var company = await GetCompanyAsync(inn);
            
            if (company == null)
            {
                return NotFound();
            }

            return company;
        }

        // PUT: api/Organizations/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutOrganization(long id, Organization organization)
        {
            if (id != organization.Id)
            {
                return BadRequest();
            }

            _context.Entry(organization).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!OrganizationExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/Organizations
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<Organization>> PostOrganization(Organization organization)
        {
          if (_context.Organizations == null)
          {
              return Problem("Entity set 'OrganizationContext.Organizations'  is null.");
          }
            _context.Organizations.Add(organization);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetOrganization), new { id = organization.Id }, organization);
        }

        // DELETE: api/Organizations/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteOrganization(long id)
        {
            if (_context.Organizations == null)
            {
                return NotFound();
            }
            var organization = await _context.Organizations.FindAsync(id);
            if (organization == null)
            {
                return NotFound();
            }

            _context.Organizations.Remove(organization);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool OrganizationExists(long id)
        {
            return (_context.Organizations?.Any(e => e.Id == id)).GetValueOrDefault();
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
