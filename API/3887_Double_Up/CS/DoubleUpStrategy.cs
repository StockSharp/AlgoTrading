using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Port of the MetaTrader expert advisor DoubleUp.mq4.
/// Combines CCI and MACD filters with martingale position sizing.
/// </summary>
public class DoubleUpStrategy : Strategy
{
private const decimal MacdScale = 1_000_000m;

private readonly StrategyParam<int> _cciPeriod;
private readonly StrategyParam<decimal> _threshold;
private readonly StrategyParam<int> _macdFastPeriod;
private readonly StrategyParam<int> _macdSlowPeriod;
private readonly StrategyParam<int> _macdSignalPeriod;
private readonly StrategyParam<decimal> _baseVolume;
private readonly StrategyParam<decimal> _initialWait;
private readonly StrategyParam<decimal> _preWait;
private readonly StrategyParam<int> _backShift;
private readonly StrategyParam<DataType> _candleType;

private CommodityChannelIndex _cci = null!;
private MovingAverageConvergenceDivergence _macd = null!;

private decimal? _cciValue;
private decimal? _macdValue;
private DateTimeOffset? _cciTime;
private DateTimeOffset? _macdTime;
private DateTimeOffset? _lastProcessedTime;

private bool _pendingBuy;
private bool _pendingSell;

private int _lossCounter;
private decimal _waitBuffer;

private Sides? _activePositionSide;
private decimal? _entryPrice;
private Sides? _queuedEntrySide;
private decimal _lastKnownPosition;

/// <summary>
/// Period of the Commodity Channel Index indicator.
/// </summary>
public int CciPeriod
{
get => _cciPeriod.Value;
set => _cciPeriod.Value = value;
}

/// <summary>
/// Absolute threshold applied to CCI and scaled MACD values.
/// </summary>
public decimal Threshold
{
get => _threshold.Value;
set => _threshold.Value = value;
}

/// <summary>
/// Fast EMA period for the MACD calculation.
/// </summary>
public int MacdFastPeriod
{
get => _macdFastPeriod.Value;
set => _macdFastPeriod.Value = value;
}

/// <summary>
/// Slow EMA period for the MACD calculation.
/// </summary>
public int MacdSlowPeriod
{
get => _macdSlowPeriod.Value;
set => _macdSlowPeriod.Value = value;
}

/// <summary>
/// Signal EMA period for the MACD calculation.
/// </summary>
public int MacdSignalPeriod
{
get => _macdSignalPeriod.Value;
set => _macdSignalPeriod.Value = value;
}

/// <summary>
/// Base order volume before martingale scaling.
/// </summary>
public decimal BaseVolume
{
get => _baseVolume.Value;
set => _baseVolume.Value = value;
}

/// <summary>
/// Initial value of the wait buffer used by the martingale logic.
/// </summary>
public decimal InitialWait
{
get => _initialWait.Value;
set => _initialWait.Value = value;
}

/// <summary>
/// Minimum number of losing exits before the wait buffer accumulates the loss counter.
/// </summary>
public decimal PreWait
{
get => _preWait.Value;
set => _preWait.Value = value;
}

/// <summary>
/// Historical shift for indicator readings. Only zero is supported in this port.
/// </summary>
public int BackShift
{
get => _backShift.Value;
set => _backShift.Value = value;
}

/// <summary>
/// Candle type requested for indicator calculations.
/// </summary>
public DataType CandleType
{
get => _candleType.Value;
set => _candleType.Value = value;
}

public DoubleUpStrategy()
{
_cciPeriod = Param(nameof(CciPeriod), 8).SetDisplay("CCI Period").SetCanOptimize(true);
_threshold = Param(nameof(Threshold), 230m).SetDisplay("Threshold").SetCanOptimize(true);
_macdFastPeriod = Param(nameof(MacdFastPeriod), 13).SetDisplay("MACD Fast").SetCanOptimize(true);
_macdSlowPeriod = Param(nameof(MacdSlowPeriod), 33).SetDisplay("MACD Slow").SetCanOptimize(true);
_macdSignalPeriod = Param(nameof(MacdSignalPeriod), 2).SetDisplay("MACD Signal").SetCanOptimize(true);
_baseVolume = Param(nameof(BaseVolume), 0.01m).SetDisplay("Base Volume").SetCanOptimize(true);
_initialWait = Param(nameof(InitialWait), 0m).SetDisplay("Initial Wait").SetCanOptimize(true);
_preWait = Param(nameof(PreWait), 2m).SetDisplay("Pre Wait").SetCanOptimize(true);
_backShift = Param(nameof(BackShift), 0).SetDisplay("Back Shift");
_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame()).SetDisplay("Candle Type");
}

/// <inheritdoc />
protected override void OnStarted(DateTimeOffset time)
{
base.OnStarted(time);

if (BackShift != 0)
throw new InvalidOperationException("BackShift parameter other than zero is not supported in DoubleUpStrategy.");

_lossCounter = 0;
_waitBuffer = InitialWait;
_pendingBuy = false;
_pendingSell = false;
_activePositionSide = null;
_entryPrice = null;
_queuedEntrySide = null;
_lastProcessedTime = null;
_cciValue = null;
_macdValue = null;
_cciTime = null;
_macdTime = null;
_lastKnownPosition = Position;

_cci = new CommodityChannelIndex { Length = CciPeriod };
_macd = new MovingAverageConvergenceDivergence
{
ShortPeriod = MacdFastPeriod,
LongPeriod = MacdSlowPeriod,
SignalPeriod = MacdSignalPeriod,
};

var subscription = SubscribeCandles(CandleType);
subscription
.Bind(_cci, ProcessCci)
.BindEx(_macd, ProcessMacd)
.Start();
}

private void ProcessCci(ICandleMessage candle, decimal value)
{
if (candle.State != CandleStates.Finished)
return;

_cciValue = value;
_cciTime = candle.CloseTime;

TryProcessSignal(candle);
}

private void ProcessMacd(ICandleMessage candle, IIndicatorValue indicatorValue)
{
if (candle.State != CandleStates.Finished)
return;

if (!indicatorValue.IsFinal || indicatorValue is not MovingAverageConvergenceDivergenceValue macdData)
return;

if (macdData.Macd is not decimal macdMain)
return;

_macdValue = macdMain;
_macdTime = candle.CloseTime;

TryProcessSignal(candle);
}

private void TryProcessSignal(ICandleMessage candle)
{
if (_cciValue is null || _macdValue is null)
return;

var time = candle.CloseTime;

if (_cciTime != time || _macdTime != time)
return;

if (_lastProcessedTime == time)
return;

if (!IsFormedAndOnlineAndAllowTrading())
return;

_lastProcessedTime = time;

var cci = _cciValue.Value;
var macdScaled = _macdValue.Value * MacdScale;

// Replicate the original flagging logic based on simultaneous extremes.
if (cci > Threshold && macdScaled > Threshold)
{
_pendingSell = true;
_pendingBuy = false;
}
else if (cci < -Threshold && macdScaled < -Threshold)
{
_pendingBuy = true;
_pendingSell = false;
}

// Execute pending entries when the CCI returns toward the midpoint.
if (_pendingBuy && cci < Threshold)
{
RequestEntry(Sides.Buy);
_pendingBuy = false;
}

if (_pendingSell && cci < -Threshold)
{
RequestEntry(Sides.Sell);
_pendingSell = false;
}
}

private void RequestEntry(Sides side)
{
if (side == Sides.Buy)
{
// Close any outstanding short position before opening a new long.
if (Position < 0m)
{
BuyMarket(-Position);
_queuedEntrySide = Sides.Buy;
return;
}

if (Position > 0m)
return;

OpenPosition(Sides.Buy);
}
else
{
// Close any outstanding long position before opening a new short.
if (Position > 0m)
{
SellMarket(Position);
_queuedEntrySide = Sides.Sell;
return;
}

if (Position < 0m)
return;

OpenPosition(Sides.Sell);
}
}

private void OpenPosition(Sides side)
{
var volume = CalculateVolume();

if (volume <= 0m)
return;

if (side == Sides.Buy)
BuyMarket(volume);
else
SellMarket(volume);
}

private decimal CalculateVolume()
{
var volume = BaseVolume;

for (var i = 0; i < _lossCounter; i++)
volume *= 2m;

return volume;
}

private void UpdateMartingaleState(decimal profit)
{
if (profit < 0m)
{
_lossCounter++;

if (_lossCounter >= PreWait)
{
_waitBuffer += _lossCounter;
_lossCounter = 0;
}
}
else if (profit > 0m)
{
var truncated = (int)Math.Truncate(_waitBuffer);
_lossCounter = truncated;
_waitBuffer = 0m;
}
}

/// <inheritdoc />
protected override void OnNewMyTrade(MyTrade trade)
{
var positionBefore = _lastKnownPosition;

base.OnNewMyTrade(trade);

var positionAfter = Position;
_lastKnownPosition = positionAfter;

if (positionAfter > 0m)
{
_activePositionSide = Sides.Buy;
_entryPrice = trade.Trade.Price;
}
else if (positionAfter < 0m)
{
_activePositionSide = Sides.Sell;
_entryPrice = trade.Trade.Price;
}
else
{
if (positionBefore > 0m && _activePositionSide == Sides.Buy && _entryPrice is decimal longEntry)
{
var profit = (trade.Trade.Price - longEntry) * trade.Trade.Volume;
UpdateMartingaleState(profit);
}
else if (positionBefore < 0m && _activePositionSide == Sides.Sell && _entryPrice is decimal shortEntry)
{
var profit = (shortEntry - trade.Trade.Price) * trade.Trade.Volume;
UpdateMartingaleState(profit);
}

_activePositionSide = null;
_entryPrice = null;

if (_queuedEntrySide is { } side)
{
_queuedEntrySide = null;
OpenPosition(side);
}
}
}
}
