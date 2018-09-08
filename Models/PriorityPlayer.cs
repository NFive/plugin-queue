namespace NFive.Queue.Models
{
	public class PriorityPlayer : IPlayer
	{
		public int Priority { get; set; }
		public string SteamId { get; set; }
		public string Name { get; set; }
	}
}
