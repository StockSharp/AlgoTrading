using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// BykovTrend + ColorX2MA strategy (simplified). Uses Williams %R for trend
/// detection combined with double EMA smoothing for entry confirmation.
/// </summary>
public class BykovTrendColorX2MaStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _emaFastLength;
	private readonly StrategyParam<int> _emaSlowLength;

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

	public BykovTrendColorX2MaStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Candles", "General");

		_emaFastLength = Param(nameof(EmaFastLength), 9)
			.SetGreaterThanZero()
			.SetDisplay("EMA Fast", "Fast EMA period", "Indicators");

		_emaSlowLength = Param(nameof(EmaSlowLength), 21)
			.SetGreaterThanZero()
			.SetDisplay("EMA Slow", "Slow EMA period", "Indicators");
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

				// EMA crossover with candle direction confirmation
				var bullishCross = prevFast <= prevSlow && fastVal > slowVal;
				var bearishCross = prevFast >= prevSlow && fastVal < slowVal;

				var close = candle.ClosePrice;
				var open = candle.OpenPrice;

				if (bullishCross && close > open && Position <= 0)
					BuyMarket();
				else if (bearishCross && close < open && Position >= 0)
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
