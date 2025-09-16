namespace StockSharp.Samples.Strategies;

using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// Bleris strategy based on comparisons of consecutive highest highs and lowest lows.
/// </summary>
public class BlerisStrategy : Strategy
{
	private readonly StrategyParam<int> _signalBarSample;
	private readonly StrategyParam<bool> _counterTrend;
	private readonly StrategyParam<decimal> _lots;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _anotherOrderPips;

	private Highest _highest = null!;
	private Lowest _lowest = null!;

	private decimal _prevHigh1;
	private decimal _prevHigh2;
	private decimal _prevLow1;
	private decimal _prevLow2;
	private decimal _lastBuyPrice;
	private decimal _lastSellPrice;

/// <summary>
/// Number of candles for each segment of trend detection.
/// </summary>
	public int SignalBarSample { get => _signalBarSample.Value; set => _signalBarSample.Value = value; }

/// <summary>
/// Reverse trading direction when true.
/// </summary>
	public bool CounterTrend { get => _counterTrend.Value; set => _counterTrend.Value = value; }

/// <summary>
/// Order volume.
/// </summary>
	public decimal Lots { get => _lots.Value; set => _lots.Value = value; }

/// <summary>
/// Candle type used for analysis.
/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

/// <summary>
/// Minimum distance in pips before adding another order of the same type.
/// </summary>
	public int AnotherOrderPips { get => _anotherOrderPips.Value; set => _anotherOrderPips.Value = value; }

	public BlerisStrategy()
{
	_signalBarSample = Param(nameof(SignalBarSample), 24).SetDisplay("Signal bar sample").SetCanOptimize(true);
	_counterTrend = Param(nameof(CounterTrend), false).SetDisplay("Counter trend").SetCanOptimize(true);
	_lots = Param(nameof(Lots), 0.3m).SetDisplay("Lot size").SetCanOptimize(true);
	_candleType = Param(nameof(CandleType), DataType.TimeFrame(TimeSpan.FromDays(1))).SetDisplay("Candle type");
	_anotherOrderPips = Param(nameof(AnotherOrderPips), 600).SetDisplay("Another order pips").SetCanOptimize(true);
}

/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
{
	base.OnStarted(time);

	StartProtection();

	_highest = new Highest { Length = SignalBarSample, CandlePrice = CandlePrice.High };
	_lowest = new Lowest { Length = SignalBarSample, CandlePrice = CandlePrice.Low };

	var subscription = SubscribeCandles(CandleType);
	subscription.Bind(_highest, _lowest, ProcessCandle).Start();
}

	private void ProcessCandle(ICandleMessage candle, decimal highest, decimal lowest)
{
	if (candle.State != CandleStates.Finished)
	return;

	var uptrend = _prevLow2 < _prevLow1 && _prevLow1 < lowest;
	var downtrend = _prevHigh2 > _prevHigh1 && _prevHigh1 > highest;

	_prevHigh2 = _prevHigh1;
	_prevHigh1 = highest;
	_prevLow2 = _prevLow1;
	_prevLow1 = lowest;

	if (uptrend && !downtrend)
{
	if (CounterTrend)
	TrySell(candle.ClosePrice);
	else
	TryBuy(candle.ClosePrice);
}
	else if (downtrend && !uptrend)
{
	if (CounterTrend)
	TryBuy(candle.ClosePrice);
	else
	TrySell(candle.ClosePrice);
}
}

	private void TryBuy(decimal price)
{
	if (AnotherOrderPips > 0 && _lastBuyPrice != 0 &&
	Math.Abs(price - _lastBuyPrice) < AnotherOrderPips * Security.PriceStep)
	return;

	var volume = Lots;
	if (volume <= 0)
	return;

	BuyMarket(volume);
	_lastBuyPrice = price;
}

	private void TrySell(decimal price)
{
	if (AnotherOrderPips > 0 && _lastSellPrice != 0 &&
	Math.Abs(price - _lastSellPrice) < AnotherOrderPips * Security.PriceStep)
	return;

	var volume = Lots;
	if (volume <= 0)
	return;

	SellMarket(volume);
	_lastSellPrice = price;
}
}

