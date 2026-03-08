namespace StockSharp.Samples.Strategies;

using System;
using Ecng.Common;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// X-MAN strategy: Stochastic + SMA trend filter.
/// </summary>
public class XManStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _smaPeriod;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int SmaPeriod { get => _smaPeriod.Value; set => _smaPeriod.Value = value; }

	public XManStrategy()
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
		var rsi = new RelativeStrengthIndex { Length = 14 };

		decimal? prevClose = null;
		decimal? prevSma = null;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(sma, rsi, (candle, smaVal, rsiVal) =>
			{
				if (candle.State != CandleStates.Finished)
					return;

				if (!IsFormedAndOnlineAndAllowTrading())
					return;

				var close = candle.ClosePrice;

				if (prevClose.HasValue && prevSma.HasValue)
				{
					var crossUp = prevClose.Value <= prevSma.Value && close > smaVal;
					var crossDown = prevClose.Value >= prevSma.Value && close < smaVal;

					if (crossUp && rsiVal < 70 && Position <= 0)
						BuyMarket();
					else if (crossDown && rsiVal > 30 && Position >= 0)
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
