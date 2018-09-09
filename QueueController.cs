using System;
using System.Collections.Generic;
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
		private ushort maxPlayers;
		private readonly CancellationTokenSource cancellationToken = new CancellationTokenSource();
		private Task thread;

		public QueueController(ILogger logger, IEventManager events, IRpcHandler rpc, Configuration configuration) : base(logger, events, rpc, configuration)
		{
			this.Events.On<Client, Session, Deferrals>("sessionCreated", OnSessionCreated);
			this.Events.On<Client, Session>("clientDisconnected", OnClientDisconnected);
			this.Events.On<Client, Session, Session>("clientReconnecting", OnClientReconnecting);
			this.Events.On<Client, Session>("clientInitialized", OnClientInitialized);

			this.maxPlayers = this.Events.Request<ushort>("maxPlayers");

			this.thread = Task.Factory.StartNew(ProcessQueue);
		}

		public void OnSessionCreated(Client client, Session session, Deferrals deferrals)
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

		public void OnClientInitialized(Client client, Session session)
		{
			this.Logger.Debug($"OnClientInitialized() {client.Name}");
			this.queue.Players.Remove(this.queue.Players.SingleOrDefault(p => p.Session.UserId == session.UserId));
		}

		public void OnClientReconnecting(Client client, Session session, Session oldSession)
		{
			this.Logger.Debug($"OnClientReconnecting() {client.Name}");
			if (oldSession.Connected == null) return;
			this.Logger.Debug($"Letting reconnected player back in {client.Name}");
			var queuePlayer = this.queue.Players.Single(p => p.Session.UserId == session.UserId);
			this.queue.Players.Remove(queuePlayer);
			this.queue.Players.Insert(0, queuePlayer);
			queuePlayer.Allow();
		}

		public void OnClientDisconnected(Client client, Session session)
		{
			var queuePlayer = this.queue.Players.SingleOrDefault(p => p.Session.Id == session.Id);
			if (queuePlayer == null) return;
			queuePlayer.Status = QueueStatus.Disconnected;
		}

		public async Task ProcessQueue()
		{
			while (!this.cancellationToken.Token.IsCancellationRequested)
			{
				var currentSessions = this.Events.Request<List<Session>>("currentSessions");

				this.Logger.Debug($"Sessions: {currentSessions.Count} | Connected: {currentSessions.Count(s => s.Connected != null)} | In Queue: {this.queue.Players.Count}");

				if (currentSessions.Count(s => s.Connected != null) < this.maxPlayers && this.queue.Players.Count > 0)
				{
					this.Logger.Debug($"Letting in player: {this.queue.Players.First().Client.Name}");
					// There is a slot available, let someone in.
					this.queue.Players.First().Allow();
				}
				
				await BaseScript.Delay(1000);
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
