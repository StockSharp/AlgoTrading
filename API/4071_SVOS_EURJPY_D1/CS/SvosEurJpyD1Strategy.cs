using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Daily EURJPY strategy converted from the MetaTrader "SVOS_EURJPY_D1" expert advisor.
/// Uses the Vertical Horizontal Filter to switch between trend and range regimes and
/// applies OSMA or Stochastic signals accordingly while enforcing pattern-based exits.
/// </summary>
public class SvosEurJpyD1Strategy : Strategy
{
	private readonly StrategyParam<decimal> _lotSize;
	private readonly StrategyParam<int> _riskBoost;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<int> _stochKPeriod;
	private readonly StrategyParam<int> _stochDPeriod;
	private readonly StrategyParam<int> _stochSlowing;
	private readonly StrategyParam<int> _stdDevPeriod;
	private readonly StrategyParam<decimal> _stdDevMinimum;
	private readonly StrategyParam<int> _vhfPeriod;
	private readonly StrategyParam<decimal> _vhfThreshold;
	private readonly StrategyParam<int> _maxTrendOrders;
	private readonly StrategyParam<int> _maxRangeOrders;
	private readonly StrategyParam<int> _macdFast;
	private readonly StrategyParam<int> _macdSlow;
	private readonly StrategyParam<int> _macdSignal;
	private readonly StrategyParam<decimal> _dojiDivisor;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _pipSizeOverride;

	private ExponentialMovingAverage _ema5 = null!;
	private ExponentialMovingAverage _ema20 = null!;
	private ExponentialMovingAverage _ema130 = null!;
	private MovingAverageConvergenceDivergenceSignal _macd = null!;
	private StochasticOscillator _stochastic = null!;
	private StandardDeviation _stdDev = null!;
	private VerticalHorizontalFilter _vhf = null!;

	private readonly List<CandleSnapshot> _history = new();
	private readonly List<PositionEntry> _longEntries = new();
	private readonly List<PositionEntry> _shortEntries = new();

	private decimal _pipSize;
	private decimal? _previousEma5;
	private decimal? _previousEma20;
	private decimal? _previousStdDev;
	private decimal? _previousVhf;
	private decimal? _previousStochMain;
	private decimal? _previousStochSignal;
	private decimal? _previousOsma;
	private decimal? _previousPrevOsma;

	/// <summary>
	/// Base lot size used for every new order.
	/// </summary>
	public decimal LotSize
	{
		get => _lotSize.Value;
		set => _lotSize.Value = value;
	}

/// <summary>
/// Multiplier applied to the lot size when the EMA trend filter is bullish or bearish.
/// </summary>
public int RiskBoost
{
	get => _riskBoost.Value;
	set => _riskBoost.Value = value;
}

/// <summary>
/// Take-profit distance expressed in pips.
/// </summary>
public decimal TakeProfitPips
{
	get => _takeProfitPips.Value;
	set => _takeProfitPips.Value = value;
}

/// <summary>
/// Stop-loss distance expressed in pips.
/// </summary>
public decimal StopLossPips
{
	get => _stopLossPips.Value;
	set => _stopLossPips.Value = value;
}

/// <summary>
/// Trailing-stop distance expressed in pips.
/// </summary>
public decimal TrailingStopPips
{
	get => _trailingStopPips.Value;
	set => _trailingStopPips.Value = value;
}

/// <summary>
/// %K length of the stochastic oscillator.
/// </summary>
public int StochKPeriod
{
	get => _stochKPeriod.Value;
	set => _stochKPeriod.Value = value;
}

/// <summary>
/// %D length of the stochastic oscillator.
/// </summary>
public int StochDPeriod
{
	get => _stochDPeriod.Value;
	set => _stochDPeriod.Value = value;
}

/// <summary>
/// Smoothing parameter applied to %K.
/// </summary>
public int StochSlowing
{
	get => _stochSlowing.Value;
	set => _stochSlowing.Value = value;
}

/// <summary>
/// Lookback period for the standard deviation filter.
/// </summary>
public int StdDevPeriod
{
	get => _stdDevPeriod.Value;
	set => _stdDevPeriod.Value = value;
}

/// <summary>
/// Minimal standard deviation required before new trades are allowed.
/// </summary>
public decimal StdDevMinimum
{
	get => _stdDevMinimum.Value;
	set => _stdDevMinimum.Value = value;
}

/// <summary>
/// Period of the Vertical Horizontal Filter.
/// </summary>
public int VhfPeriod
{
	get => _vhfPeriod.Value;
	set => _vhfPeriod.Value = value;
}

/// <summary>
/// Threshold that separates trending and ranging regimes.
/// </summary>
public decimal VhfThreshold
{
	get => _vhfThreshold.Value;
	set => _vhfThreshold.Value = value;
}

/// <summary>
/// Maximum simultaneous orders when the regime is trending.
/// </summary>
public int MaxTrendOrders
{
	get => _maxTrendOrders.Value;
	set => _maxTrendOrders.Value = value;
}

/// <summary>
/// Maximum simultaneous orders when the regime is ranging.
/// </summary>
public int MaxRangeOrders
{
	get => _maxRangeOrders.Value;
	set => _maxRangeOrders.Value = value;
}

/// <summary>
/// Fast EMA length used inside MACD (OSMA) calculations.
/// </summary>
public int MacdFastLength
{
	get => _macdFast.Value;
	set => _macdFast.Value = value;
}

/// <summary>
/// Slow EMA length used inside MACD (OSMA) calculations.
/// </summary>
public int MacdSlowLength
{
	get => _macdSlow.Value;
	set => _macdSlow.Value = value;
}

/// <summary>
/// Signal EMA length used for OSMA computations.
/// </summary>
public int MacdSignalLength
{
	get => _macdSignal.Value;
	set => _macdSignal.Value = value;
}

/// <summary>
/// Divisor used to recognise doji candles.
/// </summary>
public decimal DojiDivisor
{
	get => _dojiDivisor.Value;
	set => _dojiDivisor.Value = value;
}

/// <summary>
/// Candle type employed for signal generation.
/// </summary>
public DataType CandleType
{
	get => _candleType.Value;
	set => _candleType.Value = value;
}

/// <summary>
/// Optional pip size override. Set to zero to auto-detect from <see cref="Security"/>.
/// </summary>
public decimal PipSizeOverride
{
	get => _pipSizeOverride.Value;
	set => _pipSizeOverride.Value = value;
}
/// <summary>
/// Initializes a new instance of the <see cref="SvosEurJpyD1Strategy"/> class.
/// </summary>
public SvosEurJpyD1Strategy()
{
	_lotSize = Param(nameof(LotSize), 0.1m)
	.SetDisplay("Lot Size", "Base lot size applied to each order", "Trading")
	.SetGreaterThanZero();

	_riskBoost = Param(nameof(RiskBoost), 3)
	.SetDisplay("Risk Boost", "Multiplier for lot size when EMA trend aligns", "Trading")
	.SetGreaterThanZero();

	_takeProfitPips = Param(nameof(TakeProfitPips), 350m)
	.SetDisplay("Take Profit (pips)", "Distance to place take-profit orders", "Risk")
	.SetGreaterThanZero();

	_stopLossPips = Param(nameof(StopLossPips), 90m)
	.SetDisplay("Stop Loss (pips)", "Distance to place protective stops", "Risk")
	.SetGreaterThanZero();

	_trailingStopPips = Param(nameof(TrailingStopPips), 150m)
	.SetDisplay("Trailing Stop (pips)", "Trailing distance maintained once profits grow", "Risk")
	.SetGreaterThanZero();

	_stochKPeriod = Param(nameof(StochKPeriod), 8)
	.SetDisplay("Stochastic %K", "Length of the %K line", "Stochastic")
	.SetGreaterThanZero();

	_stochDPeriod = Param(nameof(StochDPeriod), 3)
	.SetDisplay("Stochastic %D", "Length of the %D line", "Stochastic")
	.SetGreaterThanZero();

	_stochSlowing = Param(nameof(StochSlowing), 3)
	.SetDisplay("Stochastic Smoothing", "Slowing factor applied to %K", "Stochastic")
	.SetGreaterThanZero();

	_stdDevPeriod = Param(nameof(StdDevPeriod), 20)
	.SetDisplay("StdDev Period", "Lookback for volatility filter", "Indicators")
	.SetGreaterThanZero();

	_stdDevMinimum = Param(nameof(StdDevMinimum), 0.3m)
	.SetDisplay("StdDev Minimum", "Minimal volatility required for entries", "Indicators")
	.SetGreaterThanZero();

	_vhfPeriod = Param(nameof(VhfPeriod), 20)
	.SetDisplay("VHF Period", "Length of the Vertical Horizontal Filter", "Indicators")
	.SetGreaterThanZero();

	_vhfThreshold = Param(nameof(VhfThreshold), 0.4m)
	.SetDisplay("VHF Threshold", "Value separating trend and range regimes", "Indicators")
	.SetGreaterThanZero();

	_maxTrendOrders = Param(nameof(MaxTrendOrders), 4)
	.SetDisplay("Max Trend Orders", "Cap on concurrent trades during trends", "Risk")
	.SetGreaterThanZero();

	_maxRangeOrders = Param(nameof(MaxRangeOrders), 2)
	.SetDisplay("Max Range Orders", "Cap on concurrent trades during ranges", "Risk")
	.SetGreaterThanZero();

	_macdFast = Param(nameof(MacdFastLength), 10)
	.SetDisplay("MACD Fast", "Fast EMA length for OSMA", "Indicators")
	.SetGreaterThanZero();

	_macdSlow = Param(nameof(MacdSlowLength), 25)
	.SetDisplay("MACD Slow", "Slow EMA length for OSMA", "Indicators")
	.SetGreaterThanZero();

	_macdSignal = Param(nameof(MacdSignalLength), 5)
	.SetDisplay("MACD Signal", "Signal EMA length for OSMA", "Indicators")
	.SetGreaterThanZero();

	_dojiDivisor = Param(nameof(DojiDivisor), 8.5m)
	.SetDisplay("Doji Divisor", "Ratio used to detect doji candles", "Patterns")
	.SetGreaterThanZero();

	_candleType = Param(nameof(CandleType), DataType.TimeFrame(TimeSpan.FromDays(1)))
	.SetDisplay("Candle Type", "Primary candle type for analysis", "General");

	_pipSizeOverride = Param(nameof(PipSizeOverride), 0m)
	.SetDisplay("Pip Size Override", "Optional pip size override (0 = auto)", "General");
}

/// <inheritdoc />
public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
{
	return [(Security, CandleType)];
}

/// <inheritdoc />
protected override void OnReseted()
{
	base.OnReseted();

	_history.Clear();
	_longEntries.Clear();
	_shortEntries.Clear();

	_pipSize = 0m;
	_previousEma5 = null;
	_previousEma20 = null;
	_previousStdDev = null;
	_previousVhf = null;
	_previousStochMain = null;
	_previousStochSignal = null;
	_previousOsma = null;
	_previousPrevOsma = null;
}

/// <inheritdoc />
protected override void OnStarted(DateTimeOffset time)
{
	base.OnStarted(time);

	_pipSize = PipSizeOverride > 0m ? PipSizeOverride : CalculatePipSize();
	if (_pipSize <= 0m)
	_pipSize = 0.0001m;

	_ema5 = new ExponentialMovingAverage { Length = 5 };
	_ema20 = new ExponentialMovingAverage { Length = 20 };
	_ema130 = new ExponentialMovingAverage { Length = 130 };
	_macd = new MovingAverageConvergenceDivergenceSignal
	{
		Macd =
		{
			ShortMa = { Length = MacdFastLength },
			LongMa = { Length = MacdSlowLength },
		},
	SignalMa = { Length = MacdSignalLength }
};
_stochastic = new StochasticOscillator
{
	KPeriod = StochKPeriod,
	DPeriod = StochDPeriod,
	Smooth = StochSlowing
};
_stdDev = new StandardDeviation { Length = StdDevPeriod };
_vhf = new VerticalHorizontalFilter { Length = VhfPeriod };

var subscription = SubscribeCandles(CandleType);
subscription
.BindEx(_ema5, _ema20, _ema130, _macd, _stochastic, _stdDev, _vhf, ProcessCandle)
.Start();

var area = CreateChartArea();
if (area != null)
{
	DrawCandles(area, subscription);
	DrawIndicator(area, _ema5);
	DrawIndicator(area, _ema20);
	DrawIndicator(area, _vhf);
	DrawOwnTrades(area);
}
}
private void ProcessCandle(
ICandleMessage candle,
IIndicatorValue ema5Value,
IIndicatorValue ema20Value,
IIndicatorValue ema130Value,
IIndicatorValue macdValue,
IIndicatorValue stochasticValue,
IIndicatorValue stdDevValue,
IIndicatorValue vhfValue)
{
	if (candle.State != CandleStates.Finished)
	return;

	UpdateTrailingStops(candle);
	HandleStopsAndTargets(candle);

	decimal? ema5 = ema5Value.IsFinal ? ema5Value.ToDecimal() : null;
	decimal? ema20 = ema20Value.IsFinal ? ema20Value.ToDecimal() : null;
	decimal? ema130 = ema130Value.IsFinal ? ema130Value.ToDecimal() : null;
	decimal? stdDev = stdDevValue.IsFinal ? stdDevValue.ToDecimal() : null;
	decimal? vhf = vhfValue.IsFinal ? vhfValue.ToDecimal() : null;

	decimal? stochMain = null;
	decimal? stochSignal = null;
	if (stochasticValue.IsFinal)
	{
		var stochTyped = (StochasticOscillatorValue)stochasticValue;
		if (stochTyped.K is decimal k && stochTyped.D is decimal d)
		{
			stochMain = k;
			stochSignal = d;
		}
}

decimal? osmaCurrent = null;
if (macdValue.IsFinal)
{
	var macdTyped = (MovingAverageConvergenceDivergenceSignalValue)macdValue;
	if (macdTyped.Macd is decimal macdLine && macdTyped.Signal is decimal signalLine)
	osmaCurrent = macdLine - signalLine;
}

var prevEma5 = _previousEma5;
var prevEma20 = _previousEma20;
var prevStdDev = _previousStdDev;
var prevVhf = _previousVhf;
var prevStochMain = _previousStochMain;
var prevStochSignal = _previousStochSignal;
var prevOsma = _previousOsma;
var prevPrevOsma = _previousPrevOsma;

if (IsFormedAndOnlineAndAllowTrading() &&
prevEma5.HasValue &&
prevEma20.HasValue &&
prevStdDev.HasValue &&
prevVhf.HasValue &&
prevStochMain.HasValue &&
prevStochSignal.HasValue &&
prevOsma.HasValue &&
prevPrevOsma.HasValue &&
ema5.HasValue &&
ema20.HasValue &&
ema130.HasValue &&
_history.Count >= 3)
{
	var isTrending = prevVhf.Value >= VhfThreshold;

	if (IsBullishEngulfing())
	CloseAllShorts();

	if (IsMorningStar())
	CloseAllShorts();

	if (isTrending && prevOsma.Value > 0m)
	CloseAllShorts();

	if (!isTrending && prevStochMain.Value >= prevStochSignal.Value)
	CloseAllShorts();

	if (IsBearishEngulfing())
	CloseAllLongs();

	if (IsEveningStar())
	CloseAllLongs();

	if (isTrending && prevOsma.Value < 0m)
	CloseAllLongs();

	if (!isTrending && prevStochMain.Value <= prevStochSignal.Value)
	CloseAllLongs();

	var totalOrders = _longEntries.Count + _shortEntries.Count;
	var limitReached = (isTrending && totalOrders >= MaxTrendOrders) || (!isTrending && totalOrders >= MaxRangeOrders);

	if (!limitReached)
	{
		var last = GetHistory(1);
		var allowLong = !IsEveningStar() && !IsDojiCandle() && !IsBearishEngulfing() && last.Close > last.Open && _shortEntries.Count == 0;
		var allowShort = !IsMorningStar() && !IsDojiCandle() && !IsBullishEngulfing() && last.Close < last.Open && _longEntries.Count == 0;

		var longSignal = false;
		var shortSignal = false;

		if (allowLong && prevStdDev.Value > StdDevMinimum)
		{
			if (isTrending && prevOsma.Value > 0m && prevOsma.Value > prevPrevOsma.Value)
			longSignal = true;

			if (!isTrending && prevStochMain.Value > prevStochSignal.Value)
			longSignal = true;
		}

	if (allowShort && prevStdDev.Value > StdDevMinimum)
	{
		if (isTrending && prevOsma.Value < 0m && prevOsma.Value < prevPrevOsma.Value)
		shortSignal = true;

		if (!isTrending && prevStochMain.Value < prevStochSignal.Value)
		shortSignal = true;
	}

if (longSignal)
{
	var volume = LotSize;
	if (ema5.Value > ema20.Value && ema5.Value > prevEma5.Value && ema20.Value > ema130.Value)
	volume = LotSize * RiskBoost;

	OpenLong(volume, candle.ClosePrice);
}

if (shortSignal)
{
	var volume = LotSize;
	if (ema5.Value < ema20.Value && ema5.Value < prevEma5.Value && ema20.Value < ema130.Value)
	volume = LotSize * RiskBoost;

	OpenShort(volume, candle.ClosePrice);
}
}
}

if (ema5.HasValue)
_previousEma5 = ema5.Value;

if (ema20.HasValue)
_previousEma20 = ema20.Value;

if (stdDev.HasValue)
_previousStdDev = stdDev.Value;

if (vhf.HasValue)
_previousVhf = vhf.Value;

if (stochMain.HasValue && stochSignal.HasValue)
{
	_previousStochMain = stochMain.Value;
	_previousStochSignal = stochSignal.Value;
}

if (osmaCurrent.HasValue)
{
	if (prevOsma.HasValue)
	_previousPrevOsma = prevOsma;

	_previousOsma = osmaCurrent.Value;
}

StoreCandle(candle);
}
private void OpenLong(decimal volume, decimal price)
{
	if (volume <= 0m)
	return;

	BuyMarket(volume);

	var stopDistance = StopLossPips * _pipSize;
	var takeDistance = TakeProfitPips * _pipSize;

	var entry = new PositionEntry
	{
		Volume = volume,
		EntryPrice = price,
		Stop = price - stopDistance,
		Take = price + takeDistance
	};

_longEntries.Add(entry);
}

private void OpenShort(decimal volume, decimal price)
{
	if (volume <= 0m)
	return;

	SellMarket(volume);

	var stopDistance = StopLossPips * _pipSize;
	var takeDistance = TakeProfitPips * _pipSize;

	var entry = new PositionEntry
	{
		Volume = volume,
		EntryPrice = price,
		Stop = price + stopDistance,
		Take = price - takeDistance
	};

_shortEntries.Add(entry);
}

private void CloseAllLongs()
{
	if (_longEntries.Count == 0)
	return;

	decimal totalVolume = 0m;
	foreach (var entry in _longEntries)
	totalVolume += entry.Volume;

	if (totalVolume > 0m)
	SellMarket(totalVolume);

	_longEntries.Clear();
}

private void CloseAllShorts()
{
	if (_shortEntries.Count == 0)
	return;

	decimal totalVolume = 0m;
	foreach (var entry in _shortEntries)
	totalVolume += entry.Volume;

	if (totalVolume > 0m)
	BuyMarket(totalVolume);

	_shortEntries.Clear();
}

private void UpdateTrailingStops(ICandleMessage candle)
{
	if (TrailingStopPips <= 0m || _pipSize <= 0m)
	return;

	var trailingDistance = TrailingStopPips * _pipSize;

	foreach (var entry in _longEntries)
	{
		var bestPrice = candle.HighPrice > candle.ClosePrice ? candle.HighPrice : candle.ClosePrice;
		var profit = bestPrice - entry.EntryPrice;
		if (profit > trailingDistance)
		{
			var candidate = bestPrice - trailingDistance;
			if (candidate > entry.Stop)
			entry.Stop = candidate;
		}
}

foreach (var entry in _shortEntries)
{
	var bestPrice = candle.LowPrice < candle.ClosePrice ? candle.LowPrice : candle.ClosePrice;
	var profit = entry.EntryPrice - bestPrice;
	if (profit > trailingDistance)
	{
		var candidate = bestPrice + trailingDistance;
		if (candidate < entry.Stop)
		entry.Stop = candidate;
	}
}
}

private void HandleStopsAndTargets(ICandleMessage candle)
{
	for (var i = _longEntries.Count - 1; i >= 0; i--)
	{
		var entry = _longEntries[i];
		var stopHit = candle.LowPrice <= entry.Stop;
		var takeHit = candle.HighPrice >= entry.Take;

		if (stopHit || takeHit)
		{
			SellMarket(entry.Volume);
			_longEntries.RemoveAt(i);
		}
}

for (var i = _shortEntries.Count - 1; i >= 0; i--)
{
	var entry = _shortEntries[i];
	var stopHit = candle.HighPrice >= entry.Stop;
	var takeHit = candle.LowPrice <= entry.Take;

	if (stopHit || takeHit)
	{
		BuyMarket(entry.Volume);
		_shortEntries.RemoveAt(i);
	}
}
}
private bool IsMorningStar()
{
	if (_history.Count < 3)
	return false;

	var c3 = GetHistory(3);
	var c2 = GetHistory(2);
	var c1 = GetHistory(1);

	var body3 = Body(c3);
	var body2 = Body(c2);
	var body1 = Body(c1);

	if (!(body3 > body2 && body1 > body2))
	return false;

	if (!(c3.Close < c3.Open && c1.Close > c1.Open))
	return false;

	var midpoint = BodyLow(c3) + body3 * 0.5m;
	return c1.Close > midpoint;
}

private bool IsEveningStar()
{
	if (_history.Count < 3)
	return false;

	var c3 = GetHistory(3);
	var c2 = GetHistory(2);
	var c1 = GetHistory(1);

	var body3 = Body(c3);
	var body2 = Body(c2);
	var body1 = Body(c1);

	if (!(body3 > body2 && body1 > body2))
	return false;

	if (!(c3.Close > c3.Open && c1.Close < c1.Open))
	return false;

	var midpoint = BodyHigh(c3) - body3 * 0.5m;
	return c1.Close < midpoint;
}

private bool IsBullishEngulfing()
{
	if (_history.Count < 2)
	return false;

	var c2 = GetHistory(2);
	var c1 = GetHistory(1);

	return c2.Close < c2.Open && c1.Close > c1.Open && Body(c2) < Body(c1);
}

private bool IsBearishEngulfing()
{
	if (_history.Count < 2)
	return false;

	var c2 = GetHistory(2);
	var c1 = GetHistory(1);

	return c2.Close > c2.Open && c1.Close < c1.Open && Body(c2) < Body(c1);
}

private bool IsDojiCandle()
{
	if (_history.Count < 1)
	return false;

	var c1 = GetHistory(1);
	var body = Body(c1);
	var range = c1.High - c1.Low;
	if (range <= 0m)
	return false;

	return body < range / DojiDivisor;
}

private void StoreCandle(ICandleMessage candle)
{
	var snapshot = new CandleSnapshot(candle.OpenPrice, candle.HighPrice, candle.LowPrice, candle.ClosePrice);
	_history.Add(snapshot);

	while (_history.Count > 3)
	_history.RemoveAt(0);
}

private CandleSnapshot GetHistory(int offset)
{
	return _history[_history.Count - offset];
}

private static decimal Body(CandleSnapshot candle)
{
	return Math.Abs(candle.Close - candle.Open);
}

private static decimal BodyLow(CandleSnapshot candle)
{
	return candle.Open < candle.Close ? candle.Open : candle.Close;
}

private static decimal BodyHigh(CandleSnapshot candle)
{
	return candle.Open > candle.Close ? candle.Open : candle.Close;
}

private decimal CalculatePipSize()
{
	var priceStep = Security?.PriceStep ?? 1m;
	if (priceStep <= 0m)
	priceStep = 1m;

	var decimals = GetDecimalPlaces(priceStep);
	var factor = decimals == 3 || decimals == 5 ? 10m : 1m;
	return priceStep * factor;
}

private static int GetDecimalPlaces(decimal value)
{
	value = Math.Abs(value);
	if (value == 0m)
	return 0;

	var bits = decimal.GetBits(value);
	return (bits[3] >> 16) & 0xFF;
}

private sealed class PositionEntry
{
	public decimal Volume { get; set; }
	public decimal EntryPrice { get; set; }
	public decimal Stop { get; set; }
	public decimal Take { get; set; }
}

private readonly struct CandleSnapshot
{
	public CandleSnapshot(decimal open, decimal high, decimal low, decimal close)
	{
		Open = open;
		High = high;
		Low = low;
		Close = close;
	}

public decimal Open { get; }
public decimal High { get; }
public decimal Low { get; }
public decimal Close { get; }
}

private sealed class VerticalHorizontalFilter : LengthIndicator<decimal>
{
	private readonly List<decimal> _closes = new();

	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		if (input is not ICandleMessage candle || candle.State != CandleStates.Finished)
		return new DecimalIndicatorValue(this, default, input.Time);

		_closes.Add(candle.ClosePrice);

		var required = Length + 1;
		if (required < 2)
		required = 2;

		if (_closes.Count > required)
		_closes.RemoveRange(0, _closes.Count - required);

		if (_closes.Count < required)
		return new DecimalIndicatorValue(this, default, input.Time);

		var startIndex = _closes.Count - required;
		var highest = decimal.MinValue;
		var lowest = decimal.MaxValue;

		for (var i = startIndex; i < _closes.Count; i++)
		{
			var price = _closes[i];
			if (price > highest)
			highest = price;
			if (price < lowest)
			lowest = price;
		}

	var numerator = Math.Abs(highest - lowest);

	decimal denominator = 0m;
	for (var i = startIndex + 1; i < _closes.Count; i++)
	{
		var current = _closes[i];
		var prev = _closes[i - 1];
		denominator += Math.Abs(current - prev);
	}

var value = denominator != 0m ? numerator / denominator : 0m;
return new DecimalIndicatorValue(this, value, input.Time);
}

public override void Reset()
{
	base.Reset();
	_closes.Clear();
}
}
}
