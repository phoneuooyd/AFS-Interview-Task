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

// External providers 
builder.Services.AddHttpClient<ITranslatorProvider, FunTranslationsProvider>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["FunTranslations:BaseUrl"] ?? "https://api.funtranslations.com/translate/");
    client.Timeout = TimeSpan.FromSeconds(10);
});

// Providers factory
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

// app.UseHttpsRedirection(); // Wyłączone ze względu na brak certyfikatu HTTPS w środowisku dev

app.MapControllers();

// Przekierowanie głównego URLa od razu do panelu Swagger UI
app.MapGet("/", context =>
{
    context.Response.Redirect("/swagger");
    return System.Threading.Tasks.Task.CompletedTask;
});

app.Run();
