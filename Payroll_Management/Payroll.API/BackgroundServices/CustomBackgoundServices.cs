namespace Payroll.API
{
    public class CustomBackgoundServices : BackgroundService
    {
        private readonly ILogger<CustomBackgoundServices> _logger;

        public CustomBackgoundServices(ILogger<CustomBackgoundServices> logger)
        {
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Background task running...");

                await Task.Delay(5000, stoppingToken);

                _logger.LogInformation("Background task completed an iteration.");
            }
        }
    }
}
