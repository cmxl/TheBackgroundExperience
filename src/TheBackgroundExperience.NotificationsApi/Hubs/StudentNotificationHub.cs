using Microsoft.AspNetCore.SignalR;

namespace TheBackgroundExperience.NotificationsApi.Hubs;

public class StudentNotificationHub : Hub
{
	private readonly ILogger<StudentNotificationHub> _logger;

	public StudentNotificationHub(ILogger<StudentNotificationHub> logger)
	{
		_logger = logger;
	}

	public override async Task OnConnectedAsync()
	{
		var clientId = Context.ConnectionId;
		_logger.LogInformation("SignalR client {ClientId} connected to NotificationsApi", clientId);
		
		// Join all students group
		await Groups.AddToGroupAsync(Context.ConnectionId, "Students");
		
		// Send welcome message
		await Clients.Caller.SendAsync("Connected", new { clientId, timestamp = DateTime.UtcNow });
		
		await base.OnConnectedAsync();
	}

	public override async Task OnDisconnectedAsync(Exception? exception)
	{
		var clientId = Context.ConnectionId;
		_logger.LogInformation("SignalR client {ClientId} disconnected from NotificationsApi", clientId);
		
		// Remove from all groups
		await Groups.RemoveFromGroupAsync(Context.ConnectionId, "Students");
		
		await base.OnDisconnectedAsync(exception);
	}

	// Allow clients to join specific groups if needed
	public async Task JoinGroup(string groupName)
	{
		await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
		_logger.LogDebug("Client {ClientId} joined group {GroupName}", Context.ConnectionId, groupName);
	}

	// Allow clients to leave specific groups
	public async Task LeaveGroup(string groupName)
	{
		await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
		_logger.LogDebug("Client {ClientId} left group {GroupName}", Context.ConnectionId, groupName);
	}
}