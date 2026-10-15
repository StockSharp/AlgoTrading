namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;
using Ecng.Common;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// Engulfing MFI Confirmation strategy: Engulfing pattern with MFI filter.
/// Bullish engulfing + oversold MFI for long, bearish engulfing + overbought MFI for short.
/// </summary>
public class EngulfingMfiConfirmationStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _mfiPeriod;
	private readonly StrategyParam<decimal> _oversold;
	private readonly StrategyParam<decimal> _overbought;
	private readonly StrategyParam<int> _signalCooldownCandles;

	private readonly List<ICandleMessage> _candles = new();
	private decimal _prevMfi;
	private bool _hasPrevMfi;
	private int _candlesSinceTrade;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int MfiPeriod { get => _mfiPeriod.Value; set => _mfiPeriod.Value = value; }
	public decimal Oversold { get => _oversold.Value; set => _oversold.Value = value; }
	public decimal Overbought { get => _overbought.Value; set => _overbought.Value = value; }
	public int SignalCooldownCandles { get => _signalCooldownCandles.Value; set => _signalCooldownCandles.Value = value; }

	public EngulfingMfiConfirmationStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
		_mfiPeriod = Param(nameof(MfiPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("MFI Period", "Money Flow Index period", "Indicators");
		_oversold = Param(nameof(Oversold), 30m)
			.SetDisplay("Oversold", "MFI oversold level", "Signals");
		_overbought = Param(nameof(Overbought), 70m)
			.SetDisplay("Overbought", "MFI overbought level", "Signals");
		_signalCooldownCandles = Param(nameof(SignalCooldownCandles), 6)
			.SetGreaterThanZero()
			.SetDisplay("Signal Cooldown", "Bars to wait between trades", "Trading");
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_candles.Clear();
		_prevMfi = 0m;
		_hasPrevMfi = false;
		_candlesSinceTrade = SignalCooldownCandles;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_candles.Clear();
		_hasPrevMfi = false;
		_candlesSinceTrade = SignalCooldownCandles;
		var mfi = new MoneyFlowIndex { Length = MfiPeriod };
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(mfi, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal mfiValue)
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

			if (curr is null || prev is null)
				return;

			var bullishEngulfing = prev.OpenPrice > prev.ClosePrice
				&& curr.ClosePrice > curr.OpenPrice
				&& curr.OpenPrice <= prev.ClosePrice
				&& curr.ClosePrice >= prev.OpenPrice;

			var bearishEngulfing = prev.ClosePrice > prev.OpenPrice
				&& curr.OpenPrice > curr.ClosePrice
				&& curr.OpenPrice >= prev.ClosePrice
				&& curr.ClosePrice <= prev.OpenPrice;

			if (bullishEngulfing && mfiValue < Oversold && Position <= 0 && _candlesSinceTrade >= SignalCooldownCandles)
			{
				BuyMarket();
				_candlesSinceTrade = 0;
			}
			else if (bearishEngulfing && mfiValue > Overbought && Position >= 0 && _candlesSinceTrade >= SignalCooldownCandles)
			{
				SellMarket();
				_candlesSinceTrade = 0;
			}
		}

		// Exit on MFI crossing
		if (_hasPrevMfi)
		{
			if (Position > 0 && _prevMfi >= Overbought && mfiValue < Overbought && _candlesSinceTrade >= SignalCooldownCandles)
			{
				SellMarket();
				_candlesSinceTrade = 0;
			}
			else if (Position < 0 && _prevMfi <= Oversold && mfiValue > Oversold && _candlesSinceTrade >= SignalCooldownCandles)
			{
				BuyMarket();
				_candlesSinceTrade = 0;
			}
		}

		_prevMfi = mfiValue;
		_hasPrevMfi = true;
	}
}
