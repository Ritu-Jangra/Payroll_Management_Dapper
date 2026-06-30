namespace Payroll.API
{
    public class RequestResponseLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestResponseLoggingMiddleware> _logger;

        public RequestResponseLoggingMiddleware(RequestDelegate next,
                                                ILogger<RequestResponseLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        /// <summary>
        /// Intercepts incoming HTTP requests and outgoing HTTP responses to log relevant information such as the request method, path, and response status code.
        /// This middleware can be used for monitoring and debugging purposes to track the flow of requests and responses through the application.
        /// </summary>
        /// <param name="context">HTTP context</param>
        /// <returns>Returns a task that represents the asynchronous operation</returns>
        public async Task Invoke(HttpContext context)
        {
            // Log Request
            _logger.LogInformation("Incoming Request: {Method} {Path}", context.Request.Method, context.Request.Path);

            await _next(context);

            // Log Response
            _logger.LogInformation("Outgoing Response: {StatusCode}", context.Response.StatusCode);
        }
    }
}
