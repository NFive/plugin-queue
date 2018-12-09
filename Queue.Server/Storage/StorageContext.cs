using System.Data.Entity;
using NFive.Queue.Server.Models;
using NFive.SDK.Server.Storage;

namespace NFive.Queue.Server.Storage
{
	public class StorageContext : EFContext<StorageContext>
	{
		public DbSet<QueuePlayerDto> QueuePlayers { get; set; }
	}
}
