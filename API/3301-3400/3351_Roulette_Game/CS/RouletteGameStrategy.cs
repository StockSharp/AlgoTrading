namespace StockSharp.Samples.Strategies;

using System;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// Roulette Game strategy: random-like entries based on candle direction with SMA filter.
/// Buys when candle is bullish and close above SMA. Sells when bearish and below SMA.
/// </summary>
public class RouletteGameStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _smaPeriod;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int SmaPeriod
	{
		get => _smaPeriod.Value;
		set => _smaPeriod.Value = value;
	}

	public RouletteGameStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");

		_smaPeriod = Param(nameof(SmaPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("SMA Period", "SMA period", "Indicators");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var sma = new SimpleMovingAverage { Length = SmaPeriod };

		decimal? prevClose = null;
		decimal? prevSma = null;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(sma, (candle, smaVal) =>
			{
				if (candle.State != CandleStates.Finished)
					return;

				if (!IsFormedAndOnlineAndAllowTrading())
					return;

				var close = candle.ClosePrice;
				var isBullish = close > candle.OpenPrice;

				if (prevClose.HasValue && prevSma.HasValue)
				{
					var crossUp = prevClose.Value <= prevSma.Value && close > smaVal;
					var crossDown = prevClose.Value >= prevSma.Value && close < smaVal;

					if (isBullish && crossUp && Position <= 0)
						BuyMarket();
					else if (!isBullish && crossDown && Position >= 0)
						SellMarket();
				}

				prevClose = close;
				prevSma = smaVal;
			})
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, sma);
			DrawOwnTrades(area);
		}
	}
}
