using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Blockbuster Bollinger breakout strategy.
/// Buys when price closes below lower Bollinger band (mean reversion).
/// Sells when price closes above upper Bollinger band.
/// Exits at middle band.
/// </summary>
public class BlockbusterBollingerStrategy : Strategy
{
	private readonly StrategyParam<int> _bollingerPeriod;
	private readonly StrategyParam<decimal> _bollingerWidth;
	private readonly StrategyParam<DataType> _candleType;

	public int BollingerPeriod { get => _bollingerPeriod.Value; set => _bollingerPeriod.Value = value; }
	public decimal BollingerWidth { get => _bollingerWidth.Value; set => _bollingerWidth.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public BlockbusterBollingerStrategy()
	{
		_bollingerPeriod = Param(nameof(BollingerPeriod), 20)
			.SetDisplay("BB Period", "Bollinger bands period", "Indicators");

		_bollingerWidth = Param(nameof(BollingerWidth), 1m)
			.SetDisplay("BB Width", "Bollinger bands deviation", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var bb = new BollingerBands { Length = BollingerPeriod, Width = BollingerWidth };

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

		// Breakout above upper band - momentum buy
		if (close > upper.Value && Position <= 0)
		{
			if (Position < 0)
				BuyMarket();
			BuyMarket();
		}
		// Breakout below lower band - momentum sell
		else if (close < lower.Value && Position >= 0)
		{
			if (Position > 0)
				SellMarket();
			SellMarket();
		}
		// Exit long at middle
		else if (Position > 0 && close < middle.Value)
		{
			SellMarket();
		}
		// Exit short at middle
		else if (Position < 0 && close > middle.Value)
		{
			BuyMarket();
		}
	}
}
