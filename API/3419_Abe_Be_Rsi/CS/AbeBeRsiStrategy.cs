namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;
using Ecng.Common;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// ABE BE RSI strategy: Engulfing pattern with RSI confirmation.
/// Bullish engulfing + oversold RSI for long, bearish engulfing + overbought RSI for short.
/// </summary>
public class AbeBeRsiStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<decimal> _oversold;
	private readonly StrategyParam<decimal> _overbought;
	private readonly StrategyParam<int> _signalCooldownCandles;

	private readonly List<ICandleMessage> _candles = new();
	private int _candlesSinceTrade;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int RsiPeriod { get => _rsiPeriod.Value; set => _rsiPeriod.Value = value; }
	public decimal Oversold { get => _oversold.Value; set => _oversold.Value = value; }
	public decimal Overbought { get => _overbought.Value; set => _overbought.Value = value; }
	public int SignalCooldownCandles { get => _signalCooldownCandles.Value; set => _signalCooldownCandles.Value = value; }

	public AbeBeRsiStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "RSI period", "Indicators");
		_oversold = Param(nameof(Oversold), 40m)
			.SetDisplay("Oversold", "RSI oversold level", "Signals");
		_overbought = Param(nameof(Overbought), 60m)
			.SetDisplay("Overbought", "RSI overbought level", "Signals");
		_signalCooldownCandles = Param(nameof(SignalCooldownCandles), 6)
			.SetGreaterThanZero()
			.SetDisplay("Signal Cooldown", "Bars to wait between trades", "Trading");
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_candles.Clear();
		_candlesSinceTrade = SignalCooldownCandles;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_candles.Clear();
		_candlesSinceTrade = SignalCooldownCandles;
		var rsi = new RelativeStrengthIndex { Length = RsiPeriod };
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(rsi, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished) return;

		if (_candlesSinceTrade < SignalCooldownCandles)
			_candlesSinceTrade++;

		_candles.Add(candle);
		if (_candles.Count > 5)
			_candles.RemoveAt(0);

		if (_candles.Count >= 2)
		{
			var curr = _candles[^1];
			var prev = _candles[^2];

			var bullishEngulfing = prev.OpenPrice > prev.ClosePrice
				&& curr.ClosePrice > curr.OpenPrice
				&& curr.OpenPrice <= prev.ClosePrice
				&& curr.ClosePrice >= prev.OpenPrice;

			var bearishEngulfing = prev.ClosePrice > prev.OpenPrice
				&& curr.OpenPrice > curr.ClosePrice
				&& curr.OpenPrice >= prev.ClosePrice
				&& curr.ClosePrice <= prev.OpenPrice;

			if (bullishEngulfing && rsiValue < Oversold && Position <= 0 && _candlesSinceTrade >= SignalCooldownCandles)
			{
				BuyMarket();
				_candlesSinceTrade = 0;
			}
			else if (bearishEngulfing && rsiValue > Overbought && Position >= 0 && _candlesSinceTrade >= SignalCooldownCandles)
			{
				SellMarket();
				_candlesSinceTrade = 0;
			}
		}
	}
}
