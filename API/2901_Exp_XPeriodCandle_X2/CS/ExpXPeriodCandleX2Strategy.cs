using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Exp XPeriodCandle X2 strategy (simplified). Uses candle body analysis
/// with EMA trend filter for breakout entries.
/// </summary>
public class ExpXPeriodCandleX2Strategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _emaLength;

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

	public ExpXPeriodCandleX2Strategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candles", "General");

		_emaLength = Param(nameof(EmaLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("EMA Length", "Trend EMA period", "Indicators");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var ema = new ExponentialMovingAverage { Length = EmaLength };

		decimal prevClose = 0, prevOpen = 0;
		var hasPrev = false;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ema, (ICandleMessage candle, decimal emaValue) =>
			{
				if (candle.State != CandleStates.Finished)
					return;

				if (!hasPrev)
				{
					prevClose = candle.ClosePrice;
					prevOpen = candle.OpenPrice;
					hasPrev = true;
					return;
				}

				if (!IsFormedAndOnlineAndAllowTrading())
				{
					prevClose = candle.ClosePrice;
					prevOpen = candle.OpenPrice;
					return;
				}

				var close = candle.ClosePrice;

				// Two consecutive bullish candles above EMA
				if (prevClose > prevOpen && close > candle.OpenPrice && close > emaValue && Position <= 0)
					BuyMarket();
				// Two consecutive bearish candles below EMA
				else if (prevClose < prevOpen && close < candle.OpenPrice && close < emaValue && Position >= 0)
					SellMarket();

				prevClose = close;
				prevOpen = candle.OpenPrice;
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
