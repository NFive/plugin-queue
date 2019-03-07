using NFive.Queue.Server.Models;
using NFive.SDK.Server.Storage;
using System.Data.Entity;

namespace NFive.Queue.Server.Storage
{
	public class StorageContext : EFContext<StorageContext>
	{
		public DbSet<QueuePlayerDto> QueuePlayers { get; set; }
	}
}
