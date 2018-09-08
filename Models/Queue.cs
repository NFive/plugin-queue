using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NFive.Queue.Models
{
	public class Queue
	{
		public List<QueuePlayer> Players = new List<QueuePlayer>();
		public Dictionary<QueuePlayer, Tuple<Task, CancellationTokenSource>> Threads = new Dictionary<QueuePlayer, Tuple<Task, CancellationTokenSource>>();
	}
}
