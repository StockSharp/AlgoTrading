namespace bolinger_band_strategy
{
	using System;

	using StockSharp.Algo;
	using StockSharp.Algo.Candles;
	using StockSharp.Algo.Indicators;
	using StockSharp.Algo.Strategies;
	using StockSharp.Algo.Testing;
	using StockSharp.Messages;

	internal class BoligerStrategyLowBand : Strategy
	{
		private readonly Subscription _subscription;

		public BollingerBands BollingerBands { get; set; }
		public BoligerStrategyLowBand(CandleSeries series)
		{
			_subscription = new(series);
		}

		protected override void OnStarted(DateTimeOffset time)
		{
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

			if (candle.ClosePrice <= BollingerBands.LowBand.GetCurrentValue() && Position == 0)
			{
				RegisterOrder(this.SellAtMarket(Volume));
			}

			else if (candle.ClosePrice >= BollingerBands.MovingAverage.GetCurrentValue() && Position < 0)
			{
				RegisterOrder(this.BuyAtMarket(Math.Abs(Position)));
			}
		}
	}
}