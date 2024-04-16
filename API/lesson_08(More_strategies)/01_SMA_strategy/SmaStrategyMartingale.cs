using System;
using StockSharp.Algo;
using StockSharp.Algo.Candles;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Algo.Testing;
using StockSharp.Messages;

namespace SMA_strategy
{
	internal class SmaStrategyMartingale : Strategy
	{
		private readonly Subscription _subscription;

		public SimpleMovingAverage LongSma { get; set; }
		public SimpleMovingAverage ShortSma { get; set; }

		// out of date; class Connector now provided all necessary candles events
		//private ICandleManager _candleManager;

		public SmaStrategyMartingale(CandleSeries series)
		{
			_subscription = new(series);
		}

		private bool IsRealTime(ICandleMessage candle)
		{
			return (Connector.CurrentTime - candle.CloseTime).TotalSeconds < 10;
		}

		private bool IsHistoryEmulationConnector => Connector is HistoryEmulationConnector;

		protected override void OnStarted(DateTimeOffset time)
		{
			//_candleManager = new CandleManager(Connector);// - out of date
			//_candleManager.WhenCandlesFinished(_candleSeries).Do(CandleManager_Processing).Apply();// - out of date
			//_candleManager.Start(_candleSeries);// - out of date
			Connector.WhenCandlesFinished(_subscription).Do(ProcessCandle).Apply(this);
			Connector.Subscribe(_subscription);
			base.OnStarted(time);
		}

		private void ProcessCandle(ICandleMessage candle)
		{
			var longSmaIsFormedPrev = LongSma.IsFormed;
			LongSma.Process(candle);
			ShortSma.Process(candle);

			if (!LongSma.IsFormed || !longSmaIsFormedPrev) return;
			if (!IsHistoryEmulationConnector && !IsRealTime(candle)) return;

			var isShortLessThenLongCurrent = ShortSma.GetCurrentValue() < LongSma.GetCurrentValue();
			var isShortLessThenLongPrevios = ShortSma.GetValue(1) < LongSma.GetValue(1);


			if (isShortLessThenLongPrevios == isShortLessThenLongCurrent) return;

			CancelActiveOrders();

			var direction = isShortLessThenLongCurrent ? Sides.Sell : Sides.Buy;

			var volume = Volume + Math.Abs(Position);

			var price = Security.ShrinkPrice(ShortSma.GetCurrentValue()); 
			RegisterOrder(this.CreateOrder(direction, price, volume));
		}
	}
}