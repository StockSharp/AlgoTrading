using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Blau TVI timed reversal strategy (simplified). Uses Momentum indicator
/// to detect slope changes and generate reversal entries.
/// </summary>
public class BlauTviTimedReversalStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _momentumLength;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int MomentumLength
	{
		get => _momentumLength.Value;
		set => _momentumLength.Value = value;
	}

	public BlauTviTimedReversalStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Candles", "General");

		_momentumLength = Param(nameof(MomentumLength), 12)
			.SetGreaterThanZero()
			.SetDisplay("Momentum Length", "Momentum period", "Indicators");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var momentum = new Momentum { Length = MomentumLength };

		decimal prevMom = 0;
		decimal prevPrevMom = 0;
		var count = 0;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(momentum, (ICandleMessage candle, decimal momValue) =>
			{
				if (candle.State != CandleStates.Finished)
					return;

				count++;
				if (count < 3)
				{
					prevPrevMom = prevMom;
					prevMom = momValue;
					return;
				}

				if (!IsFormedAndOnlineAndAllowTrading())
				{
					prevPrevMom = prevMom;
					prevMom = momValue;
					return;
				}

				// Detect slope reversal: was falling, now rising => buy
				if (prevMom < prevPrevMom && momValue > prevMom && Position <= 0)
					BuyMarket();
				// Was rising, now falling => sell
				else if (prevMom > prevPrevMom && momValue < prevMom && Position >= 0)
					SellMarket();

				prevPrevMom = prevMom;
				prevMom = momValue;
			})
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, momentum);
			DrawOwnTrades(area);
		}
	}
}
