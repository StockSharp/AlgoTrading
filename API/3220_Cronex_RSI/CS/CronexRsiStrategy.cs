using System;
using System.Collections.Generic;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public class CronexRsiStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _period;
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int Period { get => _period.Value; set => _period.Value = value; }
	public CronexRsiStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame()).SetDisplay("Candle Type", "Timeframe", "General");
		_period = Param(nameof(Period), 20).SetGreaterThanZero().SetDisplay("Channel Period", "Highest/Lowest lookback", "Indicators");
	}
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities() => [(Security, CandleType)];
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		var hi = new Highest { Length = Period };
		var lo = new Lowest { Length = Period };
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(hi, lo, ProcessCandle).Start();
		var area = CreateChartArea();
		if (area != null) { DrawCandles(area, subscription); DrawIndicator(area, hi); DrawIndicator(area, lo); DrawOwnTrades(area); }
	}
	private void ProcessCandle(ICandleMessage candle, decimal highest, decimal lowest)
	{
		if (candle.State != CandleStates.Finished) return;
		if (candle.ClosePrice >= highest && Position <= 0) { if (Position < 0) BuyMarket(); BuyMarket(); }
		else if (candle.ClosePrice <= lowest && Position >= 0) { if (Position > 0) SellMarket(); SellMarket(); }
	}
}
