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
/// Awesome Oscillator swing strategy converted from the "Executor AO" MetaTrader expert.
/// Implements the saucer-based entry logic with optional stop, take-profit, and trailing exit management.
/// </summary>
public class ExecutorAoStrategy : Strategy
{
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<int> _aoShortPeriod;
	private readonly StrategyParam<int> _aoLongPeriod;
	private readonly StrategyParam<decimal> _minimumAoIndent;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<decimal> _trailingStepPips;
	private readonly StrategyParam<DataType> _candleType;

	private AwesomeOscillator _ao = null!;

	private decimal? _currentAo;
	private decimal? _previousAo;
	private decimal? _previousAo2;

	private decimal _pipSize;

	private decimal? _longEntryPrice;
	private decimal? _longStop;
	private decimal? _longTake;

	private decimal? _shortEntryPrice;
	private decimal? _shortStop;
	private decimal? _shortTake;

	/// <summary>
	/// Initializes a new instance of the <see cref="ExecutorAoStrategy"/> class.
	/// </summary>
	public ExecutorAoStrategy()
	{
		_tradeVolume = Param(nameof(TradeVolume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Trade Volume", "Fixed order size", "Risk")
			.SetCanOptimize(true);

		_aoShortPeriod = Param(nameof(AoShortPeriod), 5)
			.SetGreaterThanZero()
			.SetDisplay("AO Short Period", "Fast period for Awesome Oscillator", "Indicators")
			.SetCanOptimize(true);

		_aoLongPeriod = Param(nameof(AoLongPeriod), 34)
			.SetGreaterThanZero()
			.SetDisplay("AO Long Period", "Slow period for Awesome Oscillator", "Indicators")
			.SetCanOptimize(true);

		_minimumAoIndent = Param(nameof(MinimumAoIndent), 0.001m)
			.SetNotNegative()
			.SetDisplay("Minimum AO Indent", "Minimum distance from zero before signals are valid", "Logic")
			.SetCanOptimize(true);

		_stopLossPips = Param(nameof(StopLossPips), 50m)
			.SetNotNegative()
			.SetDisplay("Stop Loss (pips)", "Protective stop distance expressed in pips", "Risk")
			.SetCanOptimize(true);

		_takeProfitPips = Param(nameof(TakeProfitPips), 50m)
			.SetNotNegative()
			.SetDisplay("Take Profit (pips)", "Target distance expressed in pips", "Risk")
			.SetCanOptimize(true);

		_trailingStopPips = Param(nameof(TrailingStopPips), 5m)
			.SetNotNegative()
			.SetDisplay("Trailing Stop (pips)", "Trailing distance in pips", "Risk")
			.SetCanOptimize(true);

		_trailingStepPips = Param(nameof(TrailingStepPips), 5m)
			.SetNotNegative()
			.SetDisplay("Trailing Step (pips)", "Minimum move before trailing adjusts", "Risk")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Primary timeframe used for analysis", "General");
	}

	/// <summary>
	/// Fixed order volume used for market entries.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set
		{
			_tradeVolume.Value = value;
			Volume = value;
		}
	}

	/// <summary>
	/// Fast period for the Awesome Oscillator calculation.
	/// </summary>
	public int AoShortPeriod
	{
		get => _aoShortPeriod.Value;
		set => _aoShortPeriod.Value = value;
	}

	/// <summary>
	/// Slow period for the Awesome Oscillator calculation.
	/// </summary>
	public int AoLongPeriod
	{
		get => _aoLongPeriod.Value;
		set => _aoLongPeriod.Value = value;
	}

	/// <summary>
	/// Minimum absolute AO value required before trades are allowed.
	/// </summary>
	public decimal MinimumAoIndent
	{
		get => _minimumAoIndent.Value;
		set => _minimumAoIndent.Value = value;
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
	/// Take-profit distance in pips.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
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
	/// Minimum step required before the trailing stop moves.
	/// </summary>
	public decimal TrailingStepPips
	{
		get => _trailingStepPips.Value;
		set => _trailingStepPips.Value = value;
	}

	/// <summary>
	/// Candle series used to generate signals.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_ao = null!;
		_currentAo = null;
		_previousAo = null;
		_previousAo2 = null;
		_pipSize = 0m;
		ResetLongState();
		ResetShortState();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = TradeVolume;
		_pipSize = CalculatePipSize();

		if (TrailingStopPips > 0m && TrailingStepPips <= 0m)
			throw new InvalidOperationException("Trailing step must be positive when trailing stop is enabled.");

		_ao = new AwesomeOscillator
		{
			ShortPeriod = AoShortPeriod,
			LongPeriod = AoLongPeriod
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_ao, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _ao);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal aoValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var previousAo = _currentAo;
		var previousAo2 = _previousAo;

		var positionClosed = HandleActivePositions(candle, previousAo);

		StoreAoValue(aoValue);

		if (positionClosed)
			return;

		if (_ao == null || !_ao.IsFormed)
			return;

		if (!previousAo.HasValue || !previousAo2.HasValue || !_currentAo.HasValue)
			return;

		if (Position != 0m)
			return;

		var current = _currentAo.Value;
		var prev = previousAo.Value;
		var prev2 = previousAo2.Value;
		var indent = MinimumAoIndent;

		if (current > prev && prev < prev2 && current <= -indent)
		{
			OpenLong(candle.ClosePrice);
			return;
		}

		if (current < prev && prev > prev2 && current >= indent)
			OpenShort(candle.ClosePrice);
	}

	private bool HandleActivePositions(ICandleMessage candle, decimal? previousAo)
	{
		if (Position > 0m)
		{
			_longEntryPrice ??= candle.ClosePrice;
			UpdateTrailingForLong(candle);

			if (_longTake.HasValue && candle.HighPrice >= _longTake.Value)
			{
				SellMarket(Math.Abs(Position));
				ResetLongState();
				return true;
			}

			if (_longStop.HasValue && candle.LowPrice <= _longStop.Value)
			{
				SellMarket(Math.Abs(Position));
				ResetLongState();
				return true;
			}

			if (previousAo.HasValue && previousAo.Value > 0m)
			{
				SellMarket(Math.Abs(Position));
				ResetLongState();
				return true;
			}
		}
		else if (Position < 0m)
		{
			_shortEntryPrice ??= candle.ClosePrice;
			UpdateTrailingForShort(candle);

			if (_shortTake.HasValue && candle.LowPrice <= _shortTake.Value)
			{
				BuyMarket(Math.Abs(Position));
				ResetShortState();
				return true;
			}

			if (_shortStop.HasValue && candle.HighPrice >= _shortStop.Value)
			{
				BuyMarket(Math.Abs(Position));
				ResetShortState();
				return true;
			}

			if (previousAo.HasValue && previousAo.Value < 0m)
			{
				BuyMarket(Math.Abs(Position));
				ResetShortState();
				return true;
			}
		}
		else
		{
			ResetLongState();
			ResetShortState();
		}

		return false;
	}

	private void OpenLong(decimal price)
	{
		var volume = GetTradeVolume();
		if (volume <= 0m)
			return;

		BuyMarket(volume);

		_longEntryPrice = price;
		_longStop = StopLossPips > 0m ? price - StopLossPips * _pipSize : null;
		_longTake = TakeProfitPips > 0m ? price + TakeProfitPips * _pipSize : null;
		ResetShortState();
	}

	private void OpenShort(decimal price)
	{
		var volume = GetTradeVolume();
		if (volume <= 0m)
			return;

		SellMarket(volume);

		_shortEntryPrice = price;
		_shortStop = StopLossPips > 0m ? price + StopLossPips * _pipSize : null;
		_shortTake = TakeProfitPips > 0m ? price - TakeProfitPips * _pipSize : null;
		ResetLongState();
	}

	private void UpdateTrailingForLong(ICandleMessage candle)
	{
		if (TrailingStopPips <= 0m || TrailingStepPips <= 0m || !_longEntryPrice.HasValue)
			return;

		var trailingDistance = TrailingStopPips * _pipSize;
		var trailingStep = TrailingStepPips * _pipSize;
		var price = candle.ClosePrice;
		var entry = _longEntryPrice.Value;

		if (price - entry > trailingDistance + trailingStep)
		{
			var minimalAllowed = price - (trailingDistance + trailingStep);
			if (!_longStop.HasValue || _longStop.Value < minimalAllowed)
				_longStop = price - trailingDistance;
		}
	}

	private void UpdateTrailingForShort(ICandleMessage candle)
	{
		if (TrailingStopPips <= 0m || TrailingStepPips <= 0m || !_shortEntryPrice.HasValue)
			return;

		var trailingDistance = TrailingStopPips * _pipSize;
		var trailingStep = TrailingStepPips * _pipSize;
		var price = candle.ClosePrice;
		var entry = _shortEntryPrice.Value;

		if (entry - price > trailingDistance + trailingStep)
		{
			var maximalAllowed = price + (trailingDistance + trailingStep);
			if (!_shortStop.HasValue || _shortStop.Value > maximalAllowed)
				_shortStop = price + trailingDistance;
		}
	}

	private decimal GetTradeVolume()
	{
		var volume = Volume;
		if (volume <= 0m)
			volume = TradeVolume;
		return volume;
	}

	private void StoreAoValue(decimal value)
	{
		_previousAo2 = _previousAo;
		_previousAo = _currentAo;
		_currentAo = value;
	}

	private decimal CalculatePipSize()
	{
		var priceStep = Security?.PriceStep ?? 1m;
		if (priceStep <= 0m)
			priceStep = 1m;

		var decimals = GetDecimalPlaces(priceStep);
		var factor = decimals == 3 || decimals == 5 ? 10m : 1m;
		return priceStep * factor;
	}

	private static int GetDecimalPlaces(decimal value)
	{
		value = Math.Abs(value);
		if (value == 0m)
			return 0;

		var bits = decimal.GetBits(value);
		return (bits[3] >> 16) & 0xFF;
	}

	private void ResetLongState()
	{
		_longEntryPrice = null;
		_longStop = null;
		_longTake = null;
	}

	private void ResetShortState()
	{
		_shortEntryPrice = null;
		_shortStop = null;
		_shortTake = null;
	}
}

