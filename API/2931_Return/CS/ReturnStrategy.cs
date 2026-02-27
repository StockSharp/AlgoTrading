using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Mean reversion strategy using Bollinger Bands.
/// Buys when price drops below lower band and sells when price rises above upper band.
/// </summary>
public class ReturnStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _period;
	private readonly StrategyParam<decimal> _width;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int Period
	{
		get => _period.Value;
		set => _period.Value = value;
	}

	public decimal Width
	{
		get => _width.Value;
		set => _width.Value = value;
	}

	public ReturnStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe", "General");

		_period = Param(nameof(Period), 20)
			.SetGreaterThanZero()
			.SetDisplay("Period", "Bollinger Bands period", "Indicators");

		_width = Param(nameof(Width), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Width", "Bollinger Bands width", "Indicators");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var bb = new BollingerBands { Length = Period, Width = Width };

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

		// Buy when price drops below lower band (mean reversion)
		if (close < lower && Position <= 0)
		{
			if (Position < 0)
				BuyMarket();
			BuyMarket();
		}
		// Sell when price rises above upper band
		else if (close > upper && Position >= 0)
		{
			if (Position > 0)
				SellMarket();
			SellMarket();
		}
		// Exit long at middle band
		else if (Position > 0 && close >= middle)
		{
			SellMarket();
		}
		// Exit short at middle band
		else if (Position < 0 && close <= middle)
		{
			BuyMarket();
		}
	}
}
