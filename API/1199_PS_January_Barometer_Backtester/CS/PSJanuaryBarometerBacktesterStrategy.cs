using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// January Barometer strategy with optional Santa Claus Rally and First Five Days filters.
/// </summary>
public class PSJanuaryBarometerBacktesterStrategy : Strategy
{
	private readonly StrategyParam<bool> _useSantaClausRally;
	private readonly StrategyParam<bool> _useFirstFiveDays;
	private readonly StrategyParam<DataType> _candleType;

	public bool UseSantaClausRally
	{
		get => _useSantaClausRally.Value;
		set => _useSantaClausRally.Value = value;
	}

	public bool UseFirstFiveDays
	{
		get => _useFirstFiveDays.Value;
		set => _useFirstFiveDays.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	private decimal? _januaryHigh;
	private bool _tradeInitiated;
	private decimal? _firstFiveReturn;
	private decimal? _santaReturn;
	private decimal _firstFiveOpen;
	private int _firstFiveCount;
	private decimal _santaOpen;
	private bool _santaPeriod;
	private int _currentYear;

	public PSJanuaryBarometerBacktesterStrategy()
	{
		_useSantaClausRally = Param(nameof(UseSantaClausRally), false)
			.SetDisplay("Santa Claus Rally", "Require positive Santa Claus Rally", "Filters");
		_useFirstFiveDays = Param(nameof(UseFirstFiveDays), false)
			.SetDisplay("First Five Days", "Require positive first five days", "Filters");
		_candleType = Param(nameof(CandleType), TimeSpan.FromDays(31).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles for monthly checks", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, CandleType);
		yield return (Security, TimeSpan.FromDays(1).TimeFrame());
	}

	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var monthly = SubscribeCandles(CandleType);
		monthly.Bind(ProcessMonthly).Start();

		var daily = SubscribeCandles(TimeSpan.FromDays(1).TimeFrame());
		daily.Bind(ProcessDaily).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, monthly);
			DrawOwnTrades(area);
		}
	}

	private void ProcessDaily(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var date = candle.OpenTime;

		if (date.Year != _currentYear)
		{
			_currentYear = date.Year;
			_firstFiveReturn = null;
			_santaReturn = null;
			_firstFiveCount = 0;
			_santaPeriod = false;
		}

		if (UseFirstFiveDays && date.Month == 1 && date.Day <= 5)
		{
			if (_firstFiveCount == 0)
				_firstFiveOpen = candle.OpenPrice;

			_firstFiveCount++;

			if (_firstFiveCount == 5)
				_firstFiveReturn = (candle.ClosePrice - _firstFiveOpen) / _firstFiveOpen;
		}

		if (!UseSantaClausRally)
			return;

		if (!_santaPeriod && date.Month == 12 && date.Day >= 25)
		{
			_santaPeriod = true;
			_santaOpen = candle.OpenPrice;
		}

		if (_santaPeriod)
		{
			_santaReturn = (candle.ClosePrice - _santaOpen) / _santaOpen;

			if (date.Month == 1 && date.Day == 2)
				_santaPeriod = false;
		}
	}

	private void ProcessMonthly(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var date = candle.OpenTime;
		var month = date.Month;

		if (month == 1)
		{
			_januaryHigh = candle.HighPrice;
			_tradeInitiated = false;
			return;
		}

		if (!_tradeInitiated &&
		month >= 2 && month <= 6 &&
		_januaryHigh is decimal janHigh &&
		candle.ClosePrice > janHigh &&
		(!UseFirstFiveDays || (_firstFiveReturn ?? decimal.MinValue) > 0m) &&
		(!UseSantaClausRally || (_santaReturn ?? decimal.MinValue) > 0m))
		{
		BuyMarket();
		_tradeInitiated = true;
		}
		else if (_tradeInitiated && month == 12)
		{
		ClosePosition();
		_tradeInitiated = false;
		}
	}
}
