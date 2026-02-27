using System;
using System.Collections.Generic;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public class BladeRunnerStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _period;
	private decimal? _prevDm;
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int Period { get => _period.Value; set => _period.Value = value; }
	public BladeRunnerStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame()).SetDisplay("Candle Type", "Timeframe", "General");
		_period = Param(nameof(Period), 14).SetGreaterThanZero().SetDisplay("DeMarker Period", "DeMarker lookback", "Indicators");
	}
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities() => [(Security, CandleType)];
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_prevDm = null;
		var dm = new DeMarker { Length = Period };
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(dm, ProcessCandle).Start();
		var area = CreateChartArea();
		if (area != null) { DrawCandles(area, subscription); DrawOwnTrades(area); }
	}
	private void ProcessCandle(ICandleMessage candle, decimal dmVal)
	{
		if (candle.State != CandleStates.Finished) return;
		if (_prevDm == null) { _prevDm = dmVal; return; }
		if (_prevDm.Value < 0.5m && dmVal >= 0.5m && Position <= 0) { if (Position < 0) BuyMarket(); BuyMarket(); }
		else if (_prevDm.Value > 0.5m && dmVal <= 0.5m && Position >= 0) { if (Position > 0) SellMarket(); SellMarket(); }
		_prevDm = dmVal;
	}
}
