namespace StockSharp.Samples.Strategies;

using System;
using Ecng.Common;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// CBC WS RSI strategy: 3 Black Crows / 3 White Soldiers with RSI confirmation.
/// Buys after 3 bullish candles with RSI below 60, sells after 3 bearish with RSI above 40.
/// </summary>
public class CbcWsRsiStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _rsiPeriod;

	private int _bullCount;
	private int _bearCount;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int RsiPeriod { get => _rsiPeriod.Value; set => _rsiPeriod.Value = value; }

	public CbcWsRsiStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "RSI period for confirmation", "Indicators");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_bullCount = 0;
		_bearCount = 0;
		var rsi = new RelativeStrengthIndex { Length = RsiPeriod };
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(rsi, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsi)
	{
		if (candle.State != CandleStates.Finished) return;

		if (candle.ClosePrice > candle.OpenPrice)
		{
			_bullCount++;
			_bearCount = 0;
		}
		else if (candle.ClosePrice < candle.OpenPrice)
		{
			_bearCount++;
			_bullCount = 0;
		}
		else
		{
			_bullCount = 0;
			_bearCount = 0;
		}

		// Exit on RSI extremes
		if (Position > 0 && rsi > 75) SellMarket();
		else if (Position < 0 && rsi < 25) BuyMarket();

		// Entry on pattern
		if (_bullCount >= 3 && rsi < 60 && Position <= 0) BuyMarket();
		else if (_bearCount >= 3 && rsi > 40 && Position >= 0) SellMarket();
	}
}
