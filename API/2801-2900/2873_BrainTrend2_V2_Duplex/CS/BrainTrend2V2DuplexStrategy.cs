using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// BrainTrend2 V2 Duplex strategy (simplified).
/// Uses ATR-based channel breakout for trend detection.
/// </summary>
public class BrainTrend2V2DuplexStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<int> _emaPeriod;
	private readonly StrategyParam<decimal> _channelMult;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	public int EmaPeriod
	{
		get => _emaPeriod.Value;
		set => _emaPeriod.Value = value;
	}

	public decimal ChannelMult
	{
		get => _channelMult.Value;
		set => _channelMult.Value = value;
	}

	public BrainTrend2V2DuplexStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Candles", "General");

		_atrPeriod = Param(nameof(AtrPeriod), 7)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "ATR length", "Indicators");

		_emaPeriod = Param(nameof(EmaPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("EMA Period", "EMA length for trend", "Indicators");

		_channelMult = Param(nameof(ChannelMult), 2.5m)
			.SetGreaterThanZero()
			.SetDisplay("Channel Mult", "ATR multiplier for channel width", "Indicators");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var atr = new AverageTrueRange { Length = AtrPeriod };
		var ema = new ExponentialMovingAverage { Length = EmaPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(atr, ema, (ICandleMessage candle, decimal atrValue, decimal emaValue) =>
			{
				if (candle.State != CandleStates.Finished)
					return;

				if (!IsFormedAndOnlineAndAllowTrading())
					return;

				var close = candle.ClosePrice;
				var upper = emaValue + ChannelMult * atrValue;
				var lower = emaValue - ChannelMult * atrValue;

				// Buy when close breaks above upper channel
				if (close > upper && Position <= 0)
				{
					BuyMarket();
				}
				// Sell when close breaks below lower channel
				else if (close < lower && Position >= 0)
				{
					SellMarket();
				}
			})
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema);
			DrawOwnTrades(area);
		}
	}
}
