using System;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that emulates hidden stop-loss and take-profit behaviour.
/// It monitors the best bid/ask prices and closes the position when the hidden levels are crossed.
/// </summary>
public class HiddenStopLossTakeProfitStrategy : Strategy
{
	private readonly StrategyParam<int> _stopLossPoints;
	private readonly StrategyParam<int> _takeProfitPoints;
	private readonly StrategyParam<ReferencePriceOption> _referencePrice;
	private readonly StrategyParam<bool> _drawLines;

	private decimal? _hiddenStopLoss;
	private decimal? _hiddenTakeProfit;
	private decimal? _bestBidPrice;
	private decimal? _bestAskPrice;
	private bool _hasTrackedPosition;
	private bool _completionLogged;
	private bool _pendingInitialization;
	private bool _exitRequested;
	private Sides? _currentSide;

	/// <summary>
	/// Hidden stop-loss distance expressed in points.
	/// </summary>
	public int StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Hidden take-profit distance expressed in points.
	/// </summary>
	public int TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Chooses the reference price used to calculate hidden levels.
	/// </summary>
	public ReferencePriceOption ReferencePrice
	{
		get => _referencePrice.Value;
		set => _referencePrice.Value = value;
	}

	/// <summary>
	/// Enables drawing horizontal lines for the hidden stop-loss and take-profit.
	/// </summary>
	public bool DrawLines
	{
		get => _drawLines.Value;
		set => _drawLines.Value = value;
	}

	/// <summary>
	/// Initializes the strategy parameters.
	/// </summary>
	public HiddenStopLossTakeProfitStrategy()
	{
		_stopLossPoints = Param(nameof(StopLossPoints), 50)
			.SetGreaterThanZero()
			.SetDisplay("Hidden Stop-loss Points", "Distance of the hidden stop-loss in points", "Hidden Protection")
			.SetCanOptimize(true)
			.SetOptimize(10, 200, 10);

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 50)
			.SetGreaterThanZero()
			.SetDisplay("Hidden Take-profit Points", "Distance of the hidden take-profit in points", "Hidden Protection")
			.SetCanOptimize(true)
			.SetOptimize(10, 200, 10);

		_referencePrice = Param(nameof(ReferencePrice), ReferencePriceOption.OpenPrice)
			.SetDisplay("Reference Price", "Base price for hidden level calculation", "Hidden Protection");

		_drawLines = Param(nameof(DrawLines), true)
			.SetDisplay("Draw Lines", "Draw horizontal lines for hidden levels", "Visualisation");
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		ResetHiddenLevels();
		_bestBidPrice = null;
		_bestAskPrice = null;
		_hasTrackedPosition = false;
		_completionLogged = false;
		_pendingInitialization = false;
		_exitRequested = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Subscribe to order book updates to receive best bid/ask prices.
		SubscribeOrderBook()
			.Bind(ProcessOrderBook)
			.Start();

		// Initialize hidden levels for an existing position if present.
		TryInitializeHiddenLevels();
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		if (Position == 0)
		{
			// Position is fully closed; reset state and print completion message once.
			ResetHiddenLevels();
			_exitRequested = false;

			if (_hasTrackedPosition && !_completionLogged)
			{
				LogInfo("Task complete - all tracked positions have been closed.");
				_completionLogged = true;
			}

			return;
		}

		// Position changed but still open; reinitialize hidden levels using current parameters.
		_completionLogged = false;
		_hasTrackedPosition = true;
		_pendingInitialization = true;

		TryInitializeHiddenLevels();
	}

	private void ProcessOrderBook(QuoteChangeMessage orderBook)
	{
		// Cache latest best bid and ask prices.
		var bestBid = orderBook.GetBestBid()?.Price;
		if (bestBid.HasValue)
			_bestBidPrice = bestBid.Value;

		var bestAsk = orderBook.GetBestAsk()?.Price;
		if (bestAsk.HasValue)
			_bestAskPrice = bestAsk.Value;

		// Attempt to initialize hidden levels once liquidity data is available.
		if (_pendingInitialization)
			TryInitializeHiddenLevels();

		// Evaluate whether hidden levels were breached.
		CheckHiddenLevels();
	}

	private void TryInitializeHiddenLevels()
	{
		if (Position == 0)
		{
			_pendingInitialization = false;
			return;
		}

		var referencePrice = GetReferencePrice();
		if (!referencePrice.HasValue)
		{
			// Wait for valid price data when mid-price is required.
			_pendingInitialization = true;
			return;
		}

		var point = GetPointValue();
		if (point <= 0)
			return;

		var side = Position > 0 ? Sides.Buy : Sides.Sell;

		var stopLoss = side == Sides.Buy
			? referencePrice.Value - StopLossPoints * point
			: referencePrice.Value + StopLossPoints * point;

		var takeProfit = side == Sides.Buy
			? referencePrice.Value + TakeProfitPoints * point
			: referencePrice.Value - TakeProfitPoints * point;

		_hiddenStopLoss = RoundToStep(stopLoss);
		_hiddenTakeProfit = RoundToStep(takeProfit);
		_currentSide = side;
		_pendingInitialization = false;

		LogInfo($"Hidden protection initialized for {side} position. Stop={_hiddenStopLoss:0.#####}, Take={_hiddenTakeProfit:0.#####}.");

		if (DrawLines)
			DrawHiddenLevels();
	}

	private void CheckHiddenLevels()
	{
		if (_currentSide is null || _hiddenStopLoss is null || _hiddenTakeProfit is null)
			return;

		if (Position == 0)
			return;

		if (_currentSide == Sides.Buy)
		{
			if (_bestBidPrice is not decimal bid)
				return;

			if (bid >= _hiddenTakeProfit)
			{
				TriggerExit($"Hidden take-profit reached at bid={bid:0.#####}.");
			}
			else if (bid <= _hiddenStopLoss)
			{
				TriggerExit($"Hidden stop-loss reached at bid={bid:0.#####}.");
			}
		}
		else
		{
			if (_bestAskPrice is not decimal ask)
				return;

			if (ask <= _hiddenTakeProfit)
			{
				TriggerExit($"Hidden take-profit reached at ask={ask:0.#####}.");
			}
			else if (ask >= _hiddenStopLoss)
			{
				TriggerExit($"Hidden stop-loss reached at ask={ask:0.#####}.");
			}
		}
	}

	private void TriggerExit(string message)
	{
		if (_exitRequested)
			return;

		_exitRequested = true;
		LogInfo(message);

		// ClosePosition sends market orders in the correct direction to flatten the position.
		ClosePosition();
	}

	private void ResetHiddenLevels()
	{
		_hiddenStopLoss = null;
		_hiddenTakeProfit = null;
		_currentSide = null;
		_pendingInitialization = false;
	}

	private decimal GetPointValue()
	{
		var step = Security?.PriceStep;
		if (step is null || step == 0)
			return 0.0001m;

		return step.Value;
	}

	private decimal? GetReferencePrice()
	{
		var averagePrice = Position.AveragePrice ?? 0m;

		return ReferencePrice switch
		{
			ReferencePriceOption.OpenPrice => averagePrice,
			ReferencePriceOption.MidPrice => GetMidPrice() ?? averagePrice,
			_ => averagePrice,
		};
	}

	private decimal? GetMidPrice()
	{
		if (_bestBidPrice is decimal bid && _bestAskPrice is decimal ask && bid > 0 && ask > 0)
			return (bid + ask) / 2m;

		return null;
	}

	private decimal RoundToStep(decimal price)
	{
		var point = GetPointValue();
		if (point <= 0)
			return price;

		return Math.Round(price / point) * point;
	}

	private void DrawHiddenLevels()
	{
		if (_hiddenStopLoss is not decimal stop || _hiddenTakeProfit is not decimal take)
			return;

		var start = CurrentTime;
		if (start == default)
			start = DateTimeOffset.Now;

		var end = start + TimeSpan.FromMinutes(30);

		// Draw simple horizontal lines to visualise hidden levels.
		DrawLine(start, stop, end, stop);
		DrawLine(start, take, end, take);
	}

	/// <summary>
	/// Defines the reference price used for hidden stop-loss and take-profit.
	/// </summary>
	public enum ReferencePriceOption
	{
		/// <summary>
		/// Use the average entry price of the current position.
		/// </summary>
		OpenPrice,

		/// <summary>
		/// Use the current mid-price calculated from best bid and best ask.
		/// </summary>
		MidPrice,
	}
}
