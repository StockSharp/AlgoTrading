using StockSharp.Algo;
using StockSharp.Algo.Strategies;
using StockSharp.Logging;
using System;

namespace MarketRule
{
	public class SimpleTradeRules : Strategy
	{
		protected override void OnStarted(DateTimeOffset time)
		{
			//Connector.RegisterTrades(Security); // - out of date
			var sub = Connector.SubscribeTrades(Security);

			sub.WhenTickTradeReceived(Connector).Do(() =>
			{
				new IMarketRule[] { Security.WhenLastTradePriceMore(Connector, 2), Security.WhenLastTradePriceLess(Connector, 2) }
					.Or()
					.Do(() =>
					{
						this.AddInfoLog($"The rule WhenLastTradePriceMore Or WhenLastTradePriceLess candle={Security.LastTick}");
					})
					.Apply(this);
			}).Once().Apply(this);

			base.OnStarted(time);
		}
	}
}