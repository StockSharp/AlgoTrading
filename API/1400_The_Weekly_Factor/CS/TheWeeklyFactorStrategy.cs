namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Strategy based on the Weekly Factor pattern.
/// Closes positions at new sessions and trades breakouts when the weekly factor condition is met.
/// </summary>
public class TheWeeklyFactorStrategy : Strategy
{
	private readonly StrategyParam<decimal> _rangeFilter;
	private readonly StrategyParam<DataType> _candleType;

	private readonly Queue<(decimal open, decimal high, decimal low, decimal close)> _week = new();
	private decimal _weekOpen;
	private decimal _weekHigh;
	private decimal _weekLow;
	private decimal _weekClose;
	private bool _weeklyFactor;

	private DateTime _sessionDate;
	private decimal _sessionOpen;
	private decimal _sessionHigh;
	private decimal _sessionLow;
	private int _barCounter;
	private int _dayCounter;
	private decimal _entryPrice;

	/// <summary>
	/// Range filter for weekly factor.
	/// </summary>
	public decimal RangeFilter
	{
		get => _rangeFilter.Value;
		set => _rangeFilter.Value = value;
	}

	/// <summary>
	/// Candle type for intraday calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="TheWeeklyFactorStrategy"/>.
	/// </summary>
	public TheWeeklyFactorStrategy()
	{
		_rangeFilter = Param(nameof(RangeFilter), 0.5m)
			.SetRange(0m, 1m)
			.SetDisplay("Range Filter", "Body to range ratio", "Parameters");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Working candle timeframe", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType), (Security, TimeSpan.FromDays(1).TimeFrame())];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_week.Clear();
		_weeklyFactor = false;
		_sessionDate = default;
		_barCounter = 0;
		_dayCounter = 0;
		_entryPrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var intraday = SubscribeCandles(CandleType);
		intraday.Bind(ProcessCandle).Start();

		var daily = SubscribeCandles(TimeSpan.FromDays(1).TimeFrame());
		daily.Bind(ProcessDaily).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, intraday);
			DrawOwnTrades(area);
		}
	}

	private void ProcessDaily(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_week.Enqueue((candle.OpenPrice, candle.HighPrice, candle.LowPrice, candle.ClosePrice));
		while (_week.Count > 5)
			_week.Dequeue();

		if (_week.Count == 0)
			return;

		_weekOpen = _week.Peek().open;
		_weekHigh = _week.Peek().high;
		_weekLow = _week.Peek().low;
		foreach (var item in _week)
		{
			if (item.high > _weekHigh)
				_weekHigh = item.high;
			if (item.low < _weekLow)
				_weekLow = item.low;
			_weekClose = item.close;
		}

		var body = Math.Abs(_weekOpen - _weekClose);
		var range = _weekHigh - _weekLow;
		_weeklyFactor = range != 0m && body < RangeFilter * range;
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var day = candle.OpenTime.Date;
		if (day != _sessionDate)
		{
			if (Position > 0)
				SellMarket(Position);
			else if (Position < 0)
				BuyMarket(Math.Abs(Position));

			_sessionDate = day;
			_sessionOpen = candle.OpenPrice;
			_sessionHigh = candle.HighPrice;
			_sessionLow = candle.LowPrice;
			_barCounter = 1;
			_dayCounter++;
		}
		else
		{
			_sessionHigh = Math.Max(_sessionHigh, candle.HighPrice);
			_sessionLow = Math.Min(_sessionLow, candle.LowPrice);
			_barCounter++;
		}

		if (_weeklyFactor && _barCounter > 1 && _barCounter < 91)
		{
			if (candle.ClosePrice > _sessionHigh && Position <= 0)
			{
				BuyMarket();
				_entryPrice = candle.ClosePrice;
				_dayCounter = 0;
			}
			else if (candle.ClosePrice < _sessionLow && Position >= 0)
			{
				SellMarket();
				_entryPrice = candle.ClosePrice;
				_dayCounter = 0;
			}
		}

		if (_dayCounter > 1 && Position != 0)
		{
			if (Position > 0 && candle.ClosePrice > _entryPrice)
				SellMarket(Position);
			else if (Position < 0 && candle.ClosePrice < _entryPrice)
				BuyMarket(Math.Abs(Position));
		}
	}
}
