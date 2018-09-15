using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;
using NFive.SDK.Core.Models;
using NFive.SDK.Core.Models.Player;

namespace NFive.Queue.Models
{
	[Table("QueuePlayers")]
	public class QueuePlayerDto : IdentityModel
	{
		[Required]
		public QueueStatus Status { get; set; } = QueueStatus.Queued;

		[Required]
		public DateTime JoinTime { get; set; } = DateTime.UtcNow;

		[Required]
		public short JoinCount { get; set; } = 0;

		[Required]
		public short Priority { get; set; } = 100;

		[Required]
		public short Position { get; set; }

		[Required]
		[ForeignKey("Session")]
		public Guid SessionId { get; set; }

		[JsonIgnore]
		public virtual Session Session { get; set; }

		public QueuePlayerDto() { }

		public QueuePlayerDto(QueuePlayer queuePlayer, short position)
		{
			this.Id = queuePlayer.Id;
			this.JoinCount = queuePlayer.JoinCount;
			this.JoinTime = queuePlayer.JoinTime;
			this.Priority = queuePlayer.Priority;
			this.SessionId = queuePlayer.Session.Id;
			this.Status = queuePlayer.Status;
			this.Position = position;
		}
	}
}
