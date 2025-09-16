using System;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Alternating long-short strategy that mirrors the original Nevalyashka MQL logic.
/// Opens an initial sell position and flips direction each time the market becomes flat.
/// </summary>
public class NevalyashkaStrategy : Strategy
{
	// User parameters.
	private readonly StrategyParam<int> _lotMultiplier;
	private readonly StrategyParam<int> _stopLossPips;
	private readonly StrategyParam<int> _takeProfitPips;

	// Cached instrument information and current quotes.
	private decimal _pipSize;
	private decimal _bestBid;
	private decimal _bestAsk;

	// Active protective orders attached to the open position.
	private Order _stopOrder;
	private Order _takeOrder;

	// Internal state for managing entries and direction flips.
	private bool _isEntryPending;
	private Sides? _pendingEntrySide;
	private decimal? _pendingEntryPrice;
	private Sides? _currentSide;
	private Sides? _lastCompletedSide;

	/// <summary>
	/// Multiplier for the minimum tradable volume.
	/// </summary>
	public int LotMultiplier
	{
		get => _lotMultiplier.Value;
		set => _lotMultiplier.Value = value;
	}

	/// <summary>
	/// Stop loss distance in pips.
	/// </summary>
	public int StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take profit distance in pips.
	/// </summary>
	public int TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="NevalyashkaStrategy"/> class.
	/// </summary>
	public NevalyashkaStrategy()
	{
		_lotMultiplier = Param(nameof(LotMultiplier), 1)
			.SetGreaterThanZero()
			.SetDisplay("Lot Multiplier", "Multiplier of minimum tradable volume", "General");

		_stopLossPips = Param(nameof(StopLossPips), 50)
			.SetDisplay("Stop Loss (pips)", "Stop loss distance in pips", "Risk");

		_takeProfitPips = Param(nameof(TakeProfitPips), 50)
			.SetDisplay("Take Profit (pips)", "Take profit distance in pips", "Risk");
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		InitializePipSize();

		// Subscribe to top-of-book updates to emulate the tick feed from the MQL version.
		SubscribeOrderBook()
			.Bind(OnOrderBook)
			.Start();
	}

	private void OnOrderBook(QuoteChangeMessage depth)
	{
		// Store the latest best bid and ask prices for placing market orders and protective offsets.
		var bestBid = depth.GetBestBid()?.Price;
		if (bestBid.HasValue && bestBid.Value > 0)
			_bestBid = bestBid.Value;

		var bestAsk = depth.GetBestAsk()?.Price;
		if (bestAsk.HasValue && bestAsk.Value > 0)
			_bestAsk = bestAsk.Value;

		TryOpenNextPosition();
	}

	private void InitializePipSize()
	{
		// Calculate pip size the same way as the MQL expert did for 3/5 digit symbols.
		var step = Security?.PriceStep ?? 0m;
		if (step <= 0)
			step = 1m;

		if (Security?.Decimals is int decimals && (decimals == 3 || decimals == 5))
			_pipSize = step * 10m;
		else
			_pipSize = step;

		if (_pipSize <= 0)
			_pipSize = 1m;
	}

	private void TryOpenNextPosition()
	{
		// Skip if we already have a position or a market order waiting for execution.
		if (Position != 0 || _isEntryPending)
			return;

		// Entry requires valid quotes and connection state.
		if (_bestBid <= 0 || _bestAsk <= 0)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_pipSize <= 0)
			InitializePipSize();

		// Alternate direction: start with sell, then flip on every completed trade.
		var sideToOpen = _lastCompletedSide switch
		{
			Sides.Buy => Sides.Sell,
			Sides.Sell => Sides.Buy,
			_ => Sides.Sell,
		};

		var entryPrice = sideToOpen == Sides.Buy ? _bestAsk : _bestBid;
		if (entryPrice <= 0)
			return;

		var volume = GetOrderVolume();
		if (volume <= 0)
			return;

		CancelProtectionOrders();

		_isEntryPending = true;
		_pendingEntrySide = sideToOpen;
		_pendingEntryPrice = entryPrice;

		if (sideToOpen == Sides.Buy)
		{
			// Buy after a completed sell position.
			BuyMarket(volume);
		}
		else
		{
			// Sell on the very first trade and after a completed buy position.
			SellMarket(volume);
		}
	}

	private decimal GetOrderVolume()
	{
		// Replicate the "Number of minimum lots" input by multiplying the exchange minimum volume.
		var minVolume = Security?.MinVolume ?? Volume;
		if (minVolume <= 0)
			minVolume = Volume > 0 ? Volume : 1m;

		var volume = LotMultiplier * minVolume;

		// Respect the exchange volume step if it is available.
		var step = Security?.VolumeStep;
		if (step is decimal volumeStep && volumeStep > 0)
		{
			var stepsCount = Math.Ceiling(volume / volumeStep);
			volume = stepsCount * volumeStep;
		}

		return volume;
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		if (Position != 0)
		{
			// We are now in the market: clear the pending flag and remember the direction.
			_isEntryPending = false;

			_currentSide = Position > 0 ? Sides.Buy : Sides.Sell;

			// Attach protective orders once the entry is confirmed.
			if (_pendingEntrySide.HasValue && _pendingEntrySide == _currentSide)
			{
				var entryPrice = _pendingEntryPrice ?? (_currentSide == Sides.Buy ? _bestAsk : _bestBid);
				if (entryPrice > 0)
					RegisterProtectionOrders(_currentSide.Value, entryPrice);
			}

			_pendingEntrySide = null;
			_pendingEntryPrice = null;

			return;
		}

		// Position closed: reset state and prepare the next flip.
		_isEntryPending = false;

		if (_currentSide.HasValue)
		{
			_lastCompletedSide = _currentSide;
			_currentSide = null;
		}

		CancelProtectionOrders();

		TryOpenNextPosition();
	}

	private void RegisterProtectionOrders(Sides side, decimal entryPrice)
	{
		CancelProtectionOrders();

		var positionVolume = Math.Abs(Position);
		if (positionVolume <= 0)
			positionVolume = GetOrderVolume();

		// Create stop loss orders if the distance is configured.
		if (StopLossPips > 0)
		{
			var stopOffset = StopLossPips * _pipSize;
			var stopPrice = side == Sides.Buy
				? entryPrice - stopOffset
				: entryPrice + stopOffset;

			if (stopPrice > 0)
			{
				_stopOrder = side == Sides.Buy
					? SellStop(positionVolume, stopPrice)
					: BuyStop(positionVolume, stopPrice);
			}
		}

		// Create take profit orders if the distance is configured.
		if (TakeProfitPips > 0)
		{
			var takeOffset = TakeProfitPips * _pipSize;
			var takePrice = side == Sides.Buy
				? entryPrice + takeOffset
				: entryPrice - takeOffset;

			if (takePrice > 0)
			{
				_takeOrder = side == Sides.Buy
					? SellLimit(positionVolume, takePrice)
					: BuyLimit(positionVolume, takePrice);
			}
		}
	}

	private void CancelProtectionOrders()
	{
		// Cancel any leftover protective orders before placing new ones.
		if (_stopOrder != null && _stopOrder.State == OrderStates.Active)
			CancelOrder(_stopOrder);

		if (_takeOrder != null && _takeOrder.State == OrderStates.Active)
			CancelOrder(_takeOrder);

		_stopOrder = null;
		_takeOrder = null;
	}
}
