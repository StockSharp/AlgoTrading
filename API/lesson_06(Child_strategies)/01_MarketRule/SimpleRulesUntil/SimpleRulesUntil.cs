using StockSharp.Algo;
using StockSharp.Algo.Strategies;
using StockSharp.Logging;
using StockSharp.Messages;
using System;

namespace MarketRule
{
	public class SimpleRulesUntil : Strategy
	{
		protected override void OnStarted(DateTimeOffset time)
		{
			//Connector.RegisterTrades(Security); // - out of date
			var tickSub = Connector.SubscribeTrades(Security);

			//Connector.RegisterMarketDepth(Security); // - out of date
			var mdSub = Connector.SubscribeMarketDepth(Security);

			var i = 0;
			mdSub.WhenOrderBookReceived(Connector).Do((depth) =>
				{
					i++;
					this.AddInfoLog($"The rule WhenOrderBookReceived BestBid={depth.GetBestBid()}, BestAsk={depth.GetBestAsk()}");
					this.AddInfoLog($"The rule WhenOrderBookReceived i={i}");
				}).Until(() => i >= 10)
				.Apply(this);

			base.OnStarted(time);
		}
	}
}