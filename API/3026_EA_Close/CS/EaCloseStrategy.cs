using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public class EaCloseStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _cciPeriod;
	private decimal? _prevCci;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int CciPeriod { get => _cciPeriod.Value; set => _cciPeriod.Value = value; }

	public EaCloseStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame()).SetDisplay("Candle Type", "Timeframe", "General");
		_cciPeriod = Param(nameof(CciPeriod), 14).SetGreaterThanZero().SetDisplay("CCI Period", "CCI lookback", "Indicators");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities() => [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevCci = null;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_prevCci = null;
		var cci = new CommodityChannelIndex { Length = CciPeriod };
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(cci, ProcessCandle).Start();
		var area = CreateChartArea();
		if (area != null) { DrawCandles(area, subscription); DrawOwnTrades(area); }
	}

	private void ProcessCandle(ICandleMessage candle, decimal cciVal)
	{
		if (candle.State != CandleStates.Finished) return;
		if (!IsFormedAndOnlineAndAllowTrading()) { _prevCci = cciVal; return; }
		if (_prevCci == null) { _prevCci = cciVal; return; }
		if (_prevCci.Value < 0m && cciVal >= 0m && Position <= 0) { if (Position < 0) BuyMarket(); BuyMarket(); }
		else if (_prevCci.Value > 0m && cciVal <= 0m && Position >= 0) { if (Position > 0) SellMarket(); SellMarket(); }
		_prevCci = cciVal;
	}
}
