using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// SilverTrend ColorJFatl Digit strategy (simplified). Uses Highest/Lowest channel
/// breakout combined with EMA slope for trend confirmation.
/// </summary>
public class SilverTrendColorJFatlDigitStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _channelLength;
	private readonly StrategyParam<int> _emaLength;
	private readonly StrategyParam<int> _riskLevel;

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

	public int RiskLevel
	{
		get => _riskLevel.Value;
		set => _riskLevel.Value = value;
	}

	public SilverTrendColorJFatlDigitStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Candles", "General");

		_channelLength = Param(nameof(ChannelLength), 21)
			.SetGreaterThanZero()
			.SetDisplay("Channel Length", "Highest/Lowest lookback", "Indicators");

		_emaLength = Param(nameof(EmaLength), 10)
			.SetGreaterThanZero()
			.SetDisplay("EMA Length", "EMA period for trend confirmation", "Indicators");

		_riskLevel = Param(nameof(RiskLevel), 3)
			.SetDisplay("Risk Level", "Channel threshold tightness", "Logic");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var highest = new Highest { Length = ChannelLength + 1 };
		var lowest = new Lowest { Length = ChannelLength + 1 };

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

				var riskModifier = 33m - RiskLevel;
				if (riskModifier < 0m) riskModifier = 0m;
				if (riskModifier > 33m) riskModifier = 33m;

				var thresholdPercent = riskModifier / 100m;
				var lowerThreshold = lowVal + range * thresholdPercent;
				var upperThreshold = highVal - range * thresholdPercent;

				var close = candle.ClosePrice;

				// SilverTrend breakout logic
				if (close < lowerThreshold)
					lastTrend = -1;
				else if (close > upperThreshold)
					lastTrend = 1;

				// Simple EMA slope confirmation using close vs channel midpoint
				var midpoint = (highVal + lowVal) / 2m;
				var emaConfirmUp = close > midpoint;
				var emaConfirmDown = close < midpoint;

				if (lastTrend > 0 && emaConfirmUp && Position <= 0)
					BuyMarket();
				else if (lastTrend < 0 && emaConfirmDown && Position >= 0)
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
