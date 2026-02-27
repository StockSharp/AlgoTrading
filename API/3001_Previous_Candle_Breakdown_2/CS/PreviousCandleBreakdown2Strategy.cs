using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Previous candle breakdown strategy v2.
/// Trades breakout of previous candle high/low filtered by EMA crossover.
/// </summary>
public class PreviousCandleBreakdown2Strategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private decimal? _prevHigh;
	private decimal? _prevLow;
	private decimal? _prevFast;
	private decimal? _prevSlow;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int FastPeriod { get => _fastPeriod.Value; set => _fastPeriod.Value = value; }
	public int SlowPeriod { get => _slowPeriod.Value; set => _slowPeriod.Value = value; }

	public PreviousCandleBreakdown2Strategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame()).SetDisplay("Candle Type", "Timeframe", "General");
		_fastPeriod = Param(nameof(FastPeriod), 10).SetGreaterThanZero().SetDisplay("Fast EMA", "Fast EMA period", "Indicators");
		_slowPeriod = Param(nameof(SlowPeriod), 30).SetGreaterThanZero().SetDisplay("Slow EMA", "Slow EMA period", "Indicators");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities() => [(Security, CandleType)];

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_prevHigh = null; _prevLow = null; _prevFast = null; _prevSlow = null;
		var fast = new ExponentialMovingAverage { Length = FastPeriod };
		var slow = new ExponentialMovingAverage { Length = SlowPeriod };
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(fast, slow, ProcessCandle).Start();
		var area = CreateChartArea();
		if (area != null) { DrawCandles(area, subscription); DrawIndicator(area, fast); DrawIndicator(area, slow); DrawOwnTrades(area); }
	}

	private void ProcessCandle(ICandleMessage candle, decimal fast, decimal slow)
	{
		if (candle.State != CandleStates.Finished) return;
		if (_prevHigh == null || _prevLow == null || _prevFast == null || _prevSlow == null)
		{
			_prevHigh = candle.HighPrice; _prevLow = candle.LowPrice; _prevFast = fast; _prevSlow = slow;
			return;
		}
		var close = candle.ClosePrice;
		var bullTrend = fast > slow;
		var bearTrend = fast < slow;
		if (bullTrend && close > _prevHigh.Value && Position <= 0)
		{
			if (Position < 0) BuyMarket();
			BuyMarket();
		}
		else if (bearTrend && close < _prevLow.Value && Position >= 0)
		{
			if (Position > 0) SellMarket();
			SellMarket();
		}
		_prevHigh = candle.HighPrice; _prevLow = candle.LowPrice; _prevFast = fast; _prevSlow = slow;
	}
}
