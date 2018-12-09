using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NFive.Queue.Server.Models
{
	public class Queue
	{
		public List<QueuePlayer> Players { get; set; } = new List<QueuePlayer>();
		public Dictionary<QueuePlayer, Tuple<Task, CancellationTokenSource>> Threads { get; set; } = new Dictionary<QueuePlayer, Tuple<Task, CancellationTokenSource>>();
	}
}
