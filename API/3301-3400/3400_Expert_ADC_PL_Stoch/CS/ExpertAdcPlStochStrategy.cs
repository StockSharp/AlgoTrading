namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;
using Ecng.Common;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// Expert ADC PL Stoch strategy: Dark Cloud Cover and Piercing Line patterns
/// with Stochastic oscillator confirmation for entries and exits.
/// </summary>
public class ExpertAdcPlStochStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _stochPeriod;
	private readonly StrategyParam<decimal> _longThreshold;
	private readonly StrategyParam<decimal> _shortThreshold;
	private readonly StrategyParam<int> _signalCooldownCandles;

	private readonly List<ICandleMessage> _candles = new();
	private decimal _prevSignal;
	private int _candlesSinceTrade;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int StochPeriod { get => _stochPeriod.Value; set => _stochPeriod.Value = value; }
	public decimal LongThreshold { get => _longThreshold.Value; set => _longThreshold.Value = value; }
	public decimal ShortThreshold { get => _shortThreshold.Value; set => _shortThreshold.Value = value; }
	public int SignalCooldownCandles { get => _signalCooldownCandles.Value; set => _signalCooldownCandles.Value = value; }

	public ExpertAdcPlStochStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
		_stochPeriod = Param(nameof(StochPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("Stoch Period", "Stochastic period", "Indicators");
		_longThreshold = Param(nameof(LongThreshold), 30m)
			.SetDisplay("Long Threshold", "Stochastic below this for long", "Signals");
		_shortThreshold = Param(nameof(ShortThreshold), 70m)
			.SetDisplay("Short Threshold", "Stochastic above this for short", "Signals");
		_signalCooldownCandles = Param(nameof(SignalCooldownCandles), 6)
			.SetGreaterThanZero()
			.SetDisplay("Signal Cooldown", "Bars to wait between trades", "Trading");
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_candles.Clear();
		_prevSignal = 0m;
		_candlesSinceTrade = SignalCooldownCandles;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_candles.Clear();
		_candlesSinceTrade = SignalCooldownCandles;
		var stoch = new StochasticOscillator { K = { Length = StochPeriod }, D = { Length = 3 } };
		var subscription = SubscribeCandles(CandleType);
		subscription.BindEx(stoch, ProcessCandle).Start();

		StartProtection(
			takeProfit: new Unit(2, UnitTypes.Percent),
			stopLoss: new Unit(1, UnitTypes.Percent)
		);
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue stochValue)
	{
		if (candle.State != CandleStates.Finished) return;

		if (_candlesSinceTrade < SignalCooldownCandles)
			_candlesSinceTrade++;

		var stochTyped = stochValue as StochasticOscillatorValue;
		if (stochTyped?.K is not decimal kValue) return;

		_candles.Add(candle);
		if (_candles.Count > 10)
			_candles.RemoveAt(0);

		if (_candles.Count >= 2)
		{
			var curr = _candles[^1];
			var prev = _candles[^2];

			// Piercing Line: bearish prev + bullish curr that closes above prev midpoint
			var isPiercing = prev.OpenPrice > prev.ClosePrice
				&& curr.ClosePrice > curr.OpenPrice
				&& curr.OpenPrice < prev.LowPrice
				&& curr.ClosePrice > (prev.OpenPrice + prev.ClosePrice) / 2m;

			// Dark Cloud Cover: bullish prev + bearish curr that closes below prev midpoint
			var isDarkCloud = prev.ClosePrice > prev.OpenPrice
				&& curr.OpenPrice > curr.ClosePrice
				&& curr.OpenPrice > prev.HighPrice
				&& curr.ClosePrice < (prev.OpenPrice + prev.ClosePrice) / 2m;

			if (isPiercing && kValue < LongThreshold && Position == 0 && _candlesSinceTrade >= SignalCooldownCandles)
			{
				BuyMarket();
				_candlesSinceTrade = 0;
			}
			else if (isDarkCloud && kValue > ShortThreshold && Position == 0 && _candlesSinceTrade >= SignalCooldownCandles)
			{
				SellMarket();
				_candlesSinceTrade = 0;
			}
		}

		_prevSignal = kValue;
	}
}
