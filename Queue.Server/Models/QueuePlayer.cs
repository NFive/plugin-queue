using JetBrains.Annotations;
using NFive.SDK.Core.Helpers;
using NFive.SDK.Core.Models;
using NFive.SDK.Core.Models.Player;
using NFive.SDK.Server;
using NFive.SDK.Server.Rpc;

namespace NFive.Queue.Server.Models
{
	public class QueuePlayer : IdentityModel
	{
		private byte dots;

		public IClient Client { get; set; }

		public Session Session { get; set; }

		public Deferrals Deferrals { get; set; }

		public QueueStatus Status { get; set; } = QueueStatus.Queued;

		public int Priority { get; set; } = 100; // TODO: Config

		[UsedImplicitly]
		public QueuePlayer()
		{
			this.Id = GuidGenerator.GenerateTimeBasedGuid();
		}

		public QueuePlayer(QueuePlayerDto queuePlayerDto)
		{
			this.Id = queuePlayerDto.Id;
			this.Session = queuePlayerDto.Session;
			this.Priority = queuePlayerDto.Priority;
		}

		public QueuePlayer(IClient client, Session session, Deferrals deferrals)
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

		public void Defer() => this.Deferrals?.Defer();

		public void Allow() => this.Deferrals?.Done();
	}
}
