using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Trade Channel breakout strategy.
/// Uses Highest/Lowest channel and ATR for stop management.
/// </summary>
public class TradeChannelStrategy : Strategy
{
	private readonly StrategyParam<int> _channelPeriod;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevUpper;
	private decimal _prevLower;
	private decimal _stopPrice;
	private bool _hasPrev;

	public int ChannelPeriod { get => _channelPeriod.Value; set => _channelPeriod.Value = value; }
	public int AtrPeriod { get => _atrPeriod.Value; set => _atrPeriod.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public TradeChannelStrategy()
	{
		_channelPeriod = Param(nameof(ChannelPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Channel Period", "Donchian channel period", "Indicators");
		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "ATR length for stop calculation", "Indicators");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_prevUpper = 0;
		_prevLower = 0;
		_stopPrice = 0;
		_hasPrev = false;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var highest = new Highest { Length = ChannelPeriod };
		var lowest = new Lowest { Length = ChannelPeriod };
		var atr = new AverageTrueRange { Length = AtrPeriod };

		SubscribeCandles(CandleType)
			.Bind(highest, lowest, atr, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal upper, decimal lower, decimal atrVal)
	{
		if (candle.State != CandleStates.Finished) return;

		if (!_hasPrev)
		{
			_prevUpper = upper;
			_prevLower = lower;
			_hasPrev = true;
			return;
		}

		if (atrVal <= 0)
		{
			_prevUpper = upper;
			_prevLower = lower;
			return;
		}

		var close = candle.ClosePrice;

		// Breakout above channel => long
		if (close >= _prevUpper && Position <= 0)
		{
			if (Position < 0) BuyMarket();
			BuyMarket();
			_stopPrice = lower - atrVal;
		}
		// Breakout below channel => short
		else if (close <= _prevLower && Position >= 0)
		{
			if (Position > 0) SellMarket();
			SellMarket();
			_stopPrice = upper + atrVal;
		}
		// Manage long
		else if (Position > 0)
		{
			// Trailing stop
			var newStop = close - atrVal * 2;
			if (newStop > _stopPrice) _stopPrice = newStop;

			if (candle.LowPrice <= _stopPrice)
			{
				SellMarket();
				_stopPrice = 0;
			}
		}
		// Manage short
		else if (Position < 0)
		{
			var newStop = close + atrVal * 2;
			if (newStop < _stopPrice) _stopPrice = newStop;

			if (candle.HighPrice >= _stopPrice)
			{
				BuyMarket();
				_stopPrice = 0;
			}
		}

		_prevUpper = upper;
		_prevLower = lower;
	}
}
