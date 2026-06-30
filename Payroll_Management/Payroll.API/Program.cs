namespace Payroll.API
{
    using Asp.Versioning;
    using Asp.Versioning.ApiExplorer;
    using Dapper;
    using FluentValidation;
    using FluentValidation.AspNetCore;
    using Microsoft.AspNetCore.HttpOverrides;
    using Microsoft.AspNetCore.SignalR;
    using Microsoft.Data.SqlClient;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using NSwag.AspNetCore;
    using Payroll.Business;
    using Payroll.Business.Services;
    using Payroll.Contracts;
    using Payroll.Data;
    using Payroll.Domain;
    using Serilog;
    using System.Data.Common;
    using System.Text.Json.Serialization;

    public static class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            Console.WriteLine(builder.Environment.EnvironmentName);
            // Add services to the container with custom configurations
            builder.Services.AddControllers().AddJsonOptions(options =>
            {
                // Configure JSON serialization to use string representation for enums
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            });

            // Create a new sql connection using configuration
            builder.Services.AddScoped<DbConnection>(serviceProvider =>
            {
                var configuration = serviceProvider.GetRequiredService<IConfiguration>();

                return new SqlConnection(configuration.GetConnectionString("DBConnection"));
            });

            // Add HttpContextAccessor to access the current HTTP context in services
            builder.Services.AddHttpContextAccessor();

            // Register application services for dependency injection
            builder.Services.AddScoped<IJwtService, JwtService>();
            builder.Services.AddScoped<IPasswordService, PasswordService>();
            builder.Services.AddScoped<IAuthService, AuthService>();
            builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
            builder.Services.AddScoped<ILeaveRepository, LeaveRepository>();
            builder.Services.AddScoped<ILeaveService, LeaveService>();
            builder.Services.AddScoped<IEmployeeRepository, EmployeeRepository>();
            builder.Services.AddScoped<IEmployeeService, EmployeeService>();
            builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
            builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();

            // Add FluentValidation services and register validators from the assembly
            builder.Services.AddFluentValidationAutoValidation();
            builder.Services.AddTransient<IValidator<LoginRequestDto>, LoginRequestDtoValidator>();
            builder.Services.AddTransient<IValidator<ApplyLeaveRequestDto>, ApplyLeaveRequestDtoValidator>();
            builder.Services.AddTransient<IValidator<UpdateLeaveRequestDto>, UpdateLeaveRequestDtoValidator>();
            builder.Services.AddTransient<IValidator<AddEmployeeRequestDto>, AddEmployeeRequestDtoValidator>();
            builder.Services.AddTransient<IValidator<UpdateEmployeeRequestDto>, UpdateEmployeeRequestDtoValidator>();

            // Add AutoMapper services and register profiles from the assembly
            builder.Services.AddAutoMapper(configAction =>
            {
                configAction.AddProfile<AutoMapperProfiles>();
            });

            // Configure JWT authentication
            builder.Services.AddJwtAuthentication(builder.Configuration);

            // Add authorization services including policies based on user claims for permissions defined in the PermissionType enum
            builder.Services.AddAuthorization(options =>
            {
                var permissions = Enum.GetNames<PermissionType>();

                foreach (var permission in permissions)
                {
                    options.AddPolicy(permission, policy => policy.RequireClaim("Permission", permission));
                }
            });

            // Configure Serilog for logging to both console and rolling file with size limit
            //Log.Logger = new LoggerConfiguration().Enrich.FromLogContext()
            //             .WriteTo.Console()
            //             .WriteTo.File(path: "logs/log.txt",
            //                           fileSizeLimitBytes: 1_000_000,
            //                           rollOnFileSizeLimit: true,     // create new file when limit reached
            //                           retainedFileCountLimit: 10,    // keep last 10 files
            //                           rollingInterval: RollingInterval.Infinite) // disable time-based rolling
            //             .CreateLogger();
            Log.Logger = new LoggerConfiguration()
                        .Enrich.FromLogContext()
                        .WriteTo.Console()
                        .CreateLogger();

            // Use Serilog as the logging provider
            builder.Host.UseSerilog();

            // Configure API versioning to support multiple versions of the API and allow clients to specify the version in the URL
            builder.Services.AddApiVersioning(options =>
            {
                options.AssumeDefaultVersionWhenUnspecified = true;
                options.DefaultApiVersion = new ApiVersion(1, 0);
                options.ReportApiVersions = true;

                options.ApiVersionReader = new UrlSegmentApiVersionReader(); // URL versioning
            }).AddApiExplorer(options =>
            {
                options.GroupNameFormat = "'v'VVV";
                options.SubstituteApiVersionInUrl = true;
            });

            // Configure Swagger to include JWT authentication
            builder.Services.AddSwaggerWithJwtAuthentication();

            builder.Services.Configure<OpenApiDocumentMiddlewareSettings>(settings =>
            {
                settings.PostProcess = (document, request) =>
                {
                    // Ensures ApiVersionNeutral + versioned controllers show correctly
                    document.Info.Title = "Payroll API";
                };
            });

            // Configure CORS to allow requests from the MVC frontend application running on https://localhost:7060
            //builder.Services.AddCors(options =>
            //{
            //    options.AddPolicy("AllowMvcApp", policy =>
            //    {
            //        policy.WithOrigins("https://localhost:7060")
            //              .AllowAnyHeader()
            //              .AllowAnyMethod()
            //              .AllowCredentials();
            //    });
            //});
            var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();

            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowMvcApp", policy =>
                {
                    policy.WithOrigins(origins!)
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials();
                });
            });
           

            // Add antiforgery services to protect against CSRF attacks, especially for state-changing operations
            builder.Services.AddAntiforgery(options =>
            {
                // Internal Cookie for server-side validation of CSRF tokens, not accessible to client-side scripts
                options.Cookie.Name = "Payroll-CSRF-INTERNAL";   // cookie name
                options.Cookie.HttpOnly = true;
                options.Cookie.SameSite = SameSiteMode.Strict;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                options.HeaderName = "X-XSRF-TOKEN";   // header name
            });

            // Add in-memory caching services to improve performance by caching frequently accessed data and reducing database load
            builder.Services.AddMemoryCache();

            // Add custom background services for tasks like cleaning up expired refresh tokens
            builder.Services.AddHostedService<CustomBackgoundServices>();

            builder.Services.AddSignalR();

            builder.Services.AddSingleton<IUserIdProvider, CustomUserIdProvider>();

            // Enum conversion Handler
            SqlMapper.AddTypeHandler(new EnumToStringHandler<Enum>());

            builder.Services.AddHsts(options =>
            {
                options.MaxAge = TimeSpan.FromDays(365);
                options.IncludeSubDomains = true;
            });

            if (!builder.Environment.IsDevelopment())
            {
                builder.Services.AddApplicationInsightsTelemetry();
            }
            builder.Services.AddHealthChecks();

            var app = builder.Build();

            app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
            });

            if (!app.Environment.IsDevelopment())
            {
                app.UseHsts();
            }

            // Enable HTTPS redirection middleware to redirect HTTP requests to HTTPS for improved security
            app.UseHttpsRedirection();

            // Enable routing middleware
            app.UseRouting();

            // Enable CORS middleware to allow cross-origin requests from the MVC frontend
            app.UseCors("AllowMvcApp");

            // Enable antiforgery middleware to validate CSRF tokens for state-changing requests and protect against CSRF attacks
            app.UseAntiforgery();

            // Get the API version description provider to dynamically generate Swagger endpoints for each API version
            app.Services.GetRequiredService<IApiVersionDescriptionProvider>();

            // Register OpenAPI docs dynamically
            if (app.Environment.IsDevelopment())
            {
                app.UseOpenApi(settings =>
                {
                    settings.Path = "/swagger/{documentName}/swagger.json";
                });

                app.UseSwaggerUi(settings =>
                {
                    settings.Path = "/swagger";
                    settings.DocumentPath = "/swagger/{documentName}/swagger.json";
                });
            }

            // Add custom middleware to handle global exceptions and return consistent error responses
            app.UseMiddleware<CustomGlobalExceptionMiddleware>();

            // Add custom middleware to log incoming requests and outgoing responses for debugging and monitoring purposes
            app.UseMiddleware<RequestResponseLoggingMiddleware>();

            // Enable authentication and authorization middleware
            app.UseAuthentication();
            app.UseAuthorization();

            // Configure the HTTP request pipeline.
            app.MapControllers();

            app.MapHub<LeaveNotificationClient>("/leaveHub");
            app.MapHealthChecks("/health");

            app.Run();
        }
    }
}
