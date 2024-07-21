using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NLog;
using NLog.Web;
using Saunter;
using Saunter.Streetlights.Api;
using StreetlightsAPI;

LogManager.Setup().LoadConfigurationFromAppSettings();

var builder = WebApplication.CreateBuilder(args);
builder.Logging.AddSimpleConsole(console => console.SingleLine = true);
builder.Host.UseNLog();

// Add Saunter to the application services. 
builder.Services.AddAsyncApiStreetlights();

builder.Services.AddScoped<IStreetlightMessageBus, StreetlightMessageBus>();
builder.Services.AddControllers();

var app = builder.Build();

app.UseDeveloperExceptionPage();

app.UseRouting();
app.UseCors(configure => configure.AllowAnyOrigin().AllowAnyMethod());

// to be fixed with issue #173
#pragma warning disable ASP0014 // Suggest using top level route registrations instead of UseEndpoints
app.UseEndpoints(endpoints =>
{
    endpoints.MapAsyncApiDocuments();
    endpoints.MapAsyncApiUi();

    endpoints.MapControllers();
});
#pragma warning restore ASP0014 // Suggest using top level route registrations instead of UseEndpoints

await app.StartAsync();

// Print the AsyncAPI doc location
var logger = app.Services.GetService<ILoggerFactory>().CreateLogger<Program>();
var options = app.Services.GetService<IOptions<AsyncApiOptions>>();
var addresses = app.Urls;
logger.LogInformation("AsyncAPI doc available at: {URL}", $"{addresses.FirstOrDefault()}{options.Value.Middleware.Route}");
logger.LogInformation("AsyncAPI UI available at: {URL}", $"{addresses.FirstOrDefault()}{options.Value.Middleware.UiBaseRoute}");

// Redirect base url to AsyncAPI UI
app.Map("/", () => Results.Redirect("index.html"));
app.Map("/index.html", () => Results.Redirect(options.Value.Middleware.UiBaseRoute));

await app.WaitForShutdownAsync();
