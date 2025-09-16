using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public enum TrailingPositionType
{
	All,
	Long,
	Short,
}

/// <summary>
/// Strategy that manages trailing stop-loss and take-profit levels similar to the original MQL Expert Advisor.
/// </summary>
public class TrailingStopAndTakeStrategy : Strategy
{
	private const decimal Epsilon = 0.0000001m;

	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _volume;
	private readonly StrategyParam<TrailingPositionType> _positionType;
	private readonly StrategyParam<decimal> _initialStopLossPoints;
	private readonly StrategyParam<decimal> _initialTakeProfitPoints;
	private readonly StrategyParam<decimal> _trailingStopLossPoints;
	private readonly StrategyParam<decimal> _trailingTakeProfitPoints;
	private readonly StrategyParam<decimal> _trailingStepPoints;
	private readonly StrategyParam<bool> _allowTrailingLoss;
	private readonly StrategyParam<decimal> _breakevenPoints;
	private readonly StrategyParam<int> _spreadMultiplier;

	private decimal _priceStep;
	private decimal _previousPosition;

	private decimal? _longStop;
	private decimal? _longTake;
	private decimal? _shortStop;
	private decimal? _shortTake;

	/// <summary>
	/// Initializes a new instance of the <see cref="TrailingStopAndTakeStrategy"/>.
	/// </summary>
	public TrailingStopAndTakeStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle aggregation used for trailing decisions", "General");

		_volume = Param(nameof(Volume), 1m)
			.SetDisplay("Volume", "Default trade volume", "Trading")
			.SetCanOptimize(true)
			.SetGreaterThanZero();

		_positionType = Param(nameof(PositionType), TrailingPositionType.All)
			.SetDisplay("Position Filter", "Positions managed by the trailing engine", "Trading");

		_initialStopLossPoints = Param(nameof(InitialStopLossPoints), 400m)
			.SetRange(0m, 10000m)
			.SetDisplay("Initial Stop", "Initial stop-loss size in price points", "Risk")
			.SetCanOptimize(true);

		_initialTakeProfitPoints = Param(nameof(InitialTakeProfitPoints), 400m)
			.SetRange(0m, 10000m)
			.SetDisplay("Initial Take", "Initial take-profit size in price points", "Risk")
			.SetCanOptimize(true);

		_trailingStopLossPoints = Param(nameof(TrailingStopLossPoints), 200m)
			.SetRange(0m, 10000m)
			.SetDisplay("Trailing Stop", "Trailing stop distance in price points", "Risk")
			.SetCanOptimize(true);

		_trailingTakeProfitPoints = Param(nameof(TrailingTakeProfitPoints), 200m)
			.SetRange(0m, 10000m)
			.SetDisplay("Trailing Take", "Trailing take-profit distance in price points", "Risk")
			.SetCanOptimize(true);

		_trailingStepPoints = Param(nameof(TrailingStepPoints), 10m)
			.SetRange(0m, 1000m)
			.SetDisplay("Trailing Step", "Minimum movement required before adjusting targets", "Risk");

		_allowTrailingLoss = Param(nameof(AllowTrailingLoss), false)
			.SetDisplay("Trail In Loss", "Allow trailing while position is not yet profitable", "Risk");

		_breakevenPoints = Param(nameof(BreakevenPoints), 6m)
			.SetRange(0m, 1000m)
			.SetDisplay("Breakeven Points", "Profit offset used for breakeven protection", "Risk");

		_spreadMultiplier = Param(nameof(SpreadMultiplier), 2)
			.SetRange(1, 20)
			.SetDisplay("Spread Multiplier", "Multiplier applied to minimal stop distance", "Execution");
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
	/// Default order volume.
	/// </summary>
	public decimal Volume
	{
		get => _volume.Value;
		set => _volume.Value = value;
	}

	/// <summary>
	/// Position filter managed by the trailing logic.
	/// </summary>
	public TrailingPositionType PositionType
	{
		get => _positionType.Value;
		set => _positionType.Value = value;
	}

	/// <summary>
	/// Initial stop-loss size expressed in price points.
	/// </summary>
	public decimal InitialStopLossPoints
	{
		get => _initialStopLossPoints.Value;
		set => _initialStopLossPoints.Value = value;
	}

	/// <summary>
	/// Initial take-profit size expressed in price points.
	/// </summary>
	public decimal InitialTakeProfitPoints
	{
		get => _initialTakeProfitPoints.Value;
		set => _initialTakeProfitPoints.Value = value;
	}

	/// <summary>
	/// Trailing stop distance expressed in price points.
	/// </summary>
	public decimal TrailingStopLossPoints
	{
		get => _trailingStopLossPoints.Value;
		set => _trailingStopLossPoints.Value = value;
	}

	/// <summary>
	/// Trailing take-profit distance expressed in price points.
	/// </summary>
	public decimal TrailingTakeProfitPoints
	{
		get => _trailingTakeProfitPoints.Value;
		set => _trailingTakeProfitPoints.Value = value;
	}

	/// <summary>
	/// Minimum movement required before stops or targets are updated.
	/// </summary>
	public decimal TrailingStepPoints
	{
		get => _trailingStepPoints.Value;
		set => _trailingStepPoints.Value = value;
	}

	/// <summary>
	/// Enables trailing adjustments while the position remains in the loss zone.
	/// </summary>
	public bool AllowTrailingLoss
	{
		get => _allowTrailingLoss.Value;
		set => _allowTrailingLoss.Value = value;
	}

	/// <summary>
	/// Profit offset in points used to define the breakeven level.
	/// </summary>
	public decimal BreakevenPoints
	{
		get => _breakevenPoints.Value;
		set => _breakevenPoints.Value = value;
	}

	/// <summary>
	/// Multiplier applied to the minimal stop distance approximation.
	/// </summary>
	public int SpreadMultiplier
	{
		get => _spreadMultiplier.Value;
		set => _spreadMultiplier.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, CandleType);
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		_priceStep = Security?.PriceStep ?? 1m;
		if (_priceStep <= 0m)
			_priceStep = 1m;

		_previousPosition = 0m;
		ResetLevels();

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
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Handle long positions first.
		if (Position > 0m)
		{
			if (PositionType == TrailingPositionType.Short)
			{
				ResetLongLevels();
			}
			else
			{
				if (_previousPosition <= 0m)
					ResetShortLevels();

				EnsureLongInitialized();
				UpdateLongTrailing(candle);
				ManageLongExits(candle);
			}
		}
		else if (Position < 0m)
		{
			if (PositionType == TrailingPositionType.Long)
			{
				ResetShortLevels();
			}
			else
			{
				if (_previousPosition >= 0m)
					ResetLongLevels();

				EnsureShortInitialized();
				UpdateShortTrailing(candle);
				ManageShortExits(candle);
			}
		}
		else
		{
			ResetLevels();
		}

		// Try to open a new position once flat.
		TryEnter(candle);

		_previousPosition = Position;
	}

	private void TryEnter(ICandleMessage candle)
	{
		if (Position != 0m || Volume <= 0m)
			return;

		// Simple directional entry mirroring the tester behavior from the MQL script.
		if (PositionType == TrailingPositionType.Long)
		{
			if (candle.ClosePrice > candle.OpenPrice)
				BuyMarket(Volume);
		}
		else if (PositionType == TrailingPositionType.Short)
		{
			if (candle.ClosePrice < candle.OpenPrice)
				SellMarket(Volume);
		}
		else
		{
			if (candle.ClosePrice > candle.OpenPrice)
				BuyMarket(Volume);
			else if (candle.ClosePrice < candle.OpenPrice)
				SellMarket(Volume);
		}
	}

	private void EnsureLongInitialized()
	{
		if (Position <= 0m)
			return;

		var entryPrice = PositionPrice;
		if (entryPrice <= 0m)
			return;

		var minDistance = GetMinStopDistance();

		if (_longStop == null)
		{
			var points = InitialStopLossPoints > 0m
				? InitialStopLossPoints
				: TrailingStopLossPoints > 0m ? TrailingStopLossPoints : 0m;

			if (points > 0m)
			{
				var candidate = entryPrice - points * _priceStep;
				var minAllowed = entryPrice - minDistance;
				_longStop = Math.Min(candidate, minAllowed);
			}
		}

		if (_longTake == null)
		{
			var points = InitialTakeProfitPoints > 0m
				? InitialTakeProfitPoints
				: TrailingTakeProfitPoints > 0m ? TrailingTakeProfitPoints : 0m;

			if (points > 0m)
			{
				var candidate = entryPrice + points * _priceStep;
				var minAllowed = entryPrice + minDistance;
				_longTake = Math.Max(candidate, minAllowed);
			}
		}
	}

	private void EnsureShortInitialized()
	{
		if (Position >= 0m)
			return;

		var entryPrice = PositionPrice;
		if (entryPrice <= 0m)
			return;

		var minDistance = GetMinStopDistance();

		if (_shortStop == null)
		{
			var points = InitialStopLossPoints > 0m
				? InitialStopLossPoints
				: TrailingStopLossPoints > 0m ? TrailingStopLossPoints : 0m;

			if (points > 0m)
			{
				var candidate = entryPrice + points * _priceStep;
				var minAllowed = entryPrice + minDistance;
				_shortStop = Math.Max(candidate, minAllowed);
			}
		}

		if (_shortTake == null)
		{
			var points = InitialTakeProfitPoints > 0m
				? InitialTakeProfitPoints
				: TrailingTakeProfitPoints > 0m ? TrailingTakeProfitPoints : 0m;

			if (points > 0m)
			{
				var candidate = entryPrice - points * _priceStep;
				var minAllowed = entryPrice - minDistance;
				_shortTake = Math.Min(candidate, minAllowed);
			}
		}
	}

	private void UpdateLongTrailing(ICandleMessage candle)
	{
		var entryPrice = PositionPrice;
		if (entryPrice <= 0m)
			return;

		var breakeven = entryPrice + BreakevenPoints * _priceStep;
		var trailingStep = Math.Max(TrailingStepPoints * _priceStep, Epsilon);
		var minDistance = GetMinStopDistance();

		if (TrailingStopLossPoints > 0m)
		{
			var candidate = candle.ClosePrice - TrailingStopLossPoints * _priceStep;
			var minAllowed = candle.ClosePrice - minDistance;
			var newStop = Math.Min(candidate, minAllowed);

			if (!AllowTrailingLoss && newStop < breakeven)
			{
				// Skip moving the stop into the loss area when disabled.
			}
			else if (_longStop == null || newStop > _longStop.Value + trailingStep)
			{
				_longStop = newStop;
			}
		}

		if (TrailingTakeProfitPoints > 0m)
		{
			var candidate = candle.ClosePrice + TrailingTakeProfitPoints * _priceStep;
			var minAllowed = candle.ClosePrice + minDistance;
			var newTake = Math.Max(candidate, minAllowed);

			if (!AllowTrailingLoss && newTake < breakeven)
				newTake = breakeven;

			if (_longTake == null || newTake < _longTake.Value - trailingStep)
				_longTake = newTake;
		}
	}

	private void UpdateShortTrailing(ICandleMessage candle)
	{
		var entryPrice = PositionPrice;
		if (entryPrice <= 0m)
			return;

		var breakeven = entryPrice - BreakevenPoints * _priceStep;
		var trailingStep = Math.Max(TrailingStepPoints * _priceStep, Epsilon);
		var minDistance = GetMinStopDistance();

		if (TrailingStopLossPoints > 0m)
		{
			var candidate = candle.ClosePrice + TrailingStopLossPoints * _priceStep;
			var minAllowed = candle.ClosePrice + minDistance;
			var newStop = Math.Max(candidate, minAllowed);

			if (!AllowTrailingLoss && newStop > breakeven)
			{
				// Skip moving the stop into the loss area when disabled.
			}
			else if (_shortStop == null || newStop < _shortStop.Value - trailingStep)
			{
				_shortStop = newStop;
			}
		}

		if (TrailingTakeProfitPoints > 0m)
		{
			var candidate = candle.ClosePrice - TrailingTakeProfitPoints * _priceStep;
			var minAllowed = candle.ClosePrice - minDistance;
			var newTake = Math.Min(candidate, minAllowed);

			if (!AllowTrailingLoss && newTake > breakeven)
				newTake = breakeven;

			if (_shortTake == null || newTake > _shortTake.Value + trailingStep)
				_shortTake = newTake;
		}
	}

	private void ManageLongExits(ICandleMessage candle)
	{
		if (_longStop.HasValue && candle.LowPrice <= _longStop.Value)
		{
			SellMarket(Position);
			ResetLongLevels();
			return;
		}

		if (_longTake.HasValue && candle.HighPrice >= _longTake.Value)
		{
			SellMarket(Position);
			ResetLongLevels();
		}
	}

	private void ManageShortExits(ICandleMessage candle)
	{
		if (_shortStop.HasValue && candle.HighPrice >= _shortStop.Value)
		{
			BuyMarket(Math.Abs(Position));
			ResetShortLevels();
			return;
		}

		if (_shortTake.HasValue && candle.LowPrice <= _shortTake.Value)
		{
			BuyMarket(Math.Abs(Position));
			ResetShortLevels();
		}
	}

	private decimal GetMinStopDistance()
	{
		var multiplier = SpreadMultiplier < 1 ? 1 : SpreadMultiplier;
		return _priceStep * multiplier;
	}

	private void ResetLevels()
	{
		ResetLongLevels();
		ResetShortLevels();
	}

	private void ResetLongLevels()
	{
		_longStop = null;
		_longTake = null;
	}

	private void ResetShortLevels()
	{
		_shortStop = null;
		_shortTake = null;
	}
}
