using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public class RsiCciDivergenceStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<int> _cciPeriod;
	private decimal? _prevRsi, _prevCci;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int RsiPeriod { get => _rsiPeriod.Value; set => _rsiPeriod.Value = value; }
	public int CciPeriod { get => _cciPeriod.Value; set => _cciPeriod.Value = value; }

	public RsiCciDivergenceStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame()).SetDisplay("Candle Type", "Timeframe", "General");
		_rsiPeriod = Param(nameof(RsiPeriod), 14).SetGreaterThanZero().SetDisplay("RSI Period", "RSI lookback", "Indicators");
		_cciPeriod = Param(nameof(CciPeriod), 14).SetGreaterThanZero().SetDisplay("CCI Period", "CCI lookback", "Indicators");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities() => [(Security, CandleType)];

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_prevRsi = null; _prevCci = null;
		var rsi = new RelativeStrengthIndex { Length = RsiPeriod };
		var cci = new CommodityChannelIndex { Length = CciPeriod };
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(rsi, cci, ProcessCandle).Start();
		var area = CreateChartArea();
		if (area != null) { DrawCandles(area, subscription); DrawOwnTrades(area); }
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsiVal, decimal cciVal)
	{
		if (candle.State != CandleStates.Finished) return;
		if (_prevRsi == null || _prevCci == null) { _prevRsi = rsiVal; _prevCci = cciVal; return; }
		var buySignal = (_prevRsi.Value < 30m && rsiVal >= 30m) || (_prevCci.Value < -100m && cciVal >= -100m);
		var sellSignal = (_prevRsi.Value > 70m && rsiVal <= 70m) || (_prevCci.Value > 100m && cciVal <= 100m);
		_prevRsi = rsiVal; _prevCci = cciVal;
		if (buySignal && Position <= 0) { if (Position < 0) BuyMarket(); BuyMarket(); }
		else if (sellSignal && Position >= 0) { if (Position > 0) SellMarket(); SellMarket(); }
	}
}
