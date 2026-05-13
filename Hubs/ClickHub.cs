using Microsoft.AspNetCore.SignalR;

namespace ShortTree.Hubs
{
    public sealed class ClickHub : Hub
    {
        public Task JoinUser(string username)
        {
            return Groups.AddToGroupAsync(Context.ConnectionId, GroupNames.User(username));
        }

        public Task LeaveUser(string username)
        {
            return Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupNames.User(username));
        }

        public Task JoinLink(string username, string slug)
        {
            return Groups.AddToGroupAsync(Context.ConnectionId, GroupNames.Link(username, slug));
        }

        public Task LeaveLink(string username, string slug)
        {
            return Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupNames.Link(username, slug));
        }
    }

    public static class GroupNames
    {
        public static string User(string username) => $"user:{username}";
        public static string Link(string username, string slug) => $"link:{username}:{slug}";
    }

    public sealed record ClickEvent(
        int LinkId,
        string Username,
        string Slug,
        int TotalClicks,
        DateTime Timestamp
    );
}
