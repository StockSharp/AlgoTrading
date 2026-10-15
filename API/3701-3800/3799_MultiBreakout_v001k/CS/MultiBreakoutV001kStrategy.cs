using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Multi-breakout strategy - Highest/Lowest channel breakout.
/// Buys when price breaks above the channel high, sells when below channel low.
/// Reverses position on opposite breakout.
/// </summary>
public class MultiBreakoutV001kStrategy : Strategy
{
	private readonly StrategyParam<int> _channelPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevClose;
	private decimal _prevHigh;
	private decimal _prevLow;
	private bool _hasPrev;

	public int ChannelPeriod { get => _channelPeriod.Value; set => _channelPeriod.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public MultiBreakoutV001kStrategy()
	{
		_channelPeriod = Param(nameof(ChannelPeriod), 20)
			.SetDisplay("Channel Period", "Lookback for breakout channel", "Indicators");

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
		_prevHigh = 0m;
		_prevLow = 0m;
		_hasPrev = false;
	}

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
			_prevClose = close;
			_prevHigh = mid;
			_prevLow = mid;
			_hasPrev = true;
			return;
		}

		// Cross above midpoint - go long
		if (_prevClose <= _prevHigh && close > mid && Position <= 0)
		{
			if (Position < 0)
				BuyMarket();
			BuyMarket();
		}
		// Cross below midpoint - go short
		else if (_prevClose >= _prevLow && close < mid && Position >= 0)
		{
			if (Position > 0)
				SellMarket();
			SellMarket();
		}

		_prevClose = close;
		_prevHigh = mid;
		_prevLow = mid;
	}
}
