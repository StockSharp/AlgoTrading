using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// One MA channel breakout strategy.
/// Places an SMA channel around price with configurable offset, trades breakouts.
/// </summary>
public class OneMaChannelBreakoutStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<decimal> _channelOffset;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int MaPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	public decimal ChannelOffset
	{
		get => _channelOffset.Value;
		set => _channelOffset.Value = value;
	}

	public OneMaChannelBreakoutStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe", "General");

		_maPeriod = Param(nameof(MaPeriod), 44)
			.SetGreaterThanZero()
			.SetDisplay("MA Period", "Moving average length", "Indicators");

		_channelOffset = Param(nameof(ChannelOffset), 0.005m)
			.SetGreaterThanZero()
			.SetDisplay("Channel Offset", "Percentage offset for channel", "Indicators");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var sma = new SimpleMovingAverage { Length = MaPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(sma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, sma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal maValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var upper = maValue * (1 + ChannelOffset);
		var lower = maValue * (1 - ChannelOffset);
		var close = candle.ClosePrice;

		// Bullish breakout above upper channel
		if (close > upper && Position <= 0)
		{
			if (Position < 0)
				BuyMarket();
			BuyMarket();
		}
		// Bearish breakdown below lower channel
		else if (close < lower && Position >= 0)
		{
			if (Position > 0)
				SellMarket();
			SellMarket();
		}
	}
}
