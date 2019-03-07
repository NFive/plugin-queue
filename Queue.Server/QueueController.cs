using CitizenFX.Core;
using JetBrains.Annotations;
using NFive.Queue.Server.Models;
using NFive.Queue.Server.Storage;
using NFive.SDK.Core.Diagnostics;
using NFive.SDK.Core.Helpers;
using NFive.SDK.Core.Models.Player;
using NFive.SDK.Server;
using NFive.SDK.Server.Controllers;
using NFive.SDK.Server.Events;
using NFive.SDK.Server.Rcon;
using NFive.SDK.Server.Rpc;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Entity.Migrations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NFive.Queue.Server
{
	[PublicAPI]
	public class QueueController : ConfigurableController<Configuration>
	{
		private Models.Queue queue = new Models.Queue();
		private ushort maxPlayers;
		private ConcurrentBag<Tuple<Task, CancellationTokenSource>> threads = new ConcurrentBag<Tuple<Task, CancellationTokenSource>>();

		public QueueController(ILogger logger, IEventManager events, IRpcHandler rpc, IRconManager rcon, Configuration configuration) : base(logger, events, rpc, rcon, configuration) => Startup();

		public void Startup()
		{
			Load();

			this.Events.On<IClient, Session, Deferrals>(SessionEvents.SessionCreated, OnSessionCreated);
			this.Events.On<IClient, Session>(SessionEvents.ClientDisconnected, OnClientDisconnected);
			this.Events.On<IClient, Session, Session>(SessionEvents.ClientReconnecting, OnClientReconnecting);
			this.Events.On<IClient, Session>(SessionEvents.ClientInitialized, OnClientInitialized);

			this.maxPlayers = this.Events.Request<ushort>(SessionEvents.GetMaxPlayers);

			StartThread(ProcessQueue, new CancellationTokenSource());
			StartThread(AutosaveQueue, new CancellationTokenSource());

		}

		private void StartThread(Func<CancellationTokenSource, Task> task, CancellationTokenSource cts)
		{
			this.threads.Add(new Tuple<Task, CancellationTokenSource>(Task.Factory.StartNew(async () => await task(cts)), cts));
		}

		public void OnSessionCreated(IClient client, Session session, Deferrals deferrals)
		{
			// Check if this is a new or reconnecting queuePlayer
			var queuePlayer = this.queue.Players.SingleOrDefault(p => p.Session.UserId == session.UserId);
			if (queuePlayer == null)
			{
				this.Logger.Debug($"Creating new queuePlayer for {session.User.Name}");
				queuePlayer = new QueuePlayer(client, session, deferrals);

				// Check if the player is in the priorityPlayers list and assign the configured priority
				var priorityPlayer = this.Configuration.PriorityPlayers.FirstOrDefault(p => p.SteamId == session.User.SteamId);
				if (priorityPlayer != null) queuePlayer.Priority = priorityPlayer.Priority;

				this.queue.Players.Add(queuePlayer);
			}
			else
			{
				this.Logger.Debug($"Reassigning queuePlayer to connected player: {session.User.Name}");
				queuePlayer.Client = client;
				queuePlayer.Session = session;
				queuePlayer.Deferrals = deferrals;
				var oldThread = this.queue.Threads.SingleOrDefault(t => t.Key.Session.UserId == session.UserId).Key;
				if (oldThread != null)
				{
					this.Logger.Debug($"Disposing of old thread for player: {session.User.Name}");
					this.queue.Threads[oldThread].Item2.Cancel();
					this.queue.Threads[oldThread].Item1.Wait();
					this.queue.Threads[oldThread].Item2.Dispose();
					this.queue.Threads[oldThread].Item1.Dispose();
					this.queue.Threads.Remove(oldThread);
				}
				queuePlayer.Status = QueueStatus.Queued;
			}
			this.Logger.Debug($"Adding new thread for player: {session.User.Name}");
			var cancellationToken = new CancellationTokenSource();
			this.queue.Threads.Add(queuePlayer, new Tuple<Task, CancellationTokenSource>(Task.Factory.StartNew(() => MonitorPlayer(queuePlayer, cancellationToken.Token), cancellationToken.Token), cancellationToken));
		}

		public void OnClientInitialized(IClient client, Session session)
		{
			// TODO: Make this option configurable and use the QueueStatus.Connecting with a deadlock timeout
			this.queue.Players.Remove(this.queue.Players.SingleOrDefault(p => p.Session.UserId == session.UserId));
		}

		public void OnClientReconnecting(IClient client, Session session, Session oldSession)
		{
			if (oldSession.Connected == null) return;
			var queuePlayer = this.queue.Players.Single(p => p.Session.UserId == session.UserId);
			this.queue.Players.Remove(queuePlayer);
			this.queue.Players.Insert(0, queuePlayer);
			queuePlayer.Allow();
		}

		public void Load()
		{
			this.Logger.Debug("Load(): Loading old queue from database");
			var lastServerActiveTime = this.Events.Request<DateTime?>(BootEvents.GetLastActiveTime) ?? DateTime.UtcNow;
			this.Logger.Debug($"Load(): lastServerActiveTime: {lastServerActiveTime}");

			using (var context = new StorageContext())
			{
				// Players who were connected before the restart
				var preRestartDisconnectGrace = lastServerActiveTime - TimeSpan.FromMilliseconds(this.Configuration.PreRestartDisconnectGrace);
				var connectedPlayers = context.QueuePlayers
					.Where(c => c.Session.Connected != null && (c.Session.Disconnected == null || c.Session.Disconnected >= preRestartDisconnectGrace))
					.OrderByDescending(c => c.Created)
					.GroupBy(c => c.SessionId)
					.ToList()
					.Select(q => new QueuePlayer(q.First())
					{
						Id = GuidGenerator.GenerateTimeBasedGuid()
					})
					.OrderByDescending(c => c.Priority);

				// Players who were queued before the restart
				var preRestartQueueDisconnectGrace = lastServerActiveTime - TimeSpan.FromMilliseconds(this.Configuration.PreRestartQueueDisconnectGrace);
				var queuePlayers = context.QueuePlayers
					.Where(q => q.Session.Connected == null && (q.Session.Disconnected == null || q.Session.Disconnected >= preRestartQueueDisconnectGrace))
					.OrderByDescending(c => c.Created)
					.GroupBy(c => c.SessionId)
					.ToList()
					.Select(q => new QueuePlayer(q.First())
					{
						Id = GuidGenerator.GenerateTimeBasedGuid()
					})
					.OrderByDescending(c => c.Priority);

				foreach (var connectedPlayer in connectedPlayers)
				{
					this.Logger.Debug($"Adding previously connected player to queue with priority {connectedPlayer.Priority + this.Configuration.RestartReconnectPriority}: {connectedPlayer.Session.User.Name}");
					connectedPlayer.Priority += (int)this.Configuration.RestartReconnectPriority;
					connectedPlayer.Status = QueueStatus.RestartConnected;
					this.queue.Players.Add(connectedPlayer);
					var cancellationToken = new CancellationTokenSource();
					this.queue.Threads.Add(connectedPlayer, new Tuple<Task, CancellationTokenSource>(Task.Factory.StartNew(() => MonitorPlayer(connectedPlayer, cancellationToken.Token), cancellationToken.Token), cancellationToken));
				}

				foreach (var queuePlayer in queuePlayers)
				{
					this.Logger.Debug($"Adding previously queued player to queue: {queuePlayer.Session.User.Name}");
					queuePlayer.Status = QueueStatus.RestartQueued;
					this.queue.Players.Add(queuePlayer);
					var cancellationToken = new CancellationTokenSource();
					this.queue.Threads.Add(queuePlayer, new Tuple<Task, CancellationTokenSource>(Task.Factory.StartNew(() => MonitorPlayer(queuePlayer, cancellationToken.Token), cancellationToken.Token), cancellationToken));
				}
			}
		}

		public async Task Save()
		{
			using (var context = new StorageContext())
			using (var transaction = context.Database.BeginTransaction())
			{
				var queuePlayers = this.queue.Players.Select((player, index) => new QueuePlayerDto(player, Convert.ToInt16(index))).ToArray();
				if (queuePlayers.Length == 0) return;
				context.QueuePlayers.AddOrUpdate(queuePlayers);
				await context.SaveChangesAsync();
				transaction.Commit();
			}
		}

		public void OnClientDisconnected(IClient client, Session session)
		{
			var queuePlayer = this.queue.Players.SingleOrDefault(p => p.Session.Id == session.Id);
			if (queuePlayer == null) return;
			queuePlayer.Status = QueueStatus.Disconnected;
		}

		public async Task ProcessQueue(CancellationTokenSource cancellationTokenSource)
		{
			while (!cancellationTokenSource.Token.IsCancellationRequested)
			{
				// Order the queue by priority
				this.queue.Players = this.queue.Players.OrderByDescending(p => p.Priority).ToList();

				// Ask the session manager how many players are currently connected
				var currentSessions = this.Events.Request<List<Session>>(SessionEvents.GetCurrentSessions);
				if (currentSessions.Count(s => s.Connected != null) < this.maxPlayers && this.queue.Players.Count > 0)
				{
					// There is a slot available, let someone in.
					this.queue.Players.First().Allow();
				}

				await BaseScript.Delay(1000);
			}
		}

		public async Task AutosaveQueue(CancellationTokenSource cancellationTokenSource)
		{
			while (!cancellationTokenSource.Token.IsCancellationRequested)
			{
				await Save();
				await BaseScript.Delay(5000);
			}
		}

		public async Task MonitorPlayer(QueuePlayer queuePlayer, CancellationToken cancellationToken)
		{
			this.Logger.Debug($"Starting new thread {Thread.CurrentThread.ManagedThreadId}");
			var serverBootTime = this.Events.Request<DateTime>(BootEvents.GetTime);

			while (!cancellationToken.IsCancellationRequested && queuePlayer.Status == QueueStatus.RestartConnected || queuePlayer.Status == QueueStatus.RestartQueued)
			{
				await BaseScript.Delay((int)this.Configuration.DeferralDelay);
				if (queuePlayer.Status == QueueStatus.RestartConnected && DateTime.UtcNow.Subtract(serverBootTime).TotalMilliseconds < this.Configuration.RestartReconnectGrace) continue;
				if (queuePlayer.Status == QueueStatus.RestartQueued && DateTime.UtcNow.Subtract(serverBootTime).TotalMilliseconds < this.Configuration.RestartRequeueGrace) continue;
				if (cancellationToken.IsCancellationRequested) return;
				this.Logger.Debug($"Thread({Thread.CurrentThread.ManagedThreadId}): Removing player due to expired restart grace period: {queuePlayer.Session.User.Name}, Status: {queuePlayer.Status}");
				queuePlayer.Status = QueueStatus.Disconnected;
				using (var context = new StorageContext())
				{
					context.QueuePlayers.AddOrUpdate(new QueuePlayerDto(queuePlayer, Convert.ToInt16(this.queue.Players.IndexOf(queuePlayer))));
					await context.SaveChangesAsync(cancellationToken);
				}
				this.queue.Players.Remove(queuePlayer);
				this.queue.Threads.Remove(queuePlayer);
				return;
			}

			if (!cancellationToken.IsCancellationRequested) queuePlayer.Defer();
			while (!cancellationToken.IsCancellationRequested && queuePlayer.Status == QueueStatus.Queued)
			{
				await BaseScript.Delay((int)this.Configuration.DeferralDelay);
				queuePlayer.Update();
			}
			while (!cancellationToken.IsCancellationRequested && queuePlayer.Status == QueueStatus.Disconnected)
			{
				await BaseScript.Delay((int)this.Configuration.DeferralDelay);
				if (DateTime.UtcNow.Subtract(queuePlayer.Session.Disconnected ?? DateTime.UtcNow).TotalMilliseconds < this.Configuration.DisconnectGrace) continue;
				this.Logger.Debug($"Thread({Thread.CurrentThread.ManagedThreadId}): Removing player due to expired disconnected grace period: {queuePlayer.Session.User.Name}");
				this.queue.Players.Remove(queuePlayer);
				break;
			}
			if (!cancellationToken.IsCancellationRequested) this.queue.Threads.Remove(queuePlayer);
			this.Logger.Debug($"Ending thread {Thread.CurrentThread.ManagedThreadId}");
		}
	}
}
