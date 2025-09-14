using System;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that places limit orders at the best bid and ask to capture the spread on FORTS.
/// It ensures orders stay at the first level and replaces them when the order book moves.
/// </summary>
public class HftSpreaderForFortsStrategy : Strategy
{
	private readonly StrategyParam<int> _spreadMultiplier;

	/// <summary>
	/// Required spread in ticks to place both buy and sell orders.
	/// </summary>
	public int SpreadMultiplier
	{
		get => _spreadMultiplier.Value;
		set => _spreadMultiplier.Value = value;
	}

	/// <summary>
	/// Initialize <see cref="HftSpreaderForFortsStrategy"/>.
	/// </summary>
	public HftSpreaderForFortsStrategy()
	{
		_spreadMultiplier = Param(nameof(SpreadMultiplier), 4)
			.SetGreaterThanZero()
			.SetDisplay("Spread Multiplier", "Spread ticks required to place orders", "General")
			.SetCanOptimize(true)
			.SetOptimize(1, 10, 1);

		Volume = 1;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		SubscribeOrderBook()
			.Bind(ProcessOrderBook)
			.Start();
	}

	private void ProcessOrderBook(QuoteChangeMessage depth)
	{
		var bid = depth.GetBestBid();
		var ask = depth.GetBestAsk();

		if (bid is null || ask is null)
			return;

		var step = Security.PriceStep ?? 1m;
		var spread = ask.Price - bid.Price;

		for (var i = ActiveOrders.Count - 1; i >= 0; i--)
		{
			var order = ActiveOrders[i];
			var expectedPrice = order.Side == Sides.Buy ? bid.Price + step : ask.Price - step;

			if (order.Price != expectedPrice)
				CancelOrder(order);
		}

		if (Position != 0)
		{
			if (ActiveOrders.Count == 0)
			{
				var price = Position > 0 ? ask.Price - step : bid.Price + step;
				var volume = Volume + Math.Abs(Position);

				if (Position > 0)
					SellLimit(price, volume);
				else
					BuyLimit(price, volume);
			}

			return;
		}

		if (ActiveOrders.Count == 0 && spread >= SpreadMultiplier * step)
		{
			BuyLimit(bid.Price + step, Volume);
			SellLimit(ask.Price - step, Volume);
		}
	}
}
