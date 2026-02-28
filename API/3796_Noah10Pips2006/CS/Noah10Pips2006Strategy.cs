using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Noah 10 Pips 2006 - session breakout strategy.
/// Tracks the previous session's high/low range.
/// Buys on breakout above the midpoint, sells on breakout below.
/// Uses Highest/Lowest indicators as channel reference.
/// </summary>
public class Noah10Pips2006Strategy : Strategy
{
	private readonly StrategyParam<int> _channelPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevHigh;
	private decimal _prevLow;
	private decimal _prevMid;
	private decimal _prevClose;
	private bool _hasPrev;

	public int ChannelPeriod { get => _channelPeriod.Value; set => _channelPeriod.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public Noah10Pips2006Strategy()
	{
		_channelPeriod = Param(nameof(ChannelPeriod), 24)
			.SetDisplay("Channel Period", "Lookback for high/low channel", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_hasPrev = false;

		var highest = new Highest { Length = ChannelPeriod };
		var lowest = new Lowest { Length = ChannelPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(highest, lowest, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal high, decimal low)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var close = candle.ClosePrice;
		var mid = (high + low) / 2m;

		if (!_hasPrev)
		{
			_prevHigh = high;
			_prevLow = low;
			_prevMid = mid;
			_prevClose = close;
			_hasPrev = true;
			return;
		}

		// Breakout above channel high - buy
		if (_prevClose <= _prevHigh && close > high && Position <= 0)
		{
			if (Position < 0)
				BuyMarket();
			BuyMarket();
		}
		// Breakout below channel low - sell
		else if (_prevClose >= _prevLow && close < low && Position >= 0)
		{
			if (Position > 0)
				SellMarket();
			SellMarket();
		}
		// Cross above midpoint from below - buy signal
		else if (_prevClose <= _prevMid && close > mid && Position <= 0)
		{
			if (Position < 0)
				BuyMarket();
			BuyMarket();
		}
		// Cross below midpoint from above - sell signal
		else if (_prevClose >= _prevMid && close < mid && Position >= 0)
		{
			if (Position > 0)
				SellMarket();
			SellMarket();
		}

		_prevHigh = high;
		_prevLow = low;
		_prevMid = mid;
		_prevClose = close;
	}
}
