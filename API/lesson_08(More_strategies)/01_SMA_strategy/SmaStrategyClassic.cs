﻿using System;
using StockSharp.Algo;
using StockSharp.Algo.Candles;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Algo.Testing;
using StockSharp.Messages;

namespace SMA_strategy
{
	public class SmaStrategyClassic : Strategy
	{
		private readonly Subscription _subscription;

		public SimpleMovingAverage LongSma { get; set; }
		public SimpleMovingAverage ShortSma { get; set; }

		public SmaStrategyClassic(CandleSeries candleSeries)
		{
			_subscription = new(candleSeries);
		}
		bool IsRealTime(ICandleMessage candle)
		{
			return (Connector.CurrentTime - candle.CloseTime).TotalSeconds < 10;
		}

		private bool IsHistoryEmulationConnector => Connector is HistoryEmulationConnector;
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
			var longSmaIsFormedPrev = LongSma.IsFormed;
			LongSma.Process(candle);
			ShortSma.Process(candle);

			if (!LongSma.IsFormed || !longSmaIsFormedPrev) return;

			var isShortLessCurrent = ShortSma.GetCurrentValue() < LongSma.GetCurrentValue();
			var isShortLessPrev = ShortSma.GetValue(1) < LongSma.GetValue(1);

			if (isShortLessCurrent == isShortLessPrev) return;
			if (!IsRealTime(candle) && !IsHistoryEmulationConnector) return;

			var volume = Volume + Math.Abs(Position);
			RegisterOrder(isShortLessCurrent ?
				this.SellAtMarket(volume) :
				this.BuyAtMarket(volume));
		}
	}
}