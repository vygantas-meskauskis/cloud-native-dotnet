using Azure.Monitor.OpenTelemetry.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Cross-cutting defaults every Ledgerly service gets with one line: <c>builder.AddServiceDefaults()</c>.
/// Started from the Aspire "service defaults" template, then adapted:
/// - our own ActivitySource/Meter ("Ledgerly") are subscribed,
/// - Azure Monitor export switches on when APPLICATIONINSIGHTS_CONNECTION_STRING is present,
/// - health endpoints are mapped in every environment (Container Apps probes need them) but reveal no details.
/// </summary>
public static class Extensions
{
    public const string ReadinessPath = "/health";
    public const string LivenessPath = "/alive";
    private const string TelemetryName = "Ledgerly";

    public static TBuilder AddServiceDefaults<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        builder.ConfigureOpenTelemetry();
        builder.AddDefaultHealthChecks();

        builder.Services.AddServiceDiscovery();

        builder.Services.ConfigureHttpClientDefaults(http =>
        {
            // Retries with jittered back-off, per-attempt + total timeouts, circuit breaker, rate limiter.
            http.AddStandardResilienceHandler();

            // Resolves "https+http://marketdata" to a real address (Aspire locally, config or DNS in Azure).
            http.AddServiceDiscovery();
        });

        return builder;
    }

    public static TBuilder ConfigureOpenTelemetry<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
        });

        builder.Services.AddOpenTelemetry()
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation()
                .AddMeter(TelemetryName))
            .WithTracing(tracing => tracing
                .AddSource(builder.Environment.ApplicationName)
                .AddSource(TelemetryName)
                .AddSource("Azure.*")
                .AddAspNetCoreInstrumentation(options =>
                    // Probes hit these every few seconds: keep them out of traces.
                    options.Filter = context =>
                        !context.Request.Path.StartsWithSegments(ReadinessPath)
                        && !context.Request.Path.StartsWithSegments(LivenessPath))
                .AddHttpClientInstrumentation());

        builder.AddOpenTelemetryExporters();
        return builder;
    }

    private static TBuilder AddOpenTelemetryExporters<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        // Locally Aspire sets this and the dashboard receives OTLP. Any OTLP backend works the same way.
        if (!string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]))
        {
            builder.Services.AddOpenTelemetry().UseOtlpExporter();
        }

        // In Azure the Container App gets this setting and telemetry goes to Application Insights.
        if (!string.IsNullOrWhiteSpace(builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]))
        {
            builder.Services.AddOpenTelemetry().UseAzureMonitor();
        }

        return builder;
    }

    public static TBuilder AddDefaultHealthChecks<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        builder.Services.AddHealthChecks()
            // Liveness: "the process is responsive". Never check dependencies here, or a DB blip restarts every replica.
            .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);

        return builder;
    }

    public static WebApplication MapDefaultEndpoints(this WebApplication app)
    {
        // Readiness: all checks (including DB, Redis, Service Bus registered by Aspire integrations).
        app.MapHealthChecks(ReadinessPath).DisableHttpMetrics();

        // Liveness: only checks tagged "live".
        app.MapHealthChecks(LivenessPath, new HealthCheckOptions
        {
            Predicate = r => r.Tags.Contains("live"),
        }).DisableHttpMetrics();

        return app;
    }
}
