using JetBrains.Annotations;
using NFive.Queue.Server.Storage;
using NFive.SDK.Server.Migrations;

namespace NFive.Queue.Server.Migrations
{
	[UsedImplicitly]
	public sealed class Configuration : MigrationConfiguration<StorageContext> { }
}
