using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Grid-style mean reversion strategy.
/// Buys dips relative to SMA, sells rallies, using ATR-based grid spacing.
/// </summary>
public class AiGridStrategy : Strategy
{
	private readonly StrategyParam<int> _maLength;
	private readonly StrategyParam<DataType> _candleType;

	public int MaLength { get => _maLength.Value; set => _maLength.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public AiGridStrategy()
	{
		_maLength = Param(nameof(MaLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("MA Length", "SMA period", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var sma = new SimpleMovingAverage { Length = MaLength };
		var atr = new StandardDeviation { Length = 14 };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(sma, atr, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal smaVal, decimal atrVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (atrVal <= 0)
			return;

		var close = candle.ClosePrice;
		var deviation = close - smaVal;

		// Grid buy: price dropped by more than 1 ATR below SMA
		if (deviation < -atrVal && Position <= 0)
		{
			if (Position < 0)
				BuyMarket();
			BuyMarket();
		}
		// Grid sell: price rose by more than 1 ATR above SMA
		else if (deviation > atrVal && Position >= 0)
		{
			if (Position > 0)
				SellMarket();
			SellMarket();
		}
		// Take profit at mean
		else if (Position > 0 && close > smaVal)
		{
			SellMarket();
		}
		else if (Position < 0 && close < smaVal)
		{
			BuyMarket();
		}
	}
}
