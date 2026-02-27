namespace StockSharp.Samples.Strategies;

using System;
using Ecng.Common;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// DreamBot strategy: Force Index momentum with EMA trend filter.
/// Buys when Force Index positive and close above EMA, sells when negative and below EMA.
/// </summary>
public class DreamBotStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _emaPeriod;

	private decimal _prevClose;
	private bool _hasPrevClose;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int EmaPeriod { get => _emaPeriod.Value; set => _emaPeriod.Value = value; }

	public DreamBotStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
		_emaPeriod = Param(nameof(EmaPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("EMA Period", "EMA trend filter period", "Indicators");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_hasPrevClose = false;
		var ema = new ExponentialMovingAverage { Length = EmaPeriod };
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ema, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal emaValue)
	{
		if (candle.State != CandleStates.Finished) return;

		var close = candle.ClosePrice;
		var volume = candle.TotalVolume;

		if (_hasPrevClose && volume > 0)
		{
			// Simple force index: (close - prevClose) * volume
			var forceIndex = (close - _prevClose) * volume;

			if (forceIndex > 0 && close > emaValue && Position <= 0)
				BuyMarket();
			else if (forceIndex < 0 && close < emaValue && Position >= 0)
				SellMarket();
		}

		_prevClose = close;
		_hasPrevClose = true;
	}
}
