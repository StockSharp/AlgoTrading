namespace StockSharp.Samples.Strategies;

using System;
using Ecng.Common;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// Simple Engulfing strategy: engulfing candlestick pattern with EMA filter.
/// Buys on bullish engulfing above EMA, sells on bearish engulfing below EMA.
/// </summary>
public class SimpleEngulfingStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _emaPeriod;

	private decimal _prevOpen;
	private decimal _prevClose;
	private bool _hasPrev;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int EmaPeriod { get => _emaPeriod.Value; set => _emaPeriod.Value = value; }

	public SimpleEngulfingStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
		_emaPeriod = Param(nameof(EmaPeriod), 50)
			.SetGreaterThanZero()
			.SetDisplay("EMA Period", "EMA trend filter period", "Indicators");
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevOpen = 0;
		_prevClose = 0;
		_hasPrev = false;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_hasPrev = false;
		var ema = new ExponentialMovingAverage { Length = EmaPeriod };
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ema, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal emaValue)
	{
		if (candle.State != CandleStates.Finished) return;

		if (_hasPrev)
		{
			var prevBearish = _prevClose < _prevOpen;
			var currBullish = candle.ClosePrice > candle.OpenPrice;
			var bullishEngulf = prevBearish && currBullish
				&& candle.OpenPrice <= _prevClose && candle.ClosePrice >= _prevOpen;

			var prevBullish = _prevClose > _prevOpen;
			var currBearish = candle.ClosePrice < candle.OpenPrice;
			var bearishEngulf = prevBullish && currBearish
				&& candle.OpenPrice >= _prevClose && candle.ClosePrice <= _prevOpen;

			if (bullishEngulf && candle.ClosePrice > emaValue && Position <= 0)
				BuyMarket();
			else if (bearishEngulf && candle.ClosePrice < emaValue && Position >= 0)
				SellMarket();
		}

		_prevOpen = candle.OpenPrice;
		_prevClose = candle.ClosePrice;
		_hasPrev = true;
	}
}
