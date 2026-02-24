using System;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Simplified from "Multi Currency Template MT5" MetaTrader expert.
/// Uses candlestick pattern (bearish followed by bullish or vice versa) for entries
/// with simple reversal logic.
/// </summary>
public class MultiCurrencyTemplateMt5Strategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _lookback;

	private ICandleMessage _prevCandle;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int Lookback
	{
		get => _lookback.Value;
		set => _lookback.Value = value;
	}

	public MultiCurrencyTemplateMt5Strategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for signal generation", "General");

		_lookback = Param(nameof(Lookback), 1)
			.SetGreaterThanZero()
			.SetDisplay("Lookback", "Number of prior candles for pattern", "Signals");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevCandle = null;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_prevCandle is null)
		{
			_prevCandle = candle;
			return;
		}

		var volume = Volume;
		if (volume <= 0)
			volume = 1;

		// Buy signal: previous candle was bullish (close > open), current closes below previous open
		// This is a reversal pattern - bearish candle after bullish suggests exhaustion, buy the dip
		var buySignal = candle.ClosePrice < _prevCandle.OpenPrice && _prevCandle.ClosePrice > _prevCandle.OpenPrice;
		// Sell signal: previous candle was bearish (close < open), current closes above previous open
		var sellSignal = candle.ClosePrice > _prevCandle.OpenPrice && _prevCandle.ClosePrice < _prevCandle.OpenPrice;

		if (buySignal)
		{
			if (Position < 0)
				BuyMarket(Math.Abs(Position));

			if (Position <= 0)
				BuyMarket(volume);
		}
		else if (sellSignal)
		{
			if (Position > 0)
				SellMarket(Position);

			if (Position >= 0)
				SellMarket(volume);
		}

		_prevCandle = candle;
	}
}
