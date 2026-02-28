using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Bollinger Band breakout strategy.
/// Buys when price closes above upper band, sells when price closes below lower band.
/// Exits at middle band.
/// </summary>
public class BollingerBandPendingStopsStrategy : Strategy
{
	private readonly StrategyParam<int> _bandPeriod;
	private readonly StrategyParam<decimal> _bandWidth;
	private readonly StrategyParam<DataType> _candleType;

	public int BandPeriod { get => _bandPeriod.Value; set => _bandPeriod.Value = value; }
	public decimal BandWidth { get => _bandWidth.Value; set => _bandWidth.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public BollingerBandPendingStopsStrategy()
	{
		_bandPeriod = Param(nameof(BandPeriod), 20)
			.SetDisplay("Band Period", "Bollinger bands period", "Indicators");

		_bandWidth = Param(nameof(BandWidth), 1m)
			.SetDisplay("Band Width", "Bollinger bands deviation", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var bb = new BollingerBands { Length = BandPeriod, Width = BandWidth };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(bb, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue value)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!value.IsFinal || value.IsEmpty)
			return;

		var bbVal = value.IsEmpty ? null : value as BollingerBandsValue;
		if (bbVal == null)
			return;

		var upper = bbVal.UpBand;
		var lower = bbVal.LowBand;
		var middle = bbVal.MovingAverage;

		if (upper == null || lower == null || middle == null)
			return;

		var close = candle.ClosePrice;

		// Breakout above upper band - buy
		if (close > upper.Value && Position <= 0)
		{
			if (Position < 0)
				BuyMarket();
			BuyMarket();
		}
		// Breakout below lower band - sell
		else if (close < lower.Value && Position >= 0)
		{
			if (Position > 0)
				SellMarket();
			SellMarket();
		}
		// Exit at middle band
		else if (Position > 0 && close < middle.Value)
		{
			SellMarket();
		}
		else if (Position < 0 && close > middle.Value)
		{
			BuyMarket();
		}
	}
}
