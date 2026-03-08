using System;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Expert Alligator strategy: Triple SMA (Alligator concept).
/// Buys when lips > teeth > jaw (bullish alignment).
/// Sells when lips < teeth < jaw (bearish alignment).
/// </summary>
public class ExpertAlligatorStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public ExpertAlligatorStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		// Alligator: Jaw=13, Teeth=8, Lips=5
		var lips = new SimpleMovingAverage { Length = 5 };
		var teeth = new SimpleMovingAverage { Length = 8 };
		var jaw = new SimpleMovingAverage { Length = 13 };

		decimal? prevLips = null;
		decimal? prevTeeth = null;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(lips, teeth, jaw, (candle, lipsVal, teethVal, jawVal) =>
			{
				if (candle.State != CandleStates.Finished)
					return;

				if (!IsFormedAndOnlineAndAllowTrading())
					return;

				if (prevLips.HasValue && prevTeeth.HasValue)
				{
					var lipsCrossUp = prevLips.Value <= prevTeeth.Value && lipsVal > teethVal;
					var lipsCrossDown = prevLips.Value >= prevTeeth.Value && lipsVal < teethVal;

					if (lipsCrossUp && teethVal > jawVal && Position <= 0)
						BuyMarket();
					else if (lipsCrossDown && teethVal < jawVal && Position >= 0)
						SellMarket();
				}

				prevLips = lipsVal;
				prevTeeth = teethVal;
			})
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, lips);
			DrawIndicator(area, teeth);
			DrawIndicator(area, jaw);
			DrawOwnTrades(area);
		}
	}
}
