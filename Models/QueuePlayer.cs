using System;
using NFive.SDK.Core.Models.Player;
using NFive.SessionManager;
using NFive.SessionManager.Models;

namespace NFive.Queue.Models
{
	public class QueuePlayer
	{
		private byte dots = 0;

		public Client Client { get; set; }
		public Session Session { get; set; }
		public Deferrals Deferrals { get; set; }
		public QueueStatus Status { get; set; } = QueueStatus.Queued;
		public DateTime JoinTime { get; set; } = DateTime.UtcNow;
		public ushort JoinCount { get; set; } = 0;
		public ushort Priority { get; set; } = 100;

		public QueuePlayer(Client client, Session session, Deferrals deferrals)
		{
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
	}
}
