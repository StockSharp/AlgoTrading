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
	private readonly StrategyParam<int> _signalCooldownCandles;

	private decimal _high;
	private decimal _low;
	private int _barCount;
	private int _candlesSinceTrade;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int EmaPeriod { get => _emaPeriod.Value; set => _emaPeriod.Value = value; }
	public int Lookback { get => _lookback.Value; set => _lookback.Value = value; }
	public int SignalCooldownCandles { get => _signalCooldownCandles.Value; set => _signalCooldownCandles.Value = value; }

	public FibonacciPotentialEntriesRetracementStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
		_emaPeriod = Param(nameof(EmaPeriod), 50)
			.SetGreaterThanZero()
			.SetDisplay("EMA Period", "EMA period", "Indicators");
		_lookback = Param(nameof(Lookback), 50)
			.SetGreaterThanZero()
			.SetDisplay("Lookback", "Lookback for high/low", "General");
		_signalCooldownCandles = Param(nameof(SignalCooldownCandles), 6)
			.SetGreaterThanZero()
			.SetDisplay("Signal Cooldown", "Bars to wait between trades", "Trading");
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_high = 0;
		_low = decimal.MaxValue;
		_barCount = 0;
		_candlesSinceTrade = SignalCooldownCandles;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_high = 0;
		_low = decimal.MaxValue;
		_barCount = 0;
		_candlesSinceTrade = SignalCooldownCandles;
		var ema = new ExponentialMovingAverage { Length = EmaPeriod };
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ema, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal emaValue)
	{
		if (candle.State != CandleStates.Finished) return;

		if (_candlesSinceTrade < SignalCooldownCandles)
			_candlesSinceTrade++;

		if (candle.HighPrice > _high) _high = candle.HighPrice;
		if (candle.LowPrice < _low) _low = candle.LowPrice;
		_barCount++;

		if (_barCount < 20) return;

		var range = _high - _low;
		if (range <= 0) return;

		var close = candle.ClosePrice;
		var fib618 = _high - range * 0.618m;
		var fib382 = _high - range * 0.382m;
		if (close > emaValue && close <= fib618 && Position <= 0 && _candlesSinceTrade >= SignalCooldownCandles)
		{
			BuyMarket();
			_candlesSinceTrade = 0;
		}
		else if (close < emaValue && close >= fib382 && Position >= 0 && _candlesSinceTrade >= SignalCooldownCandles)
		{
			SellMarket();
			_candlesSinceTrade = 0;
		}
	}
}
