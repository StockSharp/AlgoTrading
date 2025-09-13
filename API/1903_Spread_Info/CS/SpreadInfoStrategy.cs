using System;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that logs the current spread for the selected security.
/// </summary>
public class SpreadInfoStrategy : Strategy
{
	private decimal _bestBidPrice;
	private decimal _bestAskPrice;

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_bestBidPrice = default;
		_bestAskPrice = default;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Subscribe to order book updates to track best bid and ask prices.
		SubscribeOrderBook()
			.Bind(ProcessOrderBook)
			.Start();
	}

	private void ProcessOrderBook(OrderBook orderBook)
	{
		// Update best bid and ask from the order book.
		_bestBidPrice = orderBook.GetBestBid()?.Price ?? _bestBidPrice;
		_bestAskPrice = orderBook.GetBestAsk()?.Price ?? _bestAskPrice;

		// Wait until both sides are available.
		if (_bestBidPrice == 0 || _bestAskPrice == 0)
			return;

		// Calculate spread and log it.
		var spread = _bestAskPrice - _bestBidPrice;
		LogInfo($"Spread: {spread}");
	}
}
