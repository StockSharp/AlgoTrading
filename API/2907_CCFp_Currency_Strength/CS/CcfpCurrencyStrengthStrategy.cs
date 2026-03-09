using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// CCFp Currency Strength strategy (simplified). Uses fast/slow EMA crossover
/// to detect momentum shifts, inspired by currency strength comparison.
/// </summary>
public class CcfpCurrencyStrengthStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int FastLength
	{
		get => _fastLength.Value;
		set => _fastLength.Value = value;
	}

	public int SlowLength
	{
		get => _slowLength.Value;
		set => _slowLength.Value = value;
	}

	public CcfpCurrencyStrengthStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Candles", "General");

		_fastLength = Param(nameof(FastLength), 5)
			.SetGreaterThanZero()
			.SetDisplay("Fast Length", "Fast EMA period", "Indicators");

		_slowLength = Param(nameof(SlowLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("Slow Length", "Slow EMA period", "Indicators");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var emaFast = new ExponentialMovingAverage { Length = FastLength };
		var emaSlow = new ExponentialMovingAverage { Length = SlowLength };

		decimal prevFast = 0, prevSlow = 0;
		var hasPrev = false;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(emaFast, emaSlow, (ICandleMessage candle, decimal fastVal, decimal slowVal) =>
			{
				if (candle.State != CandleStates.Finished)
					return;

				if (!hasPrev)
				{
					prevFast = fastVal;
					prevSlow = slowVal;
					hasPrev = true;
					return;
				}

				if (!IsFormedAndOnlineAndAllowTrading())
				{
					prevFast = fastVal;
					prevSlow = slowVal;
					return;
				}

				// Fast EMA crosses above slow - bullish
				if (prevFast <= prevSlow && fastVal > slowVal && Position <= 0)
					BuyMarket();
				// Fast EMA crosses below slow - bearish
				else if (prevFast >= prevSlow && fastVal < slowVal && Position >= 0)
					SellMarket();

				prevFast = fastVal;
				prevSlow = slowVal;
			})
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, emaFast);
			DrawIndicator(area, emaSlow);
			DrawOwnTrades(area);
		}
	}
}
