namespace StockSharp.Samples.Strategies;

using System;
using Ecng.Common;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// Candlestick + Stochastic strategy.
/// Buys on bullish engulfing with low stochastic, sells on bearish engulfing with high stochastic.
/// </summary>
public class CandlestickStochasticStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _stochPeriod;
	private readonly StrategyParam<decimal> _stochLow;
	private readonly StrategyParam<decimal> _stochHigh;

	private ICandleMessage _prevCandle;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int StochPeriod { get => _stochPeriod.Value; set => _stochPeriod.Value = value; }
	public decimal StochLow { get => _stochLow.Value; set => _stochLow.Value = value; }
	public decimal StochHigh { get => _stochHigh.Value; set => _stochHigh.Value = value; }

	public CandlestickStochasticStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
		_stochPeriod = Param(nameof(StochPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("Stoch Period", "Stochastic K period", "Indicators");
		_stochLow = Param(nameof(StochLow), 30m)
			.SetDisplay("Stoch Low", "Stochastic oversold level", "Signals");
		_stochHigh = Param(nameof(StochHigh), 70m)
			.SetDisplay("Stoch High", "Stochastic overbought level", "Signals");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_prevCandle = null;
		var stoch = new StochasticOscillator { K = { Length = StochPeriod }, D = { Length = 3 } };
		var subscription = SubscribeCandles(CandleType);
		subscription.BindEx(stoch, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue stochValue)
	{
		if (candle.State != CandleStates.Finished) return;

		var stochTyped = stochValue as StochasticOscillatorValue;
		if (stochTyped?.K is not decimal kValue) { _prevCandle = candle; return; }

		if (_prevCandle != null)
		{
			var bullishEngulf = _prevCandle.OpenPrice > _prevCandle.ClosePrice &&
								candle.ClosePrice > candle.OpenPrice &&
								candle.ClosePrice > _prevCandle.OpenPrice &&
								candle.OpenPrice < _prevCandle.ClosePrice;

			var bearishEngulf = _prevCandle.ClosePrice > _prevCandle.OpenPrice &&
								candle.OpenPrice > candle.ClosePrice &&
								candle.OpenPrice > _prevCandle.ClosePrice &&
								candle.ClosePrice < _prevCandle.OpenPrice;

			if (bullishEngulf && kValue < StochLow && Position <= 0)
				BuyMarket();
			else if (bearishEngulf && kValue > StochHigh && Position >= 0)
				SellMarket();
		}

		_prevCandle = candle;
	}
}
