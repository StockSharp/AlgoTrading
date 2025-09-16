namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Candles;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Strategy that enables trading only during specified session times.
/// Closes open positions outside the session.
/// </summary>
public class AutoTraderRusStrategy : Strategy
{
	private readonly StrategyParam<int> _startHour;
	private readonly StrategyParam<int> _startMinute;
	private readonly StrategyParam<int> _stopHour;
	private readonly StrategyParam<int> _stopMinute;
	private readonly StrategyParam<DataType> _candleType;

	/// <summary>
	/// Session start hour.
	/// </summary>
	public int StartHour
	{
		get => _startHour.Value;
		set => _startHour.Value = value;
	}

	/// <summary>
	/// Session start minute.
	/// </summary>
	public int StartMinute
	{
		get => _startMinute.Value;
		set => _startMinute.Value = value;
	}

	/// <summary>
	/// Session stop hour.
	/// </summary>
	public int StopHour
	{
		get => _stopHour.Value;
		set => _stopHour.Value = value;
	}

	/// <summary>
	/// Session stop minute.
	/// </summary>
	public int StopMinute
	{
		get => _stopMinute.Value;
		set => _stopMinute.Value = value;
	}

	/// <summary>
	/// Candle type used for time tracking.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public AutoTraderRusStrategy()
	{
		_startHour = Param(nameof(StartHour), 9)
			.SetRange(0, 23)
			.SetCanOptimize(true)
			.SetOptimize(0, 23, 1)
			.SetDisplay("Start Hour", "Session start hour", "Session");

		_startMinute = Param(nameof(StartMinute), 30)
			.SetRange(0, 59)
			.SetCanOptimize(true)
			.SetOptimize(0, 59, 1)
			.SetDisplay("Start Minute", "Session start minute", "Session");

		_stopHour = Param(nameof(StopHour), 23)
			.SetRange(0, 23)
			.SetCanOptimize(true)
			.SetOptimize(0, 23, 1)
			.SetDisplay("Stop Hour", "Session stop hour", "Session");

		_stopMinute = Param(nameof(StopMinute), 30)
			.SetRange(0, 59)
			.SetCanOptimize(true)
			.SetOptimize(0, 59, 1)
			.SetDisplay("Stop Minute", "Session stop minute", "Session");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Candles used to track session time", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		SubscribeCandles(CandleType)
			.Bind(ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var inSession = IsInSession(candle.CloseTime);

		// Close any open position when session is inactive
		if (!inSession && Position != 0)
			ClosePosition();

		// Log current trading state and time
		LogInfo($"Trading allowed: {inSession}, Time: {candle.CloseTime:HH:mm:ss}");
	}

	// Determine whether the time is within the allowed session
	private bool IsInSession(DateTimeOffset time)
	{
		var start = new TimeSpan(StartHour, StartMinute, 0);
		var stop = new TimeSpan(StopHour, StopMinute, 0);
		var current = time.TimeOfDay;

		if (stop < start)
			return current >= start || current < stop;

		return current >= start && current < stop;
	}
}

