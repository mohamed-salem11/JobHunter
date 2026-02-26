using JobHunter.Data;
using JobHunter.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
var builder = WebApplication.CreateBuilder(args);


var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<JobHunterContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options => options.SignIn.RequireConfirmedAccount = true)
 .AddEntityFrameworkStores<JobHunterContext>()
 .AddDefaultUI()
 .AddDefaultTokenProviders();

builder.Services.AddSignalR();
builder.Services.AddAutoMapper(typeof(Program));
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = "redis-15455.c80.us-east-1-2.ec2.cloud.redislabs.com:15455,password=semFuADD3VugCBOmH6Z5xrsCDY9RWyYG,ssl=false";
    options.InstanceName = "JobHunter_";
});
var app = builder.Build();

 
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
}
app.UseStaticFiles();
app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

app.Run();
