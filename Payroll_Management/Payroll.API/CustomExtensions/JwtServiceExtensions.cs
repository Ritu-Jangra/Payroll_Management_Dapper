namespace Payroll.API
{
    using Microsoft.AspNetCore.Authentication.JwtBearer;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.IdentityModel.Tokens;
    using System.Text;

    public static class JwtServiceExtensions
    {
        /// <summary>
        /// Adds JWT authentication to the service collection.
        /// It configures the JWT bearer options, including token validation parameters and custom responses for unauthorized and forbidden requests.
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <param name="config">Configuration</param>
        /// <returns>Returns the updated service collection</returns>
        public static IServiceCollection AddJwtAuthentication(this IServiceCollection services,
                                                              IConfiguration config)
        {
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    
                    ValidIssuer = config["Jwt:Issuer"],
                    ValidAudience = config["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!)),
                    ClockSkew = TimeSpan.Zero // Optional: set clock skew to zero to prevent token expiration issues during testing
                };

                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];
                        var path = context.HttpContext.Request.Path;

                        if (!string.IsNullOrEmpty(accessToken) &&
                            path.StartsWithSegments("/leaveHub"))
                        {
                            context.Token = accessToken;
                        }

                        return Task.CompletedTask;
                    },

                    OnChallenge = async context =>
                    {
                        context.HandleResponse();

                        var problem = new ProblemDetails
                        {
                            Status = StatusCodes.Status401Unauthorized,
                            Title = "Unauthorized",
                            Detail = context.ErrorDescription ?? "Authentication is required to access this resource.",
                            Instance = context.Request.Path,
                            Type = "https://httpstatuses.com/401"
                        };

                        problem.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
                        context.Response.StatusCode = problem.Status.Value;
                        context.Response.ContentType = "application/problem+json";

                        await context.Response.WriteAsJsonAsync(problem, context.HttpContext.RequestAborted);
                    },

                    OnForbidden = async context =>
                    {
                        var problem = new ProblemDetails
                        {
                            Status = StatusCodes.Status403Forbidden,
                            Title = "Forbidden",
                            Detail = "You do not have permission to access this resource.",
                            Instance = context.Request.Path,
                            Type = "https://httpstatuses.com/403"
                        };

                        problem.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
                        context.Response.StatusCode = problem.Status.Value;
                        context.Response.ContentType = "application/problem+json";

                        await context.Response.WriteAsJsonAsync(problem, context.HttpContext.RequestAborted);
                    }
                };
            });

            return services;
        }
    }
}
