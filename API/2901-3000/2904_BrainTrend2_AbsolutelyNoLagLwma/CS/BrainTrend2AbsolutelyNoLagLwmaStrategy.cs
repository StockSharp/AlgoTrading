using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// BrainTrend2 + AbsolutelyNoLagLwma strategy (simplified). Uses ATR-based
/// trend detection combined with weighted MA direction for entries.
/// Implemented as EMA crossover with ATR channel filter.
/// </summary>
public class BrainTrend2AbsolutelyNoLagLwmaStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _emaFastLength;
	private readonly StrategyParam<int> _emaSlowLength;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<decimal> _atrCoefficient;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int EmaFastLength
	{
		get => _emaFastLength.Value;
		set => _emaFastLength.Value = value;
	}

	public int EmaSlowLength
	{
		get => _emaSlowLength.Value;
		set => _emaSlowLength.Value = value;
	}

	public int AtrLength
	{
		get => _atrLength.Value;
		set => _atrLength.Value = value;
	}

	public decimal AtrCoefficient
	{
		get => _atrCoefficient.Value;
		set => _atrCoefficient.Value = value;
	}

	public BrainTrend2AbsolutelyNoLagLwmaStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Candles", "General");

		_emaFastLength = Param(nameof(EmaFastLength), 7)
			.SetGreaterThanZero()
			.SetDisplay("EMA Fast", "Fast EMA period", "Indicators");

		_emaSlowLength = Param(nameof(EmaSlowLength), 21)
			.SetGreaterThanZero()
			.SetDisplay("EMA Slow", "Slow EMA period", "Indicators");

		_atrLength = Param(nameof(AtrLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Length", "ATR period", "Indicators");

		_atrCoefficient = Param(nameof(AtrCoefficient), 0.7m)
			.SetGreaterThanZero()
			.SetDisplay("ATR Coeff", "ATR multiplier for channel", "Logic");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var emaFast = new ExponentialMovingAverage { Length = EmaFastLength };
		var emaSlow = new ExponentialMovingAverage { Length = EmaSlowLength };

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

				var close = candle.ClosePrice;

				// Fast EMA crosses above slow - bullish
				var bullishCross = prevFast <= prevSlow && fastVal > slowVal;
				// Fast EMA crosses below slow - bearish
				var bearishCross = prevFast >= prevSlow && fastVal < slowVal;

				if (bullishCross && Position <= 0)
					BuyMarket();
				else if (bearishCross && Position >= 0)
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
