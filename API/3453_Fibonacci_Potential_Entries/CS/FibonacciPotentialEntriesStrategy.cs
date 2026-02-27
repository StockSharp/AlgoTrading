namespace StockSharp.Samples.Strategies;

using System;
using Ecng.Common;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// Fibonacci Potential Entries strategy: Two-candle reversal with RSI filter.
/// Uses price swing highs/lows as fibonacci reference points.
/// </summary>
public class FibonacciPotentialEntriesStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _rsiPeriod;

	private decimal _highestHigh;
	private decimal _lowestLow;
	private int _barCount;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int RsiPeriod { get => _rsiPeriod.Value; set => _rsiPeriod.Value = value; }

	public FibonacciPotentialEntriesStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "RSI period", "Indicators");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_highestHigh = 0;
		_lowestLow = decimal.MaxValue;
		_barCount = 0;
		var rsi = new RelativeStrengthIndex { Length = RsiPeriod };
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(rsi, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished) return;

		if (candle.HighPrice > _highestHigh) _highestHigh = candle.HighPrice;
		if (candle.LowPrice < _lowestLow) _lowestLow = candle.LowPrice;
		_barCount++;

		if (_barCount < 20) return;

		var range = _highestHigh - _lowestLow;
		if (range <= 0) return;

		var fib382 = _highestHigh - range * 0.382m;
		var fib618 = _highestHigh - range * 0.618m;
		var close = candle.ClosePrice;

		if (close <= fib618 && rsiValue < 40 && Position <= 0)
			BuyMarket();
		else if (close >= fib382 && rsiValue > 60 && Position >= 0)
			SellMarket();
	}
}
