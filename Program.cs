using Serilog;
using RevolutToDh.Models;
using RevolutToDh.Services;

var builder = WebApplication.CreateBuilder(args);

// Add Serilog configuration
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.AddRazorPages();
builder.Services.Configure<CompanySettings>(builder.Configuration.GetSection("CompanySettings"));
builder.Services.AddScoped<ICsvParserService, CsvParserService>();
builder.Services.AddScoped<IXmlGeneratorService, XmlGeneratorService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();
app.MapRazorPages();

try
{
    Log.Information("Starting web host");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Host terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
