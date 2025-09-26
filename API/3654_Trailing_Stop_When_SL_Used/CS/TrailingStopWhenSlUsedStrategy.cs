using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Trailing stop controller converted from the MetaTrader expert that updates the stop level even when the original position was opened without a stop-loss.
/// </summary>
public class TrailingStopWhenSlUsedStrategy : Strategy
{
	private readonly StrategyParam<decimal> _trailingStepPoints;

	private Order _longStopOrder;
	private Order _shortStopOrder;
	private decimal? _longStopPrice;
	private decimal? _shortStopPrice;
	private decimal _priceStep = 1m;

	/// <summary>
	/// Trailing step distance expressed in instrument points.
	/// </summary>
	public decimal TrailingStepPoints
	{
		get => _trailingStepPoints.Value;
		set => _trailingStepPoints.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="TrailingStopWhenSlUsedStrategy"/> class.
	/// </summary>
	public TrailingStopWhenSlUsedStrategy()
	{
		_trailingStepPoints = Param(nameof(TrailingStepPoints), 3m)
			.SetGreaterThanZero()
			.SetDisplay("Trailing Step (points)", "Distance between trailing updates expressed in points", "Risk management")
			.SetCanOptimize(true);
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, DataType.Ticks);
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Obtain the price step so that point-based settings can be converted into price distances.
		_priceStep = Security?.PriceStep ?? 0m;
		if (_priceStep <= 0m)
			_priceStep = 1m;

		// React to every trade tick just like the original MetaTrader trailing logic.
		SubscribeTrades()
			.Bind(ProcessTrade)
			.Start();
	}

	/// <inheritdoc />
	protected override void OnStopped()
	{
		base.OnStopped();

		// Make sure no protective orders remain after the strategy stops.
		ResetStops();
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		// Remove the opposite protective order when the position flips.
		if (Position <= 0m)
			ResetLongStop();

		if (Position >= 0m)
			ResetShortStop();

		// Clear the stored trailing levels once the position is closed.
		if (Position == 0m)
			ResetTrailingState();
	}

	private void ProcessTrade(ExecutionMessage trade)
	{
		var price = trade.TradePrice;
		if (price == null || price.Value <= 0m)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var stepDistance = TrailingStepPoints * _priceStep;
		if (stepDistance <= 0m)
			return;

		var entryPrice = PositionPrice;
		if (entryPrice == null || entryPrice.Value <= 0m)
			return;

		var volume = Math.Abs(Position);
		if (volume <= 0m)
			return;

		if (Position > 0m)
		{
			ProcessLong(price.Value, entryPrice.Value, volume, stepDistance);
		}
		else if (Position < 0m)
		{
			ProcessShort(price.Value, entryPrice.Value, volume, stepDistance);
		}
	}

	private void ProcessLong(decimal currentPrice, decimal entryPrice, decimal volume, decimal stepDistance)
	{
		var trailingLevel = currentPrice - stepDistance;
		if (trailingLevel <= 0m)
			return;

		// Initialise the stop once price has moved above the entry and no stop exists yet.
		if (_longStopPrice == null)
		{
			if (currentPrice > entryPrice && trailingLevel > entryPrice)
				UpdateLongStop(trailingLevel, volume);
		}
		// Move the stop forward when the market continues to rise.
		else if (currentPrice > entryPrice && trailingLevel > _longStopPrice.Value)
		{
			UpdateLongStop(trailingLevel, volume);
		}
	}

	private void ProcessShort(decimal currentPrice, decimal entryPrice, decimal volume, decimal stepDistance)
	{
		var trailingLevel = currentPrice + stepDistance;
		if (trailingLevel <= 0m)
			return;

		// Initialise the stop once price has moved below the entry and no stop exists yet.
		if (_shortStopPrice == null)
		{
			if (currentPrice < entryPrice && trailingLevel < entryPrice)
				UpdateShortStop(trailingLevel, volume);
		}
		// Move the stop lower when the market keeps falling.
		else if (currentPrice < entryPrice && trailingLevel < _shortStopPrice.Value)
		{
			UpdateShortStop(trailingLevel, volume);
		}
	}

	private void UpdateLongStop(decimal stopPrice, decimal volume)
	{
		if (stopPrice <= 0m || volume <= 0m)
			return;

		if (_longStopOrder != null && _longStopOrder.State == OrderStates.Active && _longStopOrder.Price == stopPrice)
			return;

		if (_longStopOrder != null && _longStopOrder.State == OrderStates.Active)
			CancelOrder(_longStopOrder);

		// Register a new protective sell stop that trails the long position.
		_longStopOrder = SellStop(volume, stopPrice);
		_longStopPrice = stopPrice;
	}

	private void UpdateShortStop(decimal stopPrice, decimal volume)
	{
		if (stopPrice <= 0m || volume <= 0m)
			return;

		if (_shortStopOrder != null && _shortStopOrder.State == OrderStates.Active && _shortStopOrder.Price == stopPrice)
			return;

		if (_shortStopOrder != null && _shortStopOrder.State == OrderStates.Active)
			CancelOrder(_shortStopOrder);

		// Register a new protective buy stop that trails the short position.
		_shortStopOrder = BuyStop(volume, stopPrice);
		_shortStopPrice = stopPrice;
	}

	private void ResetLongStop()
	{
		if (_longStopOrder != null && _longStopOrder.State == OrderStates.Active)
			CancelOrder(_longStopOrder);

		_longStopOrder = null;
		_longStopPrice = null;
	}

	private void ResetShortStop()
	{
		if (_shortStopOrder != null && _shortStopOrder.State == OrderStates.Active)
			CancelOrder(_shortStopOrder);

		_shortStopOrder = null;
		_shortStopPrice = null;
	}

	private void ResetTrailingState()
	{
		_longStopPrice = null;
		_shortStopPrice = null;
	}

	private void ResetStops()
	{
		ResetLongStop();
		ResetShortStop();
	}
}
