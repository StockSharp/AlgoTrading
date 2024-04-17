using System;
using StockSharp.Algo;
using StockSharp.Algo.Candles;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace First_strategies
{
	public class OneCandleTrend : Strategy
	{
		private readonly CandleSeries _candleSeries;
		public OneCandleTrend(CandleSeries candleSeries)
		{
			_candleSeries = candleSeries;
		}

		protected override void OnStarted(DateTimeOffset time)
		{
			Connector.CandleProcessing += CandleManager_Processing;
			Connector.SubscribeCandles(_candleSeries);
			base.OnStarted(time);
		}

		private void CandleManager_Processing(CandleSeries candleSeries, ICandleMessage candle)
		{
			if (candle.State != CandleStates.Finished) return;

			if (candle.OpenPrice < candle.ClosePrice && Position <= 0)
			{
				RegisterOrder(this.BuyAtMarket(Volume + Math.Abs(Position)));
			}

			else
			if (candle.OpenPrice > candle.ClosePrice && Position >= 0)
			{
				RegisterOrder(this.SellAtMarket(Volume + Math.Abs(Position)));
			}
		}
	}
}