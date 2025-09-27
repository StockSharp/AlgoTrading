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
/// Utility strategy that mirrors the trailSL MetaTrader script.
/// It manages open positions by moving the protective stop to break even and trailing it as price advances.
/// The strategy does not generate its own entry signals.
/// </summary>
public class TrailSlManagerStrategy : Strategy
{
	private readonly StrategyParam<bool> _enableBreakEven;
	private readonly StrategyParam<int> _breakEvenTriggerPoints;
	private readonly StrategyParam<int> _breakEvenOffsetPoints;
	private readonly StrategyParam<bool> _enableTrailing;
	private readonly StrategyParam<bool> _trailAfterBreakEven;
	private readonly StrategyParam<int> _trailStartPoints;
	private readonly StrategyParam<int> _trailStepPoints;
	private readonly StrategyParam<int> _trailOffsetPoints;
	private readonly StrategyParam<int> _initialStopPoints;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _priceStep;
	private decimal _longStop;
	private decimal _shortStop;
	private bool _longBreakEvenActive;
	private bool _shortBreakEvenActive;

	/// <summary>
	/// Enables automatic break-even adjustment.
	/// </summary>
	public bool EnableBreakEven
	{
		get => _enableBreakEven.Value;
		set => _enableBreakEven.Value = value;
	}

	/// <summary>
	/// Required profit in points before break-even is activated.
	/// </summary>
	public int BreakEvenTriggerPoints
	{
		get => _breakEvenTriggerPoints.Value;
		set => _breakEvenTriggerPoints.Value = value;
	}

	/// <summary>
	/// Additional points added to the break-even price once triggered.
	/// </summary>
	public int BreakEvenOffsetPoints
	{
		get => _breakEvenOffsetPoints.Value;
		set => _breakEvenOffsetPoints.Value = value;
	}

	/// <summary>
	/// Enables trailing stop management.
	/// </summary>
	public bool EnableTrailing
	{
		get => _enableTrailing.Value;
		set => _enableTrailing.Value = value;
	}

	/// <summary>
	/// Trailing starts only after a successful break-even move.
	/// </summary>
	public bool TrailAfterBreakEven
	{
		get => _trailAfterBreakEven.Value;
		set => _trailAfterBreakEven.Value = value;
	}

	/// <summary>
	/// Minimum profit in points before trailing begins.
	/// </summary>
	public int TrailStartPoints
	{
		get => _trailStartPoints.Value;
		set => _trailStartPoints.Value = value;
	}

	/// <summary>
	/// Distance in points between trailing recalculations.
	/// </summary>
	public int TrailStepPoints
	{
		get => _trailStepPoints.Value;
		set => _trailStepPoints.Value = value;
	}

	/// <summary>
	/// Stop loss increment applied on each trailing step (points).
	/// </summary>
	public int TrailOffsetPoints
	{
		get => _trailOffsetPoints.Value;
		set => _trailOffsetPoints.Value = value;
	}

	/// <summary>
	/// Initial protective stop distance measured in points.
	/// </summary>
	public int InitialStopPoints
	{
		get => _initialStopPoints.Value;
		set => _initialStopPoints.Value = value;
	}

	/// <summary>
	/// Candle type used for monitoring price progress.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public TrailSlManagerStrategy()
	{
		_enableBreakEven = Param(nameof(EnableBreakEven), true)
		.SetDisplay("Break Even", "Enable break-even stop adjustment", "Risk")
		.SetCanOptimize(true);
		_breakEvenTriggerPoints = Param(nameof(BreakEvenTriggerPoints), 20)
		.SetDisplay("Break Even Trigger", "Points required before moving to break-even", "Risk")
		.SetCanOptimize(true);
		_breakEvenOffsetPoints = Param(nameof(BreakEvenOffsetPoints), 10)
		.SetDisplay("Break Even Offset", "Extra points locked when break-even triggers", "Risk")
		.SetCanOptimize(true);
		_enableTrailing = Param(nameof(EnableTrailing), true)
		.SetDisplay("Trailing", "Enable trailing stop management", "Risk")
		.SetCanOptimize(true);
		_trailAfterBreakEven = Param(nameof(TrailAfterBreakEven), true)
		.SetDisplay("Trail After Break Even", "Start trailing only after break-even", "Risk")
		.SetCanOptimize(true);
		_trailStartPoints = Param(nameof(TrailStartPoints), 40)
		.SetDisplay("Trail Start", "Points of profit before trailing is considered", "Risk")
		.SetCanOptimize(true);
		_trailStepPoints = Param(nameof(TrailStepPoints), 10)
		.SetDisplay("Trail Step", "Price step that triggers a new trailing recalculation", "Risk")
		.SetCanOptimize(true);
		_trailOffsetPoints = Param(nameof(TrailOffsetPoints), 10)
		.SetDisplay("Trail Offset", "Points added to the stop on every trailing step", "Risk")
		.SetCanOptimize(true);
		_initialStopPoints = Param(nameof(InitialStopPoints), 200)
		.SetDisplay("Initial Stop", "Initial stop distance used before trailing", "Risk")
		.SetCanOptimize(true);
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay("Candle Type", "Candle subscription for monitoring", "Data");
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

		_priceStep = Security?.PriceStep ?? 0m;
		if (_priceStep <= 0m)
			throw new InvalidOperationException("Security price step must be defined.");

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	/// <inheritdoc />
	protected override void OnNewMyTrade(MyTrade trade)
	{
		base.OnNewMyTrade(trade);

		if (Position == 0)
		{
			ResetState();
			return;
		}

		if (trade.Order.Direction == Sides.Buy && Position > 0)
		{
			_longStop = InitialStopPoints > 0 ? PositionAvgPrice - InitialStopPoints * _priceStep : 0m;
			_longBreakEvenActive = false;
		}
		else if (trade.Order.Direction == Sides.Sell && Position < 0)
		{
			_shortStop = InitialStopPoints > 0 ? PositionAvgPrice + InitialStopPoints * _priceStep : 0m;
			_shortBreakEvenActive = false;
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		ManageLongPosition(candle);
		ManageShortPosition(candle);
	}

	private void ManageLongPosition(ICandleMessage candle)
	{
		if (Position <= 0)
		{
			_longStop = 0m;
			_longBreakEvenActive = false;
			return;
		}

		var entryPrice = PositionAvgPrice;
		if (entryPrice <= 0m)
			return;

		var currentPrice = candle.ClosePrice;
		var profitPoints = (currentPrice - entryPrice) / _priceStep;

		if (EnableBreakEven && !_longBreakEvenActive && profitPoints >= BreakEvenTriggerPoints && BreakEvenTriggerPoints > 0)
		{
			var newStop = BreakEvenOffsetPoints > 0
				? entryPrice + BreakEvenOffsetPoints * _priceStep
				: entryPrice;

			if (newStop < currentPrice)
			{
				_longStop = Math.Max(_longStop, newStop);
				_longBreakEvenActive = true;
			}
		}

		if (!EnableTrailing || TrailOffsetPoints <= 0 || TrailStepPoints <= 0)
			return;

		var requireBreakEven = TrailAfterBreakEven && EnableBreakEven;
		if (requireBreakEven && !_longBreakEvenActive)
			return;

		var baseStop = requireBreakEven
			? (_longStop > 0m ? _longStop : (BreakEvenOffsetPoints > 0 ? entryPrice + BreakEvenOffsetPoints * _priceStep : entryPrice))
			: (InitialStopPoints > 0 ? entryPrice - InitialStopPoints * _priceStep : (_longStop > 0m ? _longStop : 0m));

		if (baseStop <= 0m)
			return;

		if (!requireBreakEven && profitPoints < TrailStartPoints)
			return;

		if (requireBreakEven)
		{
			var baseDistance = (currentPrice - baseStop) / _priceStep;
			if (baseDistance < TrailStartPoints)
				return;
		}

		var startPrice = requireBreakEven
			? baseStop + (TrailStartPoints - TrailStepPoints) * _priceStep
			: entryPrice + (TrailStartPoints - TrailStepPoints) * _priceStep;

		var stepDistance = TrailStepPoints * _priceStep;
		if (stepDistance <= 0m)
			return;

		var openSteps = (currentPrice - startPrice) / stepDistance;
		if (openSteps <= 0m)
			return;

		var stepOpenPrice = (int)Math.Floor(openSteps);
		var currentStopSteps = _longStop > baseStop
			? (int)Math.Floor((_longStop - baseStop) / (TrailOffsetPoints * _priceStep))
			: 0;

		if (stepOpenPrice <= currentStopSteps)
			return;

		var proposedStop = baseStop + stepOpenPrice * TrailOffsetPoints * _priceStep;
		var maxStop = candle.LowPrice - _priceStep;
		if (proposedStop >= maxStop)
			proposedStop = maxStop;

		if (proposedStop > _longStop && proposedStop < currentPrice)
			_longStop = proposedStop;

		if (_longStop > 0m && candle.LowPrice <= _longStop)
			SellMarket(Position);
	}

	private void ManageShortPosition(ICandleMessage candle)
	{
		if (Position >= 0)
		{
			_shortStop = 0m;
			_shortBreakEvenActive = false;
			return;
		}

		var entryPrice = PositionAvgPrice;
		if (entryPrice <= 0m)
			return;

		var currentPrice = candle.ClosePrice;
		var profitPoints = (entryPrice - currentPrice) / _priceStep;

		if (EnableBreakEven && !_shortBreakEvenActive && profitPoints >= BreakEvenTriggerPoints && BreakEvenTriggerPoints > 0)
		{
			var newStop = BreakEvenOffsetPoints > 0
				? entryPrice - BreakEvenOffsetPoints * _priceStep
				: entryPrice;

			if (newStop > currentPrice)
			{
				_shortStop = _shortStop == 0m ? newStop : Math.Min(_shortStop, newStop);
				_shortBreakEvenActive = true;
			}
		}

		if (!EnableTrailing || TrailOffsetPoints <= 0 || TrailStepPoints <= 0)
			return;

		var requireBreakEven = TrailAfterBreakEven && EnableBreakEven;
		if (requireBreakEven && !_shortBreakEvenActive)
			return;

		var baseStop = requireBreakEven
			? (_shortStop > 0m ? _shortStop : (BreakEvenOffsetPoints > 0 ? entryPrice - BreakEvenOffsetPoints * _priceStep : entryPrice))
			: (InitialStopPoints > 0 ? entryPrice + InitialStopPoints * _priceStep : (_shortStop > 0m ? _shortStop : 0m));

		if (baseStop <= 0m)
			return;

		if (!requireBreakEven && profitPoints < TrailStartPoints)
			return;

		if (requireBreakEven)
		{
			var baseDistance = (baseStop - currentPrice) / _priceStep;
			if (baseDistance < TrailStartPoints)
				return;
		}

		var startPrice = requireBreakEven
			? baseStop - (TrailStartPoints - TrailStepPoints) * _priceStep
			: entryPrice - (TrailStartPoints - TrailStepPoints) * _priceStep;

		var stepDistance = TrailStepPoints * _priceStep;
		if (stepDistance <= 0m)
			return;

		var openSteps = (startPrice - currentPrice) / stepDistance;
		if (openSteps <= 0m)
			return;

		var stepOpenPrice = (int)Math.Floor(openSteps);
		var currentStopSteps = _shortStop > 0m
			? (int)Math.Floor((baseStop - _shortStop) / (TrailOffsetPoints * _priceStep))
			: 0;

		if (stepOpenPrice <= currentStopSteps)
			return;

		var proposedStop = baseStop - stepOpenPrice * TrailOffsetPoints * _priceStep;
		var minStop = candle.HighPrice + _priceStep;
		if (proposedStop <= minStop)
			proposedStop = minStop;

		if ((_shortStop == 0m || proposedStop < _shortStop) && proposedStop > currentPrice)
			_shortStop = proposedStop;

		if (_shortStop > 0m && candle.HighPrice >= _shortStop)
			BuyMarket(Math.Abs(Position));
	}

	private void ResetState()
	{
		_longStop = 0m;
		_shortStop = 0m;
		_longBreakEvenActive = false;
		_shortBreakEvenActive = false;
	}
}

