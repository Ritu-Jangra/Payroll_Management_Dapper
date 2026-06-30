namespace Payroll.API
{
    using FluentValidation;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;

    public class CustomGlobalExceptionMiddleware
    {
        private readonly ILogger<CustomGlobalExceptionMiddleware> _logger;
        private readonly RequestDelegate _next;

        public CustomGlobalExceptionMiddleware(ILogger<CustomGlobalExceptionMiddleware> logger,
                                               RequestDelegate next)
        {
            _logger = logger;
            _next = next;
        }

        /// <summary>
        /// Intercepts exceptions thrown during the request processing pipeline, logs the error details, and returns a standardized ProblemDetails response to the client based on the type of exception encountered.
        /// It handles common exceptions such as ValidationException, BadHttpRequestException, UnauthorizedAccessException, and KeyNotFoundException, mapping them to appropriate HTTP status codes and messages.
        /// For any unhandled exceptions, it returns a generic 500 Internal Server Error response.
        /// This middleware ensures that clients receive consistent error responses while also providing detailed logging for troubleshooting purposes.
        /// </summary>
        /// <param name="context">HTTP context</param>
        /// <returns>Returns a task that represents the asynchronous operation</returns>
        public async Task InvokeAsync(HttpContext context)
        
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled Exception occurred at {Path}", context.Request.Path);
                await HandleExceptionAsync(context, ex);
            }
        }

        /// <summary>
        /// Handles the exception by determining the appropriate HTTP status code and message based on the type of exception, and then constructs a ProblemDetails response to send back to the client.
        /// It also includes the trace identifier in the response for better correlation with logs.
        /// </summary>
        /// <param name="context">HTTP context</param>
        /// <param name="ex">Exception</param>
        /// <returns>Returns a task that represents the asynchronous operation</returns>
        private static async Task HandleExceptionAsync(HttpContext context, Exception ex)
        {
            context.Response.ContentType = "application/problem+json";

            var (statusCode, title, detail) = ex switch
            {
                ValidationException => (StatusCodes.Status400BadRequest, "Validation Failed", ex.Message),
                BadHttpRequestException => (StatusCodes.Status400BadRequest, "Bad Request", ex.Message),
                UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "Unauthorized", ex.Message),
                KeyNotFoundException => (StatusCodes.Status404NotFound, "The requested resource was not found.", ex.Message),
                _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred.", ex.Message)
            };

            context.Response.StatusCode = statusCode;

            var problemDetails = new ProblemDetails
            {
                Status = statusCode,
                Title = title,
                Detail = detail,
                Instance = context.Request.Path,
                Type = $"https://httpstatuses.com/{statusCode}"
            };

            problemDetails.Extensions["traceId"] = context.TraceIdentifier;

            await context.Response.WriteAsJsonAsync(problemDetails, context.RequestAborted);
        }
    }
}