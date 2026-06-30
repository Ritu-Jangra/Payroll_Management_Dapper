using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Payroll.AzureFunctions
{
    public class Function2
    {
        private readonly ILogger<Function2> log;

        public Function2(ILogger<Function2> logger)
        {
            log = logger;
        }

        [Function("GetMyLeaves")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "leaves/GetMyLeaves")] HttpRequest req)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string name = req.Query["name"];

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            name = name ?? data?.name;

            string responseMessage = string.IsNullOrEmpty(name)
                ? "This HTTP triggered function executed successfully. Pass a name in the query string or in the request body for a personalized response."
                : $"Hello, {name}. This HTTP triggered function executed successfully.";

            return new OkObjectResult(responseMessage);
        }

        [Function("GetTeamLeaves")]
        public IActionResult GetTeamLeaves([HttpTrigger(AuthorizationLevel.Function, "get", Route = "leaves/GetTeamLeaves")] HttpRequest req)
        {
            log.LogInformation("C# HTTP trigger GetTeamLeaves function processed a request.");

            var externalManager = Environment.GetEnvironmentVariable("Manager");
            string responseMessage = $"Hello, {externalManager}. This HTTP triggered GetTeamLeaves function executed successfully.";

            return new OkObjectResult(responseMessage);
        }
    }
}
