using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Payroll.Business;
using Payroll.Domain;

namespace Payroll.AzureFunctions
{
    public class Function3
    {
        private readonly IPasswordService _passwordService;
        private readonly ILogger<Function3> log;

        public Function3(ILogger<Function3> logger, IPasswordService passwordService)
        {
            log = logger;
            _passwordService = passwordService;
        }

        [Function("GetHashedPassword")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "Employee/GetHashedPassword")] HttpRequestData req)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");
            Employee employee = new()
            {
                FirstName = "Dummy",
                LastName = "Test",
                UserName = "TestDum",
                Email = "Test@gmail.com",
                Salary = 10000,
                Department = DepartmentType.HR,
                Password = "Testing@123",
                PasswordHash = ""
            };

            var data = _passwordService.HashPassword(employee);
            var response = req.CreateResponse(data != null ? System.Net.HttpStatusCode.OK : System.Net.HttpStatusCode.NotFound);

            if (data == null)
            {
                await response.WriteStringAsync("No Employee Found");
            }
            else
            {
                await response.WriteAsJsonAsync(data);
            }

            return response;
        }
    }
}
