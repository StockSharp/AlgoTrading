namespace StockSharp.Samples.Strategies;

using System;
using Ecng.Common;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// Meeting Lines + Stochastic strategy.
/// Buys on bullish meeting lines with low stochastic, sells on bearish meeting lines with high stochastic.
/// </summary>
public class MeetingLinesStochasticStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _stochPeriod;
	private readonly StrategyParam<decimal> _stochLow;
	private readonly StrategyParam<decimal> _stochHigh;

	private ICandleMessage _prevCandle;
	private ICandleMessage _prevPrevCandle;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int StochPeriod { get => _stochPeriod.Value; set => _stochPeriod.Value = value; }
	public decimal StochLow { get => _stochLow.Value; set => _stochLow.Value = value; }
	public decimal StochHigh { get => _stochHigh.Value; set => _stochHigh.Value = value; }

	public MeetingLinesStochasticStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
		_stochPeriod = Param(nameof(StochPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("Stochastic Period", "Stochastic K period", "Indicators");
		_stochLow = Param(nameof(StochLow), 30m)
			.SetDisplay("Stoch Low", "Stochastic oversold level", "Signals");
		_stochHigh = Param(nameof(StochHigh), 70m)
			.SetDisplay("Stoch High", "Stochastic overbought level", "Signals");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_prevCandle = null;
		_prevPrevCandle = null;
		var stoch = new StochasticOscillator { K = { Length = StochPeriod }, D = { Length = 3 } };
		var subscription = SubscribeCandles(CandleType);
		subscription.BindEx(stoch, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue stochValue)
	{
		if (candle.State != CandleStates.Finished) return;

		var stochTyped = stochValue as StochasticOscillatorValue;
		if (stochTyped?.K is not decimal kValue) { UpdateState(candle); return; }

		if (_prevCandle != null && _prevPrevCandle != null)
		{
			var avgBody = (Math.Abs(_prevCandle.ClosePrice - _prevCandle.OpenPrice) +
						   Math.Abs(_prevPrevCandle.ClosePrice - _prevPrevCandle.OpenPrice)) / 2m;

			if (avgBody > 0)
			{
				// Bullish meeting lines: prev bearish, current bullish, closes near
				var prevBearish = _prevCandle.OpenPrice > _prevCandle.ClosePrice &&
								  (_prevCandle.OpenPrice - _prevCandle.ClosePrice) > avgBody * 0.5m;
				var currBullish = candle.ClosePrice > candle.OpenPrice &&
								  (candle.ClosePrice - candle.OpenPrice) > avgBody * 0.5m;
				var closesNear = Math.Abs(candle.ClosePrice - _prevCandle.ClosePrice) < avgBody * 0.3m;

				if (prevBearish && currBullish && closesNear && kValue < StochLow && Position <= 0)
					BuyMarket();

				// Bearish meeting lines: prev bullish, current bearish, closes near
				var prevBullish = _prevCandle.ClosePrice > _prevCandle.OpenPrice &&
								  (_prevCandle.ClosePrice - _prevCandle.OpenPrice) > avgBody * 0.5m;
				var currBearish = candle.OpenPrice > candle.ClosePrice &&
								  (candle.OpenPrice - candle.ClosePrice) > avgBody * 0.5m;
				var closesNear2 = Math.Abs(candle.ClosePrice - _prevCandle.ClosePrice) < avgBody * 0.3m;

				if (prevBullish && currBearish && closesNear2 && kValue > StochHigh && Position >= 0)
					SellMarket();
			}
		}

		UpdateState(candle);
	}

	private void UpdateState(ICandleMessage candle)
	{
		_prevPrevCandle = _prevCandle;
		_prevCandle = candle;
	}
}
