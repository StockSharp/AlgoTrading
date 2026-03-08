using System;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Boring EA2 strategy: Triple SMA crossover.
/// Buys when fast crosses above medium while medium above slow.
/// Sells when fast crosses below medium while medium below slow.
/// </summary>
public class BoringEa2Strategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public BoringEa2Strategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var fast = new SimpleMovingAverage { Length = 10 };
		var med = new SimpleMovingAverage { Length = 20 };
		var slow = new SimpleMovingAverage { Length = 40 };

		decimal? prevFast = null;
		decimal? prevMed = null;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(fast, med, slow, (candle, fastVal, medVal, slowVal) =>
			{
				if (candle.State != CandleStates.Finished)
					return;

				if (!IsFormedAndOnlineAndAllowTrading())
					return;

				if (prevFast.HasValue && prevMed.HasValue)
				{
					var fastCrossUp = prevFast.Value <= prevMed.Value && fastVal > medVal;
					var fastCrossDown = prevFast.Value >= prevMed.Value && fastVal < medVal;

					if (fastCrossUp && medVal > slowVal && Position <= 0)
						BuyMarket();
					else if (fastCrossDown && medVal < slowVal && Position >= 0)
						SellMarket();
				}

				prevFast = fastVal;
				prevMed = medVal;
			})
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fast);
			DrawIndicator(area, med);
			DrawIndicator(area, slow);
			DrawOwnTrades(area);
		}
	}
}
