using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Recreates the original MetaTrader MACD alert expert.
/// Triggers informational messages when the MACD main line crosses user-defined thresholds.
/// </summary>
public class MacdAlertStrategy : Strategy
{
	private readonly StrategyParam<int> _macdFastPeriod;
	private readonly StrategyParam<int> _macdSlowPeriod;
	private readonly StrategyParam<int> _macdSignalPeriod;
	private readonly StrategyParam<decimal> _upperThreshold;
	private readonly StrategyParam<decimal> _lowerThreshold;
	private readonly StrategyParam<bool> _enableAlerts;
	private readonly StrategyParam<DataType> _candleType;

	private MovingAverageConvergenceDivergence? _macdIndicator;

	/// <summary>
	/// Fast EMA period for the MACD calculation.
	/// </summary>
	public int MacdFastPeriod
	{
		get => _macdFastPeriod.Value;
		set => _macdFastPeriod.Value = value;
	}

	/// <summary>
	/// Slow EMA period for the MACD calculation.
	/// </summary>
	public int MacdSlowPeriod
	{
		get => _macdSlowPeriod.Value;
		set => _macdSlowPeriod.Value = value;
	}

	/// <summary>
	/// Signal smoothing period inside the MACD indicator.
	/// </summary>
	public int MacdSignalPeriod
	{
		get => _macdSignalPeriod.Value;
		set => _macdSignalPeriod.Value = value;
	}

	/// <summary>
	/// Positive MACD level that triggers the bullish alert.
	/// </summary>
	public decimal UpperThreshold
	{
		get => _upperThreshold.Value;
		set => _upperThreshold.Value = value;
	}

	/// <summary>
	/// Negative MACD level that triggers the bearish alert.
	/// </summary>
	public decimal LowerThreshold
	{
		get => _lowerThreshold.Value;
		set => _lowerThreshold.Value = value;
	}

	/// <summary>
	/// Enables informational alerts.
	/// </summary>
	public bool EnableAlerts
	{
		get => _enableAlerts.Value;
		set => _enableAlerts.Value = value;
	}

	/// <summary>
	/// Candle type used to evaluate the MACD values.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="MacdAlertStrategy"/> class.
	/// </summary>
	public MacdAlertStrategy()
	{
		_macdFastPeriod = Param(nameof(MacdFastPeriod), 12)
		.SetDisplay("MACD Fast Period", "Fast EMA length for the MACD calculation", "Indicator")
		.SetGreaterThanZero()
		.SetCanOptimize(true)
		.SetOptimize(6, 18, 1);

		_macdSlowPeriod = Param(nameof(MacdSlowPeriod), 26)
		.SetDisplay("MACD Slow Period", "Slow EMA length for the MACD calculation", "Indicator")
		.SetGreaterThanZero()
		.SetCanOptimize(true)
		.SetOptimize(18, 34, 1);

		_macdSignalPeriod = Param(nameof(MacdSignalPeriod), 9)
		.SetDisplay("MACD Signal Period", "Signal smoothing length for the MACD calculation", "Indicator")
		.SetGreaterThanZero()
		.SetCanOptimize(true)
		.SetOptimize(5, 15, 1);

		_upperThreshold = Param(nameof(UpperThreshold), 0.00060m)
		.SetDisplay("Upper Threshold", "MACD value that fires the bullish alert", "Alerts");

		_lowerThreshold = Param(nameof(LowerThreshold), -0.00060m)
		.SetDisplay("Lower Threshold", "MACD value that fires the bearish alert", "Alerts");

		_enableAlerts = Param(nameof(EnableAlerts), true)
		.SetDisplay("Enable Alerts", "Write alert messages to the strategy log", "Alerts");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
		.SetDisplay("Candle Type", "Candle series used for MACD evaluation", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		// Reset internal indicator state when the strategy is reinitialized.
		_macdIndicator?.Reset();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Configure MACD with MetaTrader-equivalent parameters.
		_macdIndicator = new MovingAverageConvergenceDivergence
		{
			Fast = MacdFastPeriod,
			Slow = MacdSlowPeriod,
			Signal = MacdSignalPeriod
		};

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(_macdIndicator, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			// Plot candles and the MACD line to replicate the MetaTrader workspace.
			DrawCandles(area, subscription);
			DrawIndicator(area, _macdIndicator);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal macdValue, decimal signalValue, decimal histogramValue)
	{
		// Ignore unfinished candles to match the original bar-close evaluation.
		if (candle.State != CandleStates.Finished)
			return;

		// Discard alerts while the MACD is not formed yet.
		if (_macdIndicator is null || !_macdIndicator.IsFormed)
			return;

		// Only continue when the operator wants to receive notifications.
		if (!EnableAlerts)
			return;

		// Suppress compiler warnings for unused indicator outputs.
		_ = signalValue;
		_ = histogramValue;

		var upper = UpperThreshold;
		var lower = LowerThreshold;
		var time = candle.CloseTime;

		// Emit a bullish alert once the MACD exceeds the positive threshold.
		if (macdValue >= upper)
		{
			LogInfo($"MACD main line {macdValue:F5} exceeded the upper threshold {upper:F5} at {time:yyyy-MM-dd HH:mm:ss}.");
		}

		// Emit a bearish alert once the MACD drops below the negative threshold.
		if (macdValue <= lower)
		{
			LogInfo($"MACD main line {macdValue:F5} fell below the lower threshold {lower:F5} at {time:yyyy-MM-dd HH:mm:ss}.");
		}
	}
}
