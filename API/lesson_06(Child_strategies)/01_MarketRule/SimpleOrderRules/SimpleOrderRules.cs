using StockSharp.Algo;
using StockSharp.Algo.Strategies;
using StockSharp.Logging;
using System;

namespace MarketRule
{
	public class SimpleOrderRules : Strategy
	{
		protected override void OnStarted(DateTimeOffset time)
		{
			//Connector.RegisterTrades(Security); // - out of date
			var sub = Connector.SubscribeTrades(Security);

			sub.WhenTickTradeReceived(Connector).Do(() =>
			{
				var order = this.BuyAtMarket(1);
				var ruleReg = order.WhenRegistered(Connector);
				var ruleRegFailed = order.WhenRegisterFailed(Connector);

				ruleReg
					.Do(() => this.AddInfoLog("Order №1 Registered"))
					.Once()
					.Apply(this)
					.Exclusive(ruleRegFailed);

				ruleRegFailed
					.Do(() => this.AddInfoLog("Order №1 RegisterFailed"))
					.Once()
					.Apply(this)
					.Exclusive(ruleReg);

				RegisterOrder(order);
			}).Once().Apply(this);

			sub.WhenTickTradeReceived(Connector).Do(() =>
			{
				var order = this.BuyAtMarket(10000000);
				var ruleReg = order.WhenRegistered(Connector);
				var ruleRegFailed = order.WhenRegisterFailed(Connector);

				ruleReg
					.Do(() => this.AddInfoLog("Order №2 Registered"))
					.Once()
					.Apply(this)
					.Exclusive(ruleRegFailed);

				ruleRegFailed
					.Do(() => this.AddInfoLog("Order №2 RegisterFailed"))
					.Once()
					.Apply(this)
					.Exclusive(ruleReg);

				RegisterOrder(order);
			}).Once().Apply(this);

			base.OnStarted(time);
		}
	}
}