using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy calculating monthly and yearly returns using pivot breakouts.
/// </summary>
public class MonthlyReturnsStrategy : Strategy
{
	private readonly StrategyParam<int> _leftBars;
	private readonly StrategyParam<int> _rightBars;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<decimal> _highBuffer = new();
	private readonly List<decimal> _lowBuffer = new();

	private decimal _hPrice;
	private decimal _lPrice;
	private bool _awaitLong;
	private bool _awaitShort;

	private decimal _prevEquity = 1m;
	private decimal _curMonthReturn;
	private decimal _curYearReturn;
	private int _prevMonth = -1;
	private int _prevYear = -1;
	private readonly List<decimal> _monthReturns = new();
	private readonly List<DateTime> _monthTimes = new();
	private readonly List<decimal> _yearReturns = new();
	private readonly List<int> _yearTimes = new();

	/// <summary>
	/// Number of bars to the left for pivot detection.
	/// </summary>
	public int LeftBars
	{
		get => _leftBars.Value;
		set => _leftBars.Value = value;
	}

	/// <summary>
	/// Number of bars to the right for pivot detection.
	/// </summary>
	public int RightBars
	{
		get => _rightBars.Value;
		set => _rightBars.Value = value;
	}

	/// <summary>
	/// Candle type used by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initialize <see cref="MonthlyReturnsStrategy"/>.
	/// </summary>
	public MonthlyReturnsStrategy()
	{
		_leftBars = Param(nameof(LeftBars), 2)
			.SetDisplay("Left Bars", "Bars to the left for pivots", "General")
			.SetGreaterThanZero();

		_rightBars = Param(nameof(RightBars), 1)
			.SetDisplay("Right Bars", "Bars to the right for pivots", "General")
			.SetGreaterThanZero();

		_candleType = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		_highBuffer.Add(candle.HighPrice);
		_lowBuffer.Add(candle.LowPrice);

		var size = LeftBars + RightBars + 1;

		if (_highBuffer.Count > size)
			_highBuffer.RemoveAt(0);

		if (_lowBuffer.Count > size)
			_lowBuffer.RemoveAt(0);

		if (candle.State != CandleStates.Finished)
			return;

		if (_highBuffer.Count == size)
		{
			var idx = _highBuffer.Count - RightBars - 1;
			var candidate = _highBuffer[idx];
			var isPivot = true;
			for (var i = 0; i < _highBuffer.Count; i++)
			{
				if (i == idx)
					continue;
				if (_highBuffer[i] >= candidate)
				{
					isPivot = false;
					break;
				}
			}

			if (isPivot)
			{
				_hPrice = candidate;
				_awaitLong = true;
			}
		}

		if (_lowBuffer.Count == size)
		{
			var idx = _lowBuffer.Count - RightBars - 1;
			var candidate = _lowBuffer[idx];
			var isPivot = true;
			for (var i = 0; i < _lowBuffer.Count; i++)
			{
				if (i == idx)
					continue;
				if (_lowBuffer[i] <= candidate)
				{
					isPivot = false;
					break;
				}
			}

			if (isPivot)
			{
				_lPrice = candidate;
				_awaitShort = true;
			}
		}

		if (_awaitLong && candle.HighPrice > _hPrice && Position <= 0 && IsFormedAndOnlineAndAllowTrading())
		{
			BuyMarket(Volume + Math.Abs(Position));
			_awaitLong = false;
		}

		if (_awaitShort && candle.LowPrice < _lPrice && Position >= 0 && IsFormedAndOnlineAndAllowTrading())
		{
			SellMarket(Volume + Math.Abs(Position));
			_awaitShort = false;
		}

		UpdateReturns(candle);
	}

	private void UpdateReturns(ICandleMessage candle)
	{
		var eq = 1m + PnL;
		var barReturn = eq / _prevEquity - 1m;
		_prevEquity = eq;

		var month = candle.CloseTime.Month;
		var year = candle.CloseTime.Year;

		if (_prevMonth == -1)
		{
			_prevMonth = month;
			_prevYear = year;
		}

		if (month != _prevMonth)
		{
			_monthReturns.Add(_curMonthReturn);
			_monthTimes.Add(new DateTime(_prevYear, _prevMonth, 1));
			_curMonthReturn = 0m;
			_prevMonth = month;
		}

		if (year != _prevYear)
		{
			_yearReturns.Add(_curYearReturn);
			_yearTimes.Add(_prevYear);
			_curYearReturn = 0m;
			_prevYear = year;
		}

		_curMonthReturn = (1m + _curMonthReturn) * (1m + barReturn) - 1m;
		_curYearReturn = (1m + _curYearReturn) * (1m + barReturn) - 1m;
	}
}
