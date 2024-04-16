using System;
using StockSharp.Algo;
using StockSharp.Algo.Candles;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace First_strategies
{
	public class StairsCountertrend : Strategy
	{
        private readonly CandleSeries _candleSeries;
		public StairsCountertrend(CandleSeries candleSeries)
		{
			_candleSeries = candleSeries;
		}

		private int _bullLength;
		private int _bearLength;
		private int Length { get; set; } = 3;
		protected override void OnStarted(DateTimeOffset time)
		{
            //_candleManager = new CandleManager(Connector);// - out of date
            //_candleManager.WhenCandlesFinished(_candleSeries).Do(CandleManager_Processing).Apply();// - out of date
            //_candleManager.Start(_candleSeries);// - out of date
			Connector.CandleProcessing += CandleManager_Processing;
            Connector.SubscribeCandles(_candleSeries);
			base.OnStarted(time);
		}

		private void CandleManager_Processing(CandleSeries candleSeries, ICandleMessage candle)
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
				RegisterOrder(this.SellAtMarket(Volume + Math.Abs(Position)));
			}

			else
			if (_bearLength >= Length && Position <= 0)
			{
				RegisterOrder(this.BuyAtMarket(Volume + Math.Abs(Position)));
			}
		}
	}
}