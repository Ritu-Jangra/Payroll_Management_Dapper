using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;
using Payroll.Domain;

namespace Payroll.AzureFunctionApp;

public class FanOutFanInFunction
{
    [Function(nameof(FanOutFanInFunction))]
    public async Task<List<string>> RunOrchestrator(
        [OrchestrationTrigger] TaskOrchestrationContext context)
    {
        ILogger logger = context.CreateReplaySafeLogger(nameof(FanOutFanInFunction));
        logger.LogInformation("Hi User");
        var outputs = new List<string>();

        // Replace name and input with values relevant for your Durable Functions Activity
        var step1 = context.CallActivityAsync<Leave>(nameof(GetLeaveByStatus), StatusType.Pending);
        var step2 = context.CallActivityAsync<Leave>(nameof(GetLeaveByStatus), StatusType.Approved);
        var step3 = context.CallActivityAsync<Leave>(nameof(GetLeaveByStatus), StatusType.Rejected);

        List<Task<Leave>> allSteps = new()
        {
            step1,
            step2,
            step3
        };

        var results = await Task.WhenAll(allSteps);
        var latestLeave = results.Where(x => x != null).OrderBy(x => x.StartDate).FirstOrDefault();
        if (latestLeave != null)
        {
            logger.LogInformation(
                "Upcoming leave applied is on StartDate: {StartDate} Ends-on: {EndDate} with Reason: {Reason}",
                latestLeave.StartDate,
                latestLeave.EndDate,
                latestLeave.Reason);
        }

        // returns ["Hello Tokyo!", "Hello Seattle!", "Hello London!"]
        return outputs;
    }

    [Function(nameof(GetLeaveByStatus))]
    public Task<Leave> GetLeaveByStatus([ActivityTrigger] StatusType status, FunctionContext executionContext)
    {
        ILogger logger = executionContext.GetLogger(nameof(GetLeaveByStatus));
        logger.LogInformation($"Getting leaves as per {status.ToString()}");

        Leave leave;
        switch (status)
        {
            case StatusType.Approved:
            leave = new Leave
            {
                LeaveId = Guid.Parse("7747e180-5612-46d4-9d91-e1787298ea22"),
                Reason = ReasonType.CasualLeave,
                StartDate = GetUtcDateTime(2023, 1, 1),
                EndDate = GetUtcDateTime(2023, 1, 5),
                Status = StatusType.Approved
            };
                break;
            case StatusType.Rejected:
                leave = new Leave
                {
                    LeaveId = Guid.Parse("823e1a65-227f-416a-bc2d-41ab554f6752"),
                    Reason = ReasonType.VacationLeave,
                    StartDate = GetUtcDateTime(2023, 3, 15),
                    EndDate = GetUtcDateTime(2023, 3, 18),
                    Status = StatusType.Rejected
                };
                break;
            default:
                leave = new Leave
                {
                    LeaveId = Guid.Parse("9f5cb020-df86-4658-9250-d29c1aaf96f0"),
                    Reason = ReasonType.SickLeave,
                    StartDate = GetUtcDateTime(2023, 2, 10),
                    EndDate = GetUtcDateTime(2023, 2, 20),
                    Status = StatusType.Pending
                };
                break;
        }

        return Task.FromResult(leave);
    }

    [Function("Function_HttpStart")]
    public async Task<HttpResponseData> HttpStart(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req,
        [DurableClient] DurableTaskClient client,
        FunctionContext executionContext)
    {
        ILogger logger = executionContext.GetLogger("Function_HttpStart");

        // Function input comes from the request content.
        string instanceId = await client.ScheduleNewOrchestrationInstanceAsync(nameof(FanOutFanInFunction));

        logger.LogInformation("Started orchestration with ID = '{instanceId}'.", instanceId);

        // Returns an HTTP 202 response with an instance management payload.
        // See https://learn.microsoft.com/azure/azure-functions/durable/durable-functions-http-api#start-orchestration
        return await client.CreateCheckStatusResponseAsync(req, instanceId);
    }

    private static DateTime GetUtcDateTime(int year, int month, int day)
    {
        return new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Utc);
    }
}