using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// SilverTrend CrazyChart strategy (simplified).
/// Uses Highest/Lowest channel inversions for entries.
/// </summary>
public class SilverTrendCrazyChartStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _length;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int Length
	{
		get => _length.Value;
		set => _length.Value = value;
	}

	public SilverTrendCrazyChartStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candles", "General");

		_length = Param(nameof(Length), 14)
			.SetGreaterThanZero()
			.SetDisplay("Length", "Channel length", "Indicators");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var highest = new Highest { Length = Length };
		var lowest = new Lowest { Length = Length };

		decimal prevHigh = 0, prevLow = 0;
		bool hasPrev = false;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(highest, lowest, (ICandleMessage candle, decimal highValue, decimal lowValue) =>
			{
				if (candle.State != CandleStates.Finished)
					return;

				if (!hasPrev)
				{
					prevHigh = highValue;
					prevLow = lowValue;
					hasPrev = true;
					return;
				}

				if (!IsFormedAndOnlineAndAllowTrading())
				{
					prevHigh = highValue;
					prevLow = lowValue;
					return;
				}

				var close = candle.ClosePrice;
				var mid = (highValue + lowValue) / 2m;
				var prevMid = (prevHigh + prevLow) / 2m;

				// Price crosses above channel midpoint
				if (close > mid && candle.OpenPrice <= prevMid && Position <= 0)
				{
					BuyMarket();
				}
				// Price crosses below channel midpoint
				else if (close < mid && candle.OpenPrice >= prevMid && Position >= 0)
				{
					SellMarket();
				}

				prevHigh = highValue;
				prevLow = lowValue;
			})
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, highest);
			DrawIndicator(area, lowest);
			DrawOwnTrades(area);
		}
	}
}
