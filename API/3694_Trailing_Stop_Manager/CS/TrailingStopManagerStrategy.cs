using System;

using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that mirrors the MetaTrader "Trailing Sl" expert by managing trailing stops for existing positions.
/// </summary>
public class TrailingStopManagerStrategy : Strategy
{
	private readonly StrategyParam<int> _trailingPoints;
	private readonly StrategyParam<int> _triggerPoints;

	private decimal? _bestBidPrice;
	private decimal? _bestAskPrice;
	private decimal? _activeStopPrice;
	private bool _trailingEnabled;
	private decimal _trailingDistance;
	private decimal _triggerDistance;

	/// <summary>
	/// Trailing stop distance expressed in price steps.
	/// </summary>
	public int TrailingPoints
	{
		get => _trailingPoints.Value;
		set => _trailingPoints.Value = value;
	}

	/// <summary>
	/// Profit distance that activates the trailing stop mechanism.
	/// </summary>
	public int TriggerPoints
	{
		get => _triggerPoints.Value;
		set => _triggerPoints.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public TrailingStopManagerStrategy()
	{
		_trailingPoints = Param(nameof(TrailingPoints), 1000)
			.SetGreaterThanZero()
			.SetDisplay("Trailing Points", "Distance of the trailing stop in price steps", "Trailing Management")
			.SetCanOptimize(true)
			.SetOptimize(100, 5000, 100);

		_triggerPoints = Param(nameof(TriggerPoints), 1500)
			.SetGreaterThanZero()
			.SetDisplay("Trigger Points", "Profit in price steps required to activate the trailing stop", "Trailing Management")
			.SetCanOptimize(true)
			.SetOptimize(100, 7500, 100);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		ResetTrailingState();
		_bestBidPrice = null;
		_bestAskPrice = null;
		_trailingDistance = 0m;
		_triggerDistance = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		UpdateDistances();

		// Subscribe to the order book to receive real-time best bid/ask updates.
		SubscribeOrderBook()
			.Bind(ProcessOrderBook)
			.Start();
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		if (Position == 0m)
		{
			// No exposure to manage, reset the trailing context.
			ResetTrailingState();
			return;
		}

		_trailingEnabled = false;
		_activeStopPrice = null;
		UpdateDistances();
	}

	private void ProcessOrderBook(QuoteChangeMessage depth)
	{
		var bestBid = depth.GetBestBid()?.Price;
		if (bestBid.HasValue)
			_bestBidPrice = bestBid.Value;

		var bestAsk = depth.GetBestAsk()?.Price;
		if (bestAsk.HasValue)
			_bestAskPrice = bestAsk.Value;

		if (Position > 0m)
		{
			ProcessLongPosition();
		}
		else if (Position < 0m)
		{
			ProcessShortPosition();
		}
		else
		{
			ResetTrailingState();
		}
	}

	private void ProcessLongPosition()
	{
		if (!_bestBidPrice.HasValue)
			return;

		var entryPrice = PositionPrice;
		if (entryPrice <= 0m)
			return;

		// Calculate the floating profit using the best bid price.
		var profit = _bestBidPrice.Value - entryPrice;
		if (!_trailingEnabled)
		{
			if (profit < _triggerDistance)
				return;

			// Activate the trailing stop once the trigger distance is reached.
_trailingEnabled = true;
_activeStopPrice = _bestBidPrice.Value - _trailingDistance;
LogInfo($"Trailing stop activated for long position at {_activeStopPrice:F4}.");
		}
		else
		{
			// Move the stop only forward to lock in more profit.
			var desiredStop = _bestBidPrice.Value - _trailingDistance;
			if (!_activeStopPrice.HasValue || desiredStop > _activeStopPrice.Value)
				_activeStopPrice = desiredStop;
		}

		if (!_activeStopPrice.HasValue)
			return;

		// Exit when the best bid falls back to the trailing stop level.
		if (_bestBidPrice.Value <= _activeStopPrice.Value && Position > 0m)
		{
			SellMarket(Position);
			LogInfo($"Trailing stop hit for long position at {_bestBidPrice:F4}.");
			ResetTrailingState();
		}
	}

	private void ProcessShortPosition()
	{
		if (!_bestAskPrice.HasValue)
			return;

		var entryPrice = PositionPrice;
		if (entryPrice <= 0m)
			return;

		// Calculate the floating profit using the best ask price.
		var profit = entryPrice - _bestAskPrice.Value;
		if (!_trailingEnabled)
		{
			if (profit < _triggerDistance)
				return;

			// Activate the trailing stop for the short position.
_trailingEnabled = true;
_activeStopPrice = _bestAskPrice.Value + _trailingDistance;
LogInfo($"Trailing stop activated for short position at {_activeStopPrice:F4}.");
		}
		else
		{
			// Move the stop closer to the market as the price drops.
			var desiredStop = _bestAskPrice.Value + _trailingDistance;
			if (!_activeStopPrice.HasValue || desiredStop < _activeStopPrice.Value)
				_activeStopPrice = desiredStop;
		}

		if (!_activeStopPrice.HasValue)
			return;

		// Exit when the best ask climbs back to the trailing stop level.
		if (_bestAskPrice.Value >= _activeStopPrice.Value && Position < 0m)
		{
			BuyMarket(Math.Abs(Position));
			LogInfo($"Trailing stop hit for short position at {_bestAskPrice:F4}.");
			ResetTrailingState();
		}
	}

	private void UpdateDistances()
	{
		var step = Security?.PriceStep ?? 1m;
		if (step <= 0m)
			step = 1m;

		_trailingDistance = step * TrailingPoints;
		_triggerDistance = step * TriggerPoints;
	}

	private void ResetTrailingState()
	{
		_trailingEnabled = false;
		_activeStopPrice = null;
	}
}
