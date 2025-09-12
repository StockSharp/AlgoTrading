using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Simple trend following using two moving averages.
/// Goes long when the fast average is above the slow average and short when below.
/// </summary>
public class TrendFollowingMas3DStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;

	/// <summary>
	/// The type of candles used by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public TrendFollowingMas3DStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Candles for strategy", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var fastMa = new SimpleMovingAverage { Length = 5 };
		var slowMa = new SimpleMovingAverage { Length = 10 };

		var subscription = SubscribeCandles(CandleType);

		decimal? prevDiff = null;

		subscription
			.Bind(fastMa, slowMa, (candle, fast, slow) =>
			{
				if (candle.State != CandleStates.Finished)
					return;

				if (!IsFormedAndOnlineAndAllowTrading())
					return;

				var diff = fast - slow;

				if (prevDiff is null)
				{
					prevDiff = diff;
					return;
				}

				if (prevDiff <= 0 && diff > 0 && Position <= 0)
					BuyMarket(Volume + Math.Abs(Position));
				else if (prevDiff >= 0 && diff < 0 && Position >= 0)
					SellMarket(Volume + Math.Abs(Position));

				prevDiff = diff;
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
