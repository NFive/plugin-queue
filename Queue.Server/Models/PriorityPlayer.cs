namespace NFive.Queue.Server.Models
{
	public class PriorityPlayer
	{
		/// <summary>
		/// Gets or sets the steam identifier.
		/// </summary>
		/// <value>
		/// The steam identifier.
		/// </value>
		public long SteamId { get; set; }

		/// <summary>
		/// Gets or sets the priority.
		/// </summary>
		/// <value>
		/// Priority a player has over others in the queue (higher == more priority);
		/// </value>
		public int Priority { get; set; }
	}
}
