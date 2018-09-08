using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NFive.Queue.Models
{
	public class PriorityPlayer : IPlayer
	{
		public int Priority { get; set; }
		public string SteamId { get; set; }
		public string Name { get; set; }
	}
}
