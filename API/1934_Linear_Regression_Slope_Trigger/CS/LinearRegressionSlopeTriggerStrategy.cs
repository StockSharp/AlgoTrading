using System;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on a smoothed slope line and trigger line.
/// </summary>
public class LinearRegressionSlopeTriggerStrategy : Strategy
{
	private readonly StrategyParam<int> _slopeLength;
	private readonly StrategyParam<int> _triggerShift;
	private readonly StrategyParam<bool> _enableLong;
	private readonly StrategyParam<bool> _enableShort;
	private readonly StrategyParam<decimal> _takeProfitPercent;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<DataType> _candleType;

	private readonly ExponentialMovingAverage _trendLine = new();
	private readonly SimpleMovingAverage _triggerLine = new();
	private decimal _previousTrendValue;
	private decimal _previousSlope;
	private decimal _previousTrigger;
	private bool _isInitialized;
	private int _barsSinceTrade;

	/// <summary>
	/// Period for calculating the smoothed trend line.
	/// </summary>
	public int SlopeLength
	{
		get => _slopeLength.Value;
		set => _slopeLength.Value = value;
	}

	/// <summary>
	/// Number of bars used for trigger smoothing.
	/// </summary>
	public int TriggerShift
	{
		get => _triggerShift.Value;
		set => _triggerShift.Value = value;
	}

	/// <summary>
	/// Allow long entries.
	/// </summary>
	public bool EnableLong
	{
		get => _enableLong.Value;
		set => _enableLong.Value = value;
	}

	/// <summary>
	/// Allow short entries.
	/// </summary>
	public bool EnableShort
	{
		get => _enableShort.Value;
		set => _enableShort.Value = value;
	}

	/// <summary>
	/// Take-profit percentage from entry price.
	/// </summary>
	public decimal TakeProfitPercent
	{
		get => _takeProfitPercent.Value;
		set => _takeProfitPercent.Value = value;
	}

	/// <summary>
	/// Stop-loss percentage from entry price.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
	}

	/// <summary>
	/// Bars to wait after a completed trade.
	/// </summary>
	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
	}

	/// <summary>
	/// Type of candles used by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="LinearRegressionSlopeTriggerStrategy"/>.
	/// </summary>
	public LinearRegressionSlopeTriggerStrategy()
	{
		_slopeLength = Param(nameof(SlopeLength), 12)
			.SetGreaterThanZero()
			.SetDisplay("Slope Length", "Period for the smoothed trend line", "Indicator")
			.SetOptimize(5, 30, 1);

		_triggerShift = Param(nameof(TriggerShift), 2)
			.SetGreaterThanZero()
			.SetDisplay("Trigger Shift", "Bars used for trigger smoothing", "Indicator")
			.SetOptimize(1, 5, 1);

		_enableLong = Param(nameof(EnableLong), true)
			.SetDisplay("Enable Long", "Allow long trades", "Trading");

		_enableShort = Param(nameof(EnableShort), true)
			.SetDisplay("Enable Short", "Allow short trades", "Trading");

		_takeProfitPercent = Param(nameof(TakeProfitPercent), 5m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit %", "Take-profit percentage", "Risk Management")
			.SetOptimize(2m, 10m, 1m);

		_stopLossPercent = Param(nameof(StopLossPercent), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss %", "Stop-loss percentage", "Risk Management")
			.SetOptimize(1m, 5m, 1m);

		_cooldownBars = Param(nameof(CooldownBars), 1)
			.SetDisplay("Cooldown Bars", "Bars to wait after a completed trade", "Risk Management");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for candles", "General");
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

		_trendLine.Reset();
		_triggerLine.Reset();
		_previousTrendValue = 0m;
		_previousSlope = 0m;
		_previousTrigger = 0m;
		_isInitialized = false;
		_barsSinceTrade = CooldownBars;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_trendLine.Length = SlopeLength;
		_triggerLine.Length = TriggerShift;

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		StartProtection(
			new Unit(TakeProfitPercent, UnitTypes.Percent),
			new Unit(StopLossPercent, UnitTypes.Percent));

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var trendValue = _trendLine.Process(new DecimalIndicatorValue(_trendLine, candle.ClosePrice, candle.ServerTime) { IsFinal = true }).ToDecimal();

		if (!_trendLine.IsFormed)
			return;

		if (!_isInitialized)
		{
			_previousTrendValue = trendValue;
			_isInitialized = true;
			return;
		}

		var slope = trendValue - _previousTrendValue;
		var trigger = _triggerLine.Process(new DecimalIndicatorValue(_triggerLine, slope, candle.ServerTime) { IsFinal = true }).ToDecimal();

		if (!_triggerLine.IsFormed)
		{
			_previousTrendValue = trendValue;
			_previousSlope = slope;
			_previousTrigger = slope;
			return;
		}

		if (_barsSinceTrade < CooldownBars)
			_barsSinceTrade++;

		var buySignal = _previousTrigger <= _previousSlope && trigger > slope && slope > 0m;
		var sellSignal = _previousTrigger >= _previousSlope && trigger < slope && slope < 0m;
		var closeLong = slope >= 0m && trigger < slope;
		var closeShort = slope <= 0m && trigger > slope;

		if (_barsSinceTrade >= CooldownBars && Position == 0)
		{
			if (buySignal && EnableLong)
			{
				BuyMarket();
				_barsSinceTrade = 0;
			}
			else if (sellSignal && EnableShort)
			{
				SellMarket();
				_barsSinceTrade = 0;
			}
		}

		_previousTrendValue = trendValue;
		_previousSlope = slope;
		_previousTrigger = trigger;
	}
}
