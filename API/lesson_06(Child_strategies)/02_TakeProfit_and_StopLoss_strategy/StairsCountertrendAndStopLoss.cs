using System;
using System.Linq;
using StockSharp.Messages;
using StockSharp.Algo;
using StockSharp.Algo.Candles;
using StockSharp.Algo.Strategies;
using StockSharp.Algo.Strategies.Protective;
using StockSharp.BusinessEntities;

namespace TakeProfit_and_StopLoss_strategy
{
	public class StairsCountertrendAndStopLoss : Strategy
	{
		private readonly Subscription _subscription;
		public StairsCountertrendAndStopLoss(CandleSeries candleSeries)
		{
			_subscription = new(candleSeries);
		}

		private int _bullLength;
		private int _bearLength;
		public int Length { get; set; } = 2;
		protected override void OnStarted(DateTimeOffset time)
		{
			//_candleManager = new CandleManager(Connector);// - out of date
			//_candleManager.WhenCandlesFinished(_candleSeries).Do(CandleManager_Processing).Apply();// - out of date
			//_candleManager.Start(_candleSeries);// - out of date
			Connector.WhenCandlesFinished(_subscription).Do(CandleManager_Processing).Apply(this);
			Connector.Subscribe(_subscription);

			base.OnStarted(time);
		}

		private void CandleManager_Processing(ICandleMessage candle)
		{
			if (candle.OpenPrice < candle.ClosePrice)
			{
				_bullLength++;
				_bearLength = 0;
			}
			else
			if (candle.OpenPrice > candle.ClosePrice)
			{
				_bullLength = 0;
				_bearLength++;
			}

			if (_bullLength >= Length && Position >= 0)
			{
				var order = this.SellAtMarket(Volume + Math.Abs(Position));
				order.WhenNewTrade(Connector).Do(NewOderTrade).Until(() => order.State == OrderStates.Done).Apply(this);
				ChildStrategies.ToList().ForEach(s => s.Stop());
				RegisterOrder(order);
			}

			else
			if (_bearLength >= Length && Position <= 0)
			{
				var order = this.BuyAtMarket(Volume + Math.Abs(Position));
				order.WhenNewTrade(Connector).Do(NewOderTrade).Until(() => order.State == OrderStates.Done).Apply(this);
				ChildStrategies.ToList().ForEach(s => s.Stop());
				RegisterOrder(order);
			}
		}

		protected void NewOderTrade(MyTrade myTrade)
		{
			var stopLoss = new StopLossStrategy(myTrade, 0.2)
			{
				WaitAllTrades = true,
			};
			ChildStrategies.Add(stopLoss);
		}
	}
}