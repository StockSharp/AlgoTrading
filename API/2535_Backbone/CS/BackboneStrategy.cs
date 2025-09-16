using System;
using System.Collections.Generic;
using System.Globalization;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Backbone strategy converted from the MQL5 expert advisor.
/// Alternates long and short series with risk-based scaling, stop-loss, take-profit, and trailing stop management.
/// </summary>
public class BackboneStrategy : Strategy
{
	private readonly StrategyParam<decimal> _maxRisk;
	private readonly StrategyParam<int> _maxTrades;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _bidMax;
	private decimal _askMin;
	private int _lastDirection;
	private int _currentDirection;
	private int _longCount;
	private int _shortCount;
	private decimal _longAveragePrice;
	private decimal _shortAveragePrice;
	private decimal? _longStop;
	private decimal? _longTake;
	private decimal? _shortStop;
	private decimal? _shortTake;
	private decimal _adjustedPoint;

	/// <summary>
	/// Maximum total risk fraction shared across all positions.
	/// </summary>
	public decimal MaxRisk
	{
		get => _maxRisk.Value;
		set => _maxRisk.Value = value;
	}

	/// <summary>
	/// Maximum number of stacked entries in one direction.
	/// </summary>
	public int MaxTrades
	{
		get => _maxTrades.Value;
		set => _maxTrades.Value = value;
	}

	/// <summary>
	/// Take-profit distance expressed in pips.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Stop-loss distance expressed in pips.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Trailing stop activation distance in pips.
	/// </summary>
	public decimal TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	/// <summary>
	/// Candle type used for the calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="BackboneStrategy"/> class.
	/// </summary>
	public BackboneStrategy()
	{
		_maxRisk = Param(nameof(MaxRisk), 0.5m)
			.SetGreaterThanZero()
			.SetDisplay("Max Risk", "Maximum risk fraction shared across trades", "Risk");

		_maxTrades = Param(nameof(MaxTrades), 10)
			.SetGreaterThanZero()
			.SetDisplay("Max Trades", "Maximum number of layered entries", "Risk");

		_takeProfitPips = Param(nameof(TakeProfitPips), 170m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit", "Distance for the take-profit target (pips)", "Risk");

		_stopLossPips = Param(nameof(StopLossPips), 40m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss", "Distance for the protective stop (pips)", "Risk");

		_trailingStopPips = Param(nameof(TrailingStopPips), 300m)
			.SetGreaterThanZero()
			.SetDisplay("Trailing Stop", "Distance for the trailing stop activation (pips)", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles used for analysis", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		ResetState();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		ResetState();
		_adjustedPoint = GetAdjustedPoint();

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		// Wait for completed candles only.
		if (candle.State != CandleStates.Finished)
			return;

		// Trade only when the strategy is fully operational.
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_adjustedPoint <= 0m)
			_adjustedPoint = GetAdjustedPoint();

		UpdateExtremeLevels(candle);

		if (_currentDirection == 1)
		{
			if (HandleLongExit(candle))
				return;
		}
		else if (_currentDirection == -1)
		{
			if (HandleShortExit(candle))
				return;
		}
		else
		{
			// Reset counters when all positions are closed.
			ResetLongState();
			ResetShortState();
		}

		if (ShouldEnterLong())
		{
			EnterLong(candle);
		}
		else if (ShouldEnterShort())
		{
			EnterShort(candle);
		}
	}

	private void EnterLong(ICandleMessage candle)
	{
		var openPositions = _currentDirection == 1 ? _longCount : 0;
		var qty = CalculateOrderVolume(openPositions);
		if (qty <= 0m)
			return;

		if (_currentDirection == -1)
		{
			// Close the short series before switching sides.
			ClosePosition();
			ResetShortState();
			_currentDirection = 0;
			openPositions = 0;
		}

		BuyMarket(qty);

		openPositions++;
		_longCount = openPositions;
		_currentDirection = 1;

		var average = _longCount == 1
		? candle.ClosePrice
		: (_longAveragePrice * (_longCount - 1) + candle.ClosePrice) / _longCount;
		_longAveragePrice = average;

		if (StopLossPips > 0m && _adjustedPoint > 0m)
			_longStop = average - StopLossPips * _adjustedPoint;
		else
			_longStop = null;

		if (TakeProfitPips > 0m && _adjustedPoint > 0m)
			_longTake = average + TakeProfitPips * _adjustedPoint;
		else
			_longTake = null;

		_lastDirection = 1;
	}

	private void EnterShort(ICandleMessage candle)
	{
		var openPositions = _currentDirection == -1 ? _shortCount : 0;
		var qty = CalculateOrderVolume(openPositions);
		if (qty <= 0m)
			return;

		if (_currentDirection == 1)
		{
			// Close the long series before switching sides.
			ClosePosition();
			ResetLongState();
			_currentDirection = 0;
			openPositions = 0;
		}

		SellMarket(qty);

		openPositions++;
		_shortCount = openPositions;
		_currentDirection = -1;

		var average = _shortCount == 1
		? candle.ClosePrice
		: (_shortAveragePrice * (_shortCount - 1) + candle.ClosePrice) / _shortCount;
		_shortAveragePrice = average;

		if (StopLossPips > 0m && _adjustedPoint > 0m)
			_shortStop = average + StopLossPips * _adjustedPoint;
		else
			_shortStop = null;

		if (TakeProfitPips > 0m && _adjustedPoint > 0m)
			_shortTake = average - TakeProfitPips * _adjustedPoint;
		else
			_shortTake = null;

		_lastDirection = -1;
	}

	private bool HandleLongExit(ICandleMessage candle)
	{
		var exitTriggered = false;

		if (_longTake.HasValue && candle.HighPrice >= _longTake.Value)
		{
			// Take-profit reached for the long series.
			SellMarket(Math.Abs(Position));
			exitTriggered = true;
		}
		else if (_longStop.HasValue && candle.LowPrice <= _longStop.Value)
		{
			// Stop-loss touched for the long series.
			SellMarket(Math.Abs(Position));
			exitTriggered = true;
		}
		else if (TrailingStopPips > 0m && StopLossPips > 0m && _longCount > 0 && _adjustedPoint > 0m)
		{
			var trailingDistance = TrailingStopPips * _adjustedPoint;
			var profit = candle.ClosePrice - _longAveragePrice;
			if (trailingDistance > 0m && profit > trailingDistance)
			{
				var newStop = candle.ClosePrice - trailingDistance;
				if (!_longStop.HasValue || _longStop.Value < newStop)
					_longStop = newStop;
			}
		}

		if (exitTriggered)
		{
			ResetLongState();
			_currentDirection = 0;
			return true;
		}

		return false;
	}

	private bool HandleShortExit(ICandleMessage candle)
	{
		var exitTriggered = false;

		if (_shortTake.HasValue && candle.LowPrice <= _shortTake.Value)
		{
			// Take-profit reached for the short series.
			BuyMarket(Math.Abs(Position));
			exitTriggered = true;
		}
		else if (_shortStop.HasValue && candle.HighPrice >= _shortStop.Value)
		{
			// Stop-loss touched for the short series.
			BuyMarket(Math.Abs(Position));
			exitTriggered = true;
		}
		else if (TrailingStopPips > 0m && StopLossPips > 0m && _shortCount > 0 && _adjustedPoint > 0m)
		{
			var trailingDistance = TrailingStopPips * _adjustedPoint;
			var profit = _shortAveragePrice - candle.ClosePrice;
			if (trailingDistance > 0m && profit > trailingDistance)
			{
				var newStop = candle.ClosePrice + trailingDistance;
				if (!_shortStop.HasValue || _shortStop.Value > newStop)
					_shortStop = newStop;
			}
		}

		if (exitTriggered)
		{
			ResetShortState();
			_currentDirection = 0;
			return true;
		}

		return false;
	}

	private bool ShouldEnterLong()
	{
		var openPositions = _currentDirection == 1 ? _longCount : 0;
		if (MaxTrades <= 0)
			return false;

		var firstEntry = _lastDirection == -1 && openPositions == 0;
		var addEntry = _lastDirection == 1 && openPositions > 0 && openPositions < MaxTrades;
		return firstEntry || addEntry;
	}

	private bool ShouldEnterShort()
	{
		var openPositions = _currentDirection == -1 ? _shortCount : 0;
		if (MaxTrades <= 0)
			return false;

		var firstEntry = _lastDirection == 1 && openPositions == 0;
		var addEntry = _lastDirection == -1 && openPositions > 0 && openPositions < MaxTrades;
		return firstEntry || addEntry;
	}

	private decimal CalculateOrderVolume(int openPositions)
	{
		var defaultVolume = Volume > 0m ? Volume : 1m;
		var minVolume = Security?.VolumeMin ?? defaultVolume;
		var volumeStep = Security?.VolumeStep ?? 0m;
		var maxVolume = Security?.VolumeMax;

		if (minVolume <= 0m)
			minVolume = defaultVolume;

		if (MaxTrades <= 0 || MaxRisk <= 0m)
			return minVolume;

		var denominatorBase = (decimal)MaxTrades / MaxRisk;
		var denominator = denominatorBase - openPositions;
		if (denominator <= 0m)
			return 0m;

		var fraction = 1m / denominator;
		var equity = Portfolio?.CurrentValue ?? 0m;
		if (equity <= 0m)
			return 0m;

		var pip = _adjustedPoint;
		if (pip <= 0m)
		{
			var priceStep = Security?.PriceStep ?? 0m;
			pip = priceStep > 0m ? priceStep : 1m;
		}

		var stopLoss = StopLossPips > 0m ? StopLossPips : 1m;
		var riskPerUnit = stopLoss * pip;
		if (riskPerUnit <= 0m)
			return minVolume;

		var qty = equity * fraction / riskPerUnit;

		if (volumeStep > 0m)
			qty = Math.Floor(qty / volumeStep) * volumeStep;

		if (qty < minVolume)
			qty = minVolume;

		if (maxVolume.HasValue && maxVolume.Value > 0m && qty > maxVolume.Value)
			qty = maxVolume.Value;

		return qty;
	}

	private void UpdateExtremeLevels(ICandleMessage candle)
	{
		if (_lastDirection != 0)
			return;

		var trailingDistance = TrailingStopPips * _adjustedPoint;
		if (trailingDistance <= 0m)
			return;

		if (candle.HighPrice > _bidMax)
			_bidMax = candle.HighPrice;

		if (candle.LowPrice < _askMin)
			_askMin = candle.LowPrice;

		if (_bidMax != decimal.MinValue && candle.LowPrice < _bidMax - trailingDistance)
		{
			_lastDirection = -1;
			return;
		}

		if (_askMin != decimal.MaxValue && candle.HighPrice > _askMin + trailingDistance)
			_lastDirection = 1;
	}

	private decimal GetAdjustedPoint()
	{
		var step = Security?.PriceStep ?? 0m;
		if (step <= 0m)
			return 1m;

		var decimals = CountDecimals(step);
		if (decimals == 3 || decimals == 5)
			return step * 10m;

		return step;
	}

	private static int CountDecimals(decimal value)
	{
		value = Math.Abs(value);
		var text = value.ToString(CultureInfo.InvariantCulture);
		var index = text.IndexOf('.');
		return index < 0 ? 0 : text.Length - index - 1;
	}

	private void ResetState()
	{
		_bidMax = decimal.MinValue;
		_askMin = decimal.MaxValue;
		_lastDirection = 0;
		_currentDirection = 0;
		ResetLongState();
		ResetShortState();
		_adjustedPoint = 0m;
	}

	private void ResetLongState()
	{
		_longCount = 0;
		_longAveragePrice = 0m;
		_longStop = null;
		_longTake = null;
	}

	private void ResetShortState()
	{
		_shortCount = 0;
		_shortAveragePrice = 0m;
		_shortStop = null;
		_shortTake = null;
	}
}
