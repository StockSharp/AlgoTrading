using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Momentum strategy converted from the MetaTrader 5 expert advisor "Autotrader Momentum".
/// Compares the most recent closing price with a historical reference bar and reverses positions when momentum shifts.
/// Includes configurable fixed stops, take profit targets, and an optional trailing stop engine measured in pips.
/// </summary>
public class AutotraderMomentumStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<int> _stopLossPips;
	private readonly StrategyParam<int> _takeProfitPips;
	private readonly StrategyParam<int> _trailingStopPips;
	private readonly StrategyParam<int> _trailingStepPips;
	private readonly StrategyParam<int> _currentBarIndex;
	private readonly StrategyParam<int> _comparableBarIndex;

	private readonly List<decimal> _closeHistory = new();

	private decimal? _entryPrice;
	private decimal? _stopPrice;
	private decimal? _takeProfitPrice;
	private bool _isLongPosition;

	private decimal _pipValue;
	private decimal _stopLossOffset;
	private decimal _takeProfitOffset;
	private decimal _trailingStopOffset;
	private decimal _trailingStepOffset;

	/// <summary>
	/// Initializes a new instance of the <see cref="AutotraderMomentumStrategy"/> class.
	/// </summary>
	public AutotraderMomentumStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Primary timeframe for price comparisons", "Data");

		_tradeVolume = Param(nameof(TradeVolume), 1m)
			.SetDisplay("Trade Volume", "Base order volume used for market entries", "Trading")
			.SetGreaterThanZero();

		_stopLossPips = Param(nameof(StopLossPips), 50)
			.SetDisplay("Stop Loss (pips)", "Protective stop distance expressed in pips", "Risk")
			.SetGreaterOrEqualZero();

		_takeProfitPips = Param(nameof(TakeProfitPips), 50)
			.SetDisplay("Take Profit (pips)", "Profit target distance expressed in pips", "Risk")
			.SetGreaterOrEqualZero();

		_trailingStopPips = Param(nameof(TrailingStopPips), 5)
			.SetDisplay("Trailing Stop (pips)", "Distance maintained by the trailing stop in pips", "Risk")
			.SetGreaterOrEqualZero();

		_trailingStepPips = Param(nameof(TrailingStepPips), 5)
			.SetDisplay("Trailing Step (pips)", "Minimum progress before the trailing stop advances", "Risk")
			.SetGreaterOrEqualZero();

		_currentBarIndex = Param(nameof(CurrentBarIndex), 0)
			.SetDisplay("Current Bar Index", "Index of the candle used as the signal source", "Logic")
			.SetGreaterOrEqualZero();

		_comparableBarIndex = Param(nameof(ComparableBarIndex), 15)
			.SetDisplay("Comparable Bar Index", "Historical candle index used for momentum comparison", "Logic")
			.SetGreaterOrEqualZero();
	}

	/// <summary>
	/// Gets or sets the candle type used for generating signals.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Gets or sets the base order volume.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	/// <summary>
	/// Gets or sets the stop-loss distance in pips.
	/// </summary>
	public int StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Gets or sets the take-profit distance in pips.
	/// </summary>
	public int TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Gets or sets the trailing stop distance in pips.
	/// </summary>
	public int TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	/// <summary>
	/// Gets or sets the trailing step distance in pips.
	/// </summary>
	public int TrailingStepPips
	{
		get => _trailingStepPips.Value;
		set => _trailingStepPips.Value = value;
	}

	/// <summary>
	/// Gets or sets the index of the candle considered the "current" bar in comparisons.
	/// </summary>
	public int CurrentBarIndex
	{
		get => _currentBarIndex.Value;
		set => _currentBarIndex.Value = value;
	}

	/// <summary>
	/// Gets or sets the index of the historical bar used for comparison.
	/// </summary>
	public int ComparableBarIndex
	{
		get => _comparableBarIndex.Value;
		set => _comparableBarIndex.Value = value;
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_closeHistory.Clear();
		ResetPositionState();

		_pipValue = 0m;
		_stopLossOffset = 0m;
		_takeProfitOffset = 0m;
		_trailingStopOffset = 0m;
		_trailingStepOffset = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		if (TrailingStopPips > 0 && TrailingStepPips <= 0)
			throw new InvalidOperationException("Trailing step must be positive when trailing stop is enabled.");

		Volume = TradeVolume;

		_pipValue = CalculatePipValue();
		_stopLossOffset = StopLossPips > 0 ? StopLossPips * _pipValue : 0m;
		_takeProfitOffset = TakeProfitPips > 0 ? TakeProfitPips * _pipValue : 0m;
		_trailingStopOffset = TrailingStopPips > 0 ? TrailingStopPips * _pipValue : 0m;
		_trailingStepOffset = TrailingStepPips > 0 ? TrailingStepPips * _pipValue : 0m;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		// Ignore incomplete candles to mirror the original new-bar processing style.
		if (candle.State != CandleStates.Finished)
			return;

		// Update trailing and risk management before evaluating fresh signals.
		UpdateTrailingStop(candle);
		var exitTriggered = ManageProtectiveExits(candle);

		// Maintain the rolling window of closes used for momentum comparisons.
		UpdateCloseHistory(candle.ClosePrice);

		// Skip signal generation if an exit order has just been triggered on this bar.
		if (exitTriggered)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var requiredHistory = Math.Max(CurrentBarIndex, ComparableBarIndex) + 1;
		if (_closeHistory.Count < requiredHistory)
			return;

		var currentClose = GetCloseAtIndex(CurrentBarIndex);
		var comparableClose = GetCloseAtIndex(ComparableBarIndex);
		if (currentClose == null || comparableClose == null)
			return;

		// Enter long when the monitored bar closes above the reference bar.
		if (currentClose > comparableClose && CanBuy)
		{
			EnterPosition(true, candle);
		}
		// Enter short when the monitored bar closes below the reference bar.
		else if (currentClose < comparableClose && CanSell)
		{
			EnterPosition(false, candle);
		}
	}

	private void UpdateCloseHistory(decimal closePrice)
	{
		var maxCount = Math.Max(CurrentBarIndex, ComparableBarIndex) + 1;
		if (maxCount <= 0)
			maxCount = 1;

		_closeHistory.Add(closePrice);
		if (_closeHistory.Count > maxCount)
			_closeHistory.RemoveAt(0);
	}

	private decimal? GetCloseAtIndex(int indexFromCurrent)
	{
		if (indexFromCurrent < 0)
			return null;

		var targetIndex = _closeHistory.Count - 1 - indexFromCurrent;
		if (targetIndex < 0 || targetIndex >= _closeHistory.Count)
			return null;

		return _closeHistory[targetIndex];
	}

	private void EnterPosition(bool isLong, ICandleMessage candle)
	{
		var baseVolume = TradeVolume;
		if (baseVolume <= 0m)
			return;

		var previousPosition = Position;
		decimal volume;

		if (isLong)
		{
			volume = baseVolume;
			if (previousPosition < 0m)
				volume += Math.Abs(previousPosition);

			if (volume <= 0m)
				return;

			// Buy enough volume to close any short exposure and add the configured trade size.
			BuyMarket(volume);

			if (previousPosition <= 0m)
			{
				// Treat reversals and fresh entries as a brand-new long position.
				_entryPrice = candle.ClosePrice;
			}
			else
			{
				// Blend the existing average price with the new fill to keep risk metrics consistent.
				var existingVolume = previousPosition;
				var totalVolume = existingVolume + baseVolume;
				if (totalVolume > 0m)
				{
					var existingEntry = _entryPrice ?? candle.ClosePrice;
					_entryPrice = (existingEntry * existingVolume + candle.ClosePrice * baseVolume) / totalVolume;
				}
			}

			_isLongPosition = true;
		}
		else
		{
			volume = baseVolume;
			if (previousPosition > 0m)
				volume += previousPosition;

			if (volume <= 0m)
				return;

			// Sell enough volume to close any long exposure and add the configured trade size.
			SellMarket(volume);

			if (previousPosition >= 0m)
			{
				// Treat reversals and fresh entries as a brand-new short position.
				_entryPrice = candle.ClosePrice;
			}
			else
			{
				// Blend the existing short average price with the new fill.
				var existingVolume = Math.Abs(previousPosition);
				var totalVolume = existingVolume + baseVolume;
				if (totalVolume > 0m)
				{
					var existingEntry = _entryPrice ?? candle.ClosePrice;
					_entryPrice = (existingEntry * existingVolume + candle.ClosePrice * baseVolume) / totalVolume;
				}
			}

			_isLongPosition = false;
		}

		_stopPrice = CalculateStopPrice(_isLongPosition, _entryPrice);
		_takeProfitPrice = CalculateTakeProfit(_isLongPosition, _entryPrice);
	}

	private decimal? CalculateStopPrice(bool isLong, decimal? entryPrice)
	{
		if (entryPrice == null || _stopLossOffset <= 0m)
			return null;

		return isLong ? entryPrice - _stopLossOffset : entryPrice + _stopLossOffset;
	}

	private decimal? CalculateTakeProfit(bool isLong, decimal? entryPrice)
	{
		if (entryPrice == null || _takeProfitOffset <= 0m)
			return null;

		return isLong ? entryPrice + _takeProfitOffset : entryPrice - _takeProfitOffset;
	}

	private void UpdateTrailingStop(ICandleMessage candle)
	{
		if (_trailingStopOffset <= 0m || _trailingStepOffset <= 0m || _entryPrice == null)
			return;

		if (Position > 0m)
		{
			var progress = candle.HighPrice - _entryPrice.Value;
			if (progress <= _trailingStopOffset + _trailingStepOffset)
				return;

			// Shift the trailing stop only when the move is large enough to respect the configured step.
			var desiredStop = candle.ClosePrice - _trailingStopOffset;
			if (_stopPrice is decimal currentStop)
			{
				if (desiredStop - currentStop >= _trailingStepOffset)
					_stopPrice = desiredStop;
			}
			else
			{
				_stopPrice = desiredStop;
			}
		}
		else if (Position < 0m)
		{
			var progress = _entryPrice.Value - candle.LowPrice;
			if (progress <= _trailingStopOffset + _trailingStepOffset)
				return;

			var desiredStop = candle.ClosePrice + _trailingStopOffset;
			if (_stopPrice is decimal currentStop)
			{
				if (currentStop - desiredStop >= _trailingStepOffset)
					_stopPrice = desiredStop;
			}
			else
			{
				_stopPrice = desiredStop;
			}
		}
	}

	private bool ManageProtectiveExits(ICandleMessage candle)
	{
		if (Position > 0m)
		{
			// Close the long position if the bar traded through the stop level.
			if (_stopPrice is decimal stop && candle.LowPrice <= stop)
			{
				SellMarket(Position);
				ResetPositionState();
				return true;
			}

			// Lock in profits once the take-profit threshold has been reached.
			if (_takeProfitPrice is decimal take && candle.HighPrice >= take)
			{
				SellMarket(Position);
				ResetPositionState();
				return true;
			}
		}
		else if (Position < 0m)
		{
			var volume = Math.Abs(Position);

			if (_stopPrice is decimal stop && candle.HighPrice >= stop)
			{
				BuyMarket(volume);
				ResetPositionState();
				return true;
			}

			if (_takeProfitPrice is decimal take && candle.LowPrice <= take)
			{
				BuyMarket(volume);
				ResetPositionState();
				return true;
			}
		}
		else
		{
			// Ensure cached state is flushed once all positions are closed externally.
			ResetPositionState();
		}

		return false;
	}

	private void ResetPositionState()
	{
		_entryPrice = null;
		_stopPrice = null;
		_takeProfitPrice = null;
		_isLongPosition = false;
	}

	private decimal CalculatePipValue()
	{
		var step = Security?.PriceStep ?? 0m;
		if (step <= 0m)
			return 1m;

		var scaled = step;
		var digits = 0;
		while (scaled < 1m && digits < 10)
		{
			scaled *= 10m;
			digits++;
		}

		// Adjust for three and five decimal quotes to emulate the MetaTrader point multiplier.
		var adjust = (digits == 3 || digits == 5) ? 10m : 1m;
		return step * adjust;
	}
}
