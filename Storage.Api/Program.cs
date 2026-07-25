using Microsoft.AspNetCore.Http.Features;
using Microsoft.OpenApi.Models;
using MinimalApi.Hosting;
using MinimalApi.Hosting.Options;
using Npgsql;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace Storage.Api;

internal sealed class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        
        // Настройка OpenTelemetry
        builder.Services.AddOpenTelemetry()
            .UseOtlpExporter()
            .WithLogging()
            .WithTracing(bld => bld
                .AddNpgsql()
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation())
            .WithMetrics(bld => bld
                .AddHttpClientInstrumentation()
                .AddNpgsqlInstrumentation()
                .AddAspNetCoreInstrumentation()
                .AddRuntimeInstrumentation()
                .AddProcessInstrumentation());

        // Настройка конфигурации
        builder.Services.BindOptions<StorageOptions>(builder.Configuration);

        // Настройка Swagger
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Storage.Api",
                Description = "API для работы Log-Structured Storage (LSS).",
                Version = "v1"
            });
            c.AddServer(new OpenApiServer { Url = "/" });
            c.AddServer(new OpenApiServer { Url = "/public" });
            c.AddServer(new OpenApiServer { Url = "/internal" });
            c.IncludeXmlComments(typeof(Program).Assembly);
            c.OrderActionsBy(apiDesc => $"{apiDesc.RelativePath}:{apiDesc.HttpMethod}");
        });

        // Регистрация сервисов
        builder.Services
            .AddCors(options => options
                .AddPolicy("all", policy => policy
                    .AllowAnyOrigin()
                    .AllowAnyHeader()
                    .AllowAnyMethod()))
            .AddApiHandlers()
            .AddHealthChecks();

        // Лимит для multipart/form-data из настройки
        // Лимит загрузки 32 МБ. 0 или отрицательное значение = без ограничения
        const int bodySizeLimit = 32 * 1024 * 1024;
        builder.Services.Configure<FormOptions>(options =>
            options.MultipartBodyLengthLimit = bodySizeLimit);
        
        var app = builder.Build();
        app.UseCors("all");
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.MapApiHandlers();
        app.MapHealthChecks("/health");
        app.UseMaxRequestBodySize(bodySizeLimit);

        await app.RunAsync();
    }
}