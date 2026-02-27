using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Intraday mean reversion strategy using SMA and standard deviation bands.
/// Buys when price touches lower band, sells when touching upper band.
/// </summary>
public class IntradayV2Strategy : Strategy
{
	private readonly StrategyParam<int> _bandLength;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<decimal> _closes = new();

	public int BandLength { get => _bandLength.Value; set => _bandLength.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public IntradayV2Strategy()
	{
		_bandLength = Param(nameof(BandLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("Band Length", "Band period", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_closes.Clear();
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var sma = new SimpleMovingAverage { Length = BandLength };
		var stdev = new StandardDeviation { Length = BandLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(sma, stdev, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal smaVal, decimal stdevVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (stdevVal <= 0)
			return;

		var close = candle.ClosePrice;
		var upper = smaVal + 2m * stdevVal;
		var lower = smaVal - 2m * stdevVal;

		// Mean reversion: buy at lower band
		if (close < lower && Position <= 0)
		{
			if (Position < 0)
				BuyMarket();
			BuyMarket();
		}
		// Mean reversion: sell at upper band
		else if (close > upper && Position >= 0)
		{
			if (Position > 0)
				SellMarket();
			SellMarket();
		}

		// Exit long at middle (SMA)
		if (Position > 0 && close > smaVal)
		{
			SellMarket();
		}
		// Exit short at middle (SMA)
		else if (Position < 0 && close < smaVal)
		{
			BuyMarket();
		}
	}
}
