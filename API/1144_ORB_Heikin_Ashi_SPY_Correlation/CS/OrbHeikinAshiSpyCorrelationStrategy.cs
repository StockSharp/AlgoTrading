using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public class OrbHeikinAshiSpyCorrelationStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _orHigh;
	private decimal? _orLow;
	private bool _rangeSet;
	private decimal _stopPrice;
	private decimal _takePrice;
	private DateTime _currentDay;
	private decimal _prevHaOpen;
	private decimal _prevHaClose;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public OrbHeikinAshiSpyCorrelationStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame());
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_orHigh = null; _orLow = null; _rangeSet = false; _currentDay = default;
		_prevHaOpen = 0; _prevHaClose = 0;

		var sma = new SimpleMovingAverage { Length = 20 };
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(sma, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal smaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var day = candle.OpenTime.Date;
		if (_currentDay != day)
		{
			_currentDay = day; _orHigh = null; _orLow = null; _rangeSet = false;
		}

		// Heikin Ashi
		decimal haOpen, haClose;
		if (_prevHaOpen == 0)
		{
			haOpen = (candle.OpenPrice + candle.ClosePrice) / 2;
			haClose = (candle.OpenPrice + candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 4;
		}
		else
		{
			haOpen = (_prevHaOpen + _prevHaClose) / 2;
			haClose = (candle.OpenPrice + candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 4;
		}
		_prevHaOpen = haOpen;
		_prevHaClose = haClose;

		var bullishHa = haClose > haOpen;
		var hour = candle.OpenTime.TimeOfDay.TotalHours;

		if (hour < 1)
		{
			_orHigh = _orHigh.HasValue ? Math.Max(_orHigh.Value, candle.HighPrice) : candle.HighPrice;
			_orLow = _orLow.HasValue ? Math.Min(_orLow.Value, candle.LowPrice) : candle.LowPrice;
			return;
		}

		if (!_rangeSet && _orHigh.HasValue && _orLow.HasValue)
			_rangeSet = true;

		if (Position > 0 && (candle.LowPrice <= _stopPrice || candle.HighPrice >= _takePrice))
			SellMarket(Math.Abs(Position));
		else if (Position < 0 && (candle.HighPrice >= _stopPrice || candle.LowPrice <= _takePrice))
			BuyMarket(Math.Abs(Position));

		if (_rangeSet && Position == 0 && _orHigh.HasValue && _orLow.HasValue)
		{
			var range = _orHigh.Value - _orLow.Value;
			if (range > 0)
			{
				if (candle.ClosePrice > _orHigh.Value && bullishHa)
				{
					BuyMarket(Volume);
					_stopPrice = _orLow.Value;
					_takePrice = candle.ClosePrice + range * 1.5m;
					_rangeSet = false;
				}
				else if (candle.ClosePrice < _orLow.Value && !bullishHa)
				{
					SellMarket(Volume);
					_stopPrice = _orHigh.Value;
					_takePrice = candle.ClosePrice - range * 1.5m;
					_rangeSet = false;
				}
			}
		}

		if (hour >= 22 && Position != 0)
		{
			if (Position > 0) SellMarket(Math.Abs(Position));
			else BuyMarket(Math.Abs(Position));
		}
	}
}
