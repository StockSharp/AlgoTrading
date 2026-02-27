using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Daydream Channel Breakout strategy. Uses Highest/Lowest channel breakout.
/// </summary>
public class DaydreamChannelBreakoutStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _channelPeriod;
	private decimal? _prevHigh;
	private decimal? _prevLow;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int ChannelPeriod { get => _channelPeriod.Value; set => _channelPeriod.Value = value; }

	public DaydreamChannelBreakoutStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame()).SetDisplay("Candle Type", "Timeframe", "General");
		_channelPeriod = Param(nameof(ChannelPeriod), 25).SetGreaterThanZero().SetDisplay("Channel Period", "Donchian lookback", "Indicators");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities() => [(Security, CandleType)];

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_prevHigh = null; _prevLow = null;
		var highest = new Highest { Length = ChannelPeriod };
		var lowest = new Lowest { Length = ChannelPeriod };
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(highest, lowest, ProcessCandle).Start();
		var area = CreateChartArea();
		if (area != null) { DrawCandles(area, subscription); DrawOwnTrades(area); }
	}

	private void ProcessCandle(ICandleMessage candle, decimal high, decimal low)
	{
		if (candle.State != CandleStates.Finished) return;
		if (_prevHigh == null || _prevLow == null) { _prevHigh = high; _prevLow = low; return; }
		var close = candle.ClosePrice;
		if (close > _prevHigh.Value && Position <= 0) { if (Position < 0) BuyMarket(); BuyMarket(); }
		else if (close < _prevLow.Value && Position >= 0) { if (Position > 0) SellMarket(); SellMarket(); }
		_prevHigh = high; _prevLow = low;
	}
}
