using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on daily, premarket, and previous day levels.
/// Enters long when price crosses above premarket or previous day high.
/// Enters short when price crosses below premarket or previous day low.
/// Exits when price reaches the current day's high or low.
/// </summary>
public class HodLodPmhPmlPdhPdlStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;

	private DateTime _currentDay;
	private decimal _dailyHigh;
	private decimal _dailyLow;
	private decimal _previousDayHigh;
	private decimal _previousDayLow;
	private decimal _pmHigh;
	private decimal _pmLow;
	private bool _isPremarket;
	private decimal _prevClose;

	/// <summary>
	/// Candle type used by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="HodLodPmhPmlPdhPdlStrategy"/> class.
	/// </summary>
	public HodLodPmhPmlPdhPdlStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_currentDay = default;
		_dailyHigh = default;
		_dailyLow = default;
		_previousDayHigh = default;
		_previousDayLow = default;
		_pmHigh = default;
		_pmLow = default;
		_isPremarket = false;
		_prevClose = default;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

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

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var date = candle.OpenTime.Date;
		if (_currentDay != date)
		{
			_previousDayHigh = _dailyHigh;
			_previousDayLow = _dailyLow;
			_dailyHigh = candle.HighPrice;
			_dailyLow = candle.LowPrice;
			_currentDay = date;
		}
		else
		{
			_dailyHigh = Math.Max(_dailyHigh, candle.HighPrice);
			_dailyLow = Math.Min(_dailyLow, candle.LowPrice);
		}

		var time = candle.OpenTime.TimeOfDay;
		var isPremarket = time < new TimeSpan(9, 30, 0) || time >= new TimeSpan(16, 0, 0);
		if (isPremarket)
		{
			if (!_isPremarket)
			{
				_pmHigh = candle.HighPrice;
				_pmLow = candle.LowPrice;
			}
			else
			{
				if (candle.HighPrice > _pmHigh)
					_pmHigh = candle.HighPrice;
				if (candle.LowPrice < _pmLow)
					_pmLow = candle.LowPrice;
			}
		}
		_isPremarket = isPremarket;

		if (_prevClose != default)
		{
			var crossUpPmh = _prevClose <= _pmHigh && candle.ClosePrice > _pmHigh;
			var crossUpPdh = _prevClose <= _previousDayHigh && candle.ClosePrice > _previousDayHigh;
			var crossDownPml = _prevClose >= _pmLow && candle.ClosePrice < _pmLow;
			var crossDownPdl = _prevClose >= _previousDayLow && candle.ClosePrice < _previousDayLow;
			var crossUpDh = _prevClose <= _dailyHigh && candle.ClosePrice > _dailyHigh;
			var crossDownDl = _prevClose >= _dailyLow && candle.ClosePrice < _dailyLow;

			if (Position == 0)
			{
				if (crossUpPmh || crossUpPdh)
					BuyMarket(Volume);
				else if (crossDownPml || crossDownPdl)
					SellMarket(Volume);
			}
			else if (Position > 0 && crossUpDh)
			{
				SellMarket(Volume);
			}
			else if (Position < 0 && crossDownDl)
			{
				BuyMarket(Volume);
			}
		}

		_prevClose = candle.ClosePrice;
	}
}
