namespace StockSharp.Samples.Strategies;

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

using StockSharp.Algo;

/// <summary>
/// Grid-based pendulum strategy converted from the original MQL implementation.
/// </summary>
public class PendulumStrategy : Strategy
{
	private readonly StrategyParam<decimal> _stepSize;
	private readonly StrategyParam<decimal> _multiplier;
	private readonly StrategyParam<int> _maxLayers;
	private readonly StrategyParam<decimal> _baseVolume;
	private readonly StrategyParam<DataType> _candleType;

	private int _currentDirection;
	private int _layerIndex;
	private decimal _currentEntryPrice;
	private decimal _upperTrigger;
	private decimal _lowerTrigger;
	private decimal _takeProfitLevel;
	private decimal _stopLossLevel;
	private bool _levelsInitialized;
	private bool _hasPendingEntry;
	private int _pendingDirection;
	private decimal _pendingEntryPrice;

	public PendulumStrategy()
	{
		_stepSize = Param(nameof(StepSize), 0.001m)
			.SetDisplay("Grid Step", "Distance between consecutive grid levels", "Strategy")
			.SetGreaterThan(0m)
			.SetCanOptimize(true)
			.SetOptimize(0.0005m, 0.01m, 0.0005m);

		_multiplier = Param(nameof(Multiplier), 2m)
			.SetDisplay("Multiplier", "Scaling applied to volume and extended targets", "Strategy")
			.SetGreaterThan(1m)
			.SetCanOptimize(true)
			.SetOptimize(1.2m, 3m, 0.1m);

		_maxLayers = Param(nameof(MaxLayers), 3)
			.SetDisplay("Max Layers", "Maximum number of martingale layers", "Risk")
			.SetGreaterThan(1)
			.SetCanOptimize(true)
			.SetOptimize(2, 5, 1);

		_baseVolume = Param(nameof(BaseVolume), 1m)
			.SetDisplay("Base Volume", "Initial trade volume for the first layer", "Trading")
			.SetGreaterThan(0m)
			.SetCanOptimize(true)
			.SetOptimize(0.1m, 5m, 0.1m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Time frame used for price tracking", "General");
	}

	/// <summary>
	/// Grid step size.
	/// </summary>
	public decimal StepSize
	{
		get => _stepSize.Value;
		set => _stepSize.Value = value;
	}

	/// <summary>
	/// Volume and distance multiplier.
	/// </summary>
	public decimal Multiplier
	{
		get => _multiplier.Value;
		set => _multiplier.Value = value;
	}

	/// <summary>
	/// Maximum allowed martingale layers.
	/// </summary>
	public int MaxLayers
	{
		get => _maxLayers.Value;
		set => _maxLayers.Value = value;
	}

	/// <summary>
	/// Base trading volume.
	/// </summary>
	public decimal BaseVolume
	{
		get => _baseVolume.Value;
		set => _baseVolume.Value = value;
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
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_currentDirection = 0;
		_layerIndex = 0;
		_currentEntryPrice = 0m;
		_upperTrigger = 0m;
		_lowerTrigger = 0m;
		_takeProfitLevel = 0m;
		_stopLossLevel = 0m;
		_levelsInitialized = false;
		_hasPendingEntry = false;
		_pendingDirection = 0;
		_pendingEntryPrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var price = candle.ClosePrice;

		if (!_levelsInitialized)
			InitializeLevels(price);

		if (_hasPendingEntry && Position == 0m)
		{
			EnterDirection(_pendingDirection, _pendingEntryPrice, true);
			_hasPendingEntry = false;
			return;
		}

		if (HandleActivePosition(price))
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_currentDirection != 0 || _hasPendingEntry)
			return;

		if (price >= _upperTrigger)
		{
			EnterDirection(1, _upperTrigger, true);
		}
		else if (price <= _lowerTrigger)
		{
			EnterDirection(-1, _lowerTrigger, true);
		}
	}

	private bool HandleActivePosition(decimal price)
	{
		if (_currentDirection == 0)
			return false;

		if (_currentDirection > 0)
		{
			if (price >= _takeProfitLevel)
			{
				HandleTakeProfit(_takeProfitLevel, 1);
				return true;
			}

			if (price <= _stopLossLevel)
			{
				HandleStopLoss(price);
				return true;
			}

			if (price <= _lowerTrigger && _layerIndex < MaxLayers)
			{
				EnterDirection(-1, _lowerTrigger, false);
				return true;
			}
		}
		else
		{
			if (price <= _takeProfitLevel)
			{
				HandleTakeProfit(_takeProfitLevel, -1);
				return true;
			}

			if (price >= _stopLossLevel)
			{
				HandleStopLoss(price);
				return true;
			}

			if (price >= _upperTrigger && _layerIndex < MaxLayers)
			{
				EnterDirection(1, _upperTrigger, false);
				return true;
			}
		}

		return false;
	}

	private void HandleTakeProfit(decimal entryPrice, int direction)
	{
		CloseCurrentPosition();
		ScheduleReEntry(direction, entryPrice);
	}

	private void HandleStopLoss(decimal referencePrice)
	{
		CloseCurrentPosition();
		ResetCoreState(referencePrice);
		_hasPendingEntry = false;
		_pendingDirection = 0;
		_pendingEntryPrice = 0m;
	}

	private void CloseCurrentPosition()
	{
		if (Position > 0m)
			SellMarket(Position);
		else if (Position < 0m)
			BuyMarket(-Position);
	}

	private void ScheduleReEntry(int direction, decimal entryPrice)
	{
		ResetCoreState(entryPrice);
		_hasPendingEntry = true;
		_pendingDirection = direction;
		_pendingEntryPrice = entryPrice;
	}

	private void EnterDirection(int direction, decimal entryPrice, bool resetLayer)
	{
		if (resetLayer)
			_layerIndex = 1;
		else
			_layerIndex++;

		if (_layerIndex > MaxLayers)
		{
			_layerIndex = MaxLayers;
			return;
		}

		var desiredPosition = direction * GetVolumeForLayer(_layerIndex);
		var delta = desiredPosition - Position;

		if (delta > 0m)
		{
			BuyMarket(delta);
		}
		else if (delta < 0m)
		{
			SellMarket(-delta);
		}

		_currentDirection = direction;
		_currentEntryPrice = entryPrice;
		UpdateGridAfterEntry(direction, entryPrice);
	}

	private void ResetCoreState(decimal referencePrice)
	{
		_currentDirection = 0;
		_layerIndex = 0;
		_currentEntryPrice = 0m;
		_takeProfitLevel = 0m;
		_stopLossLevel = 0m;

		var aligned = AlignPrice(referencePrice);
		_upperTrigger = aligned;
		_lowerTrigger = aligned - StepSize;
	}

	private void InitializeLevels(decimal price)
	{
		ResetCoreState(price);
		_levelsInitialized = true;
	}

	private decimal GetVolumeForLayer(int layer)
	{
		var volume = BaseVolume * (decimal)Math.Pow((double)Multiplier, layer - 1);
		return NormalizeVolume(volume);
	}

	private void UpdateGridAfterEntry(int direction, decimal entryPrice)
	{
		if (direction > 0)
		{
			if (_layerIndex == 1)
			{
				_takeProfitLevel = entryPrice + StepSize;
				_stopLossLevel = entryPrice - StepSize * Multiplier;
				_upperTrigger = entryPrice + StepSize;
				_lowerTrigger = entryPrice - StepSize;
			}
			else
			{
				_takeProfitLevel = entryPrice + StepSize * Multiplier;
				_stopLossLevel = entryPrice - StepSize;
				_upperTrigger = entryPrice + StepSize * Multiplier;
				_lowerTrigger = entryPrice - StepSize;
			}
		}
		else
		{
			if (_layerIndex == 1)
			{
				_takeProfitLevel = entryPrice - StepSize;
				_stopLossLevel = entryPrice + StepSize * Multiplier;
				_upperTrigger = entryPrice + StepSize;
				_lowerTrigger = entryPrice - StepSize;
			}
			else
			{
				_takeProfitLevel = entryPrice - StepSize * Multiplier;
				_stopLossLevel = entryPrice + StepSize;
				_upperTrigger = entryPrice + StepSize;
				_lowerTrigger = entryPrice - StepSize * Multiplier;
			}
		}
	}

	private decimal AlignPrice(decimal price)
	{
		var step = StepSize;
		if (step <= 0m)
			return price;

		var steps = Math.Ceiling(price / step);
		return steps * step;
	}

	private decimal NormalizeVolume(decimal volume)
	{
		if (volume <= 0m)
			return 0m;

		var step = Security?.VolumeStep ?? 0m;
		if (step <= 0m)
			return volume;

		var steps = Math.Max(1m, Math.Round(volume / step, MidpointRounding.AwayFromZero));
		return steps * step;
	}
}

