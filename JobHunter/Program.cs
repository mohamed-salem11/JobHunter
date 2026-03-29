using Hangfire;
using Hangfire.MemoryStorage;
using JobHunter.Data;
using JobHunter.Models;
using JobHunter.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<JobHunterContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options => {
    options.SignIn.RequireConfirmedAccount = true;
})
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

builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<DailyDigestService>();

 builder.Services.AddHangfire(config => config
    .UseSimpleAssemblyNameTypeSerializer()  
    .UseRecommendedSerializerSettings()    
    .UseMemoryStorage());                 

builder.Services.AddHangfireServer();

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.UseHangfireDashboard("/hangfire");

RecurringJob.AddOrUpdate<DailyDigestService>(
    "daily-digest",
    service => service.SendDailyDigestAsync(),
    "0 17 * * *"  
);

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();
 
app.Run();