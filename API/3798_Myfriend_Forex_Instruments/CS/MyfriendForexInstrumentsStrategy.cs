using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// MyFriend Forex strategy - Donchian channel breakout with SMA momentum filter.
/// Buys when close breaks above upper Donchian and fast SMA > slow SMA.
/// Sells when close breaks below lower Donchian and fast SMA less than slow SMA.
/// </summary>
public class MyfriendForexInstrumentsStrategy : Strategy
{
	private readonly StrategyParam<int> _channelPeriod;
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevClose;
	private decimal _prevUpper;
	private decimal _prevLower;
	private bool _hasPrev;

	public int ChannelPeriod { get => _channelPeriod.Value; set => _channelPeriod.Value = value; }
	public int FastPeriod { get => _fastPeriod.Value; set => _fastPeriod.Value = value; }
	public int SlowPeriod { get => _slowPeriod.Value; set => _slowPeriod.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public MyfriendForexInstrumentsStrategy()
	{
		_channelPeriod = Param(nameof(ChannelPeriod), 16)
			.SetDisplay("Channel Period", "Donchian channel period", "Indicators");

		_fastPeriod = Param(nameof(FastPeriod), 3)
			.SetDisplay("Fast SMA", "Fast SMA period", "Indicators");

		_slowPeriod = Param(nameof(SlowPeriod), 9)
			.SetDisplay("Slow SMA", "Slow SMA period", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevClose = 0m;
		_prevUpper = 0m;
		_prevLower = 0m;
		_hasPrev = false;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_hasPrev = false;

		var highest = new Highest { Length = ChannelPeriod };
		var lowest = new Lowest { Length = ChannelPeriod };
		var fastSma = new SimpleMovingAverage { Length = FastPeriod };
		var slowSma = new SimpleMovingAverage { Length = SlowPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(highest, lowest, fastSma, slowSma, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal high, decimal low, decimal fast, decimal slow)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var close = candle.ClosePrice;

		if (!_hasPrev)
		{
			_prevClose = close;
			_prevUpper = high;
			_prevLower = low;
			_hasPrev = true;
			return;
		}

		// Breakout above channel with bullish momentum
		if (_prevClose <= _prevUpper && close > high && fast > slow && Position <= 0)
		{
			if (Position < 0)
				BuyMarket();
			BuyMarket();
		}
		// Breakout below channel with bearish momentum
		else if (_prevClose >= _prevLower && close < low && fast < slow && Position >= 0)
		{
			if (Position > 0)
				SellMarket();
			SellMarket();
		}
		// Mean reversion: close crosses midpoint
		else
		{
			var mid = (high + low) / 2m;
			var prevMid = (_prevUpper + _prevLower) / 2m;

			if (_prevClose <= prevMid && close > mid && fast > slow && Position <= 0)
			{
				if (Position < 0)
					BuyMarket();
				BuyMarket();
			}
			else if (_prevClose >= prevMid && close < mid && fast < slow && Position >= 0)
			{
				if (Position > 0)
					SellMarket();
				SellMarket();
			}
		}

		_prevClose = close;
		_prevUpper = high;
		_prevLower = low;
	}
}
