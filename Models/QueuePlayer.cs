using System;
using NFive.SDK.Core.Models.Player;
using NFive.SessionManager;
using NFive.SessionManager.Models;
using NFive.SDK.Core.Helpers;
using NFive.SDK.Core.Models;

namespace NFive.Queue.Models
{
	public class QueuePlayer : IdentityModel
	{
		private byte dots = 0;

		public Client Client { get; set; }
		public Session Session { get; set; }
		public Deferrals Deferrals { get; set; }
		public QueueStatus Status { get; set; } = QueueStatus.Queued;
		public DateTime JoinTime { get; set; } = DateTime.UtcNow;
		public short JoinCount { get; set; } = 0;
		public short Priority { get; set; } = 100;

		public QueuePlayer()
		{
			this.Id = GuidGenerator.GenerateTimeBasedGuid();
		}

		public QueuePlayer(Client client, Session session, Deferrals deferrals)
		{
			this.Id = GuidGenerator.GenerateTimeBasedGuid();
			this.Client = client;
			this.Session = session;
			this.Deferrals = deferrals;
		}

		public void Update()
		{
			this.dots++;
			if (this.dots > 5) this.dots = 0;
			this.Deferrals.Update("Connecting" + new string('.', this.dots));
		}

		public void Defer() => this.Deferrals.Defer();

		public void Allow() => this.Deferrals.Done();
	}
}
