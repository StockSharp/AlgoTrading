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
/// Tunnel method strategy that trades EMA crossovers on hourly candles.
/// Long trades are opened when the fast EMA crosses above the slow EMA.
/// Short trades are opened when the fast EMA crosses below the medium EMA.
/// Includes fixed stop-loss, take-profit, and a trailing stop once profit reaches a trigger.
/// </summary>
public class TunnelMethodEmaStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _mediumLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<decimal> _trailingStopPoints;
	private readonly StrategyParam<decimal> _trailingTriggerPoints;

	private bool _hasPreviousValues;
	private decimal _previousFast;
	private decimal _previousMedium;
	private decimal _previousSlow;

	private decimal _pointValue;
	private decimal _stopLossDistance;
	private decimal _takeProfitDistance;
	private decimal _trailingStopDistance;
	private decimal _trailingTriggerDistance;

	private decimal? _entryPrice;
	private decimal _highestSinceEntry;
	private decimal _lowestSinceEntry;
	private decimal? _longTrailingStop;
	private decimal? _shortTrailingStop;

	/// <summary>
	/// Initializes a new instance of the <see cref="TunnelMethodEmaStrategy"/> class.
	/// </summary>
	public TunnelMethodEmaStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe used for EMA calculations", "General");

		_fastLength = Param(nameof(FastLength), 12)
			.SetGreaterThanZero()
			.SetDisplay("Fast EMA Length", "Period of the fast EMA", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(6, 30, 2);

		_mediumLength = Param(nameof(MediumLength), 144)
			.SetGreaterThanZero()
			.SetDisplay("Medium EMA Length", "Period of the medium EMA", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(72, 200, 8);

		_slowLength = Param(nameof(SlowLength), 169)
			.SetGreaterThanZero()
			.SetDisplay("Slow EMA Length", "Period of the slow EMA", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(120, 220, 5);

		_stopLossPoints = Param(nameof(StopLossPoints), 25m)
			.SetNotNegative()
			.SetDisplay("Stop Loss (points)", "Protective stop distance expressed in price points", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(10m, 60m, 5m);

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 230m)
			.SetNotNegative()
			.SetDisplay("Take Profit (points)", "Profit target distance expressed in price points", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(100m, 400m, 20m);

		_trailingStopPoints = Param(nameof(TrailingStopPoints), 35m)
			.SetNotNegative()
			.SetDisplay("Trailing Stop (points)", "Distance maintained by the trailing stop", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(10m, 80m, 5m);

		_trailingTriggerPoints = Param(nameof(TrailingTriggerPoints), 20m)
			.SetNotNegative()
			.SetDisplay("Trailing Trigger (points)", "Profit required before the trailing stop activates", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(5m, 60m, 5m);
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
	/// Fast EMA period length.
	/// </summary>
	public int FastLength
	{
		get => _fastLength.Value;
		set => _fastLength.Value = value;
	}

	/// <summary>
	/// Medium EMA period length.
	/// </summary>
	public int MediumLength
	{
		get => _mediumLength.Value;
		set => _mediumLength.Value = value;
	}

	/// <summary>
	/// Slow EMA period length.
	/// </summary>
	public int SlowLength
	{
		get => _slowLength.Value;
		set => _slowLength.Value = value;
	}

	/// <summary>
	/// Stop-loss distance in price points.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Take-profit distance in price points.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Trailing stop distance in price points.
	/// </summary>
	public decimal TrailingStopPoints
	{
		get => _trailingStopPoints.Value;
		set => _trailingStopPoints.Value = value;
	}

	/// <summary>
	/// Trailing activation threshold in price points.
	/// </summary>
	public decimal TrailingTriggerPoints
	{
		get => _trailingTriggerPoints.Value;
		set => _trailingTriggerPoints.Value = value;
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_hasPreviousValues = false;
		_previousFast = 0m;
		_previousMedium = 0m;
		_previousSlow = 0m;

		_entryPrice = null;
		_highestSinceEntry = 0m;
		_lowestSinceEntry = 0m;
		_longTrailingStop = null;
		_shortTrailingStop = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_pointValue = GetPointValue();
		_stopLossDistance = StopLossPoints * _pointValue;
		_takeProfitDistance = TakeProfitPoints * _pointValue;
		_trailingStopDistance = TrailingStopPoints * _pointValue;
		_trailingTriggerDistance = TrailingTriggerPoints * _pointValue;

		var slowEma = new EMA { Length = SlowLength };
		var mediumEma = new EMA { Length = MediumLength };
		var fastEma = new EMA { Length = FastLength };

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(slowEma, mediumEma, fastEma, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal slowValue, decimal mediumValue, decimal fastValue)
	{
		if (candle.State != CandleStates.Finished)
			// Ignore unfinished candles to work on closed data.
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			// Do nothing when the strategy is not ready or trading is disabled.
			return;

		if (!_hasPreviousValues)
		{
			_previousSlow = slowValue;
			_previousMedium = mediumValue;
			_previousFast = fastValue;
			_hasPreviousValues = true;
			return;
		}

		UpdateRiskDistances();
		// Refresh risk distances if the price step changes during runtime.

		if (Position == 0)
		{
			ResetPositionState();
			// Clear trailing state while flat to prepare for the next trade.
		}
		else if (Position > 0)
		{
			ManageLongPosition(candle);
		}
		else
		{
			ManageShortPosition(candle);
		}

		if (Position == 0)
		{
			var shouldOpenLong = _previousFast < _previousSlow && fastValue > slowValue;
			var shouldOpenShort = _previousFast > _previousMedium && fastValue < mediumValue;

			if (shouldOpenLong && Position <= 0)
			{
				var volume = Volume + Math.Abs(Position);
				if (volume > 0)
				{
					_entryPrice = candle.ClosePrice;
					_highestSinceEntry = candle.HighPrice;
					_longTrailingStop = null;
					// Enter long with current volume when the fast EMA crosses above the slow EMA.
					BuyMarket(volume);
				}
			}
			else if (shouldOpenShort && Position >= 0)
			{
				var volume = Volume + Math.Abs(Position);
				if (volume > 0)
				{
					_entryPrice = candle.ClosePrice;
					_lowestSinceEntry = candle.LowPrice;
					_shortTrailingStop = null;
					// Enter short with current volume when the fast EMA crosses below the medium EMA.
					SellMarket(volume);
				}
			}
		}

		_previousSlow = slowValue;
		_previousMedium = mediumValue;
		_previousFast = fastValue;
	}

	private void ManageLongPosition(ICandleMessage candle)
	{
		if (_entryPrice is null)
			_entryPrice = candle.ClosePrice;

		_highestSinceEntry = Math.Max(_highestSinceEntry, candle.HighPrice);
		// Track the highest price reached since the long entry.

		if (_takeProfitDistance > 0m && candle.HighPrice >= _entryPrice.Value + _takeProfitDistance)
		{
			SellMarket(Position);
			ResetPositionState();
			return;
		}

		if (_stopLossDistance > 0m && candle.LowPrice <= _entryPrice.Value - _stopLossDistance)
		{
			SellMarket(Position);
			ResetPositionState();
			return;
		}

		if (_trailingStopDistance <= 0m || _trailingTriggerDistance <= 0m)
			return;

		if (_highestSinceEntry - _entryPrice.Value < _trailingTriggerDistance)
			return;

		var candidate = _highestSinceEntry - _trailingStopDistance;
		// Align the trailing stop with the instrument price step.
		candidate = ShrinkPrice(candidate);

		if (!_longTrailingStop.HasValue || candidate > _longTrailingStop.Value)
			_longTrailingStop = candidate;

		if (_longTrailingStop.HasValue && candle.LowPrice <= _longTrailingStop.Value)
			// Close the long position once price falls to the trailing stop.
		{
			SellMarket(Position);
			ResetPositionState();
		}
	}

	private void ManageShortPosition(ICandleMessage candle)
	{
		if (_entryPrice is null)
			_entryPrice = candle.ClosePrice;

		_lowestSinceEntry = _lowestSinceEntry == 0m ? candle.LowPrice : Math.Min(_lowestSinceEntry, candle.LowPrice);
		// Track the lowest price reached since the short entry.

		if (_takeProfitDistance > 0m && candle.LowPrice <= _entryPrice.Value - _takeProfitDistance)
		{
			BuyMarket(Math.Abs(Position));
			ResetPositionState();
			return;
		}

		if (_stopLossDistance > 0m && candle.HighPrice >= _entryPrice.Value + _stopLossDistance)
		{
			BuyMarket(Math.Abs(Position));
			ResetPositionState();
			return;
		}

		if (_trailingStopDistance <= 0m || _trailingTriggerDistance <= 0m)
			return;

		if (_entryPrice.Value - _lowestSinceEntry < _trailingTriggerDistance)
			return;

		var candidate = _lowestSinceEntry + _trailingStopDistance;
		// Align the trailing stop with the instrument price step.
		candidate = ShrinkPrice(candidate);

		if (!_shortTrailingStop.HasValue || candidate < _shortTrailingStop.Value)
			_shortTrailingStop = candidate;

		if (_shortTrailingStop.HasValue && candle.HighPrice >= _shortTrailingStop.Value)
			// Close the short position once price rises to the trailing stop.
		{
			BuyMarket(Math.Abs(Position));
			ResetPositionState();
		}
	}

	private void ResetPositionState()
	{
		_entryPrice = null;
		_highestSinceEntry = 0m;
		_lowestSinceEntry = 0m;
		_longTrailingStop = null;
		_shortTrailingStop = null;
	}

	private void UpdateRiskDistances()
	{
		var newPointValue = GetPointValue();
		if (newPointValue <= 0m)
			return;

		if (_pointValue != newPointValue)
		{
			_pointValue = newPointValue;
			_stopLossDistance = StopLossPoints * _pointValue;
			_takeProfitDistance = TakeProfitPoints * _pointValue;
			_trailingStopDistance = TrailingStopPoints * _pointValue;
			_trailingTriggerDistance = TrailingTriggerPoints * _pointValue;
		}
	}

	private decimal GetPointValue()
	{
		var step = Security?.PriceStep;
		if (step is > 0m)
			return step.Value;

		return 1m;
	}

	private decimal ShrinkPrice(decimal price)
	{
		return Security?.ShrinkPrice(price) ?? price;
	}
}

