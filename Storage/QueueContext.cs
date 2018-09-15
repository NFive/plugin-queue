using System.Data.Entity;
using NFive.Queue.Models;
using NFive.SDK.Server.Storage;

namespace NFive.Queue.Storage
{
	public class QueueContext : EFContext<QueueContext>
	{
		public DbSet<QueuePlayerDto> QueuePlayers { get; set; }
	}
}
