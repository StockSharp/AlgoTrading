using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Envelope Limit Ladder strategy - Bollinger band mean reversion.
/// Buys when price touches lower band, sells when it touches upper band.
/// Exits at the middle band.
/// </summary>
public class EnvelopeLimitLadderStrategy : Strategy
{
	private readonly StrategyParam<int> _bbPeriod;
	private readonly StrategyParam<decimal> _bbWidth;
	private readonly StrategyParam<DataType> _candleType;

	public int BbPeriod { get => _bbPeriod.Value; set => _bbPeriod.Value = value; }
	public decimal BbWidth { get => _bbWidth.Value; set => _bbWidth.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public EnvelopeLimitLadderStrategy()
	{
		_bbPeriod = Param(nameof(BbPeriod), 20)
			.SetDisplay("BB Period", "Bollinger bands period", "Indicators");

		_bbWidth = Param(nameof(BbWidth), 1.5m)
			.SetDisplay("BB Width", "Bollinger bands deviation", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var bb = new BollingerBands { Length = BbPeriod, Width = BbWidth };

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

		var bbVal = value as BollingerBandsValue;
		if (bbVal == null)
			return;

		var upper = bbVal.UpBand;
		var lower = bbVal.LowBand;
		var middle = bbVal.MovingAverage;

		if (upper == null || lower == null || middle == null)
			return;

		var close = candle.ClosePrice;

		// Mean reversion: buy at lower band
		if (close < lower.Value && Position <= 0)
		{
			if (Position < 0)
				BuyMarket();
			BuyMarket();
		}
		// Mean reversion: sell at upper band
		else if (close > upper.Value && Position >= 0)
		{
			if (Position > 0)
				SellMarket();
			SellMarket();
		}
		// Exit long at middle
		else if (Position > 0 && close > middle.Value)
		{
			SellMarket();
		}
		// Exit short at middle
		else if (Position < 0 && close < middle.Value)
		{
			BuyMarket();
		}
	}
}
