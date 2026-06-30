using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Payroll.AzureFunctions
{
    public static class InputBindingExampleFunction
    {
        //[Function("InputBindingExampleFunction")]
        public static async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequestData req,
            [CosmosDBInput(databaseName:"mydbname",
                            containerName:"mytable",
                            SqlQuery = "Select * from myTable where myTable.id = {id}",
                            Connection = "CosmosDBConnection")] IEnumerable<User> users,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            var response = req.CreateResponse(HttpStatusCode.OK);
            if (users == null || users.Any())
            {
                response.StatusCode = HttpStatusCode.NotFound;
                await response.WriteStringAsync("User Not Found");
            }
            else
            {
                await response.WriteAsJsonAsync(users);
            }

            return response;
        }
    }

    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
}
