namespace NFive.Queue.Server.Models
{
	public enum QueueStatus
	{
		Queued = 0,
		Disconnected = 1,
		Connecting = 2,
		RestartConnected = 3,
		RestartQueued = 4
	}
}
