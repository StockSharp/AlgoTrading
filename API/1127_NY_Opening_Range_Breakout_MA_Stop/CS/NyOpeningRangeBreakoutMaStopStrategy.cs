using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public class NyOpeningRangeBreakoutMaStopStrategy : Strategy
{
	private readonly StrategyParam<int> _maLength;
	private readonly StrategyParam<decimal> _tpRatio;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _rangeHigh;
	private decimal? _rangeLow;
	private bool _rangeSet;
	private bool _longBreakout;
	private bool _shortBreakout;
	private DateTime _currentDay;
	private decimal _stopPrice;
	private decimal? _takePrice;
	private ICandleMessage _prevCandle;

	public int MaLength { get => _maLength.Value; set => _maLength.Value = value; }
	public decimal TpRatio { get => _tpRatio.Value; set => _tpRatio.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public NyOpeningRangeBreakoutMaStopStrategy()
	{
		_maLength = Param(nameof(MaLength), 100).SetGreaterThanZero();
		_tpRatio = Param(nameof(TpRatio), 2.5m).SetGreaterThanZero();
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame());
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_rangeHigh = null;
		_rangeLow = null;
		_rangeSet = false;
		_longBreakout = false;
		_shortBreakout = false;
		_currentDay = default;
		_stopPrice = 0m;
		_takePrice = null;
		_prevCandle = null;

		var ma = new SimpleMovingAverage { Length = MaLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
			DrawIndicator(area, ma);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal maValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var candleTime = candle.OpenTime;
		var hour = candleTime.TimeOfDay.TotalHours;

		if (_currentDay != candleTime.Date)
		{
			_currentDay = candleTime.Date;
			_rangeHigh = null;
			_rangeLow = null;
			_rangeSet = false;
			_longBreakout = false;
			_shortBreakout = false;
		}

		// Build opening range during first portion of the day (e.g. 9:30-9:45 equivalent)
		if (hour >= 0 && hour < 1)
		{
			_rangeHigh = _rangeHigh.HasValue ? Math.Max(_rangeHigh.Value, candle.HighPrice) : candle.HighPrice;
			_rangeLow = _rangeLow.HasValue ? Math.Min(_rangeLow.Value, candle.LowPrice) : candle.LowPrice;
		}
		else if (!_rangeSet && _rangeHigh.HasValue && _rangeLow.HasValue && hour >= 1)
		{
			_rangeSet = true;
		}

		var pastCutoff = hour >= 20;

		if (_rangeSet && _prevCandle != null)
		{
			if (!_longBreakout && !pastCutoff && _prevCandle.ClosePrice > _rangeHigh)
				_longBreakout = true;

			if (!_shortBreakout && !pastCutoff && _prevCandle.ClosePrice < _rangeLow)
				_shortBreakout = true;
		}

		// Exit logic first
		if (Position > 0)
		{
			if (candle.LowPrice <= _stopPrice)
			{
				SellMarket(Math.Abs(Position));
				_stopPrice = 0; _takePrice = null;
			}
			else if (_takePrice.HasValue && candle.HighPrice >= _takePrice.Value)
			{
				SellMarket(Math.Abs(Position));
				_stopPrice = 0; _takePrice = null;
			}
			else if (candle.ClosePrice < maValue)
			{
				SellMarket(Math.Abs(Position));
				_stopPrice = 0; _takePrice = null;
			}
		}
		else if (Position < 0)
		{
			if (candle.HighPrice >= _stopPrice)
			{
				BuyMarket(Math.Abs(Position));
				_stopPrice = 0; _takePrice = null;
			}
			else if (_takePrice.HasValue && candle.LowPrice <= _takePrice.Value)
			{
				BuyMarket(Math.Abs(Position));
				_stopPrice = 0; _takePrice = null;
			}
			else if (candle.ClosePrice > maValue)
			{
				BuyMarket(Math.Abs(Position));
				_stopPrice = 0; _takePrice = null;
			}
		}

		// Entry logic
		if (_rangeSet && !pastCutoff && Position == 0 && _rangeLow.HasValue && _rangeHigh.HasValue)
		{
			if (_longBreakout && candle.ClosePrice > maValue)
			{
				var risk = candle.ClosePrice - _rangeLow.Value;
				if (risk > 0)
				{
					_stopPrice = _rangeLow.Value;
					_takePrice = candle.ClosePrice + risk * TpRatio;
					BuyMarket(Volume);
					_longBreakout = false;
				}
			}
			else if (_shortBreakout && candle.ClosePrice < maValue)
			{
				var risk = _rangeHigh.Value - candle.ClosePrice;
				if (risk > 0)
				{
					_stopPrice = _rangeHigh.Value;
					_takePrice = candle.ClosePrice - risk * TpRatio;
					SellMarket(Volume);
					_shortBreakout = false;
				}
			}
		}

		_prevCandle = candle;
	}
}
