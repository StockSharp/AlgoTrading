using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Pure Price Action Fractal strategy: detects fractals (5-bar swing highs/lows)
/// and trades breakouts with WMA trend filter.
/// </summary>
public class PurePriceActionFractalStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;

	private readonly Queue<decimal> _highs = new();
	private readonly Queue<decimal> _lows = new();
	private decimal? _lastUpFractal;
	private decimal? _lastDownFractal;

	/// <summary>
	/// Constructor.
	/// </summary>
	public PurePriceActionFractalStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");

		_fastPeriod = Param(nameof(FastPeriod), 6)
			.SetGreaterThanZero()
			.SetDisplay("Fast WMA", "Fast weighted moving average period", "Indicators");

		_slowPeriod = Param(nameof(SlowPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Slow WMA", "Slow weighted moving average period", "Indicators");
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int FastPeriod
	{
		get => _fastPeriod.Value;
		set => _fastPeriod.Value = value;
	}

	public int SlowPeriod
	{
		get => _slowPeriod.Value;
		set => _slowPeriod.Value = value;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_highs.Clear();
		_lows.Clear();
		_lastUpFractal = null;
		_lastDownFractal = null;

		var fastWma = new WeightedMovingAverage { Length = FastPeriod };
		var slowWma = new WeightedMovingAverage { Length = SlowPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(fastWma, slowWma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fastWma);
			DrawIndicator(area, slowWma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fast, decimal slow)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Track last 5 highs and lows for fractal detection
		_highs.Enqueue(candle.HighPrice);
		_lows.Enqueue(candle.LowPrice);

		while (_highs.Count > 5) _highs.Dequeue();
		while (_lows.Count > 5) _lows.Dequeue();

		if (_highs.Count == 5)
		{
			var arr = _highs.ToArray();
			// Up fractal: middle bar has highest high
			if (arr[2] > arr[0] && arr[2] > arr[1] && arr[2] > arr[3] && arr[2] > arr[4])
				_lastUpFractal = arr[2];

			var larr = _lows.ToArray();
			// Down fractal: middle bar has lowest low
			if (larr[2] < larr[0] && larr[2] < larr[1] && larr[2] < larr[3] && larr[2] < larr[4])
				_lastDownFractal = larr[2];
		}

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var bullishTrend = fast > slow;
		var bearishTrend = fast < slow;

		// Buy: bullish trend, price breaks above up fractal
		if (bullishTrend && _lastUpFractal.HasValue && candle.ClosePrice > _lastUpFractal.Value && Position <= 0)
		{
			BuyMarket();
			_lastUpFractal = null;
		}
		// Sell: bearish trend, price breaks below down fractal
		else if (bearishTrend && _lastDownFractal.HasValue && candle.ClosePrice < _lastDownFractal.Value && Position >= 0)
		{
			SellMarket();
			_lastDownFractal = null;
		}
	}
}
