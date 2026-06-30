namespace Payroll.AzureFunctions
{
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Azure.Functions.Worker;
    using Microsoft.Azure.Functions.Worker.Http;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;
    using Newtonsoft.Json;
    using System.IO;
    using System.Threading.Tasks;

    public class Function1
    {
        private readonly ILogger<Function1> _logger;
        private readonly IConfiguration _configuration;

        public Function1(ILogger<Function1> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        [Function("GetEmployeeInfo")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequestData req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");

            string name = req.Query["name"];

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            name = name ?? data?.name;

            string responseMessage = string.IsNullOrEmpty(name)
                ? "This HTTP triggered function executed successfully. Pass a name in the query string or in the request body for a personalized response."
                : $"Hello, {name}. This HTTP triggered function executed successfully.";

            return new OkObjectResult(responseMessage);
        }


        [Function("GetEmployeeInfoFromSQL")]
        public IActionResult GetEmployeeInfoFromSQL([HttpTrigger(AuthorizationLevel.Function, "get", Route = "employee/GetEmployeeInfoFromSQL")] HttpRequestData req)
        {
            _logger.LogInformation("C# HTTP trigger GetEmployeeInfoFromSQL function processed a request.");
            string connectionString = _configuration.GetConnectionString("DBConnection1");
            string responseMessage = $"Hello User, Please find the configured Connection string: {connectionString}. This HTTP triggered GetEmployeeInfoFromSQL function executed successfully.";

            return new OkObjectResult(responseMessage);
        }
    }
}
