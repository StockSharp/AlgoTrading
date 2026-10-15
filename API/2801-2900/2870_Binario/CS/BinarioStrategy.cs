using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Binario strategy (simplified). Uses two EMAs as channel and trades breakouts.
/// </summary>
public class BinarioStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _maPeriod;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int MaPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	public BinarioStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Candles", "General");

		_maPeriod = Param(nameof(MaPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("MA Period", "Moving average length", "Indicators");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var highest = new Highest { Length = MaPeriod };
		var lowest = new Lowest { Length = MaPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(highest, lowest, (ICandleMessage candle, decimal highValue, decimal lowValue) =>
			{
				if (candle.State != CandleStates.Finished)
					return;

				if (!IsFormedAndOnlineAndAllowTrading())
					return;

				var close = candle.ClosePrice;
				var mid = (highValue + lowValue) / 2m;
				var channelPadding = (highValue - lowValue) * 0.1m;

				// Buy on breakout above channel midpoint
				if (close > mid + channelPadding && Position <= 0)
				{
					BuyMarket();
				}
				// Sell on breakout below channel midpoint
				else if (close < mid - channelPadding && Position >= 0)
				{
					SellMarket();
				}
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
