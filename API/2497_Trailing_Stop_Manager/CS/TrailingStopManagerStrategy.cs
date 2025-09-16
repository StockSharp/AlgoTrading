using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Trailing stop manager that mirrors the pip-based trailing logic from the original MQL expert.
/// </summary>
public class TrailingStopManagerStrategy : Strategy
{
	/// <summary>
	/// Direction of the optional market order placed when the strategy starts.
	/// </summary>
	public enum InitialDirection
	{
		None,
		Long,
		Short
	}

	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<decimal> _trailingStepPips;
	private readonly StrategyParam<InitialDirection> _startDirection;

	private decimal _entryPrice;
	private decimal _trailingStopPrice;
	private bool _trailingActive;
	private InitialDirection _currentDirection = InitialDirection.None;
	private decimal _priceStep = 1m;

	/// <summary>
	/// Trailing stop activation distance expressed in pips.
	/// </summary>
	public decimal TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	/// <summary>
	/// Minimum pip distance that must be covered before the trailing stop is adjusted again.
	/// </summary>
	public decimal TrailingStepPips
	{
		get => _trailingStepPips.Value;
		set => _trailingStepPips.Value = value;
	}

	/// <summary>
	/// Optional market order direction executed on start to quickly demonstrate trailing behaviour.
	/// </summary>
	public InitialDirection StartDirection
	{
		get => _startDirection.Value;
		set => _startDirection.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="TrailingStopManagerStrategy"/> class.
	/// </summary>
	public TrailingStopManagerStrategy()
	{
		_trailingStopPips = Param(nameof(TrailingStopPips), 10m)
			.SetGreaterThanZero()
			.SetDisplay("Trailing Stop (pips)", "Distance to activate trailing", "Risk Management")
			.SetCanOptimize(true);

		_trailingStepPips = Param(nameof(TrailingStepPips), 5m)
			.SetGreaterOrEqualZero()
			.SetDisplay("Trailing Step (pips)", "Minimal move before adjusting stop", "Risk Management")
			.SetCanOptimize(true);

		_startDirection = Param(nameof(StartDirection), InitialDirection.None)
			.SetDisplay("Initial Direction", "Optional market order on start", "Trading");
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

		// Retrieve price step to convert pip values into price offsets.
		_priceStep = Security?.PriceStep ?? 1m;
		if (_priceStep <= 0m)
			_priceStep = 1m;

		// Subscribe to trade ticks so the trailing stop reacts to real-time price changes.
		SubscribeTrades()
			.Bind(ProcessTrade)
			.Start();

		// Optionally open an immediate position to showcase the trailing stop logic.
		switch (StartDirection)
		{
			case InitialDirection.Long:
				BuyMarket();
				break;

			case InitialDirection.Short:
				SellMarket();
				break;
		}
	}

	/// <inheritdoc />
	protected override void OnOwnTradeReceived(MyTrade trade)
	{
		base.OnOwnTradeReceived(trade);

		var tradePrice = trade.Trade?.Price ?? 0m;
		if (tradePrice <= 0m)
			return;

		// Reset trailing state whenever a new position is opened.
		if (Position > 0 && trade.Order.Side == Sides.Buy)
		{
			_entryPrice = tradePrice;
			_trailingActive = false;
			_trailingStopPrice = 0m;
			_currentDirection = InitialDirection.Long;
		}
		else if (Position < 0 && trade.Order.Side == Sides.Sell)
		{
			_entryPrice = tradePrice;
			_trailingActive = false;
			_trailingStopPrice = 0m;
			_currentDirection = InitialDirection.Short;
		}
		else if (Position == 0)
		{
			ResetTrailing();
		}
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		if (Position == 0)
			ResetTrailing();
	}

	private void ProcessTrade(ExecutionMessage trade)
	{
		var price = trade.TradePrice;
		if (price == null || price.Value <= 0m)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_entryPrice <= 0m)
			return;

		var currentPrice = price.Value;

		if (Position > 0 && _currentDirection == InitialDirection.Long)
		{
			UpdateLongTrailing(currentPrice);
		}
		else if (Position < 0 && _currentDirection == InitialDirection.Short)
		{
			UpdateShortTrailing(currentPrice);
		}
	}

	private void UpdateLongTrailing(decimal price)
	{
		var stopDistance = TrailingStopPips * _priceStep;
		if (stopDistance <= 0m)
			return;

		var stepDistance = TrailingStepPips * _priceStep;

		// Activate the trailing stop once price has moved far enough above the entry.
		if (!_trailingActive)
		{
			if (price - _entryPrice >= stopDistance)
			{
				_trailingActive = true;
				_trailingStopPrice = _entryPrice;
			}
		}
		else
		{
			var desiredStop = price - stopDistance;

			// Only move the stop forward when the configured step distance is met.
			if (stepDistance <= 0m)
			{
				if (desiredStop > _trailingStopPrice)
					_trailingStopPrice = desiredStop;
			}
			else if (desiredStop - _trailingStopPrice >= stepDistance)
			{
				_trailingStopPrice = desiredStop;
			}
		}

		// Exit once price drops to the trailing stop level.
		if (_trailingActive && price <= _trailingStopPrice)
			ExitLong();
	}

	private void UpdateShortTrailing(decimal price)
	{
		var stopDistance = TrailingStopPips * _priceStep;
		if (stopDistance <= 0m)
			return;

		var stepDistance = TrailingStepPips * _priceStep;

		// Activate the trailing stop once price has moved far enough below the entry.
		if (!_trailingActive)
		{
			if (_entryPrice - price >= stopDistance)
			{
				_trailingActive = true;
				_trailingStopPrice = _entryPrice;
			}
		}
		else
		{
			var desiredStop = price + stopDistance;

			// Only move the stop forward when the configured step distance is met.
			if (stepDistance <= 0m)
			{
				if (desiredStop < _trailingStopPrice || _trailingStopPrice == 0m)
					_trailingStopPrice = desiredStop;
			}
			else if (_trailingStopPrice - desiredStop >= stepDistance)
			{
				_trailingStopPrice = desiredStop;
			}
		}

		// Exit once price rises to the trailing stop level.
		if (_trailingActive && price >= _trailingStopPrice)
			ExitShort();
	}

	private void ExitLong()
	{
		if (Position <= 0)
			return;

		SellMarket(Position);
	}

	private void ExitShort()
	{
		if (Position >= 0)
			return;

		BuyMarket(-Position);
	}

	private void ResetTrailing()
	{
		_entryPrice = 0m;
		_trailingStopPrice = 0m;
		_trailingActive = false;
		_currentDirection = InitialDirection.None;
	}
}
