using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Counter-trend strategy. Buys after price drops below SMA, exits when price returns to SMA.
/// </summary>
public class XbugFreeStrategy : Strategy
{
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<DataType> _candleType;

	public int MaPeriod { get => _maPeriod.Value; set => _maPeriod.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public XbugFreeStrategy()
	{
		_maPeriod = Param(nameof(MaPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("MA Period", "SMA period", "Indicators");
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		var sma = new SimpleMovingAverage { Length = MaPeriod };
		var atr = new StandardDeviation { Length = 14 };
		SubscribeCandles(CandleType).Bind(sma, atr, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal sma, decimal atr)
	{
		if (candle.State != CandleStates.Finished) return;
		if (atr <= 0) return;

		var close = candle.ClosePrice;

		// Counter-trend: buy when price drops below SMA by 1 ATR
		if (close < sma - atr && Position <= 0)
		{
			if (Position < 0) BuyMarket();
			BuyMarket();
		}
		// Counter-trend: sell when price rises above SMA by 1 ATR
		else if (close > sma + atr && Position >= 0)
		{
			if (Position > 0) SellMarket();
			SellMarket();
		}
		// Exit long at SMA
		else if (Position > 0 && close >= sma)
			SellMarket();
		// Exit short at SMA
		else if (Position < 0 && close <= sma)
			BuyMarket();
	}
}
