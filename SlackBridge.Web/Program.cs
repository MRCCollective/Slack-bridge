using System.Globalization;
using Hangfire;
using Hangfire.MemoryStorage;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using SlackBridge.Web.Data;
using SlackBridge.Web.Models;
using SlackBridge.Web.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeFolder("/Admin");
});
builder.Services.AddControllers();
builder.Services.AddDataProtection();
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var culture = new CultureInfo("sv-SE");
    options.DefaultRequestCulture = new RequestCulture(culture);
    options.SupportedCultures = [culture];
    options.SupportedUICultures = [culture];
});
builder.Services.AddDbContext<SlackBridgeDbContext>(options =>
{
    var provider = builder.Configuration["Database:Provider"];
    var connectionString = builder.Configuration.GetConnectionString("SlackBridge");

    if (string.Equals(provider, "MariaDb", StringComparison.OrdinalIgnoreCase))
    {
        var serverVersion = ServerVersion.Parse(
            builder.Configuration["MySql:ServerVersion"] ?? "11.4.0-mariadb");
        options.UseMySql(connectionString, serverVersion, mySqlOptions =>
            mySqlOptions.EnableRetryOnFailure());
    }
    else
    {
        options.UseSqlServer(connectionString);
    }
});
builder.Services.AddHangfire(configuration => configuration.UseMemoryStorage());
builder.Services.AddHangfireServer();
builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.Password.RequiredLength = 10;
        options.Password.RequireNonAlphanumeric = false;
        options.User.RequireUniqueEmail = true;
    })
    .AddEntityFrameworkStores<SlackBridgeDbContext>()
    .AddDefaultTokenProviders();
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
});
builder.Services.AddHttpClient<ISlackService, SlackService>();
builder.Services.AddScoped<ICustomerInstanceContext, CustomerInstanceContext>();
builder.Services.AddScoped<IApiKeyGenerator, ApiKeyGenerator>();
builder.Services.AddScoped<IApiKeySecretProtector, ApiKeySecretProtector>();
builder.Services.AddScoped<IApiKeyValidator, ApiKeyValidator>();
builder.Services.AddScoped<ITemplateService, TemplateService>();
builder.Services.AddScoped<IEventLogService, EventLogService>();
builder.Services.AddScoped<IEventIngestionService, EventIngestionService>();
builder.Services.AddScoped<IEventDefinitionTestService, EventDefinitionTestService>();
builder.Services.AddScoped<IPlanLimitService, PlanLimitService>();
builder.Services.AddScoped<IUsageService, UsageService>();
builder.Services.AddScoped<IBillingService, BillingService>();
builder.Services.AddSingleton<ILocalClock, LocalClock>();
builder.Services.AddScoped<EventLogCleanupJob>();
builder.Services.AddHostedService<FailedSlackRetryWorker>();

var app = builder.Build();

await AppInitializer.InitializeAsync(app.Services);

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

app.UseRouting();
app.UseRequestLocalization();

app.UseAuthentication();
app.UseAuthorization();

var recurringJobManager = app.Services.GetRequiredService<IRecurringJobManager>();
recurringJobManager.AddOrUpdate<EventLogCleanupJob>(
    "event-log-cleanup",
    job => job.DeleteOldLogsAsync(CancellationToken.None),
    Cron.Daily(2),
    new RecurringJobOptions { TimeZone = LocalClock.SwedishTimeZone });

app.MapStaticAssets();
app.MapControllers();
app.MapRazorPages()
   .WithStaticAssets();

app.Run();
