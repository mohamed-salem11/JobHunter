using JobHunter.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace JobHunter.Data;
public class JobHunterContext : IdentityDbContext<ApplicationUser>
{
    public JobHunterContext(DbContextOptions<JobHunterContext> options)
        : base(options)
    {
    }

      public DbSet<Job> Jobs { get; set; }
    public DbSet<ApplicationUser> ApplicationUsers {  get; set; }

    public DbSet<JobApplication> Applications { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.Entity<JobApplication>()
       .Property(j => j.Id)
       .ValueGeneratedOnAdd();
    }
}












