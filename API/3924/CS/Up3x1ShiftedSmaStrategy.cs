using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Conversion of the MetaTrader 4 expert "up3x1.mq4" that trades a triple shifted SMA crossover.
/// The strategy manages protective exits with take profit, stop loss and trailing logic similar to the script.
/// </summary>
public class Up3x1ShiftedSmaStrategy : Strategy
{
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _mediumPeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _trailingStopPoints;
	private readonly StrategyParam<decimal> _baseVolume;
	private readonly StrategyParam<decimal> _riskFraction;
	private readonly StrategyParam<DataType> _candleType;

	private SimpleMovingAverage _fastSma;
	private SimpleMovingAverage _mediumSma;
	private SimpleMovingAverage _slowSma;

	private bool _hasPrevValues;
	private decimal _prevFast;
	private decimal _prevMedium;
	private decimal _prevSlow;

	private decimal _entryPrice;
	private decimal _highestPrice;
	private decimal _lowestPrice;

	private int _consecutiveLosses;

	public Up3x1ShiftedSmaStrategy()
	{
		_fastPeriod = Param(nameof(FastPeriod), 24)
			.SetDisplay("Fast SMA period")
			.SetCanOptimize(true);

		_mediumPeriod = Param(nameof(MediumPeriod), 60)
			.SetDisplay("Medium SMA period")
			.SetCanOptimize(true);

		_slowPeriod = Param(nameof(SlowPeriod), 120)
			.SetDisplay("Slow SMA period")
			.SetCanOptimize(true);

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 150m)
			.SetDisplay("Take profit distance in points")
			.SetCanOptimize(true);

		_stopLossPoints = Param(nameof(StopLossPoints), 100m)
			.SetDisplay("Stop loss distance in points")
			.SetCanOptimize(true);

		_trailingStopPoints = Param(nameof(TrailingStopPoints), 100m)
			.SetDisplay("Trailing stop distance in points")
			.SetCanOptimize(true);

		_baseVolume = Param(nameof(BaseVolume), 0.1m)
			.SetDisplay("Fallback trade volume")
			.SetCanOptimize(true);

		_riskFraction = Param(nameof(RiskFraction), 0.00002m)
			.SetDisplay("Fraction of portfolio value used for sizing")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), DataType.TimeFrame(TimeSpan.FromHours(1)))
			.SetDisplay("Candle type for analysis");
	}

	public int FastPeriod
	{
		get => _fastPeriod.Value;
		set => _fastPeriod.Value = value;
	}

	public int MediumPeriod
	{
		get => _mediumPeriod.Value;
		set => _mediumPeriod.Value = value;
	}

	public int SlowPeriod
	{
		get => _slowPeriod.Value;
		set => _slowPeriod.Value = value;
	}

	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	public decimal TrailingStopPoints
	{
		get => _trailingStopPoints.Value;
		set => _trailingStopPoints.Value = value;
	}

	public decimal BaseVolume
	{
		get => _baseVolume.Value;
		set => _baseVolume.Value = value;
	}

	public decimal RiskFraction
	{
		get => _riskFraction.Value;
		set => _riskFraction.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_fastSma = new SimpleMovingAverage
		{
			Length = FastPeriod
		};

		_mediumSma = new SimpleMovingAverage
		{
			Length = MediumPeriod
		};

		_slowSma = new SimpleMovingAverage
		{
			Length = SlowPeriod
		};

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(_fastSma, _mediumSma, _slowSma, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal fastValue, decimal mediumValue, decimal slowValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_fastSma.IsFormed || !_mediumSma.IsFormed || !_slowSma.IsFormed)
			return;

		if (!_hasPrevValues)
		{
			// Store the very first values to mimic the "shift 1" access from MQL.
			_prevFast = fastValue;
			_prevMedium = mediumValue;
			_prevSlow = slowValue;
			_hasPrevValues = true;
			return;
		}

		if (Position == 0)
		{
			// No position opened yet - check if a crossover took place.
			TryEnter(candle, fastValue, mediumValue, slowValue);
		}
		else if (Position > 0)
		{
			UpdateHighLow(candle);

			if (!TryHandleLongExit(candle, fastValue, mediumValue, slowValue))
			{
				// Apply trailing stop only when no other exit condition fired.
				TryApplyLongTrailing(candle);
			}
		}
		else
		{
			UpdateHighLow(candle);

			if (!TryHandleShortExit(candle, fastValue, mediumValue, slowValue))
			{
				// Apply trailing stop only when no other exit condition fired.
				TryApplyShortTrailing(candle);
			}
		}

		_prevFast = fastValue;
		_prevMedium = mediumValue;
		_prevSlow = slowValue;
	}

	private void TryEnter(ICandleMessage candle, decimal fastValue, decimal mediumValue, decimal slowValue)
	{
		var volume = CalculateOrderVolume();

		if (volume <= 0m)
		{
			LogWarning("Calculated volume is zero, entry skipped.");
			return;
		}

		// Buy when the previous fast/medium/slow SMAs were aligned upward and the current bar keeps the order.
		var buyCondition = _prevFast < _prevMedium && _prevMedium < _prevSlow &&
			mediumValue < fastValue && fastValue < slowValue;

		if (buyCondition)
		{
			BuyMarket(volume);
			_entryPrice = candle.ClosePrice;
			_highestPrice = candle.HighPrice;
			_lowestPrice = candle.LowPrice;
			LogInfo($"Enter long at {_entryPrice} with volume {volume}.");
			return;
		}

		// Sell when the previous fast/medium/slow SMAs were aligned downward and the current bar keeps the order.
		var sellCondition = _prevFast > _prevMedium && _prevMedium > _prevSlow &&
			mediumValue > fastValue && fastValue > slowValue;

		if (sellCondition)
		{
			SellMarket(volume);
			_entryPrice = candle.ClosePrice;
			_highestPrice = candle.HighPrice;
			_lowestPrice = candle.LowPrice;
			LogInfo($"Enter short at {_entryPrice} with volume {volume}.");
		}
	}

	private void UpdateHighLow(ICandleMessage candle)
	{
		if (Position > 0)
		{
			// Track the highest price reached by the position for trailing stop calculations.
			_highestPrice = Math.Max(_highestPrice, candle.HighPrice);
			_lowestPrice = _lowestPrice == 0m ? candle.LowPrice : Math.Min(_lowestPrice, candle.LowPrice);
		}
		else if (Position < 0)
		{
			// Track both extremes for short positions.
			_lowestPrice = _lowestPrice == 0m ? candle.LowPrice : Math.Min(_lowestPrice, candle.LowPrice);
			_highestPrice = Math.Max(_highestPrice, candle.HighPrice);
		}
	}

	private bool TryHandleLongExit(ICandleMessage candle, decimal fastValue, decimal mediumValue, decimal slowValue)
	{
		var exitPrice = 0m;
		string reason = null;

		var takeOffset = GetPriceOffset(TakeProfitPoints);
		var stopOffset = GetPriceOffset(StopLossPoints);

		if (TakeProfitPoints > 0m)
		{
			var target = _entryPrice + takeOffset;
			if (candle.HighPrice >= target)
			{
				exitPrice = target;
				reason = "Take profit reached";
			}
		}

		if (exitPrice == 0m && StopLossPoints > 0m)
		{
			var stop = _entryPrice - stopOffset;
			if (candle.LowPrice <= stop)
			{
				exitPrice = stop;
				reason = "Stop loss triggered";
			}
		}

		if (exitPrice == 0m)
		{
			// Mirror the original "ma6 < ma4 < ma5" reversal exit.
			var reversal = _prevFast > _prevMedium && _prevMedium > _prevSlow &&
				slowValue < fastValue && fastValue < mediumValue;

			if (reversal)
			{
				exitPrice = candle.ClosePrice;
				reason = "Moving average reversal";
			}
		}

		if (exitPrice == 0m)
			return false;

		ExitPosition(exitPrice, reason);
		return true;
	}

	private bool TryHandleShortExit(ICandleMessage candle, decimal fastValue, decimal mediumValue, decimal slowValue)
	{
		var exitPrice = 0m;
		string reason = null;

		var takeOffset = GetPriceOffset(TakeProfitPoints);
		var stopOffset = GetPriceOffset(StopLossPoints);

		if (TakeProfitPoints > 0m)
		{
			var target = _entryPrice - takeOffset;
			if (candle.LowPrice <= target)
			{
				exitPrice = target;
				reason = "Take profit reached";
			}
		}

		if (exitPrice == 0m && StopLossPoints > 0m)
		{
			var stop = _entryPrice + stopOffset;
			if (candle.HighPrice >= stop)
			{
				exitPrice = stop;
				reason = "Stop loss triggered";
			}
		}

		if (exitPrice == 0m)
		{
			// Mirror the original "ma6 > ma4 > ma5" reversal exit.
			var reversal = _prevFast < _prevMedium && _prevMedium < _prevSlow &&
				slowValue > fastValue && fastValue > mediumValue;

			if (reversal)
			{
				exitPrice = candle.ClosePrice;
				reason = "Moving average reversal";
			}
		}

		if (exitPrice == 0m)
			return false;

		ExitPosition(exitPrice, reason);
		return true;
	}

	private void TryApplyLongTrailing(ICandleMessage candle)
	{
		if (TrailingStopPoints <= 0m || Position <= 0)
			return;

		var trailOffset = GetPriceOffset(TrailingStopPoints);

		if (_highestPrice - _entryPrice < trailOffset)
			return;

		var trail = _highestPrice - trailOffset;

		if (candle.LowPrice > trail)
			return;

		// Lock in profits by exiting at the computed trailing stop level.
		ExitPosition(trail, "Trailing stop hit");
	}

	private void TryApplyShortTrailing(ICandleMessage candle)
	{
		if (TrailingStopPoints <= 0m || Position >= 0)
			return;

		var trailOffset = GetPriceOffset(TrailingStopPoints);

		if (_entryPrice - _lowestPrice < trailOffset)
			return;

		var trail = _lowestPrice + trailOffset;

		if (candle.HighPrice < trail)
			return;

		// Lock in profits by exiting at the computed trailing stop level.
		ExitPosition(trail, "Trailing stop hit");
	}

	private void ExitPosition(decimal exitPrice, string reason)
	{
		var isLong = Position > 0;
		var volume = Math.Abs(Position);

		if (volume <= 0m)
			return;

		if (isLong)
			SellMarket(volume);
		else
			BuyMarket(volume);

		var pnl = isLong
			? (exitPrice - _entryPrice) * volume
			: (_entryPrice - exitPrice) * volume;

		LogInfo($"Exit {(isLong ? "long" : "short")} at {exitPrice} because {reason}. Approx PnL: {pnl}.");

		var isLoss = pnl < 0m;

		if (isLoss)
			_consecutiveLosses++;
		else
			_consecutiveLosses = 0;

		ResetState();
	}

	private decimal CalculateOrderVolume()
	{
		var volume = BaseVolume;

		var portfolioValue = Portfolio?.CurrentValue ?? 0m;

		if (portfolioValue > 0m && RiskFraction > 0m)
		{
			var dynamicVolume = portfolioValue * RiskFraction;

			if (dynamicVolume > 0m)
				volume = dynamicVolume;
		}

		if (_consecutiveLosses > 1)
		{
			var reduction = volume * _consecutiveLosses / 3m;
			volume -= reduction;
		}

		if (volume < BaseVolume)
			volume = BaseVolume;

		volume = AdjustVolumeToInstrument(volume);

		return volume;
	}

	private decimal AdjustVolumeToInstrument(decimal volume)
	{
		var security = Security;

		if (security == null)
			return volume;

		var step = security.VolumeStep ?? 0m;

		if (step > 0m)
			volume = Math.Floor(volume / step) * step;

		var minVolume = security.MinVolume ?? 0m;

		if (minVolume > 0m && volume < minVolume)
			return 0m;

		var maxVolume = security.MaxVolume ?? 0m;

		if (maxVolume > 0m && volume > maxVolume)
			volume = maxVolume;

		return volume;
	}

	private decimal GetPriceOffset(decimal points)
	{
		var step = Security?.PriceStep ?? 0m;

		if (step <= 0m)
			return points;

		return points * step;
	}

	private void ResetState()
	{
		_entryPrice = 0m;
		_highestPrice = 0m;
		_lowestPrice = 0m;
	}
}
