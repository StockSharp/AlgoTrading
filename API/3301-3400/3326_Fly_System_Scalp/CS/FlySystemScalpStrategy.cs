using System;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Fly System Scalp strategy: EMA crossover + RSI confirmation.
/// Buys when close crosses above EMA and RSI confirms.
/// Sells when close crosses below EMA and RSI confirms.
/// </summary>
public class FlySystemScalpStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _emaPeriod;
	private readonly StrategyParam<int> _rsiPeriod;

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

	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	public FlySystemScalpStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");

		_emaPeriod = Param(nameof(EmaPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("EMA Period", "EMA period", "Indicators");

		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "RSI period", "Indicators");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var ema = new ExponentialMovingAverage { Length = EmaPeriod };
		var rsi = new RelativeStrengthIndex { Length = RsiPeriod };

		decimal? prevClose = null;
		decimal? prevEma = null;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ema, rsi, (candle, emaVal, rsiVal) =>
			{
				if (candle.State != CandleStates.Finished)
					return;

				if (!IsFormedAndOnlineAndAllowTrading())
					return;

				var close = candle.ClosePrice;

				if (prevClose.HasValue && prevEma.HasValue)
				{
					var crossUp = prevClose.Value <= prevEma.Value && close > emaVal;
					var crossDown = prevClose.Value >= prevEma.Value && close < emaVal;

					if (crossUp && rsiVal < 55m && Position <= 0)
						BuyMarket();
					else if (crossDown && rsiVal > 45m && Position >= 0)
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
