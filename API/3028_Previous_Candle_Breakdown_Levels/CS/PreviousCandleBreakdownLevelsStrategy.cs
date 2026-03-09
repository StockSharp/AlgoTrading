using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public class PreviousCandleBreakdownLevelsStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private decimal? _prevHigh, _prevLow;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int FastPeriod { get => _fastPeriod.Value; set => _fastPeriod.Value = value; }
	public int SlowPeriod { get => _slowPeriod.Value; set => _slowPeriod.Value = value; }

	public PreviousCandleBreakdownLevelsStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame()).SetDisplay("Candle Type", "Timeframe", "General");
		_fastPeriod = Param(nameof(FastPeriod), 8).SetGreaterThanZero().SetDisplay("Fast EMA", "Fast period", "Indicators");
		_slowPeriod = Param(nameof(SlowPeriod), 21).SetGreaterThanZero().SetDisplay("Slow EMA", "Slow period", "Indicators");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities() => [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevHigh = null;
		_prevLow = null;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_prevHigh = null; _prevLow = null;
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
		if (!IsFormedAndOnlineAndAllowTrading()) { _prevHigh = candle.HighPrice; _prevLow = candle.LowPrice; return; }
		if (_prevHigh == null) { _prevHigh = candle.HighPrice; _prevLow = candle.LowPrice; return; }
		var close = candle.ClosePrice;
		if (fast > slow && close > _prevHigh.Value && Position <= 0) { if (Position < 0) BuyMarket(); BuyMarket(); }
		else if (fast < slow && close < _prevLow.Value && Position >= 0) { if (Position > 0) SellMarket(); SellMarket(); }
		_prevHigh = candle.HighPrice; _prevLow = candle.LowPrice;
	}
}
