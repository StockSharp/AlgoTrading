using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// EInTradePanel strategy. Uses ATR breakout for entries.
/// </summary>
public class EInTradePanelStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _multiplier;
	private decimal? _prevClose;
	private decimal? _prevAtr;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int AtrPeriod { get => _atrPeriod.Value; set => _atrPeriod.Value = value; }
	public decimal Multiplier { get => _multiplier.Value; set => _multiplier.Value = value; }

	public EInTradePanelStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame()).SetDisplay("Candle Type", "Timeframe", "General");
		_atrPeriod = Param(nameof(AtrPeriod), 14).SetGreaterThanZero().SetDisplay("ATR Period", "ATR lookback", "Indicators");
		_multiplier = Param(nameof(Multiplier), 1.5m).SetDisplay("Multiplier", "ATR multiplier for breakout", "Indicators");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities() => [(Security, CandleType)];

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_prevClose = null; _prevAtr = null;
		var atr = new AverageTrueRange { Length = AtrPeriod };
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(atr, ProcessCandle).Start();
		var area = CreateChartArea();
		if (area != null) { DrawCandles(area, subscription); DrawIndicator(area, atr); DrawOwnTrades(area); }
	}

	private void ProcessCandle(ICandleMessage candle, decimal atrVal)
	{
		if (candle.State != CandleStates.Finished) return;
		if (_prevClose == null || _prevAtr == null) { _prevClose = candle.ClosePrice; _prevAtr = atrVal; return; }
		var threshold = _prevAtr.Value * Multiplier;
		var diff = candle.ClosePrice - _prevClose.Value;
		if (diff > threshold && Position <= 0) { if (Position < 0) BuyMarket(); BuyMarket(); }
		else if (diff < -threshold && Position >= 0) { if (Position > 0) SellMarket(); SellMarket(); }
		_prevClose = candle.ClosePrice; _prevAtr = atrVal;
	}
}
