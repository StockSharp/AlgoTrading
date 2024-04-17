﻿using StockSharp.Algo;
using StockSharp.Algo.Candles;
using StockSharp.Algo.Strategies;
using StockSharp.Algo.Strategies.Quoting;
using StockSharp.Messages;

namespace Quoting_strategy
{
	using System;
	using System.Linq;

	public class StairsCountertrend : Strategy
	{
		private readonly Subscription _subscription;
		public StairsCountertrend(CandleSeries candleSeries)
		{
			_subscription = new(candleSeries);
		}

		private int _bullLength;
		private int _bearLength;
		public int Length { get; set; } = 3;
		protected override void OnStarted(DateTimeOffset time)
		{
			// history connector disable filtered market depths for performance reason
			Connector.SupportFilteredMarketDepth = true;

			//Connector.RegisterMarketDepth(Security); // - out of date
			//Connector.SubscribeMarketDepth(Security);

			//Connector.RegisterSecurity(Security); // - out of date
			//Connector.SubscribeLevel1(Security);

			//_candleManager = new CandleManager(Connector);// - out of date
			//_candleManager.WhenCandlesFinished(_candleSeries).Do(CandleManager_Processing).Apply();// - out of date
			//_candleManager.Start(_candleSeries);// - out of date
			Connector.WhenCandlesFinished(_subscription).Do(CandleManager_Processing).Apply(this);
			Connector.Subscribe(_subscription);

			base.OnStarted(time);
		}

		private void CandleManager_Processing(ICandleMessage candle)
		{
			if (candle.State != CandleStates.Finished) return;

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
				ChildStrategies.ToList().ForEach(s => s.Stop());
				var strategy = new MarketQuotingStrategy(Sides.Sell, 1)
				{
					WaitAllTrades = true
				};
				ChildStrategies.Add(strategy);
			}

			else
			if (_bearLength >= Length && Position <= 0)
			{
				ChildStrategies.ToList().ForEach(s => s.Stop());
				var strategy = new MarketQuotingStrategy(Sides.Buy, 1)
				{
					WaitAllTrades = true
				};
				ChildStrategies.Add(strategy);
			}
		}
	}
}