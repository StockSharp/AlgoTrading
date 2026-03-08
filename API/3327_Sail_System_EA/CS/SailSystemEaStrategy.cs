using System;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Sail System EA strategy: Momentum + SMA crossover trend.
/// Buys when close crosses above SMA and momentum confirms.
/// Sells when close crosses below SMA and momentum confirms.
/// </summary>
public class SailSystemEaStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _smaPeriod;
	private readonly StrategyParam<int> _momPeriod;

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

	public int MomPeriod
	{
		get => _momPeriod.Value;
		set => _momPeriod.Value = value;
	}

	public SailSystemEaStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");

		_smaPeriod = Param(nameof(SmaPeriod), 30)
			.SetGreaterThanZero()
			.SetDisplay("SMA Period", "SMA period", "Indicators");

		_momPeriod = Param(nameof(MomPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Momentum", "Momentum period", "Indicators");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var sma = new SimpleMovingAverage { Length = SmaPeriod };
		var mom = new Momentum { Length = MomPeriod };

		decimal? prevClose = null;
		decimal? prevSma = null;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(sma, mom, (candle, smaVal, momVal) =>
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

					if (crossUp && momVal > 100m && Position <= 0)
						BuyMarket();
					else if (crossDown && momVal < 100m && Position >= 0)
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
