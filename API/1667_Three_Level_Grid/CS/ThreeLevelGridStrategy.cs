using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Three-level grid strategy using EMA as center line.
/// Buys on dips below EMA at different levels, sells on rises above EMA.
/// </summary>
public class ThreeLevelGridStrategy : Strategy
{
	private readonly StrategyParam<int> _emaLength;
	private readonly StrategyParam<DataType> _candleType;

	public int EmaLength { get => _emaLength.Value; set => _emaLength.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public ThreeLevelGridStrategy()
	{
		_emaLength = Param(nameof(EmaLength), 30)
			.SetGreaterThanZero()
			.SetDisplay("EMA Length", "EMA period for center line", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var ema = new ExponentialMovingAverage { Length = EmaLength };
		var atr = new AverageTrueRange { Length = 14 };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ema, atr, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal emaVal, decimal atrVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (atrVal <= 0)
			return;

		var close = candle.ClosePrice;
		var diff = close - emaVal;

		// Buy at different grid levels below EMA
		if (diff < -1.5m * atrVal && Position <= 0)
		{
			if (Position < 0)
				BuyMarket();
			BuyMarket();
		}
		// Sell at different grid levels above EMA
		else if (diff > 1.5m * atrVal && Position >= 0)
		{
			if (Position > 0)
				SellMarket();
			SellMarket();
		}
		// Mean reversion exit
		else if (Position > 0 && close > emaVal + 0.5m * atrVal)
		{
			SellMarket();
		}
		else if (Position < 0 && close < emaVal - 0.5m * atrVal)
		{
			BuyMarket();
		}
	}
}
