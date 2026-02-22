using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public class OpeningRangeBreakout2Strategy : Strategy
{
	private readonly StrategyParam<decimal> _rewardRisk;
	private readonly StrategyParam<decimal> _retrace;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _orHigh;
	private decimal? _orLow;
	private bool _tradeTaken;
	private bool _pendingLong;
	private bool _pendingShort;
	private decimal _rangeRisk;
	private decimal _stopLoss;
	private decimal _takeProfit;
	private bool _wasInOr;
	private DateTime _currentDay;

	public decimal RewardRisk { get => _rewardRisk.Value; set => _rewardRisk.Value = value; }
	public decimal Retrace { get => _retrace.Value; set => _retrace.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public OpeningRangeBreakout2Strategy()
	{
		_rewardRisk = Param(nameof(RewardRisk), 1.1m);
		_retrace = Param(nameof(Retrace), 0.5m);
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame());
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_orHigh = null;
		_orLow = null;
		_tradeTaken = false;
		_pendingLong = false;
		_pendingShort = false;
		_wasInOr = false;
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
			_tradeTaken = false;
			_pendingLong = false;
			_pendingShort = false;
		}

		var hour = candle.OpenTime.TimeOfDay.TotalHours;
		var inOr = hour >= 0 && hour < 1; // first hour as opening range
		var inRtd = hour >= 0 && hour < 20;

		if (inOr)
		{
			_orHigh = _orHigh.HasValue ? Math.Max(_orHigh.Value, candle.HighPrice) : candle.HighPrice;
			_orLow = _orLow.HasValue ? Math.Min(_orLow.Value, candle.LowPrice) : candle.LowPrice;
		}

		if (_wasInOr && !inOr && _orHigh.HasValue && _orLow.HasValue)
		{
			var range = _orHigh.Value - _orLow.Value;
			if (range > 0 && !_tradeTaken)
			{
				_rangeRisk = range * Retrace;
				_pendingLong = true;
				_pendingShort = true;
			}
		}

		// Exit logic
		if (Position > 0)
		{
			if (candle.LowPrice <= _stopLoss || candle.HighPrice >= _takeProfit)
				SellMarket(Math.Abs(Position));
		}
		else if (Position < 0)
		{
			if (candle.HighPrice >= _stopLoss || candle.LowPrice <= _takeProfit)
				BuyMarket(Math.Abs(Position));
		}

		// Entry logic
		if (_pendingLong && _orHigh.HasValue && candle.HighPrice >= _orHigh.Value && Position <= 0 && _rangeRisk > 0)
		{
			if (Position < 0) BuyMarket(Math.Abs(Position));
			BuyMarket(Volume);
			_stopLoss = _orHigh.Value - _rangeRisk;
			_takeProfit = _orHigh.Value + _rangeRisk * RewardRisk;
			_pendingLong = false;
			_pendingShort = false;
			_tradeTaken = true;
		}
		else if (_pendingShort && _orLow.HasValue && candle.LowPrice <= _orLow.Value && Position >= 0 && _rangeRisk > 0)
		{
			if (Position > 0) SellMarket(Math.Abs(Position));
			SellMarket(Volume);
			_stopLoss = _orLow.Value + _rangeRisk;
			_takeProfit = _orLow.Value - _rangeRisk * RewardRisk;
			_pendingShort = false;
			_pendingLong = false;
			_tradeTaken = true;
		}

		// End of day close
		if (!inRtd && Position != 0)
		{
			if (Position > 0) SellMarket(Math.Abs(Position));
			else BuyMarket(Math.Abs(Position));
		}

		_wasInOr = inOr;
	}
}
