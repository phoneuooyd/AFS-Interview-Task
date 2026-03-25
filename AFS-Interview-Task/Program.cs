using AFS_Interview_Task.Infrastructure;
using AFS_Interview_Task.Middleware;
using AFS_Interview_Task.Providers;
using AFS_Interview_Task.Providers.FunTranslations;
using AFS_Interview_Task.Repositories;
using AFS_Interview_Task.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Database setup
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Exception Handler
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// Middleware & Accessories
builder.Services.AddScoped<ICorrelationIdAccessor, CorrelationIdAccessor>();

// Translation routing configuration - map request translator => concrete provider key
builder.Services.Configure<TranslatorRoutingOptions>(builder.Configuration.GetSection("TranslatorRouting"));
builder.Services.Configure<TranslationExecutionOptions>(builder.Configuration.GetSection("TranslationExecution"));
builder.Services.Configure<RapidApiLeetDecoderOptions>(builder.Configuration.GetSection("RapidApiLeetDecoder"));

// External providers
builder.Services.AddHttpClient<FunTranslationsProvider>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["FunTranslations:BaseUrl"] ?? "https://api.funtranslations.com/translate/");
    client.Timeout = TimeSpan.FromSeconds(10);
});

builder.Services.AddHttpClient<RapidApiLeetSpeakDecoderProvider>(client =>
{
    var baseUrl = builder.Configuration["RapidApiLeetDecoder:BaseUrl"] ?? "https://leet-speak-encoder-and-decoder-api-apiverve.p.rapidapi.com/";
    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(10);
});

// Providers factory
builder.Services.AddScoped<ITranslatorProvider, FunTranslationsProvider>();
builder.Services.AddScoped<ITranslatorProvider, RapidApiLeetSpeakDecoderProvider>();
builder.Services.AddScoped<TranslatorProviderFactory>();

// Scoped services & repos
builder.Services.AddScoped<ITranslationLogRepository, TranslationLogRepository>();
builder.Services.AddScoped<ITranslationService, TranslationService>();

var app = builder.Build();

app.UseExceptionHandler(); // .NET 8 global exception handling

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<CorrelationIdMiddleware>();

// dev env doesn't have https thus off
// app.UseHttpsRedirection(); 

app.MapControllers();

// Swagger UI
app.MapGet("/", context =>
{
    context.Response.Redirect("/swagger");
    return System.Threading.Tasks.Task.CompletedTask;
});

await app.RunAsync();
