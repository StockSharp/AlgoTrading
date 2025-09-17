using System;
using System.Globalization;
using System.Threading;

using StockSharp.Algo;
using StockSharp.Algo.Strategies;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Monitors the connector connection state and logs online/offline transitions.
/// </summary>
public class IsConnectedStrategy : Strategy
{
	private readonly StrategyParam<int> _checkIntervalSeconds;

	private readonly object _sync = new();

	private Timer _checkTimer;
	private bool _previousState;
	private DateTimeOffset? _lastOnlineStart;
	private DateTimeOffset? _lastOfflineStart;

	/// <summary>
	/// Initializes a new instance of <see cref="IsConnectedStrategy"/>.
	/// </summary>
	public IsConnectedStrategy()
	{
		_checkIntervalSeconds = Param(nameof(CheckIntervalSeconds), 1)
		.SetGreaterThanZero()
		.SetDisplay("Check interval (sec)", "Polling period for connection status", "Monitoring");
	}

	/// <summary>
	/// Interval in seconds between successive connection checks.
	/// </summary>
	public int CheckIntervalSeconds
	{
		get => _checkIntervalSeconds.Value;
		set => _checkIntervalSeconds.Value = value;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Ensure the connector is available before accessing its state.
		if (Connector == null)
		throw new InvalidOperationException("Connector is not assigned.");

		LogInfo("Connection monitor initialized.");

		lock (_sync)
		{
			_previousState = Connector.IsConnected;
			_lastOnlineStart = _previousState ? time : null;
			_lastOfflineStart = _previousState ? null : time;

			StartTimer();
		}

		// Report the initial state immediately after startup.
		LogCurrentState(_previousState, time, null);
	}

	/// <inheritdoc />
	protected override void OnStopped()
	{
		base.OnStopped();
		// Avoid leaving an active timer when the strategy stops.
		StopTimer();

		var now = GetCurrentTime();
		var stateText = _previousState ? "online" : "offline";
		LogInfo($"Strategy stopped while {stateText} at {FormatTime(now)}.");
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		StopTimer();

		lock (_sync)
		{
			_previousState = false;
			_lastOnlineStart = null;
			_lastOfflineStart = null;
		}
	}

	private void StartTimer()
	{
		// Ensure any previous timer is cancelled before scheduling a new one.
		StopTimer();

		var interval = TimeSpan.FromSeconds(CheckIntervalSeconds);
		_checkTimer = new Timer(CheckConnectionState, null, interval, interval);
	}

	private void StopTimer()
	{
		lock (_sync)
		{
			_checkTimer?.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
			_checkTimer?.Dispose();
			_checkTimer = null;
		}
	}

	private void CheckConnectionState(object? state)
	{
		DateTimeOffset now;
		bool? newState = null;
		TimeSpan? previousDuration = null;

		lock (_sync)
		{
			// Skip processing if the connector reference disappeared.
			if (Connector == null)
			return;

			now = GetCurrentTime();
			var isConnected = Connector.IsConnected;

			if (isConnected == _previousState)
			return;

			// Detect transitions to the online state and measure downtime.
			if (isConnected)
			{
				if (_lastOfflineStart != null)
				previousDuration = now - _lastOfflineStart.Value;

				_lastOnlineStart = now;
				_lastOfflineStart = null;
			}
			// Detect transitions to the offline state and measure uptime.
			else
			{
				if (_lastOnlineStart != null)
				previousDuration = now - _lastOnlineStart.Value;

				_lastOfflineStart = now;
				_lastOnlineStart = null;
			}

			_previousState = isConnected;
			newState = isConnected;
		}

		if (newState.HasValue)
		LogCurrentState(newState.Value, now, previousDuration);
	}

	private void LogCurrentState(bool isConnected, DateTimeOffset time, TimeSpan? previousDuration)
	{
		var stateText = isConnected ? "Online" : "Offline";

		if (previousDuration != null)
		{
			var duration = FormatDuration(previousDuration.Value);
			var previousStateText = isConnected ? "offline" : "online";
			LogInfo($"{stateText} at {FormatTime(time)} after being {previousStateText} for {duration}.");
		}
		else
		{
			LogInfo($"{stateText} at {FormatTime(time)}.");
		}
	}

	private static string FormatDuration(TimeSpan duration)
	{
		return duration.ToString("c", CultureInfo.InvariantCulture);
	}

	private static string FormatTime(DateTimeOffset time)
	{
		return time.ToString("O", CultureInfo.InvariantCulture);
	}

	private DateTimeOffset GetCurrentTime()
	{
		var time = CurrentTime;
		return time != default ? time : DateTimeOffset.Now;
	}
}
