using JetBrains.Annotations;
using NFive.SDK.Server.Migrations;
using NFive.Queue.Server.Storage;

namespace NFive.Queue.Server.Migrations
{
	[UsedImplicitly]
	public sealed class Configuration : MigrationConfiguration<StorageContext> { }
}
