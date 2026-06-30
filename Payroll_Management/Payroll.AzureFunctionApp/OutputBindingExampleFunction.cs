namespace Payroll.AzureFunctions
{
    using Microsoft.Azure.Functions.Worker;
    using Microsoft.Azure.Functions.Worker.Http;
    using Microsoft.Extensions.Logging;
    using System.Text;
    using System.Threading.Tasks;

    public static class OutputBindingExampleFunction
    {
        [Function("BindingsFunction")]
        public static async Task<SubmitFeedbackData> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequestData req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string requestData = await req.ReadAsStringAsync(Encoding.UTF8);

            string feedbackContent = $"Feedback Recieved {requestData}";

            return new SubmitFeedbackData()
            {
                OutputData = feedbackContent,
                HttpResponseData = req.CreateResponse(System.Net.HttpStatusCode.OK)
            };
        }
    }

    public class SubmitFeedbackData
    {
        [BlobOutput("feedback/{rand-guid}.txt", Connection = "AzureWebJobsStorage")]
        public string OutputData { get; set; }

        public HttpResponseData HttpResponseData { get; set; }
    }

}
