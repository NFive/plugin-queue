using System.Collections.Generic;
using JetBrains.Annotations;
using NFive.Queue.Server.Models;
using NFive.SDK.Core.Controllers;

namespace NFive.Queue.Server
{
	[PublicAPI]
	public class Configuration : ControllerConfiguration
	{
		/// <summary>
		/// Gets or sets the priority players.
		/// </summary>
		/// <value>
		/// The priority players.
		/// </value>
		public List<PriorityPlayer> PriorityPlayers { get; set; } = new List<PriorityPlayer>();

		/// <summary>
		/// Gets or sets the disconnect grace.
		/// </summary>
		/// <value>
		/// Number of ms a user has to reconnect after disconnected before loosing queue position.
		/// </value>
		public uint DisconnectGrace { get; set; } = 60000;

		/// <summary>
		/// Gets or sets the pre restart disconnect grace.
		/// </summary>
		/// <value>
		/// Number of ms a user can disconnect before a restart and be give queue priority after restart.
		/// </value>
		public uint PreRestartDisconnectGrace { get; set; } = 150000;

		/// <summary>
		/// Gets or sets the pre restart queue disconnect grace.
		/// </summary>
		/// <value>
		/// Number of ms a user can disconnect before a restart and be give their queue position back after restart.
		/// </value>
		public uint PreRestartQueueDisconnectGrace { get; set; } = 150000;

		/// <summary>
		/// Gets or sets the restart reconnect grace.
		/// </summary>
		/// <value>
		/// Number of ms a connected player has queue priority after server restart.
		/// </value>
		public uint RestartReconnectGrace { get; set; } = 300000;

		/// <summary>
		/// Gets or sets the restart reconnect priority.
		/// </summary>
		/// <value>
		/// How much priority that will be added to connected players joining within the RestartReconnectGrace period.
		/// </value>
		public uint RestartReconnectPriority { get; set; } = 1000;

		/// <summary>
		/// Gets or sets the restart requeue grace.
		/// </summary>
		/// <value>
		/// Number of ms a queued player keeps their queue position after restart. 
		/// </value>
		public uint RestartRequeueGrace { get; set; } = 300000;

		/// <summary>
		/// Gets or sets the deferral delay.
		/// </summary>
		/// <value>
		/// How often in ms the deferral updates will be sent to the client.
		/// </value>
		public uint DeferralDelay { get; set; } = 500;

		/// <summary>
		/// Gets or sets the connection timeout.
		/// </summary>
		/// <value>
		/// Number of ms a player can be connecting before being force disconnected.
		/// </value>
		public uint ConnectionTimeout { get; set; } = 120000;
	}
}
