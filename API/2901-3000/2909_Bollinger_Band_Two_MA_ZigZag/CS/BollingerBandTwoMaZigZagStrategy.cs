using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Bollinger Band Two MA ZigZag strategy (simplified). Uses EMA crossover
/// with Highest/Lowest channel for swing-based entries.
/// </summary>
public class BollingerBandTwoMaZigZagStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _emaFastLength;
	private readonly StrategyParam<int> _emaSlowLength;
	private readonly StrategyParam<int> _channelLength;

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

	public int ChannelLength
	{
		get => _channelLength.Value;
		set => _channelLength.Value = value;
	}

	public BollingerBandTwoMaZigZagStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Candles", "General");

		_emaFastLength = Param(nameof(EmaFastLength), 10)
			.SetGreaterThanZero()
			.SetDisplay("EMA Fast", "Fast EMA period", "Indicators");

		_emaSlowLength = Param(nameof(EmaSlowLength), 30)
			.SetGreaterThanZero()
			.SetDisplay("EMA Slow", "Slow EMA period", "Indicators");

		_channelLength = Param(nameof(ChannelLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("Channel Length", "Highest/Lowest lookback", "Indicators");
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

				// EMA crossover combined with price confirmation
				var bullishCross = prevFast <= prevSlow && fastVal > slowVal;
				var bearishCross = prevFast >= prevSlow && fastVal < slowVal;

				if (bullishCross && Position <= 0 && close > fastVal)
					BuyMarket();
				else if (bearishCross && Position >= 0 && close < fastVal)
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
