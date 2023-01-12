using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace EmployeeManagement.Models
{
    public class AppDbContext : IdentityDbContext<ApplicationUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<Employee> Employees { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Seed();
            foreach (var foriegn in modelBuilder.Model.GetEntityTypes()
                                                    .SelectMany(x=>x.GetForeignKeys()))
            {
                foriegn.DeleteBehavior = DeleteBehavior.Restrict;
            }
        }
    }
}