using System;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Bull/Bear Candle Martingale strategy: Bullish/bearish candle direction + EMA filter.
/// Buys after strong bullish candle when close crosses above EMA.
/// Sells after strong bearish candle when close crosses below EMA.
/// </summary>
public class BullBearCandleMartingaleStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _emaPeriod;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int EmaPeriod
	{
		get => _emaPeriod.Value;
		set => _emaPeriod.Value = value;
	}

	public BullBearCandleMartingaleStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");

		_emaPeriod = Param(nameof(EmaPeriod), 30)
			.SetGreaterThanZero()
			.SetDisplay("EMA Period", "EMA period", "Indicators");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var ema = new ExponentialMovingAverage { Length = EmaPeriod };

		decimal? prevClose = null;
		decimal? prevEma = null;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ema, (candle, emaVal) =>
			{
				if (candle.State != CandleStates.Finished)
					return;

				if (!IsFormedAndOnlineAndAllowTrading())
					return;

				var close = candle.ClosePrice;
				var bullish = close > candle.OpenPrice;
				var bearish = close < candle.OpenPrice;
				var bodySize = Math.Abs(close - candle.OpenPrice);
				var range = candle.HighPrice - candle.LowPrice;

				// Require strong candle body (>50% of range)
				var strongCandle = range > 0 && bodySize / range > 0.5m;

				if (prevClose.HasValue && prevEma.HasValue && strongCandle)
				{
					var crossUp = prevClose.Value <= prevEma.Value && close > emaVal;
					var crossDown = prevClose.Value >= prevEma.Value && close < emaVal;

					if (bullish && crossUp && Position <= 0)
						BuyMarket();
					else if (bearish && crossDown && Position >= 0)
						SellMarket();
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
