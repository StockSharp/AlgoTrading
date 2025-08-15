using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Three EMA Cross Strategy - uses fast/slow EMA crossover with trend EMA filter
/// </summary>
public class ThreeEmaCrossStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _fastEmaLength;
	private readonly StrategyParam<int> _slowEmaLength;
	private readonly StrategyParam<int> _trendEmaLength;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<int> _crossBackBars;

	private ExponentialMovingAverage _fastEma;
	private ExponentialMovingAverage _slowEma;
	private ExponentialMovingAverage _trendEma;

	private decimal _previousFastEma;
	private decimal _previousSlowEma;
	private bool _crossoverOccurred;
	private int _barsSinceCross;

	/// <summary>
	/// Candle type for strategy calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Fast EMA length.
	/// </summary>
	public int FastEmaLength
	{
		get => _fastEmaLength.Value;
		set => _fastEmaLength.Value = value;
	}

	/// <summary>
	/// Slow EMA length.
	/// </summary>
	public int SlowEmaLength
	{
		get => _slowEmaLength.Value;
		set => _slowEmaLength.Value = value;
	}

	/// <summary>
	/// Trend EMA length.
	/// </summary>
	public int TrendEmaLength
	{
		get => _trendEmaLength.Value;
		set => _trendEmaLength.Value = value;
	}

	/// <summary>
	/// Stop loss percentage.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
	}

	/// <summary>
	/// Check cross in the last X bars.
	/// </summary>
	public int CrossBackBars
	{
		get => _crossBackBars.Value;
		set => _crossBackBars.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public ThreeEmaCrossStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_fastEmaLength = Param(nameof(FastEmaLength), 10)
			.SetGreaterThanZero()
			.SetDisplay("Fast EMA", "Fast EMA length", "Moving Averages")
			.SetCanOptimize(true)
			.SetOptimize(5, 20, 3);

		_slowEmaLength = Param(nameof(SlowEmaLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("Slow EMA", "Slow EMA length", "Moving Averages")
			.SetCanOptimize(true)
			.SetOptimize(15, 30, 5);

		_trendEmaLength = Param(nameof(TrendEmaLength), 100)
			.SetGreaterThanZero()
			.SetDisplay("Trend EMA", "Trend EMA length", "Moving Averages")
			.SetCanOptimize(true)
			.SetOptimize(50, 200, 25);

		_stopLossPercent = Param(nameof(StopLossPercent), 99.0m)
			.SetRange(0.1m, 100.0m)
			.SetDisplay("Stop Loss %", "Stop loss percentage", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(1.0m, 10.0m, 1.0m);

		_crossBackBars = Param(nameof(CrossBackBars), 10)
			.SetGreaterThanZero()
			.SetDisplay("Cross Back Bars", "Check cross in the last X candles", "Strategy")
			.SetCanOptimize(true)
			.SetOptimize(5, 20, 3);
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

		_previousFastEma = default;
		_previousSlowEma = default;
		_crossoverOccurred = default;
		_barsSinceCross = default;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Initialize indicators
		_fastEma = new ExponentialMovingAverage { Length = FastEmaLength };
		_slowEma = new ExponentialMovingAverage { Length = SlowEmaLength };
		_trendEma = new ExponentialMovingAverage { Length = TrendEmaLength };

		// Create subscription for candles
		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_fastEma, _slowEma, _trendEma, ProcessCandle)
			.Start();

		// Setup chart visualization
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _fastEma);
			DrawIndicator(area, _slowEma);
			DrawIndicator(area, _trendEma);
			DrawOwnTrades(area);
		}

		// Setup protection
		StartProtection(new Unit(), new Unit(StopLossPercent / 100m, UnitTypes.Percent));
	}

	private void ProcessCandle(ICandleMessage candle, decimal fastEmaValue, decimal slowEmaValue, decimal trendEmaValue)
	{
		// Skip unfinished candles
		if (candle.State != CandleStates.Finished)
			return;

		// Wait for indicators to form
		if (!_fastEma.IsFormed || !_slowEma.IsFormed || !_trendEma.IsFormed)
			return;

		var currentPrice = candle.ClosePrice;
		var lowPrice = candle.LowPrice;

		// Detect EMA crossover
		if (_previousFastEma != 0 && _previousSlowEma != 0)
		{
			var crossedOver = _previousFastEma <= _previousSlowEma && fastEmaValue > slowEmaValue;
			if (crossedOver)
			{
				_crossoverOccurred = true;
				_barsSinceCross = 0;
			}
		}

		// Increment bars since cross
		if (_crossoverOccurred)
			_barsSinceCross++;

		// Reset cross flag if too many bars have passed
		if (_barsSinceCross > CrossBackBars)
			_crossoverOccurred = false;

		CheckEntryConditions(candle, fastEmaValue, slowEmaValue, trendEmaValue);
		CheckExitConditions(candle, fastEmaValue, slowEmaValue);

		// Store previous values
		_previousFastEma = fastEmaValue;
		_previousSlowEma = slowEmaValue;
	}

	private void CheckEntryConditions(ICandleMessage candle, decimal fastEmaValue, decimal slowEmaValue, decimal trendEmaValue)
	{
		var currentPrice = candle.ClosePrice;
		var lowPrice = candle.LowPrice;

		// Entry conditions:
		// 1. Recent crossover occurred (within last X bars)
		// 2. Current close >= fast EMA and previous low <= fast EMA (pullback to EMA)
		// 3. Trend EMA <= current close (trend filter)
		var entryCondition = _crossoverOccurred &&
							 currentPrice >= fastEmaValue &&
							 lowPrice <= fastEmaValue &&
							 trendEmaValue <= currentPrice;

		if (entryCondition && Position == 0)
		{
			RegisterOrder(CreateOrder(Sides.Buy, currentPrice, Volume));
		}
	}

	private void CheckExitConditions(ICandleMessage candle, decimal fastEmaValue, decimal slowEmaValue)
	{
		// Exit on EMA crossunder (fast EMA crosses below slow EMA)
		if (Position > 0 && 
			_previousFastEma > _previousSlowEma && 
			fastEmaValue < slowEmaValue)
		{
			RegisterOrder(CreateOrder(Sides.Sell, candle.ClosePrice, Math.Abs(Position)));
		}
	}
}