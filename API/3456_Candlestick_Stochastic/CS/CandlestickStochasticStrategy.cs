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
	private readonly StrategyParam<int> _signalCooldownCandles;

	private ICandleMessage _prevCandle;
	private int _candlesSinceTrade;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int StochPeriod { get => _stochPeriod.Value; set => _stochPeriod.Value = value; }
	public decimal StochLow { get => _stochLow.Value; set => _stochLow.Value = value; }
	public decimal StochHigh { get => _stochHigh.Value; set => _stochHigh.Value = value; }
	public int SignalCooldownCandles { get => _signalCooldownCandles.Value; set => _signalCooldownCandles.Value = value; }

	public CandlestickStochasticStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
		_stochPeriod = Param(nameof(StochPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("Stoch Period", "Stochastic K period", "Indicators");
		_stochLow = Param(nameof(StochLow), 40m)
			.SetDisplay("Stoch Low", "Stochastic oversold level", "Signals");
		_stochHigh = Param(nameof(StochHigh), 60m)
			.SetDisplay("Stoch High", "Stochastic overbought level", "Signals");
		_signalCooldownCandles = Param(nameof(SignalCooldownCandles), 4)
			.SetGreaterThanZero()
			.SetDisplay("Signal Cooldown", "Bars to wait between trades", "Trading");
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevCandle = null;
		_candlesSinceTrade = SignalCooldownCandles;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_prevCandle = null;
		_candlesSinceTrade = SignalCooldownCandles;
		var rsi = new RelativeStrengthIndex { Length = StochPeriod };
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(rsi, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal stochValue)
	{
		if (candle.State != CandleStates.Finished) return;

		if (_candlesSinceTrade < SignalCooldownCandles)
			_candlesSinceTrade++;

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

			if (bullishEngulf && stochValue < StochLow && Position <= 0 && _candlesSinceTrade >= SignalCooldownCandles)
			{
				BuyMarket();
				_candlesSinceTrade = 0;
			}
			else if (bearishEngulf && stochValue > StochHigh && Position >= 0 && _candlesSinceTrade >= SignalCooldownCandles)
			{
				SellMarket();
				_candlesSinceTrade = 0;
			}
		}

		_prevCandle = candle;
	}
}
