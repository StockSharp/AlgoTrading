namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Scheduler that mimics the MetaTrader utility "Auto Trading Publish" by toggling auto trading at specific hours.
/// </summary>
public class AutoTradingPublishStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _startHour;
	private readonly StrategyParam<int> _stopHour;

	private DateTimeOffset? _lastEnableTime;
	private DateTimeOffset? _lastDisableTime;

	/// <summary>
	/// Initializes strategy parameters.
	/// </summary>
	public AutoTradingPublishStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe used to monitor the trading clock.", "General");

		_startHour = Param(nameof(StartHour), 1)
			.SetNotNegative()
			.SetLessOrEqual(23)
			.SetDisplay("Start Hour", "Hour of day (0-23) when auto trading becomes enabled.", "Schedule");

		_stopHour = Param(nameof(StopHour), 8)
			.SetNotNegative()
			.SetLessOrEqual(23)
			.SetDisplay("Stop Hour", "Hour of day (0-23) when auto trading becomes disabled.", "Schedule");
	}

	/// <summary>
	/// Timeframe used to poll the clock.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Hour of the day when auto trading should be enabled.
	/// </summary>
	public int StartHour
	{
		get => _startHour.Value;
		set => _startHour.Value = value;
	}

	/// <summary>
	/// Hour of the day when auto trading should be disabled.
	/// </summary>
	public int StopHour
	{
		get => _stopHour.Value;
		set => _stopHour.Value = value;
	}

	/// <summary>
	/// Indicates whether auto trading is currently active.
	/// </summary>
	public bool AutoTradingActive { get; private set; }

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		AutoTradingActive = false;
		_lastEnableTime = null;
		_lastDisableTime = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		AutoTradingActive = false;
		_lastEnableTime = null;
		_lastDisableTime = null;

		var subscription = SubscribeCandles(CandleType);
		subscription.WhenCandlesFinished(ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
		}

		LogInfo("Auto trading scheduler initialized. Start={0:00}, Stop={1:00}.", StartHour, StopHour);
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return; // Wait for a fully formed candle to avoid premature time triggers.

		UpdateTradingWindow(candle.CloseTime);
	}

	private void UpdateTradingWindow(DateTimeOffset time)
	{
		var hour = time.Hour;

		if (hour == StartHour && ShouldTrigger(ref _lastEnableTime, time))
		{
			SetAutoTradingState(true, time); // Enable trading at the configured start hour.
		}

		if (hour == StopHour && ShouldTrigger(ref _lastDisableTime, time))
		{
			SetAutoTradingState(false, time); // Disable trading at the configured stop hour.
		}
	}

	private static bool ShouldTrigger(ref DateTimeOffset? lastTrigger, DateTimeOffset time)
	{
		if (lastTrigger is DateTimeOffset previous && previous.Date == time.Date && previous.Hour == time.Hour)
			return false; // Skip duplicate actions within the same hour to mirror the MT4 utility behaviour.

		lastTrigger = time;
		return true;
	}

	private void SetAutoTradingState(bool enable, DateTimeOffset time)
	{
		if (AutoTradingActive == enable)
			return; // No change needed because the desired state is already active.

		AutoTradingActive = enable;

		if (enable)
		{
			LogInfo("Auto trading enabled at {0:O}.", time);
		}
		else
		{
			LogInfo("Auto trading disabled at {0:O}.", time);
		}
	}
}
