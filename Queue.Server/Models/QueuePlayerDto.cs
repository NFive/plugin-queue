using JetBrains.Annotations;
using Newtonsoft.Json;
using NFive.SDK.Core.Models;
using NFive.SDK.Core.Models.Player;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NFive.Queue.Server.Models
{
	[Table("QueuePlayers")]
	public class QueuePlayerDto : IdentityModel
	{
		[Required]
		public int Priority { get; set; } = 100;

		[Required]
		public short Position { get; set; }

		[Required]
		[ForeignKey("Session")]
		public Guid SessionId { get; set; }

		[JsonIgnore]
		public virtual Session Session { get; set; }

		[UsedImplicitly]
		public QueuePlayerDto() { }

		public QueuePlayerDto(QueuePlayer queuePlayer, short position)
		{
			this.Id = queuePlayer.Id;
			this.Priority = queuePlayer.Priority;
			this.SessionId = queuePlayer.Session.Id;
			this.Position = position;
		}
	}
}
