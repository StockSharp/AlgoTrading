using System;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Trader Tool Manager strategy: EMA + Momentum trend follower.
/// Buys when close crosses above EMA and momentum > 100.
/// Sells when close crosses below EMA and momentum < 100.
/// </summary>
public class TraderToolEaStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _emaPeriod;
	private readonly StrategyParam<int> _momPeriod;

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

	public int MomPeriod
	{
		get => _momPeriod.Value;
		set => _momPeriod.Value = value;
	}

	public TraderToolEaStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");

		_emaPeriod = Param(nameof(EmaPeriod), 30)
			.SetGreaterThanZero()
			.SetDisplay("EMA Period", "EMA period", "Indicators");

		_momPeriod = Param(nameof(MomPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Momentum", "Momentum period", "Indicators");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var ema = new ExponentialMovingAverage { Length = EmaPeriod };
		var mom = new Momentum { Length = MomPeriod };

		decimal? prevClose = null;
		decimal? prevEma = null;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ema, mom, (candle, emaVal, momVal) =>
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

					if (crossUp && momVal > 100m && Position <= 0)
						BuyMarket();
					else if (crossDown && momVal < 100m && Position >= 0)
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
