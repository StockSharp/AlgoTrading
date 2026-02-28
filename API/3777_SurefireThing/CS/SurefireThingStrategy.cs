using System;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Daily range breakout strategy.
/// Calculates buy/sell levels from previous day's range.
/// Buys when price drops below the lower level, sells when above the upper level.
/// Closes position at end of day.
/// </summary>
public class SurefireThingStrategy : Strategy
{
	private readonly StrategyParam<decimal> _rangeMultiplier;
	private readonly StrategyParam<DataType> _candleType;

	private DateTime? _currentDay;
	private decimal _buyLevel;
	private decimal _sellLevel;
	private bool _levelsReady;
	private decimal _prevDayClose;
	private decimal _prevDayHigh;
	private decimal _prevDayLow;
	private decimal _dayHigh;
	private decimal _dayLow;
	private decimal _dayClose;
	private bool _hasPrevDay;
	private bool _tradedToday;

	public decimal RangeMultiplier
	{
		get => _rangeMultiplier.Value;
		set => _rangeMultiplier.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public SurefireThingStrategy()
	{
		_rangeMultiplier = Param(nameof(RangeMultiplier), 0.5m)
			.SetDisplay("Range Mult", "Multiplier for range-based levels", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle series", "General");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_currentDay = null;
		_levelsReady = false;
		_hasPrevDay = false;
		_tradedToday = false;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var day = candle.OpenTime.Date;

		// New day detected
		if (_currentDay == null || day > _currentDay.Value)
		{
			// Close position at end of previous day
			if (Position > 0)
				SellMarket();
			else if (Position < 0)
				BuyMarket();

			// Save previous day stats
			if (_currentDay != null)
			{
				_prevDayClose = _dayClose;
				_prevDayHigh = _dayHigh;
				_prevDayLow = _dayLow;
				_hasPrevDay = true;
			}

			// Calculate new levels
			if (_hasPrevDay)
			{
				var range = _prevDayHigh - _prevDayLow;
				if (range > 0)
				{
					var halfRange = range * RangeMultiplier;
					_buyLevel = _prevDayClose - halfRange;
					_sellLevel = _prevDayClose + halfRange;
					_levelsReady = true;
				}
			}

			_currentDay = day;
			_dayHigh = candle.HighPrice;
			_dayLow = candle.LowPrice;
			_dayClose = candle.ClosePrice;
			_tradedToday = false;
		}
		else
		{
			if (candle.HighPrice > _dayHigh) _dayHigh = candle.HighPrice;
			if (candle.LowPrice < _dayLow) _dayLow = candle.LowPrice;
			_dayClose = candle.ClosePrice;
		}

		if (!_levelsReady)
			return;

		var price = candle.ClosePrice;

		// Only one trade per day per direction
		if (!_tradedToday && Position == 0)
		{
			if (price <= _buyLevel)
			{
				BuyMarket();
				_tradedToday = true;
			}
			else if (price >= _sellLevel)
			{
				SellMarket();
				_tradedToday = true;
			}
		}
	}
}
