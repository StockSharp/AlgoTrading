using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Virtual Trailing Stop Level1 strategy (simplified). Uses EMA with
/// percentage-based trailing stop for position management.
/// </summary>
public class VirtualTrailingStopLevel1Strategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _emaLength;
	private readonly StrategyParam<decimal> _trailingPercent;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int EmaLength
	{
		get => _emaLength.Value;
		set => _emaLength.Value = value;
	}

	public decimal TrailingPercent
	{
		get => _trailingPercent.Value;
		set => _trailingPercent.Value = value;
	}

	public VirtualTrailingStopLevel1Strategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Candles", "General");

		_emaLength = Param(nameof(EmaLength), 15)
			.SetGreaterThanZero()
			.SetDisplay("EMA Length", "EMA period", "Indicators");

		_trailingPercent = Param(nameof(TrailingPercent), 1.5m)
			.SetGreaterThanZero()
			.SetDisplay("Trailing %", "Trailing stop percent", "Risk");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var ema = new ExponentialMovingAverage { Length = EmaLength };
		decimal highSinceEntry = 0;
		decimal lowSinceEntry = decimal.MaxValue;
		decimal prevClose = 0;
		decimal prevEma = 0;
		var hasPrev = false;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ema, (ICandleMessage candle, decimal emaVal) =>
			{
				if (candle.State != CandleStates.Finished)
					return;

				if (!hasPrev)
				{
					prevClose = candle.ClosePrice;
					prevEma = emaVal;
					hasPrev = true;
					return;
				}

				if (!IsFormedAndOnlineAndAllowTrading())
				{
					prevClose = candle.ClosePrice;
					prevEma = emaVal;
					return;
				}

				var close = candle.ClosePrice;

				// Trailing stop management
				if (Position > 0)
				{
					if (candle.HighPrice > highSinceEntry) highSinceEntry = candle.HighPrice;
					if (close < highSinceEntry * (1m - TrailingPercent / 100m))
					{
						SellMarket();
						highSinceEntry = 0;
						lowSinceEntry = decimal.MaxValue;
						return;
					}
				}
				else if (Position < 0)
				{
					if (candle.LowPrice < lowSinceEntry) lowSinceEntry = candle.LowPrice;
					if (close > lowSinceEntry * (1m + TrailingPercent / 100m))
					{
						BuyMarket();
						highSinceEntry = 0;
						lowSinceEntry = decimal.MaxValue;
						return;
					}
				}

				// Entry based on EMA
				var bullishCross = prevClose <= prevEma && close > emaVal;
				var bearishCross = prevClose >= prevEma && close < emaVal;

				if (bullishCross && Position <= 0)
				{
					BuyMarket();
					highSinceEntry = candle.HighPrice;
					lowSinceEntry = decimal.MaxValue;
				}
				else if (bearishCross && Position >= 0)
				{
					SellMarket();
					lowSinceEntry = candle.LowPrice;
					highSinceEntry = 0;
				}

				prevClose = close;
				prevEma = emaVal;
			})
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema);
			DrawOwnTrades(area);
		}
	}
}
