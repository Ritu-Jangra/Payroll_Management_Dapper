using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Payroll.AzureFunctions
{
    public static class QueueTriggerFunction
    {
        [Function("MyTestQueueTrigger")]
        public static void Run([QueueTrigger("myqueue-items", Connection = "")]string myQueueItem, ILogger log)
        {
            string message = $"C# Queue trigger function processed: {myQueueItem}";
            log.LogInformation(message);
        }
    }
}
