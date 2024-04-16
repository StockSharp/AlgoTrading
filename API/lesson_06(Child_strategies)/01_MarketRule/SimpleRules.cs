using StockSharp.Algo;
using StockSharp.Algo.Strategies;
using StockSharp.Logging;
using StockSharp.Messages;
using System;

namespace MarketRule
{
    public class SimpleRules : Strategy
    {
        protected override void OnStarted(DateTimeOffset time)
        {
            //Connector.RegisterTrades(Security); // - out of date
            var tickSub = Connector.SubscribeTrades(Security);

            //Connector.RegisterMarketDepth(Security); // - out of date
            var mdSub = Connector.SubscribeMarketDepth(Security);

			//-----------------------Create a rule. Method №1-----------------------------------
			mdSub.WhenOrderBookReceived(Connector).Do((depth) =>
            {
                this.AddInfoLog($"The rule WhenOrderBookReceived №1 BestBid={depth.GetBestBid()}, BestAsk={depth.GetBestAsk()}");
            }).Once().Apply(this);
			
            //-----------------------Create a rule. Method №2-----------------------------------
            var whenMarketDepthChanged = mdSub.WhenOrderBookReceived(Connector);
			
            whenMarketDepthChanged.Do((depth) =>
            {
                this.AddInfoLog($"The rule WhenOrderBookReceived №2 BestBid={depth.GetBestBid()}, BestAsk={depth.GetBestAsk()}");
            }).Once().Apply(this);

			//----------------------Rule inside rule-----------------------------------
			mdSub.WhenOrderBookReceived(Connector).Do((depth) =>
            {
                this.AddInfoLog($"The rule WhenOrderBookReceived №3 BestBid={depth.GetBestBid()}, BestAsk={depth.GetBestAsk()}");

				//----------------------not a Once rule-----------------------------------
				mdSub.WhenOrderBookReceived(Connector).Do((depth1) =>
                {
                    this.AddInfoLog($"The rule WhenOrderBookReceived №4 BestBid={depth1.GetBestBid()}, BestAsk={depth1.GetBestAsk()}");
                }).Apply(this);
            }).Once().Apply(this);
			
            base.OnStarted(time);
        }
    }
}