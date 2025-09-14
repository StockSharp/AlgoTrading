using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Alerts when an order stays active longer than the specified time.
/// </summary>
public class OrderTimeAlertStrategy : Strategy
{
	private readonly StrategyParam<int> _alertDelaySeconds;
	private readonly StrategyParam<int> _timerFrequencySeconds;
	private readonly StrategyParam<bool> _useLogging;
	private readonly StrategyParam<string> _soundName;

	private readonly HashSet<long> _alertedOrders = new();

	/// <summary>
	/// Seconds before the alert is triggered for an active order.
	/// </summary>
	public int AlertDelaySeconds
	{
		get => _alertDelaySeconds.Value;
		set => _alertDelaySeconds.Value = value;
	}

	/// <summary>
	/// Timer frequency in seconds.
	/// </summary>
	public int TimerFrequencySeconds
	{
		get => _timerFrequencySeconds.Value;
		set => _timerFrequencySeconds.Value = value;
	}

	/// <summary>
	/// Enable logging of alert messages.
	/// </summary>
	public bool UseLogging
	{
		get => _useLogging.Value;
		set => _useLogging.Value = value;
	}

	/// <summary>
	/// Sound file name for the alert (not used).
	/// </summary>
	public string SoundName
	{
		get => _soundName.Value;
		set => _soundName.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public OrderTimeAlertStrategy()
	{
		_alertDelaySeconds = Param(nameof(AlertDelaySeconds), 10)
			.SetGreaterThanZero()
			.SetDisplay("Alert Delay", "Seconds before an alert is triggered", "General")
			.SetCanOptimize(true)
			.SetOptimize(5, 60, 5);

		_timerFrequencySeconds = Param(nameof(TimerFrequencySeconds), 1)
			.SetGreaterThanZero()
			.SetDisplay("Timer Frequency", "Timer check frequency in seconds", "General");

		_useLogging = Param(nameof(UseLogging), true)
			.SetDisplay("Use Logging", "Log alert messages", "General");

		_soundName = Param(nameof(SoundName), "Alert2.wav")
			.SetDisplay("Sound Name", "Sound file for alert (not used)", "General");
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Start periodic timer to monitor active orders.
		TimerInterval = TimeSpan.FromSeconds(TimerFrequencySeconds);
	}

	/// <inheritdoc />
	protected override void OnTimer()
	{
		base.OnTimer();

		var now = CurrentTime;

		foreach (var order in Orders)
		{
			if (order.State != OrderStates.Active)
				continue;

			var elapsed = now - order.Time;

			if (elapsed.TotalSeconds < AlertDelaySeconds)
				continue;

			if (!_alertedOrders.Add(order.Id))
				continue;

			if (UseLogging)
				LogInfo($"Order #{order.Id} has been active for {elapsed.TotalSeconds:F0} seconds.");
		}
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_alertedOrders.Clear();
	}
}
