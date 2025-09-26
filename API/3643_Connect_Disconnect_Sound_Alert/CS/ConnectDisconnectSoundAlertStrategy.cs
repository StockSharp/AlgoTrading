using System;
using System.Threading;

using StockSharp.Algo.Strategies;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Monitors the connector state and raises alerts whenever the connection is lost or restored.
/// </summary>
public class ConnectDisconnectSoundAlertStrategy : Strategy
{
	private readonly StrategyParam<int> _checkIntervalSeconds;
	private readonly StrategyParam<bool> _logDurations;

	private Timer _timer;
	private bool? _previousState;
	private bool _isFirstNotification;
	private DateTimeOffset? _lastConnectionMoment;
	private DateTimeOffset? _lastDisconnectionMoment;

	/// <summary>
	/// Interval in seconds between connection status checks.
	/// </summary>
	public int CheckIntervalSeconds
	{
		get => _checkIntervalSeconds.Value;
		set => _checkIntervalSeconds.Value = value;
	}

	/// <summary>
	/// Enables logging of connection and disconnection durations.
	/// </summary>
	public bool LogDurations
	{
		get => _logDurations.Value;
		set => _logDurations.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="ConnectDisconnectSoundAlertStrategy"/> class.
	/// </summary>
	public ConnectDisconnectSoundAlertStrategy()
	{
		_checkIntervalSeconds = Param(nameof(CheckIntervalSeconds), 1)
			.SetGreaterThanZero()
			.SetDisplay("Check interval", "Polling interval for connector state in seconds", "General");

		_logDurations = Param(nameof(LogDurations), true)
			.SetDisplay("Log durations", "Log connection and disconnection durations", "General");
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		StopTimer();

		// Reset internal tracking variables to initial state.
		_previousState = null;
		_isFirstNotification = true;
		_lastConnectionMoment = null;
		_lastDisconnectionMoment = null;
	}

	/// <inheritdoc />
	protected override void OnStopped()
	{
		StopTimer();
		base.OnStopped();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		if (Connector == null)
			throw new InvalidOperationException("Connector is not assigned.");

		var intervalSeconds = CheckIntervalSeconds;

		if (intervalSeconds <= 0)
			throw new InvalidOperationException("Check interval must be greater than zero.");

		// Remember current connection state to detect future changes.
		_previousState = Connector.IsConnected;
		_isFirstNotification = true;

		if (_previousState == true)
		{
			_lastConnectionMoment = time;
			LogInfo($"Initial connector state: connected at {time:O}.");
		}
		else
		{
			_lastDisconnectionMoment = time;
			LogInfo($"Initial connector state: disconnected at {time:O}.");
		}

		var interval = TimeSpan.FromSeconds(intervalSeconds);

		// Start periodic polling of connector status.
		_timer = new Timer(OnTimer, null, interval, interval);
	}

	private void OnTimer(object state)
	{
		var connector = Connector;

		if (connector == null)
			return;

		var isConnected = connector.IsConnected;
		var previous = _previousState;

		// No previous state captured means the strategy was reset in the meantime.
		if (previous == null)
		{
			_previousState = isConnected;
			return;
		}

		if (previous.Value == isConnected)
			return;

		_previousState = isConnected;
		var now = CurrentTime;

		if (isConnected)
		{
			HandleConnectionRestored(now);
		}
		else
		{
			HandleConnectionLost(now);
		}

		_isFirstNotification = false;
	}

	private void HandleConnectionRestored(DateTimeOffset now)
	{
		_lastConnectionMoment = now;

		// Log the event and include duration information when requested.
		if (_isFirstNotification)
		{
			LogInfo($"Connection detected at {now:O} (initial notification).");
		}
		else
		{
			LogInfo($"Connection restored at {now:O}.");
		}

		if (LogDurations && _lastDisconnectionMoment != null)
		{
			var offlineDuration = now - _lastDisconnectionMoment.Value;
			LogInfo($"Connection was offline for {offlineDuration:hh\\:mm\\:ss}.");
		}

		_lastDisconnectionMoment = null;
	}

	private void HandleConnectionLost(DateTimeOffset now)
	{
		_lastDisconnectionMoment = now;

		// Log the event and include duration information when requested.
		if (_isFirstNotification)
		{
			LogInfo($"Connection lost at {now:O} (initial notification).");
		}
		else
		{
			LogInfo($"Connection lost at {now:O}.");
		}

		if (LogDurations && _lastConnectionMoment != null)
		{
			var onlineDuration = now - _lastConnectionMoment.Value;
			LogInfo($"Connection was online for {onlineDuration:hh\\:mm\\:ss}.");
		}
	}

	private void StopTimer()
	{
		// Dispose timer safely to stop polling when the strategy stops.
		_timer?.Dispose();
		_timer = null;
	}
}
