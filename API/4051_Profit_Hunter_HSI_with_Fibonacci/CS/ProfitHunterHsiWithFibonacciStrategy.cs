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

using StockSharp.Algo.Candles;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Port of the MetaTrader 4 expert advisor "Profit Hunter HSI with fibonacci".
/// Combines a short-term EMA trend filter with Fibonacci retracement levels
/// calculated from daily candles to define support and resistance zones.
/// </summary>
public class ProfitHunterHsiWithFibonacciStrategy : Strategy
{
	private static readonly decimal[] _trailingThresholds =
	{
		60m, 80m, 100m, 120m, 140m, 160m, 180m, 200m, 220m, 240m, 260m
	};

	private readonly StrategyParam<decimal> _trailingBuffer;

	private readonly StrategyParam<int> _numBars;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<TimeSpan> _timeFrame;
	private readonly StrategyParam<int> _daysBackForHigh;
	private readonly StrategyParam<int> _daysBackForLow;

	private readonly List<ICandleMessage> _intradayCandles = new();
	private readonly List<ICandleMessage> _dailyCandles = new();

	private ExponentialMovingAverage _ema;

	private decimal? _support;
	private decimal? _resistance;

	private decimal? _fib000;
	private decimal? _fib146;
	private decimal? _fib236;
	private decimal? _fib382;
	private decimal? _fib50;
	private decimal? _fib618;
	private decimal? _fib764;
	private decimal? _fib91;
	private decimal? _fib100;
	private decimal? _fib1618;
	private decimal? _fib2618;
	private decimal? _fib4236;
	private bool? _highFirst;
	private bool _fibLevelsReady;

	private TrendDirections _trend;
	private StrategySignals _signal;

	private decimal _pipSize;
	private decimal _entryPrice;
	private decimal? _stopLossPrice;
	private int _trailingStage;
	private Sides? _pendingEntrySide;
	private decimal? _pendingStopLossPrice;
	private Sides? _currentPositionSide;

	private enum TrendDirections
	{
		Unknown,
		Up,
		Down
	}

	private enum StrategySignals
	{
		None,
		ReverseBuy,
		ReverseSell,
		TradingArea,
		Continuation
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="ProfitHunterHsiWithFibonacciStrategy"/> class.
	/// </summary>
	public ProfitHunterHsiWithFibonacciStrategy()
	{
		_numBars = Param(nameof(NumBars), 3)
		.SetGreaterThanZero()
		.SetDisplay("Bars Shift", "Number of previous candles inspected to locate reference levels.", "Levels")
		.SetCanOptimize(true);

		_maPeriod = Param(nameof(MaPeriod), 5)
		.SetGreaterThanZero()
		.SetDisplay("EMA Period", "Length of the EMA trend filter applied to close prices.", "Trend")
		.SetCanOptimize(true);

		_timeFrame = Param(nameof(TimeFrame), TimeSpan.FromMinutes(1))
		.SetDisplay("Time Frame", "Primary candle time frame processed by the strategy.", "Data");

		_daysBackForHigh = Param(nameof(DaysBackForHigh), 1)
		.SetNotNegative()
		.SetDisplay("Days Back (High)", "Daily candle index that provides the swing high for Fibonacci levels.", "Fibonacci")
		.SetCanOptimize(true);

		_daysBackForLow = Param(nameof(DaysBackForLow), 1)
		.SetNotNegative()
		.SetDisplay("Days Back (Low)", "Daily candle index that provides the swing low for Fibonacci levels.", "Fibonacci")
		.SetCanOptimize(true);


		_trailingBuffer = Param(nameof(TrailingBuffer), 5m)
			.SetNotNegative()
			.SetDisplay("Trailing Buffer", "Pip buffer subtracted from trailing thresholds", "Risk")
			.SetCanOptimize(true);

		ResetInternalState();
	}

	/// <summary>
	/// Number of historical candles inspected when reading support and resistance levels.
	/// </summary>
	public int NumBars
	{
		get => _numBars.Value;
		set => _numBars.Value = value;
	}

	/// <summary>
	/// Period of the EMA used to classify the short-term trend.
	/// </summary>
	public int MaPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	/// <summary>
	/// Intraday candle time frame processed by the strategy.
	/// </summary>
	public TimeSpan TimeFrame
	{
		get => _timeFrame.Value;
		set => _timeFrame.Value = value;
	}

	/// <summary>
	/// Index of the daily candle delivering the swing high reference.
	/// </summary>
	public int DaysBackForHigh
	{
		get => _daysBackForHigh.Value;
		set => _daysBackForHigh.Value = value;
	}

	/// <summary>
	/// Index of the daily candle delivering the swing low reference.
	/// </summary>
	public int DaysBackForLow
	{
		get => _daysBackForLow.Value;
		set => _daysBackForLow.Value = value;
	}

	/// <summary>
	/// Additional pip buffer applied when trailing stop-loss orders.
	/// </summary>
	public decimal TrailingBuffer
	{
		get => _trailingBuffer.Value;
		set => _trailingBuffer.Value = value;
	}


	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		ResetInternalState();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		ResetInternalState();

		_pipSize = Security?.PriceStep ?? 0m;
		if (_pipSize <= 0m)
		_pipSize = 1m;

		_ema = new ExponentialMovingAverage
		{
			Length = MaPeriod
		};

		var intradaySubscription = SubscribeCandles(TimeFrame);
		intradaySubscription.Bind(_ema, ProcessIntradayCandle).Start();

		var dailySubscription = SubscribeCandles(TimeSpan.FromDays(1));
		dailySubscription.Bind(ProcessDailyCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, intradaySubscription);
			DrawOwnTrades(area);
		}
	}

	/// <inheritdoc />
	protected override void OnOrderRegisterFailed(OrderFail fail)
	{
		base.OnOrderRegisterFailed(fail);

		if (_pendingEntrySide != null && fail.Order.Side == _pendingEntrySide)
		{
			_pendingEntrySide = null;
			_pendingStopLossPrice = null;
		}
	}

	/// <inheritdoc />
	protected override void OnPositionReceived(Position position)
	{
		base.OnPositionReceived(position);

		if (Position == 0m)
		{
			_currentPositionSide = null;
			_entryPrice = 0m;
			_stopLossPrice = null;
			_pendingEntrySide = null;
			_pendingStopLossPrice = null;
			_trailingStage = 0;
			return;
		}

		if (Position > 0m && delta > 0m)
		{
			_currentPositionSide = Sides.Buy;
			_entryPrice = PositionPrice is decimal price ? price : 0m;
			_stopLossPrice = _pendingStopLossPrice;
			_pendingEntrySide = null;
			_pendingStopLossPrice = null;
			_trailingStage = 0;
		}
		else if (Position < 0m && delta < 0m)
		{
			_currentPositionSide = Sides.Sell;
			_entryPrice = PositionPrice is decimal price ? price : 0m;
			_stopLossPrice = _pendingStopLossPrice;
			_pendingEntrySide = null;
			_pendingStopLossPrice = null;
			_trailingStage = 0;
		}
	}

	private void ProcessDailyCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		_dailyCandles.Insert(0, candle);

		var maxIndex = Math.Max(DaysBackForHigh, DaysBackForLow) + 10;
		if (_dailyCandles.Count > maxIndex)
		_dailyCandles.RemoveRange(maxIndex, _dailyCandles.Count - maxIndex);

		UpdateFibonacciLevels();
	}

	private void ProcessIntradayCandle(ICandleMessage candle, decimal emaValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		_intradayCandles.Insert(0, candle);

		var maxCount = Math.Max(NumBars + 5, 200);
		if (_intradayCandles.Count > maxCount)
		_intradayCandles.RemoveRange(maxCount, _intradayCandles.Count - maxCount);

		if (_ema == null || !_ema.IsFormed)
		return;

		UpdateSupportAndResistance();

		var close = candle.ClosePrice;
		var ask = close;
		var bid = close;

		UpdateTrend(ask, bid, emaValue);
		UpdateSignal(ask, bid);

		TryOpenPosition(ask, bid);
		ManagePosition(ask, bid);
	}

	private void UpdateSupportAndResistance()
	{
		var shift = NumBars;
		if (shift <= 0)
		return;

		if (_intradayCandles.Count < shift)
		return;

		var reference = _intradayCandles[shift - 1];
		_support = reference.LowPrice;
		_resistance = reference.HighPrice;
	}

	private void UpdateTrend(decimal ask, decimal bid, decimal emaValue)
	{
		if (bid < emaValue)
		{
			_trend = TrendDirections.Down;
		}
		else if (ask > emaValue)
		{
			_trend = TrendDirections.Up;
		}
		else
		{
			_trend = TrendDirections.Unknown;
		}
	}

	private void UpdateSignal(decimal ask, decimal bid)
	{
		if (!_fibLevelsReady || !_highFirst.HasValue || !_fib146.HasValue || !_fib236.HasValue || !_fib764.HasValue || !_fib91.HasValue)
		{
			_signal = StrategySignals.None;
			return;
		}

		if (_highFirst.Value)
		{
			if (ask < _fib236.Value)
			{
				_signal = StrategySignals.ReverseBuy;
			}
			else if (bid > _fib764.Value)
			{
				_signal = StrategySignals.ReverseSell;
			}
			else if (bid > _fib236.Value && bid < _fib764.Value)
			{
				_signal = StrategySignals.TradingArea;
			}
			else if (bid > _fib91.Value || ask < _fib146.Value)
			{
				_signal = StrategySignals.Continuation;
			}
			else
			{
				_signal = StrategySignals.None;
			}
		}
		else
		{
			if (bid > _fib236.Value)
			{
				_signal = StrategySignals.ReverseSell;
			}
			else if (ask < _fib764.Value)
			{
				_signal = StrategySignals.ReverseBuy;
			}
			else if (bid < _fib236.Value && bid > _fib764.Value)
			{
				_signal = StrategySignals.TradingArea;
			}
			else if (ask < _fib91.Value || bid > _fib146.Value)
			{
				_signal = StrategySignals.Continuation;
			}
			else
			{
				_signal = StrategySignals.None;
			}
		}
	}

	private void TryOpenPosition(decimal ask, decimal bid)
	{
		if (Position != 0m || HasActiveOrders)
		return;

		if (!_support.HasValue || !_resistance.HasValue || !_fibLevelsReady || !_highFirst.HasValue)
		return;

		var volume = Volume;
		if (volume <= 0m)
		return;

		switch (_trend)
		{
			case TrendDirections.Up:
			{
				if (_signal == StrategySignals.TradingArea && ask > _resistance.Value)
				{
					SendEntryOrder(Sides.Buy, volume, _support);
				}
				else if (_signal == StrategySignals.ReverseSell && _highFirst == false && ask < _resistance.Value)
				{
					SendEntryOrder(Sides.Sell, volume, _fib146);
				}
				else if (_signal == StrategySignals.ReverseBuy && _highFirst == false && bid < _resistance.Value)
				{
					SendEntryOrder(Sides.Buy, volume, _fib91);
				}
				break;
			}

			case TrendDirections.Down:
			{
				if (_signal == StrategySignals.TradingArea && bid < _support.Value)
				{
					SendEntryOrder(Sides.Sell, volume, _resistance);
				}
				else if (_signal == StrategySignals.ReverseSell && _highFirst == true && bid < _resistance.Value)
				{
					SendEntryOrder(Sides.Sell, volume, _fib91);
				}
				else if (_signal == StrategySignals.ReverseBuy && _highFirst == true && bid < _resistance.Value)
				{
					SendEntryOrder(Sides.Buy, volume, _fib146);
				}
				break;
			}
		}
	}

	private void ManagePosition(decimal ask, decimal bid)
	{
		if (Position == 0m || HasActiveOrders)
		return;

		if (_currentPositionSide == Sides.Buy)
		{
			if (_support.HasValue && bid <= _support.Value)
			{
				SellMarket(Math.Abs(Position));
				return;
			}

			if (_stopLossPrice.HasValue && bid <= _stopLossPrice.Value)
			{
				SellMarket(Math.Abs(Position));
				return;
			}

			UpdateTrailingForLong(bid);
		}
		else if (_currentPositionSide == Sides.Sell)
		{
			if (_resistance.HasValue && ask >= _resistance.Value)
			{
				BuyMarket(Math.Abs(Position));
				return;
			}

			if (_stopLossPrice.HasValue && ask >= _stopLossPrice.Value)
			{
				BuyMarket(Math.Abs(Position));
				return;
			}

			UpdateTrailingForShort(ask);
		}
	}

	private void UpdateTrailingForLong(decimal price)
	{
		if (_currentPositionSide != Sides.Buy || _pipSize <= 0m || _entryPrice <= 0m)
		return;

		var profitDistance = price - _entryPrice;
		if (profitDistance <= 0m)
		return;

		for (var i = _trailingStage; i < _trailingThresholds.Length; i++)
		{
			var threshold = _trailingThresholds[i] * _pipSize;
			if (profitDistance < threshold)
			break;

			var newStop = _entryPrice + (_trailingThresholds[i] - TrailingBuffer) * _pipSize;
			if (!_stopLossPrice.HasValue || newStop > _stopLossPrice.Value)
			_stopLossPrice = newStop;

			_trailingStage = i + 1;
		}
	}

	private void UpdateTrailingForShort(decimal price)
	{
		if (_currentPositionSide != Sides.Sell || _pipSize <= 0m || _entryPrice <= 0m)
		return;

		var profitDistance = _entryPrice - price;
		if (profitDistance <= 0m)
		return;

		for (var i = _trailingStage; i < _trailingThresholds.Length; i++)
		{
			var threshold = _trailingThresholds[i] * _pipSize;
			if (profitDistance < threshold)
			break;

			var newStop = _entryPrice - (_trailingThresholds[i] - TrailingBuffer) * _pipSize;
			if (!_stopLossPrice.HasValue || newStop < _stopLossPrice.Value)
			_stopLossPrice = newStop;

			_trailingStage = i + 1;
		}
	}

	private void UpdateFibonacciLevels()
	{
		var highIndex = DaysBackForHigh;
		var lowIndex = DaysBackForLow;

		if (_dailyCandles.Count <= highIndex || _dailyCandles.Count <= lowIndex)
		{
			_fibLevelsReady = false;
			return;
		}

		var highCandle = _dailyCandles[highIndex];
		var lowCandle = _dailyCandles[lowIndex];

		var high = highCandle.HighPrice;
		var low = lowCandle.LowPrice;

		if (high <= 0m || low <= 0m)
		{
			_fibLevelsReady = false;
			return;
		}

		var range = Math.Abs(high - low);
		if (range <= 0m)
		{
			_fibLevelsReady = false;
			return;
		}

		_highFirst = highIndex <= lowIndex;

		if (_highFirst.Value)
		{
			_fib000 = low;
			_fib146 = low + range * 0.09m;
			_fib236 = low + range * 0.236m;
			_fib382 = low + range * 0.382m;
			_fib50 = (high + low) / 2m;
			_fib618 = low + range * 0.618m;
			_fib764 = low + range * 0.764m;
			_fib91 = low + range * 0.91m;
			_fib100 = high;
			_fib1618 = high + range * 0.618m;
			_fib2618 = high + range + range * 0.618m;
			_fib4236 = high + range * 3m + range * 0.236m;
		}
		else
		{
			_fib000 = high;
			_fib146 = high - range * 0.09m;
			_fib236 = high - range * 0.236m;
			_fib382 = high - range * 0.382m;
			_fib50 = (high + low) / 2m;
			_fib618 = high - range * 0.618m;
			_fib764 = high - range * 0.764m;
			_fib91 = high - range * 0.91m;
			_fib100 = low;
			_fib1618 = low - range * 0.618m;
			_fib2618 = low - range - range * 0.618m;
			_fib4236 = low - range * 3m - range * 0.236m;
		}

		_fibLevelsReady = true;
	}

	private void SendEntryOrder(Sides side, decimal volume, decimal? stopPrice)
	{
		if (side == Sides.Buy)
		{
			BuyMarket(volume);
		}
		else
		{
			SellMarket(volume);
		}

		_pendingEntrySide = side;
		_pendingStopLossPrice = stopPrice;
		_trailingStage = 0;
	}

	private bool HasActiveOrders => Orders.Any(o => o.State.IsActive());

	private void ResetInternalState()
	{
		_intradayCandles.Clear();
		_dailyCandles.Clear();
		_support = null;
		_resistance = null;
		_fib000 = _fib146 = _fib236 = _fib382 = _fib50 = _fib618 = _fib764 = _fib91 = _fib100 = _fib1618 = _fib2618 = _fib4236 = null;
		_highFirst = null;
		_fibLevelsReady = false;
		_trend = TrendDirections.Unknown;
		_signal = StrategySignals.None;
		_pipSize = 0m;
		_entryPrice = 0m;
		_stopLossPrice = null;
		_trailingStage = 0;
		_pendingEntrySide = null;
		_pendingStopLossPrice = null;
		_currentPositionSide = null;
		_ema = null;
	}
}
