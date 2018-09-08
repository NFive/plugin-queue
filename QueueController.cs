using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CitizenFX.Core;
using JetBrains.Annotations;
using NFive.Queue.Models;
using NFive.SDK.Core.Diagnostics;
using NFive.SDK.Core.Models.Player;
using NFive.SDK.Server.Controllers;
using NFive.SDK.Server.Events;
using NFive.SDK.Server.Rpc;
using NFive.SessionManager;
using NFive.SessionManager.Models;

namespace NFive.Queue
{
	[PublicAPI]
	public class QueueController : ConfigurableController<Configuration>
	{
		private Models.Queue queue = new Models.Queue();
		private readonly CancellationTokenSource cancellationToken = new CancellationTokenSource();
		private Task thread;

		public QueueController(ILogger logger, IEventManager events, IRpcHandler rpc, Configuration configuration) : base(logger, events, rpc, configuration)
		{
			this.Events.On<Client, Session, Deferrals>("sessionCreated", OnSessionCreated);
			this.Events.On<Client, Session>("clientDisconnected", OnClientDisconnected);
			this.Events.On<Client, Session, Session>("clientReconnected", OnClientReconnected);

			this.thread = Task.Factory.StartNew(ProcessQueue);
		}

		public async void OnSessionCreated(Client client, Session session, Deferrals deferrals)
		{
			var queuePlayer = this.queue.Players.SingleOrDefault(p => p.Session.UserId == session.UserId);
			var cancellationToken = new CancellationTokenSource();
			if (queuePlayer == null)
			{
				queuePlayer = new QueuePlayer(client, session, deferrals);
				this.queue.Players.Add(queuePlayer);
			}
			else
			{
				this.Logger.Debug($"Found existing queuePlayer for {client.Name}");
				queuePlayer.Client = client;
				queuePlayer.Session = session;
				queuePlayer.Deferrals = deferrals;
				var oldThread = this.queue.Threads.SingleOrDefault(t => t.Key.Session.UserId == session.UserId).Key;
				if (oldThread != null)
				{
					this.queue.Threads[oldThread].Item2.Cancel();
					this.queue.Threads[oldThread].Item1.Wait();
					this.queue.Threads[oldThread].Item2.Dispose();
					this.queue.Threads[oldThread].Item1.Dispose();
					this.queue.Threads.Remove(oldThread);
				}
				queuePlayer.Status = QueueStatus.Queued;
			}

			this.queue.Threads.Add(queuePlayer, new Tuple<Task, CancellationTokenSource>(Task.Factory.StartNew(() => MonitorPlayer(queuePlayer), cancellationToken.Token), cancellationToken));
		}

		public async void OnClientReconnected(Client client, Session session, Session oldSession)
		{
			this.Logger.Debug($"OnClientReconnected() {client.Name}");
		}

		public async void OnClientDisconnected(Client client, Session session)
		{
			this.Logger.Debug($"OnClientDisconnected() {client.Name}");
			this.Logger.Debug($"Session ID {session.Id}");
			this.Logger.Debug($"Queue Session IDs {string.Join(", ", this.queue.Players.Select(p => p.Session.Id))}");
			var queuePlayer = this.queue.Players.SingleOrDefault(p => p.Session.Id == session.Id);
			if (queuePlayer == null) return;
			this.Logger.Debug($"Setting queuePlayer to Disconnected: {client.Name}");
			queuePlayer.Status = QueueStatus.Disconnected;
		}

		public async Task ProcessQueue()
		{
			while (!this.cancellationToken.Token.IsCancellationRequested)
			{
				await BaseScript.Delay(100);
			}
		}

		public async Task MonitorPlayer(QueuePlayer queuePlayer)
		{
			queuePlayer.Defer();
			while (queuePlayer.Status == QueueStatus.Queued)
			{
				await BaseScript.Delay((int)this.Configuration.DeferralDelay);
				queuePlayer.Update();
			}
			while (queuePlayer.Status == QueueStatus.Disconnected)
			{
				await BaseScript.Delay((int)this.Configuration.DeferralDelay);
				if (DateTime.UtcNow.Subtract(queuePlayer.Session.Disconnected ?? DateTime.UtcNow).TotalMilliseconds < this.Configuration.DisconnectGrace) continue;
				this.queue.Players.Remove(queuePlayer);
				break;
			}
			this.queue.Threads.Remove(queuePlayer);
		}
	}
}
