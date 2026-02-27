using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// XPeriod candle system with Bollinger Bands breakout.
/// Buys on close above upper band with bullish candle, sells on close below lower band with bearish candle.
/// </summary>
public class XPeriodCandleSystemTmPlusStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _bbPeriod;
	private readonly StrategyParam<decimal> _bbWidth;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int BbPeriod
	{
		get => _bbPeriod.Value;
		set => _bbPeriod.Value = value;
	}

	public decimal BbWidth
	{
		get => _bbWidth.Value;
		set => _bbWidth.Value = value;
	}

	public XPeriodCandleSystemTmPlusStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe", "General");

		_bbPeriod = Param(nameof(BbPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("BB Period", "Bollinger Bands period", "Indicators");

		_bbWidth = Param(nameof(BbWidth), 2m)
			.SetGreaterThanZero()
			.SetDisplay("BB Width", "Bollinger Bands width", "Indicators");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var bb = new BollingerBands { Length = BbPeriod, Width = BbWidth };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(bb, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, bb);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue value)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var bb = (BollingerBandsValue)value;
		if (bb.UpBand is not decimal upper ||
			bb.LowBand is not decimal lower ||
			bb.MovingAverage is not decimal middle)
			return;

		var close = candle.ClosePrice;
		var isBullish = candle.ClosePrice > candle.OpenPrice;
		var isBearish = candle.ClosePrice < candle.OpenPrice;

		// Buy: close above upper band with bullish candle
		if (close > upper && isBullish && Position <= 0)
		{
			if (Position < 0)
				BuyMarket();
			BuyMarket();
		}
		// Sell: close below lower band with bearish candle
		else if (close < lower && isBearish && Position >= 0)
		{
			if (Position > 0)
				SellMarket();
			SellMarket();
		}
		// Exit long at middle band
		else if (Position > 0 && close < middle)
		{
			SellMarket();
		}
		// Exit short at middle band
		else if (Position < 0 && close > middle)
		{
			BuyMarket();
		}
	}
}
