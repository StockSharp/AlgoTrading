using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public class Orb15mFirst15minBreakoutStrategy : Strategy
{
	private readonly StrategyParam<decimal> _rMultiple;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _orHigh;
	private decimal? _orLow;
	private bool _rangeSet;
	private decimal _stopPrice;
	private decimal _takePrice;
	private DateTime _currentDay;

	public decimal RMultiple { get => _rMultiple.Value; set => _rMultiple.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public Orb15mFirst15minBreakoutStrategy()
	{
		_rMultiple = Param(nameof(RMultiple), 2.0m).SetGreaterThanZero();
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame());
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_orHigh = null;
		_orLow = null;
		_rangeSet = false;
		_currentDay = default;

		var sma = new SimpleMovingAverage { Length = 10 };
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
			_currentDay = day;
			_orHigh = null;
			_orLow = null;
			_rangeSet = false;
		}

		var hour = candle.OpenTime.TimeOfDay.TotalHours;

		// First hour as opening range
		if (hour < 1)
		{
			_orHigh = _orHigh.HasValue ? Math.Max(_orHigh.Value, candle.HighPrice) : candle.HighPrice;
			_orLow = _orLow.HasValue ? Math.Min(_orLow.Value, candle.LowPrice) : candle.LowPrice;
			return;
		}

		if (!_rangeSet && _orHigh.HasValue && _orLow.HasValue)
			_rangeSet = true;

		// Exit
		if (Position > 0)
		{
			if (candle.LowPrice <= _stopPrice || candle.HighPrice >= _takePrice)
				SellMarket(Math.Abs(Position));
		}
		else if (Position < 0)
		{
			if (candle.HighPrice >= _stopPrice || candle.LowPrice <= _takePrice)
				BuyMarket(Math.Abs(Position));
		}

		// Entry
		if (_rangeSet && Position == 0 && _orHigh.HasValue && _orLow.HasValue)
		{
			var range = _orHigh.Value - _orLow.Value;
			if (range > 0)
			{
				if (candle.ClosePrice > _orHigh.Value)
				{
					BuyMarket(Volume);
					_stopPrice = _orLow.Value;
					_takePrice = candle.ClosePrice + range * RMultiple;
					_rangeSet = false;
				}
				else if (candle.ClosePrice < _orLow.Value)
				{
					SellMarket(Volume);
					_stopPrice = _orHigh.Value;
					_takePrice = candle.ClosePrice - range * RMultiple;
					_rangeSet = false;
				}
			}
		}

		// End of day close
		if (hour >= 22 && Position != 0)
		{
			if (Position > 0) SellMarket(Math.Abs(Position));
			else BuyMarket(Math.Abs(Position));
		}
	}
}
