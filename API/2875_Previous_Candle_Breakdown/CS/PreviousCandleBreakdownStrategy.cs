using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Breakout strategy that trades when the current price moves beyond the previous candle high or low with optional MA filter and trailing stops.
/// </summary>
public class PreviousCandleBreakdownStrategy : Strategy
{
private readonly StrategyParam<DataType> _candleType;
private readonly StrategyParam<decimal> _indentPips;
private readonly StrategyParam<int> _fastPeriod;
private readonly StrategyParam<int> _fastShift;
private readonly StrategyParam<int> _slowPeriod;
private readonly StrategyParam<int> _slowShift;
private readonly StrategyParam<MovingAverageTypeEnum> _maType;
private readonly StrategyParam<decimal> _stopLossPips;
private readonly StrategyParam<decimal> _takeProfitPips;
private readonly StrategyParam<decimal> _trailingStopPips;
private readonly StrategyParam<decimal> _trailingStepPips;
private readonly StrategyParam<decimal> _orderVolume;
private readonly StrategyParam<decimal> _riskPercent;
private readonly StrategyParam<int> _maxPositions;
private readonly StrategyParam<decimal> _profitClose;

private LengthIndicator<decimal>? _fastMa;
private LengthIndicator<decimal>? _slowMa;
private ShiftBuffer? _fastBuffer;
private ShiftBuffer? _slowBuffer;

private decimal _pipSize;
private decimal _positionVolume;
private decimal _avgPrice;
private decimal? _breakoutHigh;
private decimal? _breakoutLow;
private DateTimeOffset? _breakoutTime;
private DateTimeOffset? _lastLongEntryTime;
private DateTimeOffset? _lastShortEntryTime;
private DateTimeOffset? _pendingLongEntryTime;
private DateTimeOffset? _pendingShortEntryTime;
private decimal? _longStopPrice;
private decimal? _longTakeProfit;
private decimal? _shortStopPrice;
private decimal? _shortTakeProfit;
private decimal? _longTrailingPrice;
private decimal? _shortTrailingPrice;

/// <summary>
/// Available moving average methods.
/// </summary>
public enum MovingAverageTypeEnum
{
/// <summary>
/// Simple moving average.
/// </summary>
Simple,

/// <summary>
/// Exponential moving average.
/// </summary>
Exponential,

/// <summary>
/// Smoothed moving average.
/// </summary>
Smoothed,

/// <summary>
/// Weighted moving average.
/// </summary>
Weighted
}

/// <summary>
/// Candle type used for reference levels.
/// </summary>
public DataType CandleType
{
get => _candleType.Value;
set => _candleType.Value = value;
}

/// <summary>
/// Offset added to the previous candle extremes in pips.
/// </summary>
public decimal IndentPips
{
get => _indentPips.Value;
set => _indentPips.Value = value;
}

/// <summary>
/// Fast moving average period.
/// </summary>
public int FastPeriod
{
get => _fastPeriod.Value;
set => _fastPeriod.Value = value;
}

/// <summary>
/// Shift applied to the fast moving average.
/// </summary>
public int FastShift
{
get => _fastShift.Value;
set => _fastShift.Value = value;
}

/// <summary>
/// Slow moving average period.
/// </summary>
public int SlowPeriod
{
get => _slowPeriod.Value;
set => _slowPeriod.Value = value;
}

/// <summary>
/// Shift applied to the slow moving average.
/// </summary>
public int SlowShift
{
get => _slowShift.Value;
set => _slowShift.Value = value;
}

/// <summary>
/// Moving average calculation method.
/// </summary>
public MovingAverageTypeEnum MaType
{
get => _maType.Value;
set => _maType.Value = value;
}

/// <summary>
/// Stop loss distance in pips.
/// </summary>
public decimal StopLossPips
{
get => _stopLossPips.Value;
set => _stopLossPips.Value = value;
}

/// <summary>
/// Take profit distance in pips.
/// </summary>
public decimal TakeProfitPips
{
get => _takeProfitPips.Value;
set => _takeProfitPips.Value = value;
}

/// <summary>
/// Trailing stop distance in pips.
/// </summary>
public decimal TrailingStopPips
{
get => _trailingStopPips.Value;
set => _trailingStopPips.Value = value;
}

/// <summary>
/// Minimum price improvement before trailing stop is moved.
/// </summary>
public decimal TrailingStepPips
{
get => _trailingStepPips.Value;
set => _trailingStepPips.Value = value;
}

/// <summary>
/// Fixed order volume. When set to zero the volume is calculated from <see cref="RiskPercent"/> and stop loss.
/// </summary>
public decimal OrderVolume
{
get => _orderVolume.Value;
set => _orderVolume.Value = value;
}

/// <summary>
/// Risk percentage of portfolio equity used when <see cref="OrderVolume"/> is zero.
/// </summary>
public decimal RiskPercent
{
get => _riskPercent.Value;
set => _riskPercent.Value = value;
}

/// <summary>
/// Maximum number of entries per direction.
/// </summary>
public int MaxPositions
{
get => _maxPositions.Value;
set => _maxPositions.Value = value;
}

/// <summary>
/// Floating profit threshold that triggers a full position exit.
/// </summary>
public decimal ProfitClose
{
get => _profitClose.Value;
set => _profitClose.Value = value;
}

/// <summary>
/// Initializes a new instance of the <see cref="PreviousCandleBreakdownStrategy"/>.
/// </summary>
public PreviousCandleBreakdownStrategy()
{
_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
.SetDisplay("Candle Type", "Reference timeframe for breakout levels", "Data");

_indentPips = Param(nameof(IndentPips), 10m)
.SetDisplay("Indent (pips)", "Offset added to the previous candle high/low", "Trade");

_fastPeriod = Param(nameof(FastPeriod), 10)
.SetDisplay("Fast MA", "Fast moving average length", "Filter");

_fastShift = Param(nameof(FastShift), 3)
.SetDisplay("Fast Shift", "Shift applied to the fast moving average", "Filter");

_slowPeriod = Param(nameof(SlowPeriod), 30)
.SetDisplay("Slow MA", "Slow moving average length", "Filter");

_slowShift = Param(nameof(SlowShift), 0)
.SetDisplay("Slow Shift", "Shift applied to the slow moving average", "Filter");

_maType = Param(nameof(MaType), MovingAverageTypeEnum.Simple)
.SetDisplay("MA Type", "Moving average calculation method", "Filter");

_stopLossPips = Param(nameof(StopLossPips), 50m)
.SetDisplay("Stop Loss (pips)", "Protective stop distance in pips", "Risk");

_takeProfitPips = Param(nameof(TakeProfitPips), 150m)
.SetDisplay("Take Profit (pips)", "Take profit distance in pips", "Risk");

_trailingStopPips = Param(nameof(TrailingStopPips), 15m)
.SetDisplay("Trailing Stop (pips)", "Trailing stop distance in pips", "Risk");

_trailingStepPips = Param(nameof(TrailingStepPips), 5m)
.SetDisplay("Trailing Step (pips)", "Price improvement required before updating the trailing stop", "Risk");

_orderVolume = Param(nameof(OrderVolume), 0m)
.SetDisplay("Order Volume", "Fixed trade volume. Leave zero to risk a percentage of equity.", "Money Management");

_riskPercent = Param(nameof(RiskPercent), 5m)
.SetDisplay("Risk %", "Risk percentage of portfolio equity when volume is not fixed", "Money Management");

_maxPositions = Param(nameof(MaxPositions), 10)
.SetGreaterThanZero()
.SetDisplay("Max Positions", "Maximum number of entries allowed in one direction", "Money Management");

_profitClose = Param(nameof(ProfitClose), 100m)
.SetDisplay("Profit Close", "Close all positions when floating profit reaches this amount", "Money Management");
}

/// <inheritdoc />
public override System.Collections.Generic.IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
{
return [(Security, CandleType)];
}

/// <inheritdoc />
protected override void OnReseted()
{
base.OnReseted();

_fastMa = null;
_slowMa = null;
_fastBuffer = null;
_slowBuffer = null;
_pipSize = 0m;
_positionVolume = 0m;
_avgPrice = 0m;
_breakoutHigh = null;
_breakoutLow = null;
_breakoutTime = null;
_lastLongEntryTime = null;
_lastShortEntryTime = null;
_pendingLongEntryTime = null;
_pendingShortEntryTime = null;
_longStopPrice = null;
_longTakeProfit = null;
_shortStopPrice = null;
_shortTakeProfit = null;
_longTrailingPrice = null;
_shortTrailingPrice = null;
}

/// <inheritdoc />
protected override void OnStarted(DateTimeOffset time)
{
base.OnStarted(time);

_pipSize = GetPipSize();

LengthIndicator<decimal>? fast = null;
LengthIndicator<decimal>? slow = null;

var useMaFilter = FastPeriod > 0 && SlowPeriod > 0;

if (useMaFilter)
{
fast = CreateMovingAverage(MaType, FastPeriod);
slow = CreateMovingAverage(MaType, SlowPeriod);
_fastBuffer = new ShiftBuffer(Math.Max(0, FastShift));
_slowBuffer = new ShiftBuffer(Math.Max(0, SlowShift));
}

_fastMa = fast;
_slowMa = slow;

var subscription = SubscribeCandles(CandleType);

if (useMaFilter && fast != null && slow != null)
{
subscription
.Bind(fast, slow, ProcessCandle)
.Start();
}
else
{
subscription
.Bind(ProcessCandle)
.Start();
}

var area = CreateChartArea();
if (area != null)
{
DrawCandles(area, subscription);
if (fast != null)
DrawIndicator(area, fast);
if (slow != null)
DrawIndicator(area, slow);
DrawOwnTrades(area);
}
}

private void ProcessCandle(ICandleMessage candle)
{
ProcessCandleInternal(candle, null, null);
}

private void ProcessCandle(ICandleMessage candle, decimal fastValue, decimal slowValue)
{
ProcessCandleInternal(candle, fastValue, slowValue);
}

private void ProcessCandleInternal(ICandleMessage candle, decimal? fastValue, decimal? slowValue)
{
if (candle.State == CandleStates.None)
return;

var indent = GetPriceOffset(IndentPips);
var stopOffset = GetPriceOffset(StopLossPips);
var takeOffset = GetPriceOffset(TakeProfitPips);
var trailingOffset = GetPriceOffset(TrailingStopPips);
var trailingStep = GetPriceOffset(TrailingStepPips);

var useTrailing = trailingOffset > 0m && trailingStep > 0m;

var buyOk = true;
var sellOk = true;

if (FastPeriod > 0 && SlowPeriod > 0 && fastValue.HasValue && slowValue.HasValue && _fastBuffer != null && _slowBuffer != null)
{
if (_fastBuffer.TryGetValue(fastValue.Value, out var fastShifted) && _slowBuffer.TryGetValue(slowValue.Value, out var slowShifted))
{
buyOk = fastShifted > slowShifted;
sellOk = fastShifted < slowShifted;
}
else
{
buyOk = false;
sellOk = false;
}
}

// Exit all trades when floating profit target is reached.
var openProfit = CalculateOpenProfit(candle.ClosePrice);
if (ProfitClose > 0m && openProfit >= ProfitClose)
{
CloseAllPositions();
return;
}

// Manage protective logic before evaluating new entries.
if (ManageOpenPositions(candle, stopOffset, takeOffset, useTrailing ? trailingOffset : 0m, useTrailing ? trailingStep : 0m))
return;

if (_breakoutHigh.HasValue && _breakoutLow.HasValue)
{
var breakoutHigh = _breakoutHigh.Value + indent;
var breakoutLow = _breakoutLow.Value - indent;
var barTime = candle.OpenTime;

if (buyOk && candle.HighPrice >= breakoutHigh && TryGetOrderVolume(true, barTime, stopOffset, out var longVolume))
{
_pendingLongEntryTime = barTime;
BuyMarket(longVolume);
}

if (sellOk && candle.LowPrice <= breakoutLow && TryGetOrderVolume(false, barTime, stopOffset, out var shortVolume))
{
_pendingShortEntryTime = barTime;
SellMarket(shortVolume);
}
}

if (candle.State == CandleStates.Finished)
{
_breakoutHigh = candle.HighPrice;
_breakoutLow = candle.LowPrice;
_breakoutTime = candle.OpenTime;
}
}

/// <inheritdoc />
protected override void OnOwnTradeReceived(MyTrade trade)
{
if (trade?.Order is null || trade.Trade is null)
return;

var price = trade.Trade.Price;
var volume = trade.Trade.Volume;
var side = trade.Order.Side;
var prevVolume = _positionVolume;

var pendingLongTime = _pendingLongEntryTime;
var pendingShortTime = _pendingShortEntryTime;

if (side == Sides.Buy)
{
if (_positionVolume >= 0m)
{
var newVolume = _positionVolume + volume;
_avgPrice = newVolume > 0m ? ((_avgPrice * _positionVolume) + price * volume) / newVolume : 0m;
_positionVolume = newVolume;
}
else
{
var shortVolume = Math.Abs(_positionVolume);
var covering = Math.Min(volume, shortVolume);
_positionVolume += covering;

var remainder = volume - covering;
if (_positionVolume == 0m)
{
_avgPrice = 0m;
if (remainder > 0m)
{
_positionVolume = remainder;
_avgPrice = price;
}
}
else if (_positionVolume > 0m)
{
_avgPrice = price;
}
}

_pendingLongEntryTime = null;
}
else if (side == Sides.Sell)
{
if (_positionVolume <= 0m)
{
var newVolume = Math.Abs(_positionVolume) + volume;
_avgPrice = newVolume > 0m ? ((_avgPrice * Math.Abs(_positionVolume)) + price * volume) / newVolume : 0m;
_positionVolume -= volume;
}
else
{
var longVolume = _positionVolume;
var covering = Math.Min(volume, longVolume);
_positionVolume -= covering;

var remainder = volume - covering;
if (_positionVolume == 0m)
{
_avgPrice = 0m;
if (remainder > 0m)
{
_positionVolume = -remainder;
_avgPrice = price;
}
}
else if (_positionVolume < 0m)
{
_avgPrice = price;
}
}

_pendingShortEntryTime = null;
}

if (prevVolume <= 0m && _positionVolume > 0m)
{
_longStopPrice = stopLossPriceForEntry(_avgPrice);
_longTakeProfit = takeProfitPriceForEntry(_avgPrice);
_longTrailingPrice = null;
_lastLongEntryTime = pendingLongTime ?? _breakoutTime;
}
else if (prevVolume >= 0m && _positionVolume < 0m)
{
_shortStopPrice = stopLossPriceForEntry(_avgPrice, false);
_shortTakeProfit = takeProfitPriceForEntry(_avgPrice, false);
_shortTrailingPrice = null;
_lastShortEntryTime = pendingShortTime ?? _breakoutTime;
}
else if (_positionVolume == 0m)
{
_longStopPrice = null;
_longTakeProfit = null;
_shortStopPrice = null;
_shortTakeProfit = null;
_longTrailingPrice = null;
_shortTrailingPrice = null;
}

decimal? stopLossPriceForEntry(decimal entryPrice, bool isLong = true)
{
var offset = GetPriceOffset(StopLossPips);
if (offset <= 0m)
return null;

return isLong ? entryPrice - offset : entryPrice + offset;
}

decimal? takeProfitPriceForEntry(decimal entryPrice, bool isLong = true)
{
var offset = GetPriceOffset(TakeProfitPips);
if (offset <= 0m)
return null;

return isLong ? entryPrice + offset : entryPrice - offset;
}
}

private bool ManageOpenPositions(ICandleMessage candle, decimal stopOffset, decimal takeOffset, decimal trailingOffset, decimal trailingStep)
{
if (_positionVolume > 0m)
{
var volume = _positionVolume;

if (_longTakeProfit is null && takeOffset > 0m)
_longTakeProfit = _avgPrice + takeOffset;

if (_longTakeProfit.HasValue && candle.HighPrice >= _longTakeProfit.Value)
{
SellMarket(volume);
return true;
}

if (trailingOffset > 0m && trailingStep > 0m)
{
var diff = candle.ClosePrice - _avgPrice;
if (diff > trailingOffset + trailingStep)
{
var candidate = candle.ClosePrice - trailingOffset;
if (!_longTrailingPrice.HasValue || candidate > _longTrailingPrice.Value + trailingStep)
_longTrailingPrice = candidate;
}

if (_longTrailingPrice.HasValue && (!_longStopPrice.HasValue || _longTrailingPrice.Value > _longStopPrice.Value))
_longStopPrice = _longTrailingPrice;
}

if (_longStopPrice is null && stopOffset > 0m)
_longStopPrice = _avgPrice - stopOffset;

if (_longStopPrice.HasValue && candle.LowPrice <= _longStopPrice.Value)
{
SellMarket(volume);
return true;
}
}
else if (_positionVolume < 0m)
{
var volume = Math.Abs(_positionVolume);

if (_shortTakeProfit is null && takeOffset > 0m)
_shortTakeProfit = _avgPrice - takeOffset;

if (_shortTakeProfit.HasValue && candle.LowPrice <= _shortTakeProfit.Value)
{
BuyMarket(volume);
return true;
}

if (trailingOffset > 0m && trailingStep > 0m)
{
var diff = _avgPrice - candle.ClosePrice;
if (diff > trailingOffset + trailingStep)
{
var candidate = candle.ClosePrice + trailingOffset;
if (!_shortTrailingPrice.HasValue || candidate < _shortTrailingPrice.Value - trailingStep)
_shortTrailingPrice = candidate;
}

if (_shortTrailingPrice.HasValue && (!_shortStopPrice.HasValue || _shortTrailingPrice.Value < _shortStopPrice.Value))
_shortStopPrice = _shortTrailingPrice;
}

if (_shortStopPrice is null && stopOffset > 0m)
_shortStopPrice = _avgPrice + stopOffset;

if (_shortStopPrice.HasValue && candle.HighPrice >= _shortStopPrice.Value)
{
BuyMarket(volume);
return true;
}
}

return false;
}

private bool TryGetOrderVolume(bool isLong, DateTimeOffset barTime, decimal stopOffset, out decimal volume)
{
volume = CalculateOrderVolume(stopOffset);
if (volume <= 0m)
return false;

var referenceVolume = OrderVolume > 0m ? OrderVolume : volume;

if (isLong)
{
if (_pendingLongEntryTime.HasValue && _pendingLongEntryTime.Value == barTime)
return false;
if (_lastLongEntryTime.HasValue && _lastLongEntryTime.Value == barTime)
return false;

if (_positionVolume >= 0m)
{
var maxVolume = MaxPositions * referenceVolume;
if (maxVolume > 0m)
{
var remaining = maxVolume - _positionVolume;
if (remaining <= 0m)
return false;
volume = Math.Min(volume, remaining);
}
}
}
else
{
if (_pendingShortEntryTime.HasValue && _pendingShortEntryTime.Value == barTime)
return false;
if (_lastShortEntryTime.HasValue && _lastShortEntryTime.Value == barTime)
return false;

if (_positionVolume <= 0m)
{
var maxVolume = MaxPositions * referenceVolume;
if (maxVolume > 0m)
{
var remaining = maxVolume - Math.Abs(_positionVolume);
if (remaining <= 0m)
return false;
volume = Math.Min(volume, remaining);
}
}
}

return volume > 0m;
}

private decimal CalculateOrderVolume(decimal stopOffset)
{
if (OrderVolume > 0m)
return OrderVolume;

if (RiskPercent <= 0m || stopOffset <= 0m)
return 0m;

var portfolio = Portfolio;
if (portfolio is null)
return 0m;

var equity = portfolio.CurrentValue ?? portfolio.BeginValue ?? 0m;
if (equity <= 0m)
return 0m;

var priceStep = Security?.PriceStep ?? 0m;
var stepPrice = Security?.StepPrice ?? 0m;
if (priceStep <= 0m || stepPrice <= 0m)
return 0m;

var perUnitRisk = stopOffset / priceStep * stepPrice;
if (perUnitRisk <= 0m)
return 0m;

var riskAmount = equity * RiskPercent / 100m;
var rawVolume = riskAmount / perUnitRisk;

var volumeStep = Security?.VolumeStep ?? 0m;
if (volumeStep > 0m)
{
var steps = Math.Max(1m, Math.Floor(rawVolume / volumeStep));
return steps * volumeStep;
}

return Math.Max(rawVolume, 0m);
}

private decimal CalculateOpenProfit(decimal currentPrice)
{
if (_positionVolume == 0m)
return 0m;

var diff = _positionVolume > 0m ? currentPrice - _avgPrice : _avgPrice - currentPrice;
return PriceToMoney(diff, Math.Abs(_positionVolume));
}

private decimal PriceToMoney(decimal priceDiff, decimal volume)
{
if (priceDiff == 0m || volume <= 0m)
return 0m;

var priceStep = Security?.PriceStep ?? 0m;
var stepPrice = Security?.StepPrice ?? 0m;

if (priceStep <= 0m || stepPrice <= 0m)
return priceDiff * volume;

return priceDiff / priceStep * stepPrice * volume;
}

private void CloseAllPositions()
{
if (_positionVolume > 0m)
SellMarket(_positionVolume);
else if (_positionVolume < 0m)
BuyMarket(Math.Abs(_positionVolume));
}

private decimal GetPriceOffset(decimal value)
{
if (value == 0m)
return 0m;

if (_pipSize > 0m)
return value * _pipSize;

return value;
}

private decimal GetPipSize()
{
var priceStep = Security?.PriceStep ?? 0m;
if (priceStep <= 0m)
return 0m;

var decimals = Security?.Decimals;
if (decimals == 3 || decimals == 5)
return priceStep * 10m;

return priceStep;
}

private static LengthIndicator<decimal> CreateMovingAverage(MovingAverageTypeEnum type, int length)
{
return type switch
{
MovingAverageTypeEnum.Simple => new SimpleMovingAverage { Length = length },
MovingAverageTypeEnum.Exponential => new ExponentialMovingAverage { Length = length },
MovingAverageTypeEnum.Smoothed => new SmoothedMovingAverage { Length = length },
MovingAverageTypeEnum.Weighted => new WeightedMovingAverage { Length = length },
_ => new SimpleMovingAverage { Length = length }
};
}

private sealed class ShiftBuffer
{
private readonly decimal[] _buffer;
private int _index;
private int _count;
private readonly int _shift;

public ShiftBuffer(int shift)
{
_shift = Math.Max(0, shift);
_buffer = new decimal[_shift + 1];
}

public bool TryGetValue(decimal value, out decimal shifted)
{
_buffer[_index] = value;

if (_count < _buffer.Length)
_count++;

_index++;
if (_index >= _buffer.Length)
_index = 0;

if (_count > _shift)
{
var idx = _index - 1 - _shift;
if (idx < 0)
idx += _buffer.Length;

shifted = _buffer[idx];
return true;
}

if (_shift == 0 && _count > 0)
{
shifted = value;
return true;
}

shifted = 0m;
return false;
}
}
}
