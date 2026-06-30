namespace Payroll.API
{
    using Microsoft.Extensions.DependencyInjection;
    using NSwag;
    using NSwag.Generation.Processors.Security;

    public static class SwaggerExtensions
    {
        public static IServiceCollection AddSwaggerWithJwtAuthentication(
            this IServiceCollection services)
        {
            services.AddOpenApiDocument(config =>
            {
                // Enables API VERSION GROUPS
                config.ApiGroupNames = new[] { "v1", "v2" };

                config.PostProcess = document =>
                {
                    document.Info.Title = "Payroll API";
                };

                // JWT GLOBAL
                config.AddSecurity("Bearer", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = OpenApiSecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    In = OpenApiSecurityApiKeyLocation.Header
                });

                config.OperationProcessors.Add(new AspNetCoreOperationSecurityScopeProcessor("Bearer"));
            });

            return services;
        }
    }
}