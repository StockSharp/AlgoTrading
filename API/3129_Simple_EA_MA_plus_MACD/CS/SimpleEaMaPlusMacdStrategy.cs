using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Port of the MetaTrader "Simple EA MA plus MACD" expert advisor.
/// Generates signals when a shifted moving average stays below/above candle highs while MACD crosses zero.
/// Executes breakout entries above/below the signal bar and manages trades with stop-loss, take-profit, and trailing exit logic.
/// </summary>
public class SimpleEaMaPlusMacdStrategy : Strategy
{
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<decimal> _trailingStepPips;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<int> _maShift;
	private readonly StrategyParam<MaMethod> _maMethod;
	private readonly StrategyParam<AppliedPriceType> _maAppliedPrice;
	private readonly StrategyParam<int> _macdFastPeriod;
	private readonly StrategyParam<int> _macdSlowPeriod;
	private readonly StrategyParam<int> _macdSignalPeriod;
	private readonly StrategyParam<AppliedPriceType> _macdAppliedPrice;
	private readonly StrategyParam<DataType> _candleType;

	private MovingAverage _maIndicator = null!;
	private MovingAverageConvergenceDivergence _macdIndicator = null!;
	private readonly List<decimal> _maSeries = new();

	private decimal? _macdCurrent;
	private decimal? _macdPrev1;
	private decimal? _macdPrev2;

	private bool _pendingBuySignal;
	private bool _pendingSellSignal;
	private decimal _signalHigh;
	private decimal _signalLow;

	private decimal? _lastClosedHigh;
	private decimal? _lastClosedLow;

	private decimal _pipSize;
	private decimal? _longEntryPrice;
	private decimal? _shortEntryPrice;
	private decimal _longHighest;
	private decimal _shortLowest;
	private decimal? _longTrailingLevel;
	private decimal? _shortTrailingLevel;
	private bool _longExitRequested;
	private bool _shortExitRequested;

	/// <summary>
	/// Initializes strategy parameters with defaults matching the original expert advisor.
	/// </summary>
	public SimpleEaMaPlusMacdStrategy()
	{
		_takeProfitPips = Param(nameof(TakeProfitPips), 50m)
			.SetNotNegative()
			.SetDisplay("Take Profit (pips)", "Distance from entry to the profit target in pips", "Risk")
			.SetCanOptimize(true);

		_stopLossPips = Param(nameof(StopLossPips), 50m)
			.SetNotNegative()
			.SetDisplay("Stop Loss (pips)", "Distance from entry to the protective stop in pips", "Risk")
			.SetCanOptimize(true);

		_trailingStopPips = Param(nameof(TrailingStopPips), 5m)
			.SetNotNegative()
			.SetDisplay("Trailing Stop (pips)", "Trailing distance maintained once the trade is in profit", "Risk")
			.SetCanOptimize(true);

		_trailingStepPips = Param(nameof(TrailingStepPips), 5m)
			.SetNotNegative()
			.SetDisplay("Trailing Step (pips)", "Additional progress required before trailing stop is advanced", "Risk")
			.SetCanOptimize(true);

		_maPeriod = Param(nameof(MaPeriod), 100)
			.SetGreaterThanZero()
			.SetDisplay("MA Period", "Number of bars used in the moving average", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(20, 200, 10);

		_maShift = Param(nameof(MaShift), 0)
			.SetNotNegative()
			.SetDisplay("MA Shift", "Forward shift applied to the moving average", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(0, 5, 1);

		_maMethod = Param(nameof(MaMethod), MaMethod.LinearWeighted)
			.SetDisplay("MA Method", "Moving average calculation method", "Indicators")
			.SetCanOptimize(true);

		_maAppliedPrice = Param(nameof(MaAppliedPrice), AppliedPriceType.Weighted)
			.SetDisplay("MA Price", "Price source for moving average input", "Indicators")
			.SetCanOptimize(true);

		_macdFastPeriod = Param(nameof(MacdFastPeriod), 12)
			.SetGreaterThanZero()
			.SetDisplay("MACD Fast EMA", "Length of the fast EMA in MACD", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(6, 18, 2);

		_macdSlowPeriod = Param(nameof(MacdSlowPeriod), 26)
			.SetGreaterThanZero()
			.SetDisplay("MACD Slow EMA", "Length of the slow EMA in MACD", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(18, 40, 2);

		_macdSignalPeriod = Param(nameof(MacdSignalPeriod), 9)
			.SetGreaterThanZero()
			.SetDisplay("MACD Signal", "Signal line smoothing period", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(5, 15, 1);

		_macdAppliedPrice = Param(nameof(MacdAppliedPrice), AppliedPriceType.Weighted)
			.SetDisplay("MACD Price", "Price source fed into MACD", "Indicators")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Primary timeframe used for calculations", "Data")
			.SetCanOptimize(false);
	}

	/// <summary>
	/// Profit target distance in pips.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Stop-loss distance in pips.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Trailing stop distance in pips.
	/// </summary>
	public decimal TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	/// <summary>
	/// Trailing step in pips.
	/// </summary>
	public decimal TrailingStepPips
	{
		get => _trailingStepPips.Value;
		set => _trailingStepPips.Value = value;
	}

	/// <summary>
	/// Moving average length.
	/// </summary>
	public int MaPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	/// <summary>
	/// Horizontal shift applied to the moving average.
	/// </summary>
	public int MaShift
	{
		get => _maShift.Value;
		set => _maShift.Value = value;
	}

	/// <summary>
	/// Moving average smoothing method.
	/// </summary>
	public MaMethod MaMethod
	{
		get => _maMethod.Value;
		set => _maMethod.Value = value;
	}

	/// <summary>
	/// Price source for the moving average.
	/// </summary>
	public AppliedPriceType MaAppliedPrice
	{
		get => _maAppliedPrice.Value;
		set => _maAppliedPrice.Value = value;
	}

	/// <summary>
	/// Fast EMA period inside MACD.
	/// </summary>
	public int MacdFastPeriod
	{
		get => _macdFastPeriod.Value;
		set => _macdFastPeriod.Value = value;
	}

	/// <summary>
	/// Slow EMA period inside MACD.
	/// </summary>
	public int MacdSlowPeriod
	{
		get => _macdSlowPeriod.Value;
		set => _macdSlowPeriod.Value = value;
	}

	/// <summary>
	/// MACD signal line period.
	/// </summary>
	public int MacdSignalPeriod
	{
		get => _macdSignalPeriod.Value;
		set => _macdSignalPeriod.Value = value;
	}

	/// <summary>
	/// Price source for MACD calculations.
	/// </summary>
	public AppliedPriceType MacdAppliedPrice
	{
		get => _macdAppliedPrice.Value;
		set => _macdAppliedPrice.Value = value;
	}

	/// <summary>
	/// Candle type used by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		if (Security is null)
			yield break;

		yield return (Security, CandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_maIndicator = null!;
		_macdIndicator = null!;
		ResetState();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		ResetState();

		_maIndicator = CreateMovingAverage(MaMethod, MaPeriod);
		_macdIndicator = new MovingAverageConvergenceDivergence
		{
			ShortPeriod = MacdFastPeriod,
			LongPeriod = MacdSlowPeriod,
			SignalPeriod = MacdSignalPeriod
		};

		_pipSize = Security?.PriceStep ?? 0m;

		if (Security?.Decimals is int decimals && (decimals == 3 || decimals == 5))
			_pipSize *= 10m;

		if (_pipSize <= 0m)
			_pipSize = Security?.PriceStep ?? 1m;

		var subscription = SubscribeCandles(CandleType);
		subscription.ForEach(ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _maIndicator);
			DrawOwnTrades(area);
		}

		var macdArea = CreateChartArea("MACD");
		if (macdArea != null)
		{
			DrawIndicator(macdArea, _macdIndicator);
		}
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		if (Position > 0)
		{
			_longEntryPrice = Position.AveragePrice ?? _longEntryPrice;
			_longHighest = _longEntryPrice ?? 0m;
			_longTrailingLevel = null;
			_longExitRequested = false;

			_shortEntryPrice = null;
			_shortTrailingLevel = null;
			_shortExitRequested = false;
		}
		else if (Position < 0)
		{
			_shortEntryPrice = Position.AveragePrice ?? _shortEntryPrice;
			_shortLowest = _shortEntryPrice ?? 0m;
			_shortTrailingLevel = null;
			_shortExitRequested = false;

			_longEntryPrice = null;
			_longTrailingLevel = null;
			_longExitRequested = false;
		}
		else
		{
			_longEntryPrice = null;
			_shortEntryPrice = null;
			_longTrailingLevel = null;
			_shortTrailingLevel = null;
			_longExitRequested = false;
			_shortExitRequested = false;
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var exitTriggered = ManageOpenPosition(candle);

		var maInput = GetAppliedPrice(candle, MaAppliedPrice);
		var maValue = _maIndicator.Process(maInput, candle.OpenTime, true);
		if (maValue.IsFinal)
		{
			var maDecimal = maValue.ToDecimal();
			UpdateMaSeries(maDecimal);
		}

		var macdInput = GetAppliedPrice(candle, MacdAppliedPrice);
		var macdRaw = _macdIndicator.Process(macdInput, candle.OpenTime, true);
		if (macdRaw is MovingAverageConvergenceDivergenceValue macdValue && macdValue.Macd is decimal macdLine)
		{
			UpdateMacdSeries(macdLine);
		}

		if (!_maIndicator.IsFormed || !_macdIndicator.IsFormed)
		{
			UpdatePreviousCandleData(candle);
			return;
		}

		if (exitTriggered)
		{
			UpdatePreviousCandleData(candle);
			return;
		}

		EvaluateSignals(candle);
		UpdatePreviousCandleData(candle);
	}

	private bool ManageOpenPosition(ICandleMessage candle)
	{
		var exitTriggered = false;

		if (Position > 0)
		{
			var volume = Math.Abs(Position);
			if (volume <= 0m)
				return false;

			var entryPrice = _longEntryPrice ?? Position.AveragePrice ?? candle.ClosePrice;
			_longEntryPrice ??= entryPrice;

			if (_longHighest < entryPrice)
				_longHighest = entryPrice;

			var trailingDistance = TrailingStopPips > 0m ? TrailingStopPips * _pipSize : 0m;
			var trailingStep = TrailingStepPips > 0m ? TrailingStepPips * _pipSize : 0m;

			if (candle.HighPrice > _longHighest)
			{
				var progress = candle.HighPrice - _longHighest;
				_longHighest = candle.HighPrice;

				if (trailingDistance > 0m && (trailingStep <= 0m || progress >= trailingStep))
				{
					var candidate = candle.HighPrice - trailingDistance;
					if (!_longTrailingLevel.HasValue || candidate > _longTrailingLevel.Value)
						_longTrailingLevel = candidate;
				}
			}

			if (trailingDistance > 0m && _longTrailingLevel.HasValue && candle.LowPrice <= _longTrailingLevel.Value)
			{
				RequestLongExit(volume, "trailing stop hit");
				exitTriggered = true;
			}

			var stopLossDistance = StopLossPips > 0m ? StopLossPips * _pipSize : 0m;
			if (stopLossDistance > 0m && candle.LowPrice <= entryPrice - stopLossDistance)
			{
				RequestLongExit(volume, "stop-loss hit");
				exitTriggered = true;
			}

			var takeProfitDistance = TakeProfitPips > 0m ? TakeProfitPips * _pipSize : 0m;
			if (takeProfitDistance > 0m && candle.HighPrice >= entryPrice + takeProfitDistance)
			{
				RequestLongExit(volume, "take-profit reached");
				exitTriggered = true;
			}
		}
		else if (Position < 0)
		{
			var volume = Math.Abs(Position);
			if (volume <= 0m)
				return false;

			var entryPrice = _shortEntryPrice ?? Position.AveragePrice ?? candle.ClosePrice;
			_shortEntryPrice ??= entryPrice;

			if (_shortLowest <= 0m || _shortLowest > entryPrice)
				_shortLowest = entryPrice;

			var trailingDistance = TrailingStopPips > 0m ? TrailingStopPips * _pipSize : 0m;
			var trailingStep = TrailingStepPips > 0m ? TrailingStepPips * _pipSize : 0m;

			if (_shortLowest > candle.LowPrice)
			{
				var progress = _shortLowest - candle.LowPrice;
				_shortLowest = candle.LowPrice;

				if (trailingDistance > 0m && (trailingStep <= 0m || progress >= trailingStep))
				{
					var candidate = candle.LowPrice + trailingDistance;
					if (!_shortTrailingLevel.HasValue || candidate < _shortTrailingLevel.Value)
						_shortTrailingLevel = candidate;
				}
			}

			if (trailingDistance > 0m && _shortTrailingLevel.HasValue && candle.HighPrice >= _shortTrailingLevel.Value)
			{
				RequestShortExit(volume, "trailing stop hit");
				exitTriggered = true;
			}

			var stopLossDistance = StopLossPips > 0m ? StopLossPips * _pipSize : 0m;
			if (stopLossDistance > 0m && candle.HighPrice >= entryPrice + stopLossDistance)
			{
				RequestShortExit(volume, "stop-loss hit");
				exitTriggered = true;
			}

			var takeProfitDistance = TakeProfitPips > 0m ? TakeProfitPips * _pipSize : 0m;
			if (takeProfitDistance > 0m && candle.LowPrice <= entryPrice - takeProfitDistance)
			{
				RequestShortExit(volume, "take-profit reached");
				exitTriggered = true;
			}
		}
		else
		{
			_longExitRequested = false;
			_shortExitRequested = false;
		}

		return exitTriggered;
	}

	private void EvaluateSignals(ICandleMessage candle)
	{
		if (_pendingBuySignal || _pendingSellSignal)
		{
			HandlePendingBreakouts(candle);
			return;
		}

		if (!TryGetMaValue(0, out var maCurrent) || !TryGetMaValue(1, out var maPrevious))
			return;

		if (_lastClosedHigh is not decimal prevHigh || _lastClosedLow is not decimal prevLow)
			return;

		if (_macdPrev1 is not decimal macdPrev || _macdPrev2 is not decimal macdPrev2)
			return;

		var currentHigh = candle.HighPrice;
		var currentLow = candle.LowPrice;

		if (maPrevious < prevHigh && maCurrent < currentHigh && macdPrev2 < 0m && macdPrev > 0m)
		{
			_pendingBuySignal = true;
			_pendingSellSignal = false;
			_signalHigh = prevHigh;
			_signalLow = prevLow;
			LogInfo($"Buy signal detected. Previous MA below candle highs and MACD crossed above zero at {candle.OpenTime:u}.");
			return;
		}

		if (maPrevious > prevHigh && maCurrent > currentHigh && macdPrev2 > 0m && macdPrev < 0m)
		{
			_pendingSellSignal = true;
			_pendingBuySignal = false;
			_signalHigh = prevHigh;
			_signalLow = prevLow;
			LogInfo($"Sell signal detected. Previous MA above candle highs and MACD crossed below zero at {candle.OpenTime:u}.");
		}
	}

	private void HandlePendingBreakouts(ICandleMessage candle)
	{
		var closePrice = candle.ClosePrice;

		if (_pendingBuySignal)
		{
			if (closePrice > _signalHigh)
			{
				TryOpenLong(candle);
				ResetSignals();
			}
			else if (closePrice < _signalLow)
			{
				LogInfo($"Buy signal cancelled: close {closePrice:F5} fell below signal low {_signalLow:F5}.");
				ResetSignals();
			}
		}
		else if (_pendingSellSignal)
		{
			if (closePrice < _signalLow)
			{
				TryOpenShort(candle);
				ResetSignals();
			}
			else if (closePrice > _signalHigh)
			{
				LogInfo($"Sell signal cancelled: close {closePrice:F5} rose above signal high {_signalHigh:F5}.");
				ResetSignals();
			}
		}
	}

	private void TryOpenLong(ICandleMessage candle)
	{
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var volume = Volume;

		if (Position < 0)
			volume += Math.Abs(Position);
		else if (Position > 0)
			return;

		if (volume <= 0m)
			return;

		_longEntryPrice ??= candle.ClosePrice;
		_longHighest = Math.Max(_longEntryPrice.Value, candle.HighPrice);
		_longTrailingLevel = null;

		BuyMarket(volume);
		LogInfo($"Entered long position after breakout above {_signalHigh:F5} on candle {candle.OpenTime:u}.");
	}

	private void TryOpenShort(ICandleMessage candle)
	{
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var volume = Volume;

		if (Position > 0)
			volume += Math.Abs(Position);
		else if (Position < 0)
			return;

		if (volume <= 0m)
			return;

		_shortEntryPrice ??= candle.ClosePrice;
		_shortLowest = Math.Min(_shortEntryPrice.Value, candle.LowPrice);
		_shortTrailingLevel = null;

		SellMarket(volume);
		LogInfo($"Entered short position after breakout below {_signalLow:F5} on candle {candle.OpenTime:u}.");
	}

	private void RequestLongExit(decimal volume, string reason)
	{
		if (_longExitRequested)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		SellMarket(volume);
		_longExitRequested = true;
		LogInfo($"Closing long position because {reason}.");
	}

	private void RequestShortExit(decimal volume, string reason)
	{
		if (_shortExitRequested)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		BuyMarket(volume);
		_shortExitRequested = true;
		LogInfo($"Closing short position because {reason}.");
	}

	private void UpdateMaSeries(decimal value)
	{
		_maSeries.Add(value);
		var maxSize = Math.Max(MaShift + 3, 3);
		while (_maSeries.Count > maxSize)
			_maSeries.RemoveAt(0);
	}

	private bool TryGetMaValue(int offset, out decimal value)
	{
		var index = _maSeries.Count - 1 - MaShift - offset;
		if (index < 0 || index >= _maSeries.Count)
		{
			value = 0m;
			return false;
		}

		value = _maSeries[index];
		return true;
	}

	private void UpdateMacdSeries(decimal macdLine)
	{
		_macdPrev2 = _macdPrev1;
		_macdPrev1 = _macdCurrent;
		_macdCurrent = macdLine;
	}

	private void UpdatePreviousCandleData(ICandleMessage candle)
	{
		_lastClosedHigh = candle.HighPrice;
		_lastClosedLow = candle.LowPrice;
	}

	private void ResetSignals()
	{
		_pendingBuySignal = false;
		_pendingSellSignal = false;
		_signalHigh = 0m;
		_signalLow = 0m;
	}

	private void ResetState()
	{
		_maSeries.Clear();
		_macdCurrent = null;
		_macdPrev1 = null;
		_macdPrev2 = null;
		_pendingBuySignal = false;
		_pendingSellSignal = false;
		_signalHigh = 0m;
		_signalLow = 0m;
		_lastClosedHigh = null;
		_lastClosedLow = null;
		_longEntryPrice = null;
		_shortEntryPrice = null;
		_longHighest = 0m;
		_shortLowest = 0m;
		_longTrailingLevel = null;
		_shortTrailingLevel = null;
		_longExitRequested = false;
		_shortExitRequested = false;
	}

	private static MovingAverage CreateMovingAverage(MaMethod method, int period)
	{
		var length = Math.Max(1, period);
		return method switch
		{
			MaMethod.Simple => new SimpleMovingAverage { Length = length },
			MaMethod.Exponential => new ExponentialMovingAverage { Length = length },
			MaMethod.Smoothed => new SmoothedMovingAverage { Length = length },
			MaMethod.LinearWeighted => new WeightedMovingAverage { Length = length },
			_ => new WeightedMovingAverage { Length = length }
		};
	}

	private static decimal GetAppliedPrice(ICandleMessage candle, AppliedPriceType type)
	{
		return type switch
		{
			AppliedPriceType.Open => candle.OpenPrice,
			AppliedPriceType.High => candle.HighPrice,
			AppliedPriceType.Low => candle.LowPrice,
			AppliedPriceType.Median => (candle.HighPrice + candle.LowPrice) / 2m,
			AppliedPriceType.Typical => (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m,
			AppliedPriceType.Weighted => (candle.HighPrice + candle.LowPrice + candle.ClosePrice + candle.ClosePrice) / 4m,
			_ => candle.ClosePrice
		};
	}
}

/// <summary>
/// Moving average methods supported by the strategy.
/// </summary>
public enum MaMethod
{
	Simple,
	Exponential,
	Smoothed,
	LinearWeighted
}

/// <summary>
/// Price sources replicating MetaTrader applied price options.
/// </summary>
public enum AppliedPriceType
{
	Close,
	Open,
	High,
	Low,
	Median,
	Typical,
	Weighted
}
