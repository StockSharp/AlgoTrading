using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Trade Channel Breakout strategy - Highest/Lowest channel breakout.
/// Buys when close crosses above channel midpoint.
/// Sells when close crosses below channel midpoint.
/// </summary>
public class TradeChannelBreakoutStrategy : Strategy
{
	private readonly StrategyParam<int> _channelPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevClose;
	private decimal _prevMid;
	private bool _hasPrev;

	public int ChannelPeriod { get => _channelPeriod.Value; set => _channelPeriod.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public TradeChannelBreakoutStrategy()
	{
		_channelPeriod = Param(nameof(ChannelPeriod), 20)
			.SetDisplay("Channel Period", "Highest/Lowest lookback", "Indicators");
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities() => [(Security, CandleType)];
	protected override void OnReseted() { base.OnReseted(); _prevClose = 0m; _prevMid = 0m; _hasPrev = false; }

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_hasPrev = false;

		var highest = new Highest { Length = ChannelPeriod };
		var lowest = new Lowest { Length = ChannelPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(highest, lowest, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal highest, decimal lowest)
	{
		if (candle.State != CandleStates.Finished) return;

		var close = candle.ClosePrice;
		var mid = (highest + lowest) / 2;

		if (!_hasPrev) { _prevClose = close; _prevMid = mid; _hasPrev = true; return; }

		if (_prevClose <= _prevMid && close > mid && Position <= 0)
		{
			if (Position < 0) BuyMarket();
			BuyMarket();
		}
		else if (_prevClose >= _prevMid && close < mid && Position >= 0)
		{
			if (Position > 0) SellMarket();
			SellMarket();
		}

		_prevClose = close; _prevMid = mid;
	}
}
