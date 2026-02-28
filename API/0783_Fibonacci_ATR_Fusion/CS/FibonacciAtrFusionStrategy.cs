namespace StockSharp.Samples.Strategies;

using System;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// Fibonacci ATR Fusion Strategy.
/// Integrates weighted buying pressure ratios and ATR thresholds.
/// </summary>
public class FibonacciAtrFusionStrategy : Strategy
{
	private readonly StrategyParam<decimal> _longEntryThreshold;
	private readonly StrategyParam<decimal> _shortEntryThreshold;
	private readonly StrategyParam<decimal> _longExitThreshold;
	private readonly StrategyParam<decimal> _shortExitThreshold;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevWeighted;
	private decimal _prevClose;
	private bool _hasPrev;

	// Manual running averages for buying pressure and ATR at Fibonacci periods
	private decimal _bp8Sum, _bp13Sum, _bp21Sum, _bp34Sum, _bp55Sum;
	private decimal _atr8, _atr13, _atr21, _atr34, _atr55;
	private int _candleCount;

	public decimal LongEntryThreshold
	{
		get => _longEntryThreshold.Value;
		set => _longEntryThreshold.Value = value;
	}

	public decimal ShortEntryThreshold
	{
		get => _shortEntryThreshold.Value;
		set => _shortEntryThreshold.Value = value;
	}

	public decimal LongExitThreshold
	{
		get => _longExitThreshold.Value;
		set => _longExitThreshold.Value = value;
	}

	public decimal ShortExitThreshold
	{
		get => _shortExitThreshold.Value;
		set => _shortExitThreshold.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public FibonacciAtrFusionStrategy()
	{
		_longEntryThreshold = Param(nameof(LongEntryThreshold), 58m)
			.SetDisplay("Long Entry", "Threshold to enter long", "General");

		_shortEntryThreshold = Param(nameof(ShortEntryThreshold), 42m)
			.SetDisplay("Short Entry", "Threshold to enter short", "General");

		_longExitThreshold = Param(nameof(LongExitThreshold), 42m)
			.SetDisplay("Long Exit", "Threshold to exit long", "General");

		_shortExitThreshold = Param(nameof(ShortExitThreshold), 58m)
			.SetDisplay("Short Exit", "Threshold to exit short", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevWeighted = 50;
		_prevClose = 0;
		_hasPrev = false;
		_candleCount = 0;

		var atr = new AverageTrueRange { Length = 14 };
		var ema = new ExponentialMovingAverage { Length = 21 };

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(atr, ema, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal atrValue, decimal emaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_candleCount++;

		if (!_hasPrev)
		{
			_prevClose = candle.ClosePrice;
			_hasPrev = true;
			return;
		}

		// Calculate buying pressure
		var trueLow = Math.Min(candle.LowPrice, _prevClose);
		var trueRange = Math.Max(candle.HighPrice, _prevClose) - trueLow;
		var bp = trueRange > 0 ? (candle.ClosePrice - trueLow) / trueRange * 100m : 50m;

		// Simple exponential smoothing at different Fibonacci periods
		var alpha8 = 2m / (8m + 1m);
		var alpha13 = 2m / (13m + 1m);
		var alpha21 = 2m / (21m + 1m);
		var alpha34 = 2m / (34m + 1m);
		var alpha55 = 2m / (55m + 1m);

		_bp8Sum = _candleCount == 1 ? bp : _bp8Sum * (1 - alpha8) + bp * alpha8;
		_bp13Sum = _candleCount == 1 ? bp : _bp13Sum * (1 - alpha13) + bp * alpha13;
		_bp21Sum = _candleCount == 1 ? bp : _bp21Sum * (1 - alpha21) + bp * alpha21;
		_bp34Sum = _candleCount == 1 ? bp : _bp34Sum * (1 - alpha34) + bp * alpha34;
		_bp55Sum = _candleCount == 1 ? bp : _bp55Sum * (1 - alpha55) + bp * alpha55;

		_prevClose = candle.ClosePrice;

		if (_candleCount < 55)
			return;

		// Weighted composite
		var weighted = (5m * _bp8Sum + 4m * _bp13Sum + 3m * _bp21Sum + 2m * _bp34Sum + _bp55Sum) / 15m;

		// Crossover detection
		var longEntry = _prevWeighted <= LongEntryThreshold && weighted > LongEntryThreshold;
		var shortEntry = _prevWeighted >= ShortEntryThreshold && weighted < ShortEntryThreshold;
		var longExit = _prevWeighted >= LongExitThreshold && weighted < LongExitThreshold;
		var shortExit = _prevWeighted <= ShortExitThreshold && weighted > ShortExitThreshold;

		if (longExit && Position > 0)
		{
			SellMarket();
		}
		else if (shortExit && Position < 0)
		{
			BuyMarket();
		}

		if (longEntry && Position <= 0)
		{
			BuyMarket();
		}
		else if (shortEntry && Position >= 0)
		{
			SellMarket();
		}

		_prevWeighted = weighted;
	}
}
