using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Conversion of the MetaTrader 5 expert advisor "Starter".
/// Uses the Commodity Channel Index and a moving average slope filter to open trades with adaptive position sizing and trailing protection.
/// </summary>
public class StarterStrategy : Strategy
{
	private readonly StrategyParam<decimal> _maximumRisk;
	private readonly StrategyParam<decimal> _decreaseFactor;
	private readonly StrategyParam<int> _cciPeriod;
	private readonly StrategyParam<decimal> _cciLevel;
	private readonly StrategyParam<int> _cciCurrentBar;
	private readonly StrategyParam<int> _cciPreviousBar;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<MovingAverageMethod> _maMethod;
	private readonly StrategyParam<int> _maCurrentBar;
	private readonly StrategyParam<decimal> _maDelta;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<decimal> _trailingStepPips;
	private readonly StrategyParam<DataType> _candleType;

	private CommodityChannelIndex _cci = null!;
	private LengthIndicator<decimal> _movingAverage = null!;

	private readonly List<decimal> _cciHistory = new();
	private readonly List<decimal> _maHistory = new();

	private decimal _pipSize;
	private int _historyCapacity;

	private decimal? _longEntryPrice;
	private decimal? _shortEntryPrice;
	private decimal? _longStop;
	private decimal? _shortStop;

	private decimal _signedPosition;
	private Sides? _lastEntrySide;
	private decimal _lastEntryPrice;
	private int _consecutiveLosses;

	/// <summary>
	/// Initializes a new instance of the <see cref="StarterStrategy"/> class.
	/// </summary>
	public StarterStrategy()
	{
		_maximumRisk = Param(nameof(MaximumRisk), 0.02m)
			.SetNotNegative()
			.SetDisplay("Maximum Risk", "Fraction of portfolio equity risked per trade", "Risk Management");

		_decreaseFactor = Param(nameof(DecreaseFactor), 3m)
			.SetNotNegative()
			.SetDisplay("Decrease Factor", "Lot reduction factor after consecutive losses", "Risk Management");

		_cciPeriod = Param(nameof(CciPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("CCI Period", "Number of bars for the Commodity Channel Index", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(5, 60, 1);

		_cciLevel = Param(nameof(CciLevel), 100m)
			.SetGreaterThanZero()
			.SetDisplay("CCI Level", "Threshold used for oversold/overbought detection", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(50m, 200m, 10m);

		_cciCurrentBar = Param(nameof(CciCurrentBar), 0)
			.SetNotNegative()
			.SetDisplay("CCI Current Bar", "Shift for the current CCI value", "Indicators");

		_cciPreviousBar = Param(nameof(CciPreviousBar), 1)
			.SetNotNegative()
			.SetDisplay("CCI Previous Bar", "Shift for the previous CCI value", "Indicators");

		_maPeriod = Param(nameof(MaPeriod), 120)
			.SetGreaterThanZero()
			.SetDisplay("MA Period", "Number of bars for the moving average", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(20, 200, 5);

		_maMethod = Param(nameof(MaMethod), MovingAverageMethod.Simple)
			.SetDisplay("MA Method", "Smoothing method applied to the moving average", "Indicators");

		_maCurrentBar = Param(nameof(MaCurrentBar), 0)
			.SetNotNegative()
			.SetDisplay("MA Current Bar", "Shift for the moving average", "Indicators");

		_maDelta = Param(nameof(MaDelta), 0.001m)
			.SetNotNegative()
			.SetDisplay("MA Delta", "Minimum slope difference between current and previous MA", "Signals")
			.SetCanOptimize(true)
			.SetOptimize(0.0001m, 0.01m, 0.0001m);

		_stopLossPips = Param(nameof(StopLossPips), 0m)
			.SetNotNegative()
			.SetDisplay("Stop Loss (pips)", "Initial protective stop distance in pips", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(0m, 200m, 10m);

		_trailingStopPips = Param(nameof(TrailingStopPips), 5m)
			.SetNotNegative()
			.SetDisplay("Trailing Stop (pips)", "Base trailing distance in pips", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(0m, 200m, 5m);

		_trailingStepPips = Param(nameof(TrailingStepPips), 5m)
			.SetNotNegative()
			.SetDisplay("Trailing Step (pips)", "Minimum improvement required before moving the trailing stop", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(0m, 200m, 5m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Primary timeframe processed by the strategy", "General");
	}

	/// <summary>
	/// Risk per trade expressed as a fraction of portfolio equity.
	/// </summary>
	public decimal MaximumRisk
	{
		get => _maximumRisk.Value;
		set => _maximumRisk.Value = value;
	}

	/// <summary>
	/// Lot reduction factor applied after consecutive losing trades.
	/// </summary>
	public decimal DecreaseFactor
	{
		get => _decreaseFactor.Value;
		set => _decreaseFactor.Value = value;
	}

	/// <summary>
	/// Period for the Commodity Channel Index indicator.
	/// </summary>
	public int CciPeriod
	{
		get => _cciPeriod.Value;
		set => _cciPeriod.Value = value;
	}

	/// <summary>
	/// Overbought/oversold CCI threshold.
	/// </summary>
	public decimal CciLevel
	{
		get => _cciLevel.Value;
		set => _cciLevel.Value = value;
	}

	/// <summary>
	/// Index of the bar considered "current" for CCI comparisons.
	/// </summary>
	public int CciCurrentBar
	{
		get => _cciCurrentBar.Value;
		set => _cciCurrentBar.Value = value;
	}

	/// <summary>
	/// Index of the bar considered "previous" for CCI comparisons.
	/// </summary>
	public int CciPreviousBar
	{
		get => _cciPreviousBar.Value;
		set => _cciPreviousBar.Value = value;
	}

	/// <summary>
	/// Period for the trend filter moving average.
	/// </summary>
	public int MaPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	/// <summary>
	/// Moving average smoothing method.
	/// </summary>
	public MovingAverageMethod MaMethod
	{
		get => _maMethod.Value;
		set => _maMethod.Value = value;
	}

	/// <summary>
	/// Shift for the moving average value considered "current".
	/// </summary>
	public int MaCurrentBar
	{
		get => _maCurrentBar.Value;
		set => _maCurrentBar.Value = value;
	}

	/// <summary>
	/// Minimum slope difference between current and previous moving average values.
	/// </summary>
	public decimal MaDelta
	{
		get => _maDelta.Value;
		set => _maDelta.Value = value;
	}

	/// <summary>
	/// Initial stop-loss distance expressed in pips.
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
	/// Minimum improvement before advancing the trailing stop in pips.
	/// </summary>
	public decimal TrailingStepPips
	{
		get => _trailingStepPips.Value;
		set => _trailingStepPips.Value = value;
	}

	/// <summary>
	/// Candle data type processed by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_pipSize = GetPipSize();
		_historyCapacity = CalculateHistoryCapacity();
		_cciHistory.Clear();
		_maHistory.Clear();
		_longEntryPrice = null;
		_shortEntryPrice = null;
		_longStop = null;
		_shortStop = null;
		_signedPosition = 0m;
		_lastEntrySide = null;
		_lastEntryPrice = 0m;
		_consecutiveLosses = 0;

		_cci = new CommodityChannelIndex { Length = CciPeriod };
		_movingAverage = CreateMovingAverage(MaMethod, MaPeriod);

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_cci, _movingAverage, OnProcessCandle)
			.Start();
	}

	private void OnProcessCandle(ICandleMessage candle, decimal cciValue, decimal maValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_cci.IsFormed || !_movingAverage.IsFormed)
			return;

		// Store the latest indicator values so we can access shifted history like in MetaTrader.
		AddHistory(_cciHistory, cciValue);
		AddHistory(_maHistory, maValue);

		if (Position != 0)
		{
			// Manage trailing stop and protective exits for open positions before evaluating new entries.
			UpdateTrailing(candle);
			CheckProtectiveStops(candle);
		}

		if (Position != 0)
			// The original expert only opens a new position when no trades are active.
			return;

		if (!TryGetHistoryValue(_maHistory, MaCurrentBar, out var maCurrent) ||
			!TryGetHistoryValue(_maHistory, MaCurrentBar + 1, out var maPrevious))
			return;

		if (!TryGetHistoryValue(_cciHistory, CciCurrentBar, out var cciCurrent) ||
			!TryGetHistoryValue(_cciHistory, CciPreviousBar, out var cciPrevious))
			return;

		// Compare the moving average slope and CCI swings to detect breakout conditions.
		var maSlope = maCurrent - maPrevious;

		if (maSlope > MaDelta && cciCurrent > cciPrevious &&
			cciCurrent > -CciLevel && cciPrevious < -CciLevel)
		{
			TryEnterLong(candle.ClosePrice);
		}
		else if (maSlope < -MaDelta && cciCurrent < cciPrevious &&
			cciCurrent < CciLevel && cciPrevious > CciLevel)
		{
			TryEnterShort(candle.ClosePrice);
		}
	}

	private void TryEnterLong(decimal price)
	{
		var volume = CalculateTradeVolume(price);
		if (volume <= 0m)
			return;

		BuyMarket(volume);
		LogInfo($"Opening long position at {price} with volume {volume}.");
	}

	private void TryEnterShort(decimal price)
	{
		var volume = CalculateTradeVolume(price);
		if (volume <= 0m)
			return;

		SellMarket(volume);
		LogInfo($"Opening short position at {price} with volume {volume}.");
	}

	private void CheckProtectiveStops(ICandleMessage candle)
	{
		if (Position > 0 && _longStop.HasValue && candle.LowPrice <= _longStop.Value)
		{
			var volume = Math.Abs(Position);
			if (volume > 0)
			{
				SellMarket(volume);
				LogInfo($"Long stop-loss triggered at {_longStop.Value}.");
			}

			ResetLongProtection();
			return;
		}

		if (Position < 0 && _shortStop.HasValue && candle.HighPrice >= _shortStop.Value)
		{
			var volume = Math.Abs(Position);
			if (volume > 0)
			{
				BuyMarket(volume);
				LogInfo($"Short stop-loss triggered at {_shortStop.Value}.");
			}

			ResetShortProtection();
		}
	}

	private void UpdateTrailing(ICandleMessage candle)
	{
		if (TrailingStopPips <= 0m || _pipSize <= 0m)
			return;

		var offset = TrailingStopPips * _pipSize;
		var step = TrailingStepPips * _pipSize;

		if (Position > 0 && _longEntryPrice.HasValue)
		{
			// Advance the long stop only when price improves by at least the configured step.
			var targetStop = candle.ClosePrice - offset;
			var threshold = candle.ClosePrice - (offset + step);

			if (!_longStop.HasValue || _longStop.Value < threshold)
			{
				_longStop = targetStop;
				LogInfo($"Trailing long stop moved to {_longStop.Value}.");
			}
		}
		else if (Position < 0 && _shortEntryPrice.HasValue)
		{
			// Mirror the trailing logic for short positions.
			var targetStop = candle.ClosePrice + offset;
			var threshold = candle.ClosePrice + (offset + step);

			if (!_shortStop.HasValue || _shortStop.Value > threshold)
			{
				_shortStop = targetStop;
				LogInfo($"Trailing short stop moved to {_shortStop.Value}.");
			}
		}
	}

	private decimal CalculateTradeVolume(decimal price)
	{
		// Start from the configured strategy volume; fall back to 1 if undefined.
		var baseVolume = Volume > 0 ? Volume : 1m;

		if (price <= 0m)
			return NormalizeVolume(baseVolume);

		var equity = Portfolio?.CurrentValue ?? 0m;
		if (equity <= 0m || MaximumRisk <= 0m)
			return NormalizeVolume(baseVolume);

		// Position size equals equity * risk percent divided by price, mimicking the original risk formula.
		var volume = equity * MaximumRisk / price;

		if (DecreaseFactor > 0m && _consecutiveLosses > 1)
		{
			// Reduce the lot size after two or more losses, replicating MetaTrader's "DecreaseFactor" behavior.
			var reduction = volume * _consecutiveLosses / DecreaseFactor;
			volume -= reduction;
		}

		if (volume <= 0m)
			volume = baseVolume;

		return NormalizeVolume(volume);
	}

	private decimal NormalizeVolume(decimal volume)
	{
		var security = Security;
		if (security != null)
		{
			var step = security.VolumeStep ?? 1m;
			if (step <= 0m)
				step = 1m;

			if (volume < step)
				volume = step;

			var steps = Math.Floor(volume / step);
			if (steps < 1m)
				steps = 1m;

			volume = steps * step;
		}

		if (volume <= 0m)
			volume = 1m;

		return volume;
	}

	private void AddHistory(List<decimal> history, decimal value)
	{
		history.Add(value);

		if (history.Count > _historyCapacity)
			history.RemoveRange(0, history.Count - _historyCapacity);
	}

	private static bool TryGetHistoryValue(List<decimal> history, int shift, out decimal value)
	{
		value = default;

		if (shift < 0)
			return false;

		var index = history.Count - 1 - shift;
		if (index < 0 || index >= history.Count)
			return false;

		value = history[index];
		return true;
	}

	private void ResetLongProtection()
	{
		_longEntryPrice = null;
		_longStop = null;
	}

	private void ResetShortProtection()
	{
		_shortEntryPrice = null;
		_shortStop = null;
	}

	private decimal GetPipSize()
	{
		var security = Security;
		if (security == null)
			return 0m;

		var step = security.PriceStep ?? 0m;
		if (step <= 0m)
			return 0m;

		return step;
	}

	private int CalculateHistoryCapacity()
	{
		var cciRequirement = Math.Max(CciCurrentBar, CciPreviousBar) + CciPeriod + 5;
		var maRequirement = MaCurrentBar + MaPeriod + 5;

		return Math.Max(cciRequirement, maRequirement);
	}

	private static LengthIndicator<decimal> CreateMovingAverage(MovingAverageMethod method, int period)
	{
		return method switch
		{
			MovingAverageMethod.Simple => new SimpleMovingAverage { Length = period },
			MovingAverageMethod.Exponential => new ExponentialMovingAverage { Length = period },
			MovingAverageMethod.Smoothed => new SmoothedMovingAverage { Length = period },
			MovingAverageMethod.LinearWeighted => new WeightedMovingAverage { Length = period },
			_ => new SimpleMovingAverage { Length = period }
		};
	}

	/// <inheritdoc />
	protected override void OnOwnTradeReceived(MyTrade trade)
	{
		base.OnOwnTradeReceived(trade);

		// Track executed volume to update position state and the loss streak counter.
		var volume = trade.Trade.Volume;
		if (volume <= 0m)
			return;

		var delta = trade.Order.Side == Sides.Buy ? volume : -volume;
		var previousPosition = _signedPosition;
		_signedPosition += delta;

		if (previousPosition == 0m && _signedPosition != 0m)
		{
			_lastEntrySide = trade.Order.Side;
			_lastEntryPrice = trade.Trade.Price;

			if (_lastEntrySide == Sides.Buy)
			{
				_longEntryPrice = trade.Trade.Price;
				_longStop = StopLossPips > 0m && _pipSize > 0m ? _lastEntryPrice - (StopLossPips * _pipSize) : null;
				ResetShortProtection();
			}
			else if (_lastEntrySide == Sides.Sell)
			{
				_shortEntryPrice = trade.Trade.Price;
				_shortStop = StopLossPips > 0m && _pipSize > 0m ? _lastEntryPrice + (StopLossPips * _pipSize) : null;
				ResetLongProtection();
			}
		}
		else if (previousPosition != 0m && _signedPosition == 0m)
		{
			var exitPrice = trade.Trade.Price;

			if (_lastEntrySide != null && _lastEntryPrice != 0m)
			{
				var profit = _lastEntrySide == Sides.Buy
					? exitPrice - _lastEntryPrice
					: _lastEntryPrice - exitPrice;

				if (profit > 0m)
				{
					_consecutiveLosses = 0;
				}
				else if (profit < 0m)
				{
					_consecutiveLosses++;
				}
			}

			_lastEntrySide = null;
			_lastEntryPrice = 0m;
			ResetLongProtection();
			ResetShortProtection();
		}
	}
}

/// <summary>
/// Moving average methods supported by <see cref="StarterStrategy"/>.
/// </summary>
public enum MovingAverageMethod
{
	/// <summary>
	/// Simple moving average (equivalent to MODE_SMA in MetaTrader).
	/// </summary>
	Simple,

	/// <summary>
	/// Exponential moving average (MODE_EMA).
	/// </summary>
	Exponential,

	/// <summary>
	/// Smoothed moving average (MODE_SMMA).
	/// </summary>
	Smoothed,

	/// <summary>
	/// Linear weighted moving average (MODE_LWMA).
	/// </summary>
	LinearWeighted
}
