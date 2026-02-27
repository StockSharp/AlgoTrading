namespace StockSharp.Samples.Strategies;

using System;
using Ecng.Common;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// One Price SL TP strategy: EMA + candle body size filter.
/// Buys when bullish candle with large body closes above EMA, sells on opposite.
/// </summary>
public class OnePriceSlTpStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _emaPeriod;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int EmaPeriod { get => _emaPeriod.Value; set => _emaPeriod.Value = value; }

	public OnePriceSlTpStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
		_emaPeriod = Param(nameof(EmaPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("EMA Period", "EMA trend filter period", "Indicators");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		var ema = new ExponentialMovingAverage { Length = EmaPeriod };
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ema, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal emaValue)
	{
		if (candle.State != CandleStates.Finished) return;

		var body = Math.Abs(candle.ClosePrice - candle.OpenPrice);
		var range = candle.HighPrice - candle.LowPrice;
		if (range <= 0) return;

		// Strong candle: body > 60% of range
		if (body < range * 0.6m) return;

		if (candle.ClosePrice > candle.OpenPrice && candle.ClosePrice > emaValue && Position <= 0)
			BuyMarket();
		else if (candle.ClosePrice < candle.OpenPrice && candle.ClosePrice < emaValue && Position >= 0)
			SellMarket();
	}
}
