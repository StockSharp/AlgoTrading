using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// MACD strategy with EMA trend filter.
/// Buys when MACD histogram is positive and price is above trend EMA.
/// Sells when MACD histogram is negative and price is below trend EMA.
/// </summary>
public class MacdSampleTrendFilterStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<int> _trendPeriod;

	private int _prevSignal;

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

	public int TrendPeriod
	{
		get => _trendPeriod.Value;
		set => _trendPeriod.Value = value;
	}

	public MacdSampleTrendFilterStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe", "General");

		_fastPeriod = Param(nameof(FastPeriod), 12)
			.SetGreaterThanZero()
			.SetDisplay("Fast Period", "Fast EMA for MACD", "Indicators");

		_slowPeriod = Param(nameof(SlowPeriod), 26)
			.SetGreaterThanZero()
			.SetDisplay("Slow Period", "Slow EMA for MACD", "Indicators");

		_trendPeriod = Param(nameof(TrendPeriod), 100)
			.SetGreaterThanZero()
			.SetDisplay("Trend Period", "Trend EMA period", "Indicators");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevSignal = 0;

		var fastEma = new ExponentialMovingAverage { Length = FastPeriod };
		var slowEma = new ExponentialMovingAverage { Length = SlowPeriod };
		var trendEma = new ExponentialMovingAverage { Length = TrendPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(fastEma, slowEma, trendEma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fastEma);
			DrawIndicator(area, slowEma);
			DrawIndicator(area, trendEma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fast, decimal slow, decimal trend)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var close = candle.ClosePrice;
		var macdLine = fast - slow;

		// Signal: MACD positive + price above trend = bullish; MACD negative + price below trend = bearish
		var signal = 0;
		if (macdLine > 0 && close > trend)
			signal = 1;
		else if (macdLine < 0 && close < trend)
			signal = -1;

		if (signal == _prevSignal)
			return;

		var oldSignal = _prevSignal;
		_prevSignal = signal;

		if (signal == 1 && oldSignal <= 0)
		{
			if (Position < 0)
				BuyMarket();
			if (Position <= 0)
				BuyMarket();
		}
		else if (signal == -1 && oldSignal >= 0)
		{
			if (Position > 0)
				SellMarket();
			if (Position >= 0)
				SellMarket();
		}
	}
}
