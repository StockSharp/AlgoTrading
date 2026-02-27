namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;
using Ecng.Common;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// ABE BE Stoch strategy: Engulfing pattern with Stochastic confirmation.
/// Bullish engulfing + oversold stochastic for long, bearish engulfing + overbought for short.
/// </summary>
public class AbeBeStochStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _stochPeriod;
	private readonly StrategyParam<decimal> _oversold;
	private readonly StrategyParam<decimal> _overbought;

	private readonly List<ICandleMessage> _candles = new();
	private decimal _prevK;
	private bool _hasPrevK;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int StochPeriod { get => _stochPeriod.Value; set => _stochPeriod.Value = value; }
	public decimal Oversold { get => _oversold.Value; set => _oversold.Value = value; }
	public decimal Overbought { get => _overbought.Value; set => _overbought.Value = value; }

	public AbeBeStochStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
		_stochPeriod = Param(nameof(StochPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("Stoch Period", "Stochastic K period", "Indicators");
		_oversold = Param(nameof(Oversold), 30m)
			.SetDisplay("Oversold", "Stochastic oversold level", "Signals");
		_overbought = Param(nameof(Overbought), 70m)
			.SetDisplay("Overbought", "Stochastic overbought level", "Signals");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_candles.Clear();
		_hasPrevK = false;
		var stoch = new StochasticOscillator { K = { Length = StochPeriod }, D = { Length = 3 } };
		var subscription = SubscribeCandles(CandleType);
		subscription.BindEx(stoch, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue stochValue)
	{
		if (candle.State != CandleStates.Finished) return;

		var stochTyped = stochValue as StochasticOscillatorValue;
		if (stochTyped?.K is not decimal kValue) return;

		_candles.Add(candle);
		if (_candles.Count > 5)
			_candles.RemoveAt(0);

		if (_candles.Count >= 2)
		{
			var curr = _candles[^1];
			var prev = _candles[^2];

			// Bullish engulfing: prev bearish, curr bullish, curr body engulfs prev body
			var bullishEngulfing = prev.OpenPrice > prev.ClosePrice
				&& curr.ClosePrice > curr.OpenPrice
				&& curr.OpenPrice <= prev.ClosePrice
				&& curr.ClosePrice >= prev.OpenPrice;

			// Bearish engulfing: prev bullish, curr bearish, curr body engulfs prev body
			var bearishEngulfing = prev.ClosePrice > prev.OpenPrice
				&& curr.OpenPrice > curr.ClosePrice
				&& curr.OpenPrice >= prev.ClosePrice
				&& curr.ClosePrice <= prev.OpenPrice;

			if (bullishEngulfing && kValue < Oversold && Position <= 0)
				BuyMarket();
			else if (bearishEngulfing && kValue > Overbought && Position >= 0)
				SellMarket();
		}

		// Exit on stochastic cross
		if (_hasPrevK)
		{
			if (Position > 0 && _prevK >= Overbought && kValue < Overbought)
				SellMarket();
			else if (Position < 0 && _prevK <= Oversold && kValue > Oversold)
				BuyMarket();
		}

		_prevK = kValue;
		_hasPrevK = true;
	}
}
