using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Previous candle breakdown strategy (simplified).
/// Enters when price breaks above/below the previous candle's high/low.
/// </summary>
public class PreviousCandleBreakdownStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public PreviousCandleBreakdownStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Candles", "General");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		decimal prevHigh = 0, prevLow = 0;
		bool hasPrev = false;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind((ICandleMessage candle) =>
			{
				if (candle.State != CandleStates.Finished)
					return;

				if (!hasPrev)
				{
					prevHigh = candle.HighPrice;
					prevLow = candle.LowPrice;
					hasPrev = true;
					return;
				}

				if (!IsFormedAndOnlineAndAllowTrading())
				{
					prevHigh = candle.HighPrice;
					prevLow = candle.LowPrice;
					return;
				}

				// Breakout above previous high
				if (candle.ClosePrice > prevHigh && Position <= 0)
				{
					BuyMarket();
				}
				// Breakdown below previous low
				else if (candle.ClosePrice < prevLow && Position >= 0)
				{
					SellMarket();
				}

				prevHigh = candle.HighPrice;
				prevLow = candle.LowPrice;
			})
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}
}
