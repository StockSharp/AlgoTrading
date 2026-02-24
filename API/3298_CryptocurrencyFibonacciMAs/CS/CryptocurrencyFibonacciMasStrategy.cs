using System;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Cryptocurrency Fibonacci MAs strategy: EMA 8/13/21 stack alignment.
/// Buys when EMA8 > EMA13 > EMA21 (bullish stack).
/// Sells when EMA8 < EMA13 < EMA21 (bearish stack).
/// </summary>
public class CryptocurrencyFibonacciMasStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public CryptocurrencyFibonacciMasStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var ema8 = new ExponentialMovingAverage { Length = 8 };
		var ema13 = new ExponentialMovingAverage { Length = 13 };
		var ema21 = new ExponentialMovingAverage { Length = 21 };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ema8, ema13, ema21, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema8);
			DrawIndicator(area, ema13);
			DrawIndicator(area, ema21);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal ema8, decimal ema13, decimal ema21)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var bullish = ema8 > ema13 && ema13 > ema21;
		var bearish = ema8 < ema13 && ema13 < ema21;

		if (bullish && Position <= 0)
		{
			BuyMarket();
		}
		else if (bearish && Position >= 0)
		{
			SellMarket();
		}
	}
}
