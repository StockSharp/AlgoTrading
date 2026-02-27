namespace StockSharp.Samples.Strategies;

using System;
using Ecng.Common;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// Ichimoku Price Action strategy: EMA trend with price action confirmation.
/// Buys on bullish candle above EMA, sells on bearish candle below EMA.
/// </summary>
public class IchimokuPriceActionStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _emaPeriod;

	private ICandleMessage _prevCandle;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int EmaPeriod { get => _emaPeriod.Value; set => _emaPeriod.Value = value; }

	public IchimokuPriceActionStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
		_emaPeriod = Param(nameof(EmaPeriod), 26)
			.SetGreaterThanZero()
			.SetDisplay("EMA Period", "EMA trend filter", "Indicators");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_prevCandle = null;
		var ema = new ExponentialMovingAverage { Length = EmaPeriod };
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ema, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal emaValue)
	{
		if (candle.State != CandleStates.Finished) return;

		if (_prevCandle != null)
		{
			var prevBearish = _prevCandle.ClosePrice < _prevCandle.OpenPrice;
			var currBullish = candle.ClosePrice > candle.OpenPrice;
			var prevBullish = _prevCandle.ClosePrice > _prevCandle.OpenPrice;
			var currBearish = candle.ClosePrice < candle.OpenPrice;

			if (prevBearish && currBullish && candle.ClosePrice > emaValue && Position <= 0)
				BuyMarket();
			else if (prevBullish && currBearish && candle.ClosePrice < emaValue && Position >= 0)
				SellMarket();
		}

		_prevCandle = candle;
	}
}
