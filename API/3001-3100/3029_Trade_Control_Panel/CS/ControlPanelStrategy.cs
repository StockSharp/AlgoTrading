using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public class ControlPanelStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _momPeriod;
	private decimal? _prevMom;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int MomPeriod { get => _momPeriod.Value; set => _momPeriod.Value = value; }

	public ControlPanelStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame()).SetDisplay("Candle Type", "Timeframe", "General");
		_momPeriod = Param(nameof(MomPeriod), 10).SetGreaterThanZero().SetDisplay("Momentum Period", "Momentum lookback", "Indicators");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities() => [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevMom = null;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_prevMom = null;
		var mom = new Momentum { Length = MomPeriod };
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(mom, ProcessCandle).Start();
		var area = CreateChartArea();
		if (area != null) { DrawCandles(area, subscription); DrawOwnTrades(area); }
	}

	private void ProcessCandle(ICandleMessage candle, decimal momVal)
	{
		if (candle.State != CandleStates.Finished) return;
		if (!IsFormedAndOnlineAndAllowTrading()) { _prevMom = momVal; return; }
		if (_prevMom == null) { _prevMom = momVal; return; }
		if (_prevMom.Value < 100m && momVal >= 100m && Position <= 0) { if (Position < 0) BuyMarket(); BuyMarket(); }
		else if (_prevMom.Value > 100m && momVal <= 100m && Position >= 0) { if (Position > 0) SellMarket(); SellMarket(); }
		_prevMom = momVal;
	}
}
