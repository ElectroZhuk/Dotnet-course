using Microsoft.EntityFrameworkCore;
using System.Diagnostics.CodeAnalysis;

namespace OrganizationAPI.Models
{
    public class OrganizationContext : DbContext
    {
        public OrganizationContext(DbContextOptions<OrganizationContext> options) : base(options)
        {
            
        }

        public DbSet<Organization> Organizations { get; set; } = null!;
    }
}
