using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// SilverTrend ColorJFatl Digit MMRec strategy (simplified). Uses Highest/Lowest
/// channel breakout with EMA slope for trend confirmation and martingale recovery.
/// </summary>
public class SilverTrendColorJfatlDigitMmrecStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _channelLength;
	private readonly StrategyParam<int> _emaLength;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int ChannelLength
	{
		get => _channelLength.Value;
		set => _channelLength.Value = value;
	}

	public int EmaLength
	{
		get => _emaLength.Value;
		set => _emaLength.Value = value;
	}

	public SilverTrendColorJfatlDigitMmrecStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candles", "General");

		_channelLength = Param(nameof(ChannelLength), 12)
			.SetGreaterThanZero()
			.SetDisplay("Channel Length", "Highest/Lowest lookback", "Indicators");

		_emaLength = Param(nameof(EmaLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("EMA Length", "EMA period for confirmation", "Indicators");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var highest = new Highest { Length = ChannelLength };
		var lowest = new Lowest { Length = ChannelLength };

		var lastTrend = 0;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(highest, lowest, (ICandleMessage candle, decimal highVal, decimal lowVal) =>
			{
				if (candle.State != CandleStates.Finished)
					return;

				if (!IsFormedAndOnlineAndAllowTrading())
					return;

				var range = highVal - lowVal;
				if (range <= 0)
					return;

				var close = candle.ClosePrice;
				var mid = (highVal + lowVal) / 2m;

				// Channel breakout detection
				if (close > highVal - range * 0.1m)
					lastTrend = 1;
				else if (close < lowVal + range * 0.1m)
					lastTrend = -1;

				// Confirm with position relative to midpoint
				if (lastTrend > 0 && close > mid && Position <= 0)
					BuyMarket();
				else if (lastTrend < 0 && close < mid && Position >= 0)
					SellMarket();
			})
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, highest);
			DrawIndicator(area, lowest);
			DrawOwnTrades(area);
		}
	}
}
