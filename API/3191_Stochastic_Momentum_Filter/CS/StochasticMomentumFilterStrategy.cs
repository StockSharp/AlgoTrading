using System;
using System.Collections.Generic;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public class StochasticMomentumFilterStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _momentumPeriod;
	private decimal? _prevMom;
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int MomentumPeriod { get => _momentumPeriod.Value; set => _momentumPeriod.Value = value; }
	public StochasticMomentumFilterStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame()).SetDisplay("Candle Type", "Timeframe", "General");
		_momentumPeriod = Param(nameof(MomentumPeriod), 14).SetGreaterThanZero().SetDisplay("Momentum Period", "Momentum lookback", "Indicators");
	}
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities() => [(Security, CandleType)];
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_prevMom = null;
		var mom = new Momentum { Length = MomentumPeriod };
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(mom, ProcessCandle).Start();
		var area = CreateChartArea();
		if (area != null) { DrawCandles(area, subscription); DrawOwnTrades(area); }
	}
	private void ProcessCandle(ICandleMessage candle, decimal momVal)
	{
		if (candle.State != CandleStates.Finished) return;
		if (_prevMom == null) { _prevMom = momVal; return; }
		if (_prevMom.Value < 100m && momVal >= 100m && Position <= 0) { if (Position < 0) BuyMarket(); BuyMarket(); }
		else if (_prevMom.Value > 100m && momVal <= 100m && Position >= 0) { if (Position > 0) SellMarket(); SellMarket(); }
		_prevMom = momVal;
	}
}
