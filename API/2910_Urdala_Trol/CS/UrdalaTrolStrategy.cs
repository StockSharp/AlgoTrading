using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Urdala Trol strategy (simplified). Uses EMA with trailing stop logic
/// for grid-style entries based on trend direction.
/// </summary>
public class UrdalaTrolStrategy : Strategy
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

	public UrdalaTrolStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candles", "General");

		_emaLength = Param(nameof(EmaLength), 14)
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

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ema, (ICandleMessage candle, decimal emaVal) =>
			{
				if (candle.State != CandleStates.Finished)
					return;

				if (!IsFormedAndOnlineAndAllowTrading())
					return;

				var close = candle.ClosePrice;
				var high = candle.HighPrice;
				var low = candle.LowPrice;

				// Track trailing stop levels
				if (Position > 0)
				{
					if (high > highSinceEntry)
						highSinceEntry = high;

					var trailStop = highSinceEntry * (1m - TrailingPercent / 100m);
					if (close < trailStop)
					{
						SellMarket();
						highSinceEntry = 0;
						lowSinceEntry = decimal.MaxValue;
						return;
					}
				}
				else if (Position < 0)
				{
					if (low < lowSinceEntry)
						lowSinceEntry = low;

					var trailStop = lowSinceEntry * (1m + TrailingPercent / 100m);
					if (close > trailStop)
					{
						BuyMarket();
						highSinceEntry = 0;
						lowSinceEntry = decimal.MaxValue;
						return;
					}
				}

				// Entry signals based on EMA
				if (close > emaVal && Position <= 0)
				{
					BuyMarket();
					highSinceEntry = high;
					lowSinceEntry = decimal.MaxValue;
				}
				else if (close < emaVal && Position >= 0)
				{
					SellMarket();
					lowSinceEntry = low;
					highSinceEntry = 0;
				}
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
