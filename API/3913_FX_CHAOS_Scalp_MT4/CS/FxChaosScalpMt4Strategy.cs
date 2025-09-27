using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// FX-CHAOS scalp strategy ported from the MT4 expert advisor.
/// </summary>
public class FxChaosScalpMt4Strategy : Strategy
{
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _takeProfitPoints;
private readonly StrategyParam<decimal> _breakoutBufferPoints;
private readonly StrategyParam<decimal> _spreadPoints;
private readonly StrategyParam<int> _zigZagWindowSize;
private readonly StrategyParam<DataType> _tradingCandleType;
private readonly StrategyParam<DataType> _dailyCandleType;

	private AwesomeOscillator _awesomeOscillator;
private FractalZigZagTracker _hourlyTracker;
private FractalZigZagTracker _dailyTracker;

	private decimal _previousHigh;
	private decimal _previousLow;
	private bool _hasPrevious;

	private decimal _entryPrice;
	private bool _hasEntry;

	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	public decimal BreakoutBufferPoints
	{
		get => _breakoutBufferPoints.Value;
		set => _breakoutBufferPoints.Value = value;
	}

	public decimal SpreadPoints
	{
		get => _spreadPoints.Value;
		set => _spreadPoints.Value = value;
	}

	public DataType TradingCandleType
	{
		get => _tradingCandleType.Value;
		set => _tradingCandleType.Value = value;
	}

public DataType DailyCandleType
{
get => _dailyCandleType.Value;
set => _dailyCandleType.Value = value;
}

private void ApplyZigZagWindow(int value)
{
var normalized = NormalizeWindow(value);
_zigZagWindowSize.Value = normalized;
_hourlyTracker = new FractalZigZagTracker(normalized);
_dailyTracker = new FractalZigZagTracker(normalized);
}

private static int NormalizeWindow(int value)
{
if (value < 5)
value = 5;

if ((value & 1) == 0)
value += 1;

return value;
}

public int ZigZagWindowSize
{
get => _zigZagWindowSize.Value;
set => ApplyZigZagWindow(value);
}

public FxChaosScalpMt4Strategy()
{
		_orderVolume = Param(nameof(OrderVolume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Volume", "Order volume in lots", "Trading");

		_stopLossPoints = Param(nameof(StopLossPoints), 0m)
			.SetDisplay("Stop Loss (pts)", "Stop loss distance in points", "Risk");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 13m)
			.SetDisplay("Take Profit (pts)", "Take profit distance in points", "Risk");

		_breakoutBufferPoints = Param(nameof(BreakoutBufferPoints), 2m)
			.SetDisplay("Breakout Buffer", "Extra points added to previous extremes", "Setup");

_spreadPoints = Param(nameof(SpreadPoints), 0m)
.SetDisplay("Spread (pts)", "Average spread in points added to buy stop", "Setup");

_zigZagWindowSize = Param(nameof(ZigZagWindowSize), 5)
.SetRange(5, 15)
.SetDisplay("ZigZag Window", "Number of candles considered by the ZigZag tracker.", "Setup");

_tradingCandleType = Param(nameof(TradingCandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Trading Candle", "Primary trading timeframe", "General");

_dailyCandleType = Param(nameof(DailyCandleType), TimeSpan.FromDays(1).TimeFrame())
.SetDisplay("Daily Candle", "Higher timeframe for ZigZag filter", "General");

ApplyZigZagWindow(_zigZagWindowSize.Value);
}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return
		[
			(Security, TradingCandleType),
			(Security, DailyCandleType)
		];
	}

	/// <inheritdoc />
protected override void OnReseted()
{
base.OnReseted();

Volume = OrderVolume;
ApplyZigZagWindow(_zigZagWindowSize.Value);
_previousHigh = 0m;
		_previousLow = 0m;
		_hasPrevious = false;
		_entryPrice = 0m;
		_hasEntry = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = OrderVolume;

		_awesomeOscillator = new AwesomeOscillator
		{
			ShortPeriod = 5,
			LongPeriod = 34
		};

		var dailySubscription = SubscribeCandles(DailyCandleType);
		dailySubscription.Bind(ProcessDailyCandle).Start();

		var tradingSubscription = SubscribeCandles(TradingCandleType);
		tradingSubscription.BindEx(_awesomeOscillator, ProcessTradingCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, tradingSubscription);
			DrawIndicator(area, _awesomeOscillator);
			DrawOwnTrades(area);
		}
	}

	private void ProcessDailyCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Update higher timeframe ZigZag filter.
		_dailyTracker.Update(candle);
	}

	private void ProcessTradingCandle(ICandleMessage candle, IIndicatorValue aoValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Track ZigZag swings for the trading timeframe.
		_hourlyTracker.Update(candle);

		if (ManageRisk(candle))
		{
			UpdatePreviousLevels(candle);
			return;
		}

		if (!_hasPrevious)
		{
			UpdatePreviousLevels(candle);
			return;
		}

		if (!aoValue.IsFinal)
		{
			UpdatePreviousLevels(candle);
			return;
		}

		if (_hourlyTracker.LastValue is not decimal hourlyZigZag ||
			_dailyTracker.LastValue is not decimal dailyZigZag)
		{
			UpdatePreviousLevels(candle);
			return;
		}

		var ao = aoValue.GetValue<decimal>();
		var open = candle.OpenPrice;
		var close = candle.ClosePrice;

		var step = GetPriceStep();
		var breakoutOffset = BreakoutBufferPoints * step;
		var spreadOffset = SpreadPoints * step;

		var adjustedHigh = _previousHigh + breakoutOffset + spreadOffset;
		var adjustedLow = _previousLow - breakoutOffset;

		// Evaluate breakout conditions relative to previous levels and ZigZag bands.
		var longSignal = open < adjustedHigh && close > adjustedHigh && close < hourlyZigZag && ao < 0m && close > dailyZigZag;
		var shortSignal = open > adjustedLow && close < adjustedLow && close > hourlyZigZag && ao > 0m && close < dailyZigZag;

		if (longSignal && Position <= 0)
		{
			if (Position < 0)
			{
				ClosePosition();
				_hasEntry = false;
				_entryPrice = 0m;
			}

			if (Position <= 0)
			{
				BuyMarket();
				_entryPrice = close;
				_hasEntry = true;
			}
		}
		else if (shortSignal && Position >= 0)
		{
			if (Position > 0)
			{
				ClosePosition();
				_hasEntry = false;
				_entryPrice = 0m;
			}

			if (Position >= 0)
			{
				SellMarket();
				_entryPrice = close;
				_hasEntry = true;
			}
		}

		if (Position == 0)
		{
			_hasEntry = false;
			_entryPrice = 0m;
		}

		UpdatePreviousLevels(candle);
	}

	private void UpdatePreviousLevels(ICandleMessage candle)
	{
		_previousHigh = candle.HighPrice;
		_previousLow = candle.LowPrice;
		_hasPrevious = true;
	}

	private bool ManageRisk(ICandleMessage candle)
	{
		if (Position == 0)
		{
			_hasEntry = false;
			_entryPrice = 0m;
			return false;
		}

		if (!_hasEntry)
			return false;

		var step = GetPriceStep();

		if (Position > 0)
		{
			var stop = StopLossPoints > 0m ? _entryPrice - StopLossPoints * step : (decimal?)null;
			var take = TakeProfitPoints > 0m ? _entryPrice + TakeProfitPoints * step : (decimal?)null;

			if (stop is decimal stopPrice && candle.LowPrice <= stopPrice)
			{
				ClosePosition();
				_hasEntry = false;
				_entryPrice = 0m;
				return true;
			}

			if (take is decimal takePrice && candle.HighPrice >= takePrice)
			{
				ClosePosition();
				_hasEntry = false;
				_entryPrice = 0m;
				return true;
			}
		}
		else if (Position < 0)
		{
			var stop = StopLossPoints > 0m ? _entryPrice + StopLossPoints * step : (decimal?)null;
			var take = TakeProfitPoints > 0m ? _entryPrice - TakeProfitPoints * step : (decimal?)null;

			if (stop is decimal stopPrice && candle.HighPrice >= stopPrice)
			{
				ClosePosition();
				_hasEntry = false;
				_entryPrice = 0m;
				return true;
			}

			if (take is decimal takePrice && candle.LowPrice <= takePrice)
			{
				ClosePosition();
				_hasEntry = false;
				_entryPrice = 0m;
				return true;
			}
		}

		return false;
	}

	private decimal GetPriceStep()
	{
		var step = Security?.PriceStep;
		return step is decimal value && value > 0m ? value : 1m;
	}

private sealed class FractalZigZagTracker
{
private readonly int _windowSize;
private readonly CandleInfo[] _window;
private int _count;
private decimal? _lastValue;
private int _direction;

public FractalZigZagTracker(int windowSize)
{
_windowSize = windowSize;
_window = new CandleInfo[_windowSize];
}

public decimal? LastValue => _lastValue;

public void Reset()
{
Array.Clear(_window);
_count = 0;
_lastValue = null;
_direction = 0;
}

public decimal? Update(ICandleMessage candle)
{
if (_count < _windowSize)
{
_window[_count++] = new CandleInfo(candle.HighPrice, candle.LowPrice);
if (_count < _windowSize)
return _lastValue;

Evaluate();
return _lastValue;
}

for (var i = 0; i < _windowSize - 1; i++)
_window[i] = _window[i + 1];

_window[_windowSize - 1] = new CandleInfo(candle.HighPrice, candle.LowPrice);

Evaluate();
return _lastValue;
}

private void Evaluate()
{
if (_count < _windowSize)
return;

var center = _windowSize / 2;
var pivot = _window[center];
var isUp = true;
var isDown = true;

for (var i = 0; i < _windowSize; i++)
{
if (i == center)
continue;

var sample = _window[i];

if (i < center)
{
if (!(pivot.High > sample.High))
isUp = false;

if (!(pivot.Low < sample.Low))
isDown = false;
}
else
{
if (!(pivot.High >= sample.High))
isUp = false;

if (!(pivot.Low <= sample.Low))
isDown = false;
}

if (!isUp && !isDown)
break;
}

if (isUp)
{
if (_direction == 1)
{
if (_lastValue == null || pivot.High > _lastValue.Value)
_lastValue = pivot.High;
}
else
{
_lastValue = pivot.High;
_direction = 1;
}
}
else if (isDown)
{
if (_direction == -1)
{
if (_lastValue == null || pivot.Low < _lastValue.Value)
_lastValue = pivot.Low;
}
else
{
_lastValue = pivot.Low;
_direction = -1;
}
}
}

		private readonly struct CandleInfo
		{
			public CandleInfo(decimal high, decimal low)
			{
				High = high;
				Low = low;
			}

			public decimal High { get; }

			public decimal Low { get; }
		}
	}
}
