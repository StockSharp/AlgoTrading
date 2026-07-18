using System;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Moving average crossover strategy using two EMAs.
/// Enters long on golden cross, short on death cross.
/// </summary>
public class MovingAverageCrossoverSpreadStrategy : Strategy
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

	public MovingAverageCrossoverSpreadStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(60).TimeFrame())
			.SetDisplay("Candle Type", "Candle type", "General");

		_fastPeriod = Param(nameof(FastPeriod), 20)
			.SetDisplay("Fast EMA", "Fast EMA period", "Indicators");

		_slowPeriod = Param(nameof(SlowPeriod), 50)
			.SetDisplay("Slow EMA", "Slow EMA period", "Indicators");
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var fastMa = new ExponentialMovingAverage { Length = FastPeriod };
		var slowMa = new ExponentialMovingAverage { Length = SlowPeriod };

		decimal? prevFast = null;
		decimal? prevSlow = null;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(fastMa, slowMa, (candle, fastValue, slowValue) =>
			{
				if (candle.State != CandleStates.Finished)
					return;

				if (!IsFormedAndOnlineAndAllowTrading())
					return;

				if (prevFast.HasValue && prevSlow.HasValue)
				{
					var crossUp = prevFast.Value <= prevSlow.Value && fastValue > slowValue;
					var crossDown = prevFast.Value >= prevSlow.Value && fastValue < slowValue;

					if (crossUp && Position <= 0)
						BuyMarket();
					else if (crossDown && Position >= 0)
						SellMarket();
				}

				prevFast = fastValue;
				prevSlow = slowValue;
			})
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fastMa);
			DrawIndicator(area, slowMa);
			DrawOwnTrades(area);
		}
	}
}
