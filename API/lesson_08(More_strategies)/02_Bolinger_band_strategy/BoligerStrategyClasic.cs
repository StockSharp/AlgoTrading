namespace bolinger_band_strategy
{
	using System;

	using StockSharp.Algo;
	using StockSharp.Algo.Candles;
	using StockSharp.Algo.Indicators;
	using StockSharp.Algo.Strategies;
	using StockSharp.Algo.Testing;
	using StockSharp.Messages;

	internal class BoligerStrategyClasic : Strategy
	{
		private readonly Subscription _subscription;

		public BollingerBands BollingerBands { get; set; }
		public BoligerStrategyClasic(CandleSeries series)
		{
			_subscription = new(series);
		}
		protected override void OnStarted(DateTimeOffset time)
		{
			//_candleManager = new CandleManager(Connector);// - out of date
			//_candleManager.WhenCandlesFinished(_candleSeries).Do(CandleManager_Processing).Apply();// - out of date
			//_candleManager.Start(_candleSeries);// - out of date
			Connector.WhenCandlesFinished(_subscription).Do(ProcessCandle).Apply(this);
			Connector.Subscribe(_subscription);
			base.OnStarted(time);
		}
		private bool IsRealTime(ICandleMessage candle)
		{
			return (Connector.CurrentTime - candle.CloseTime).TotalSeconds < 10;
		}

		private bool IsHistoryEmulationConnector => Connector is HistoryEmulationConnector;

		private void ProcessCandle(ICandleMessage candle)
		{
			BollingerBands.Process(candle);

			if (!BollingerBands.IsFormed) return;
			if (!IsHistoryEmulationConnector && !IsRealTime(candle)) return;

			if (candle.ClosePrice >= BollingerBands.UpBand.GetCurrentValue() && Position >= 0)
			{
				RegisterOrder(this.SellAtMarket(Volume + Math.Abs(Position)));
			}

			else
			if (candle.ClosePrice <= BollingerBands.LowBand.GetCurrentValue() && Position <= 0)
			{
				RegisterOrder(this.BuyAtMarket(Volume + Math.Abs(Position)));
			}
		}
	}
}