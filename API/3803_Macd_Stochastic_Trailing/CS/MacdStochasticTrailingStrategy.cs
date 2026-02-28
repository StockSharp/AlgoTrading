using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// MACD + Stochastic trailing strategy.
/// Enters long when MACD histogram is positive and Stochastic K crosses above D from oversold.
/// Enters short when MACD histogram is negative and Stochastic K crosses below D from overbought.
/// </summary>
public class MacdStochasticTrailingStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevK;
	private decimal? _prevD;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public MacdStochasticTrailingStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevK = null;
		_prevD = null;

		var macd = new MovingAverageConvergenceDivergenceSignal();
		var stoch = new StochasticOscillator();

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(macd, stoch, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue macdValue, IIndicatorValue stochValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!macdValue.IsFinal || !stochValue.IsFinal)
			return;

		var macdVal = (MovingAverageConvergenceDivergenceSignalValue)macdValue;
		var stochVal = (StochasticOscillatorValue)stochValue;

		if (macdVal.Macd is not decimal macd || macdVal.Signal is not decimal signal)
			return;
		if (stochVal.K is not decimal k || stochVal.D is not decimal d)
			return;

		if (_prevK is not decimal prevK || _prevD is not decimal prevD)
		{
			_prevK = k;
			_prevD = d;
			return;
		}

		var histogram = macd - signal;
		var kCrossUp = prevK <= prevD && k > d;
		var kCrossDown = prevK >= prevD && k < d;

		// Buy: MACD histogram positive + stochastic K crosses above D
		if (histogram > 0 && kCrossUp && k < 50 && Position <= 0)
		{
			if (Position < 0)
				BuyMarket();
			BuyMarket();
		}
		// Sell: MACD histogram negative + stochastic K crosses below D
		else if (histogram < 0 && kCrossDown && k > 50 && Position >= 0)
		{
			if (Position > 0)
				SellMarket();
			SellMarket();
		}

		_prevK = k;
		_prevD = d;
	}
}
