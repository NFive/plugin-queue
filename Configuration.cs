using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JetBrains.Annotations;
using NFive.Queue.Models;
using NFive.SDK.Core.Controllers;

namespace NFive.Queue
{
	[PublicAPI]
	public class Configuration : IControllerConfiguration
	{
		public List<PriorityPlayer> PriorityPlayers { get; set; } = new List<PriorityPlayer>();
		public uint DisconnectGrace { get; set; } = 60000;
		public uint ConnectionTimeout { get; set; } = 60000;
		public uint DeferralDelay { get; set; } = 500;
		public int MaxClients { get; set; }
		public bool QueueWhenNotFull { get; set; } = false;
		public string ServerName { get; set; }
	}
}
