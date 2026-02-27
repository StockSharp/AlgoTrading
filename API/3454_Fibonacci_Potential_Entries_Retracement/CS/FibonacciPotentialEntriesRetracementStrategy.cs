namespace StockSharp.Samples.Strategies;

using System;
using Ecng.Common;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// Fibonacci Retracement Entries strategy: EMA trend with Fibonacci retracement levels.
/// Buys on retracement to 61.8% in uptrend, sells on retracement to 38.2% in downtrend.
/// </summary>
public class FibonacciPotentialEntriesRetracementStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _emaPeriod;
	private readonly StrategyParam<int> _lookback;

	private decimal _high;
	private decimal _low;
	private int _barCount;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int EmaPeriod { get => _emaPeriod.Value; set => _emaPeriod.Value = value; }
	public int Lookback { get => _lookback.Value; set => _lookback.Value = value; }

	public FibonacciPotentialEntriesRetracementStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
		_emaPeriod = Param(nameof(EmaPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("EMA Period", "EMA period", "Indicators");
		_lookback = Param(nameof(Lookback), 50)
			.SetGreaterThanZero()
			.SetDisplay("Lookback", "Lookback for high/low", "General");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_high = 0;
		_low = decimal.MaxValue;
		_barCount = 0;
		var ema = new ExponentialMovingAverage { Length = EmaPeriod };
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ema, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal emaValue)
	{
		if (candle.State != CandleStates.Finished) return;

		if (candle.HighPrice > _high) _high = candle.HighPrice;
		if (candle.LowPrice < _low) _low = candle.LowPrice;
		_barCount++;

		if (_barCount < 20) return;

		var range = _high - _low;
		if (range <= 0) return;

		var close = candle.ClosePrice;
		var fib618 = _high - range * 0.618m;
		var fib382 = _high - range * 0.382m;

		if (close > emaValue && close <= fib618 && Position <= 0)
			BuyMarket();
		else if (close < emaValue && close >= fib382 && Position >= 0)
			SellMarket();
	}
}
