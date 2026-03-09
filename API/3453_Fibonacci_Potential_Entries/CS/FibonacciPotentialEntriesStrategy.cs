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
	private readonly StrategyParam<int> _signalCooldownCandles;

	private decimal _highestHigh;
	private decimal _lowestLow;
	private decimal _prevClose;
	private int _barCount;
	private int _candlesSinceTrade;
	private bool _hasPrevClose;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int RsiPeriod { get => _rsiPeriod.Value; set => _rsiPeriod.Value = value; }
	public int SignalCooldownCandles { get => _signalCooldownCandles.Value; set => _signalCooldownCandles.Value = value; }

	public FibonacciPotentialEntriesStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "RSI period", "Indicators");
		_signalCooldownCandles = Param(nameof(SignalCooldownCandles), 6)
			.SetGreaterThanZero()
			.SetDisplay("Signal Cooldown", "Bars to wait between trades", "Trading");
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_highestHigh = 0;
		_lowestLow = decimal.MaxValue;
		_prevClose = 0;
		_barCount = 0;
		_candlesSinceTrade = SignalCooldownCandles;
		_hasPrevClose = false;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_highestHigh = 0;
		_lowestLow = decimal.MaxValue;
		_prevClose = 0;
		_barCount = 0;
		_candlesSinceTrade = SignalCooldownCandles;
		_hasPrevClose = false;
		var rsi = new RelativeStrengthIndex { Length = RsiPeriod };
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(rsi, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished) return;

		if (_candlesSinceTrade < SignalCooldownCandles)
			_candlesSinceTrade++;

		if (candle.HighPrice > _highestHigh) _highestHigh = candle.HighPrice;
		if (candle.LowPrice < _lowestLow) _lowestLow = candle.LowPrice;
		_barCount++;

		if (_barCount < 20) return;

		var range = _highestHigh - _lowestLow;
		if (range <= 0) return;

		var fib382 = _highestHigh - range * 0.382m;
		var fib618 = _highestHigh - range * 0.618m;
		var close = candle.ClosePrice;
		var crossedIntoBuyZone = _hasPrevClose && _prevClose > fib618 && close <= fib618;
		var crossedIntoSellZone = _hasPrevClose && _prevClose < fib382 && close >= fib382;

		if (crossedIntoBuyZone && rsiValue < 40 && Position <= 0 && _candlesSinceTrade >= SignalCooldownCandles)
		{
			BuyMarket();
			_candlesSinceTrade = 0;
		}
		else if (crossedIntoSellZone && rsiValue > 60 && Position >= 0 && _candlesSinceTrade >= SignalCooldownCandles)
		{
			SellMarket();
			_candlesSinceTrade = 0;
		}

		_prevClose = close;
		_hasPrevClose = true;
	}
}
