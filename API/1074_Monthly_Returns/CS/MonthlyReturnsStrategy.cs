using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
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
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<decimal> _breakoutOffsetPercent;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<decimal> _highBuffer = new();
	private readonly List<decimal> _lowBuffer = new();

	private decimal _hPrice;
	private decimal _lPrice;
	private bool _awaitLong;
	private bool _awaitShort;
	private int _barIndex;
	private int _lastSignalBar = int.MinValue;

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
	/// Minimum number of finished candles between signals.
	/// </summary>
	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
	}

	/// <summary>
	/// Minimum breakout offset in percents from pivot level.
	/// </summary>
	public decimal BreakoutOffsetPercent
	{
		get => _breakoutOffsetPercent.Value;
		set => _breakoutOffsetPercent.Value = value;
	}

	/// <summary>
	/// Initialize <see cref="MonthlyReturnsStrategy"/>.
	/// </summary>
	public MonthlyReturnsStrategy()
	{
		_leftBars = Param(nameof(LeftBars), 6)
			.SetDisplay("Left Bars", "Bars to the left for pivots", "General")
			.SetGreaterThanZero();

		_rightBars = Param(nameof(RightBars), 3)
			.SetDisplay("Right Bars", "Bars to the right for pivots", "General")
			.SetGreaterThanZero();

		_cooldownBars = Param(nameof(CooldownBars), 14)
			.SetDisplay("Cooldown Bars", "Finished candles between entries", "General")
			.SetGreaterThanZero();

		_breakoutOffsetPercent = Param(nameof(BreakoutOffsetPercent), 0.10m)
			.SetDisplay("Breakout Offset %", "Min breakout offset from pivot", "General")
			.SetCanOptimize(true)
			.SetGreaterThanZero();

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(10).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_barIndex++;

		_highBuffer.Add(candle.HighPrice);
		_lowBuffer.Add(candle.LowPrice);

		var size = LeftBars + RightBars + 1;

		if (_highBuffer.Count > size)
			_highBuffer.RemoveAt(0);

		if (_lowBuffer.Count > size)
			_lowBuffer.RemoveAt(0);

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

		var canSignal = _barIndex - _lastSignalBar >= CooldownBars;
		var longTrigger = _hPrice * (1m + BreakoutOffsetPercent / 100m);
		var shortTrigger = _lPrice * (1m - BreakoutOffsetPercent / 100m);

		if (_awaitLong && canSignal && candle.HighPrice > longTrigger && Position <= 0 && IsFormedAndOnlineAndAllowTrading())
		{
			BuyMarket(Volume + Math.Abs(Position));
			_awaitLong = false;
			_awaitShort = false;
			_lastSignalBar = _barIndex;
		}

		if (_awaitShort && canSignal && candle.LowPrice < shortTrigger && Position >= 0 && IsFormedAndOnlineAndAllowTrading())
		{
			SellMarket(Volume + Math.Abs(Position));
			_awaitShort = false;
			_awaitLong = false;
			_lastSignalBar = _barIndex;
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

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_highBuffer.Clear();
		_lowBuffer.Clear();
		_monthReturns.Clear();
		_monthTimes.Clear();
		_yearReturns.Clear();
		_yearTimes.Clear();

		_hPrice = 0m;
		_lPrice = 0m;
		_awaitLong = false;
		_awaitShort = false;
		_barIndex = 0;
		_lastSignalBar = int.MinValue;

		_prevEquity = 1m;
		_curMonthReturn = 0m;
		_curYearReturn = 0m;
		_prevMonth = -1;
		_prevYear = -1;
	}
}
