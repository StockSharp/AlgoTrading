namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Strategy that analyzes historical bar data and logs gaps in the series.
/// </summary>
public class AnalyzeHistoryStrategy : Strategy
{
	private readonly StrategyParam<int> _minGapInBars;
	private readonly StrategyParam<DataType> _candleType;

	private bool _hasPrevTime;
	private DateTimeOffset _prevTime;

	/// <summary>
	/// Minimum gap to detect in number of bars.
	/// </summary>
	public int MinGapInBars
	{
		get => _minGapInBars.Value;
		set => _minGapInBars.Value = value;
	}

	/// <summary>
	/// Type of candles to analyze.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public AnalyzeHistoryStrategy()
	{
		_minGapInBars = Param(nameof(MinGapInBars), 10)
			.SetDisplay("Min Gap (bars)", "Minimum gap detected (in number of bars)", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(5, 50, 5);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
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

		var subscription = SubscribeCandles(CandleType);
		// Bind candle handler and start subscription
		subscription.Bind(ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return; // only process completed candles

		var time = candle.OpenTime;

		if (!_hasPrevTime)
		{
			// Log first available date
			LogInfo($"History starts from {time:yyyy-MM-dd}");
			_prevTime = time;
			_hasPrevTime = true;
			return;
		}

		var timeFrame = ((TimeFrameCandleMessage)candle).TimeFrame;
		var gapThreshold = TimeSpan.FromTicks(timeFrame.Ticks * MinGapInBars);

		// Check if the time between bars exceeds threshold
		if (time - _prevTime > gapThreshold)
		{
			// Ignore weekend gaps (Friday to Sunday)
			if (time.DayOfWeek != DayOfWeek.Sunday || _prevTime.DayOfWeek != DayOfWeek.Friday)
				LogInfo($"gap from {_prevTime:O} to {time:O}");
		}

		_prevTime = time;
	}

	/// <inheritdoc />
	protected override void OnStopped()
	{
		base.OnStopped();

		if (_hasPrevTime)
			LogInfo($"History ends on {_prevTime:yyyy-MM-dd}");
	}
}
