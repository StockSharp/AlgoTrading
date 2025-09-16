using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Trailing take profit manager that keeps profit targets aligned with the current market depth.
/// </summary>
public class TrailingTakeProfitStrategy : Strategy
{
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<decimal> _trailingStepPoints;
	private readonly StrategyParam<bool> _trailInLossZone;
	private readonly StrategyParam<decimal> _breakevenPoints;
	private readonly StrategyParam<int> _spreadMultiplier;
	private readonly StrategyParam<ManagedPositionType> _positionType;

	private decimal? _lastBid;
	private decimal? _lastAsk;
	private Order _takeProfitOrder;
	private decimal? _currentTakeProfitPrice;
	private decimal _previousPosition;

	/// <summary>
	/// Defines which position sides should be managed.
	/// </summary>
	public enum ManagedPositionType
	{
		/// <summary>
		/// Manage both long and short positions.
		/// </summary>
		All,

		/// <summary>
		/// Manage only long positions.
		/// </summary>
		Long,

		/// <summary>
		/// Manage only short positions.
		/// </summary>
		Short
	}

	/// <summary>
	/// Distance in price steps used to set the take profit target.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Minimum distance in price steps required to trail the take profit.
	/// </summary>
	public decimal TrailingStepPoints
	{
		get => _trailingStepPoints.Value;
		set => _trailingStepPoints.Value = value;
	}

	/// <summary>
	/// Allow trailing the target even if it moves into the loss zone.
	/// </summary>
	public bool TrailInLossZone
	{
		get => _trailInLossZone.Value;
		set => _trailInLossZone.Value = value;
	}

	/// <summary>
	/// Minimal profit in points that should remain when trailing is active.
	/// </summary>
	public decimal BreakevenPoints
	{
		get => _breakevenPoints.Value;
		set => _breakevenPoints.Value = value;
	}

	/// <summary>
	/// Multiplier applied to the price step when calculating the stop level distance.
	/// </summary>
	public int SpreadMultiplier
	{
		get => _spreadMultiplier.Value;
		set => _spreadMultiplier.Value = value;
	}

	/// <summary>
	/// Position side that should be handled by the trailing logic.
	/// </summary>
	public ManagedPositionType PositionType
	{
		get => _positionType.Value;
		set => _positionType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="TrailingTakeProfitStrategy"/>.
	/// </summary>
	public TrailingTakeProfitStrategy()
	{
		_takeProfitPoints = Param(nameof(TakeProfitPoints), 100m)
		.SetGreaterThanZero()
		.SetDisplay("Take Profit Points", "Distance for initial take profit", "Trailing")
		.SetCanOptimize(true)
		.SetOptimize(20m, 300m, 20m);

		_trailingStepPoints = Param(nameof(TrailingStepPoints), 10m)
		.SetGreaterThanZero()
		.SetDisplay("Trailing Step Points", "Minimum reduction before moving take profit", "Trailing")
		.SetCanOptimize(true)
		.SetOptimize(5m, 50m, 5m);

		_trailInLossZone = Param(nameof(TrailInLossZone), false)
		.SetDisplay("Trail In Loss Zone", "Allow take profit to trail into loss area", "Trailing");

		_breakevenPoints = Param(nameof(BreakevenPoints), 6m)
		.SetGreaterOrEqualZero()
		.SetDisplay("Breakeven Points", "Minimal profit preserved by trailing", "Risk Management");

		_spreadMultiplier = Param(nameof(SpreadMultiplier), 2)
		.SetGreaterThanZero()
		.SetDisplay("Spread Multiplier", "Stop level multiplier based on price step", "Execution");

		_positionType = Param(nameof(PositionType), ManagedPositionType.All)
		.SetDisplay("Position Type", "Select which sides are managed", "Filters");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		if (Security == null)
		yield break;

		yield return (Security, DataType.Level1);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_lastBid = default;
		_lastAsk = default;
		_takeProfitOrder = null;
		_currentTakeProfitPrice = default;
		_previousPosition = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Subscribe to Level1 to receive bid/ask updates for trailing decisions.
		SubscribeLevel1()
		.Bind(ProcessLevel1)
		.Start();
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		var position = Position;

		if (position == 0m)
		{
			CancelTakeProfit();
		}
		else
		{
			var isLong = position > 0m;

			if (!IsManagedSide(isLong))
			{
				CancelTakeProfit();
			}
			else if (Math.Sign(_previousPosition) != Math.Sign(position))
			{
				CreateInitialTakeProfit(isLong);
			}
		}

		_previousPosition = position;
	}

	private void ProcessLevel1(Level1ChangeMessage level1)
	{
		if (level1.Changes.TryGetValue(Level1Fields.BestBidPrice, out var bidValue) && bidValue is decimal bid)
		_lastBid = bid;

		if (level1.Changes.TryGetValue(Level1Fields.BestAskPrice, out var askValue) && askValue is decimal ask)
		_lastAsk = ask;

		UpdateTrailingTakeProfit();
	}

	private void CreateInitialTakeProfit(bool isLong)
	{
		var volume = Math.Abs(Position);
		var entryPrice = PositionPrice;

		if (volume <= 0m || entryPrice <= 0m)
		return;

		var targetPrice = CalculateDesiredPrice(isLong, entryPrice, includeBreakeven: false);

		PlaceTakeProfit(volume, targetPrice, isLong);
	}

	private void UpdateTrailingTakeProfit()
	{
		var position = Position;

		if (position == 0m || _takeProfitOrder == null)
		return;

		var isLong = position > 0m;

		if (!IsManagedSide(isLong))
		return;

		if (_takeProfitOrder.State == OrderStates.Done || _takeProfitOrder.State == OrderStates.Failed)
		return;

		var marketPrice = isLong ? _lastAsk : _lastBid;

		if (marketPrice is null)
		return;

		var stepValue = GetTrailingStepValue();

		if (stepValue <= 0m)
		return;

		var targetPrice = CalculateDesiredPrice(isLong, marketPrice.Value, includeBreakeven: true);

		if (_currentTakeProfitPrice is not decimal currentTarget)
		return;

		if (isLong)
		{
			if (targetPrice <= currentTarget - stepValue)
			MoveTakeProfit(targetPrice);
		}
		else
		{
			if (targetPrice >= currentTarget + stepValue)
			MoveTakeProfit(targetPrice);
		}
	}

	private decimal CalculateDesiredPrice(bool isLong, decimal referencePrice, bool includeBreakeven)
	{
		var step = GetPriceStep();

		if (step <= 0m)
		return referencePrice;

		var offset = TakeProfitPoints * step;
		var desired = isLong ? referencePrice + offset : referencePrice - offset;

		desired = EnsureStopLevel(desired, referencePrice, isLong);

		if (!TrailInLossZone && includeBreakeven)
		{
			var breakeven = GetBreakevenPrice(isLong, step);
			desired = isLong ? Math.Max(desired, breakeven) : Math.Min(desired, breakeven);
		}

		return desired;
	}

	private decimal GetBreakevenPrice(bool isLong, decimal step)
	{
		var entryPrice = PositionPrice;

		if (entryPrice <= 0m)
		return entryPrice;

		var offset = BreakevenPoints * step;

		return isLong ? entryPrice + offset : entryPrice - offset;
	}

	private decimal EnsureStopLevel(decimal desired, decimal reference, bool isLong)
	{
		var minDistance = GetStopLevelDistance();

		if (minDistance <= 0m)
		return desired;

		return isLong
		? Math.Max(desired, reference + minDistance)
		: Math.Min(desired, reference - minDistance);
	}

	private void MoveTakeProfit(decimal price)
	{
		var volume = Math.Abs(Position);

		if (volume <= 0m)
		{
			CancelTakeProfit();
			return;
		}

		PlaceTakeProfit(volume, price, Position > 0m);
	}

	private void PlaceTakeProfit(decimal volume, decimal price, bool isLong)
	{
		if (_takeProfitOrder != null)
		{
			if (_takeProfitOrder.State == OrderStates.Active || _takeProfitOrder.State == OrderStates.Pending)
			CancelOrder(_takeProfitOrder);
		}

		_currentTakeProfitPrice = price;
		_takeProfitOrder = isLong
		? SellLimit(volume, price)
		: BuyLimit(volume, price);
	}

	private void CancelTakeProfit()
	{
		if (_takeProfitOrder != null)
		{
			if (_takeProfitOrder.State == OrderStates.Active || _takeProfitOrder.State == OrderStates.Pending)
			CancelOrder(_takeProfitOrder);
		}

		_takeProfitOrder = null;
		_currentTakeProfitPrice = null;
	}

	private bool IsManagedSide(bool isLong)
	{
		return PositionType switch
		{
			ManagedPositionType.All => true,
			ManagedPositionType.Long => isLong,
			ManagedPositionType.Short => !isLong,
			_ => true
		};
	}

	private decimal GetPriceStep()
	{
		var step = Security?.MinPriceStep ?? Security?.PriceStep;
		return step ?? 0.01m;
	}

	private decimal GetTrailingStepValue()
	{
		return TrailingStepPoints * GetPriceStep();
	}

	private decimal GetStopLevelDistance()
	{
		var step = GetPriceStep();
		var multiplier = Math.Max(1, SpreadMultiplier);
		return multiplier * step;
	}
}
