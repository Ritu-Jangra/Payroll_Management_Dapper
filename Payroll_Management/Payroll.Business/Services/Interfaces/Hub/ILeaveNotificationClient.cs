namespace Payroll.Business.Services
{
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.SignalR;
    using Payroll.Domain;

    public interface ILeaveNotificationClient
    {
        Task ReceiveLeaveNotification(Leave leave);
    }

    [Authorize]
    public sealed class LeaveNotificationClient : Hub<ILeaveNotificationClient>
    {
        public override Task OnConnectedAsync()
        {
            Console.WriteLine($"CONNECTED: {Context.UserIdentifier} | {Context.ConnectionId}");
            return base.OnConnectedAsync();
        }
    }
}
