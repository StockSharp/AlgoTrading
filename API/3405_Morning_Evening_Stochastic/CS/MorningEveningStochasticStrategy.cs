namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;
using Ecng.Common;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// Morning/Evening Star pattern strategy with Stochastic confirmation.
/// Buys on morning star + oversold stochastic, sells on evening star + overbought stochastic.
/// </summary>
public class MorningEveningStochasticStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _stochPeriod;
	private readonly StrategyParam<decimal> _oversold;
	private readonly StrategyParam<decimal> _overbought;
	private readonly StrategyParam<int> _signalCooldownCandles;

	private readonly List<ICandleMessage> _candles = new();
	private decimal _prevK;
	private bool _hasPrevK;
	private int _candlesSinceTrade;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int StochPeriod { get => _stochPeriod.Value; set => _stochPeriod.Value = value; }
	public decimal Oversold { get => _oversold.Value; set => _oversold.Value = value; }
	public decimal Overbought { get => _overbought.Value; set => _overbought.Value = value; }
	public int SignalCooldownCandles { get => _signalCooldownCandles.Value; set => _signalCooldownCandles.Value = value; }

	public MorningEveningStochasticStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
		_stochPeriod = Param(nameof(StochPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("Stoch Period", "Stochastic K period", "Indicators");
		_oversold = Param(nameof(Oversold), 30m)
			.SetDisplay("Oversold", "Stochastic oversold level", "Signals");
		_overbought = Param(nameof(Overbought), 70m)
			.SetDisplay("Overbought", "Stochastic overbought level", "Signals");
		_signalCooldownCandles = Param(nameof(SignalCooldownCandles), 6)
			.SetGreaterThanZero()
			.SetDisplay("Signal Cooldown", "Bars to wait between trades", "Trading");
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_candles.Clear();
		_prevK = 0m;
		_hasPrevK = false;
		_candlesSinceTrade = SignalCooldownCandles;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_candles.Clear();
		_hasPrevK = false;
		_candlesSinceTrade = SignalCooldownCandles;
		var stoch = new StochasticOscillator { K = { Length = StochPeriod }, D = { Length = 3 } };
		var subscription = SubscribeCandles(CandleType);
		subscription.BindEx(stoch, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue stochValue)
	{
		if (candle.State != CandleStates.Finished) return;

		if (_candlesSinceTrade < SignalCooldownCandles)
			_candlesSinceTrade++;

		var stochTyped = stochValue as StochasticOscillatorValue;
		if (stochTyped?.K is not decimal kValue) return;

		_candles.Add(candle);
		if (_candles.Count > 5)
			_candles.RemoveAt(0);

		if (_candles.Count >= 3)
		{
			var c3 = _candles[^1]; // current
			var c2 = _candles[^2]; // middle (star)
			var c1 = _candles[^3]; // first

			var body1 = Math.Abs(c1.ClosePrice - c1.OpenPrice);
			var body2 = Math.Abs(c2.ClosePrice - c2.OpenPrice);
			var body3 = Math.Abs(c3.ClosePrice - c3.OpenPrice);

			// Morning Star: bearish + small body + bullish, close above midpoint of first
			var isMorningStar = c1.OpenPrice > c1.ClosePrice  // first bearish
				&& body2 < body1 * 0.5m                       // small middle body
				&& c3.ClosePrice > c3.OpenPrice                // third bullish
				&& c3.ClosePrice > (c1.OpenPrice + c1.ClosePrice) / 2m;

			// Evening Star: bullish + small body + bearish, close below midpoint of first
			var isEveningStar = c1.ClosePrice > c1.OpenPrice   // first bullish
				&& body2 < body1 * 0.5m                        // small middle body
				&& c3.OpenPrice > c3.ClosePrice                // third bearish
				&& c3.ClosePrice < (c1.OpenPrice + c1.ClosePrice) / 2m;

			if (isMorningStar && kValue < Oversold && Position <= 0 && _candlesSinceTrade >= SignalCooldownCandles)
			{
				BuyMarket();
				_candlesSinceTrade = 0;
			}
			else if (isEveningStar && kValue > Overbought && Position >= 0 && _candlesSinceTrade >= SignalCooldownCandles)
			{
				SellMarket();
				_candlesSinceTrade = 0;
			}
		}

		// Exit on stochastic cross
		if (_hasPrevK)
		{
			if (Position > 0 && _prevK >= Overbought && kValue < Overbought && _candlesSinceTrade >= SignalCooldownCandles)
			{
				SellMarket();
				_candlesSinceTrade = 0;
			}
			else if (Position < 0 && _prevK <= Oversold && kValue > Oversold && _candlesSinceTrade >= SignalCooldownCandles)
			{
				BuyMarket();
				_candlesSinceTrade = 0;
			}
		}

		_prevK = kValue;
		_hasPrevK = true;
	}
}
