using Microsoft.AspNetCore.SignalR;

namespace OrganizationAPI.Hubs
{
    public class OrganizationHub : Hub
    {
        public async Task SendMessage(string user, string message)
        {
            await Clients.All.SendAsync("ReceiveMessageFromUser", user, message).ConfigureAwait(false);
        }
    }
}
