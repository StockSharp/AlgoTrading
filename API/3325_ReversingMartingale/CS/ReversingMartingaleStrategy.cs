using System;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Reversing Martingale strategy: WMA crossover.
/// Buys when fast WMA crosses above slow WMA. Sells on cross below.
/// </summary>
public class ReversingMartingaleStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int FastPeriod
	{
		get => _fastPeriod.Value;
		set => _fastPeriod.Value = value;
	}

	public int SlowPeriod
	{
		get => _slowPeriod.Value;
		set => _slowPeriod.Value = value;
	}

	public ReversingMartingaleStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");

		_fastPeriod = Param(nameof(FastPeriod), 10)
			.SetGreaterThanZero()
			.SetDisplay("Fast WMA", "Fast WMA period", "Indicators");

		_slowPeriod = Param(nameof(SlowPeriod), 30)
			.SetGreaterThanZero()
			.SetDisplay("Slow WMA", "Slow WMA period", "Indicators");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var fast = new WeightedMovingAverage { Length = FastPeriod };
		var slow = new WeightedMovingAverage { Length = SlowPeriod };

		decimal? prevFast = null;
		decimal? prevSlow = null;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(fast, slow, (candle, fastVal, slowVal) =>
			{
				if (candle.State != CandleStates.Finished)
					return;

				if (!IsFormedAndOnlineAndAllowTrading())
					return;

				if (prevFast.HasValue && prevSlow.HasValue)
				{
					var crossUp = prevFast.Value <= prevSlow.Value && fastVal > slowVal;
					var crossDown = prevFast.Value >= prevSlow.Value && fastVal < slowVal;

					if (crossUp && Position <= 0)
						BuyMarket();
					else if (crossDown && Position >= 0)
						SellMarket();
				}

				prevFast = fastVal;
				prevSlow = slowVal;
			})
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fast);
			DrawIndicator(area, slow);
			DrawOwnTrades(area);
		}
	}
}
