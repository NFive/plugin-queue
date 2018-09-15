using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JetBrains.Annotations;
using NFive.Queue.Storage;
using NFive.SDK.Server.Migrations;

namespace NFive.Queue.Migrations
{
	[UsedImplicitly]
	public sealed class Configuration : MigrationConfiguration<QueueContext> { }
}
