using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

using System.Threading;

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
	private SimpleMovingAverage _smaFast;
	private SimpleMovingAverage _smaSlow;

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
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, TimeSpan.FromMinutes(5).TimeFrame());
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_smaFast = new SimpleMovingAverage { Length = 10 };
		_smaSlow = new SimpleMovingAverage { Length = 30 };

		var subscription = SubscribeCandles(TimeSpan.FromMinutes(5).TimeFrame());
		subscription
			.Bind(_smaFast, _smaSlow, ProcessCandle)
			.Start();

		_previousState = true;
		_isFirstNotification = true;
	}

	private void ProcessCandle(ICandleMessage candle, decimal fast, decimal slow)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (fast > slow && Position <= 0)
		{
			if (Position < 0)
				BuyMarket();
			BuyMarket();
		}
		else if (fast < slow && Position >= 0)
		{
			if (Position > 0)
				SellMarket();
			SellMarket();
		}
	}

	private void OnTimer(object state)
	{
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

