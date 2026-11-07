using Scalar.AspNetCore;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static class ApiDefaultsExtensions
{
    private const string LocalhostCorsPolicy = "LocalhostPolicy";

    public static IServiceCollection AddApiDefaults(this IServiceCollection services)
    {
        services.AddControllers();
        services.AddProblemDetails();
        services.AddHealthChecks();
        services.AddCors(options =>
        {
            options.AddPolicy(LocalhostCorsPolicy, policy =>
            {
                policy
                    .SetIsOriginAllowed(origin => origin.StartsWith("http://localhost") || origin.StartsWith("https://localhost"))
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
        });

        services.AddOpenApi();

        return services;
    }

    public static WebApplication UseApiPipeline(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.UseCors(LocalhostCorsPolicy);
            app.MapOpenApi();
            app.MapScalarApiReference();
        }

        app.UsePathBase(new PathString("/api"));
        app.UseExceptionHandler();
        app.UseAuthentication();
        app.UseStatusCodePages();
        app.UseAuthorization();
        app.MapControllers();
        app.MapHealthChecks("/health");

        return app;
    }
}
