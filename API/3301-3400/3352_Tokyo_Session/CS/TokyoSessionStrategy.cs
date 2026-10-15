namespace StockSharp.Samples.Strategies;

using System;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// Tokyo Session strategy: breakout from session range.
/// Tracks high/low during first hours, then buys breakout above high, sells below low.
/// </summary>
public class TokyoSessionStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _atrPeriod;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	public TokyoSessionStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");

		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "ATR period for volatility filter", "Indicators");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var atr = new AverageTrueRange { Length = AtrPeriod };

		var sessionHigh = decimal.MinValue;
		var sessionLow = decimal.MaxValue;
		var candleCount = 0;
		var rangeSet = false;
		decimal? prevClose = null;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(atr, (candle, atrVal) =>
			{
				if (candle.State != CandleStates.Finished)
					return;

				if (!IsFormedAndOnlineAndAllowTrading())
					return;

				candleCount++;

				// Build range from first 4 candles (2 hours of 30min candles)
				if (candleCount <= 4)
				{
					if (candle.HighPrice > sessionHigh)
						sessionHigh = candle.HighPrice;
					if (candle.LowPrice < sessionLow)
						sessionLow = candle.LowPrice;

					if (candleCount == 4)
						rangeSet = true;

					prevClose = candle.ClosePrice;
					return;
				}

				if (!rangeSet)
				{
					prevClose = candle.ClosePrice;
					return;
				}

				// Reset range every 48 candles (24 hours of 30min candles)
				if (candleCount % 48 == 0)
				{
					sessionHigh = candle.HighPrice;
					sessionLow = candle.LowPrice;
					rangeSet = false;
					candleCount = 0;
					prevClose = candle.ClosePrice;
					return;
				}

				var close = candle.ClosePrice;

				if (prevClose.HasValue)
				{
					var crossAboveHigh = prevClose.Value <= sessionHigh && close > sessionHigh;
					var crossBelowLow = prevClose.Value >= sessionLow && close < sessionLow;

					if (crossAboveHigh && Position <= 0)
						BuyMarket();
					else if (crossBelowLow && Position >= 0)
						SellMarket();
				}

				prevClose = close;
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
