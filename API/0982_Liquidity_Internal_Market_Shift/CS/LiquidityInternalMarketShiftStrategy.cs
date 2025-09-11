using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Liquidity + Internal Market Shift strategy.
/// Trades when internal shift aligns with liquidity lines.
/// </summary>
public class LiquidityInternalMarketShiftStrategy : Strategy
{
public enum TradeMode
{
Both,
BullishOnly,
BearishOnly
}

private readonly StrategyParam<TradeMode> _mode;
private readonly StrategyParam<DateTimeOffset> _startDate;
private readonly StrategyParam<DateTimeOffset> _endDate;
private readonly StrategyParam<bool> _enableTakeProfit;
private readonly StrategyParam<int> _stopLossPips;
private readonly StrategyParam<int> _takeProfitPips;
private readonly StrategyParam<int> _upperLiquidityLookback;
private readonly StrategyParam<int> _lowerLiquidityLookback;
private readonly StrategyParam<DataType> _candleType;

private readonly Highest _highest = new();
private readonly Lowest _lowest = new();

private int _bullishCount;
private int _bearishCount;
private decimal? _lowestBullishPrice;
private decimal? _highestBearishPrice;
private decimal? _previousBullishPrice;
private decimal? _previousBearishPrice;
private int _lastInternalShift;

private bool _touchedLowerLiquidityLine;
private bool _touchedUpperLiquidityLine;
private bool _lockedBullish;
private bool _lockedBearish;
private int? _barSinceLiquidityTouch;

private decimal? _entryPrice;
private DateTimeOffset _entryTime;
private bool _isLongPosition;

public TradeMode Mode { get => _mode.Value; set => _mode.Value = value; }
public DateTimeOffset StartDate { get => _startDate.Value; set => _startDate.Value = value; }
public DateTimeOffset EndDate { get => _endDate.Value; set => _endDate.Value = value; }
public bool EnableTakeProfit { get => _enableTakeProfit.Value; set => _enableTakeProfit.Value = value; }
public int StopLossPips { get => _stopLossPips.Value; set => _stopLossPips.Value = value; }
public int TakeProfitPips { get => _takeProfitPips.Value; set => _takeProfitPips.Value = value; }
public int UpperLiquidityLookback { get => _upperLiquidityLookback.Value; set => _upperLiquidityLookback.Value = value; }
public int LowerLiquidityLookback { get => _lowerLiquidityLookback.Value; set => _lowerLiquidityLookback.Value = value; }
public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

public LiquidityInternalMarketShiftStrategy()
{
_mode = Param(nameof(Mode), TradeMode.Both)
.SetDisplay("Mode", "Trading mode", "General");

_startDate = Param(nameof(StartDate), new DateTimeOffset(new DateTime(2024, 1, 1), TimeSpan.Zero))
.SetDisplay("Start Date", "Start of trading period", "General");

_endDate = Param(nameof(EndDate), new DateTimeOffset(new DateTime(2024, 12, 31, 23, 59, 0), TimeSpan.Zero))
.SetDisplay("End Date", "End of trading period", "General");

_enableTakeProfit = Param(nameof(EnableTakeProfit), true)
.SetDisplay("Enable Take Profit", "Use take profit", "Risk");

_stopLossPips = Param(nameof(StopLossPips), 10)
.SetGreaterThanZero()
.SetDisplay("Stop Loss (pips)", "Stop loss in pips", "Risk");

_takeProfitPips = Param(nameof(TakeProfitPips), 20)
.SetGreaterThanZero()
.SetDisplay("Take Profit (pips)", "Take profit in pips", "Risk");

_upperLiquidityLookback = Param(nameof(UpperLiquidityLookback), 10)
.SetGreaterThanZero()
.SetDisplay("Upper Liquidity Lookback", "Lookback for upper liquidity", "Signals");

_lowerLiquidityLookback = Param(nameof(LowerLiquidityLookback), 10)
.SetGreaterThanZero()
.SetDisplay("Lower Liquidity Lookback", "Lookback for lower liquidity", "Signals");

_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
.SetDisplay("Candle Type", "Type of candles", "General");
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

_bullishCount = 0;
_bearishCount = 0;
_lowestBullishPrice = null;
_highestBearishPrice = null;
_previousBullishPrice = null;
_previousBearishPrice = null;
_lastInternalShift = 0;
_touchedLowerLiquidityLine = false;
_touchedUpperLiquidityLine = false;
_lockedBullish = false;
_lockedBearish = false;
_barSinceLiquidityTouch = null;
_entryPrice = null;
_isLongPosition = false;
}

/// <inheritdoc />
protected override void OnStarted(DateTimeOffset time)
{
base.OnStarted(time);

_highest.Length = UpperLiquidityLookback;
_lowest.Length = LowerLiquidityLookback;

var subscription = SubscribeCandles(CandleType);
subscription.Bind(ProcessCandle).Start();

StartProtection();

var area = CreateChartArea();
if (area != null)
{
DrawCandles(area, subscription);
DrawOwnTrades(area);
}
}

private void ProcessCandle(ICandleMessage candle)
{
if (candle.State != CandleStates.Finished)
return;

var recentHigh = _highest.Process(candle.HighPrice, candle.OpenTime, true).ToDecimal();
var recentLow = _lowest.Process(candle.LowPrice, candle.OpenTime, true).ToDecimal();

if (candle.LowPrice <= recentLow)
_touchedLowerLiquidityLine = true;

if (candle.HighPrice >= recentHigh)
_touchedUpperLiquidityLine = true;

bool isBullish = candle.ClosePrice > candle.OpenPrice;
bool isBearish = candle.ClosePrice < candle.OpenPrice;

if (isBullish)
{
_bullishCount++;
_bearishCount = 0;
if (_bullishCount == 1 || candle.LowPrice < _lowestBullishPrice)
_lowestBullishPrice = candle.LowPrice;
}
else if (isBearish)
{
_bearishCount++;
_bullishCount = 0;
if (_bearishCount == 1 || candle.HighPrice > _highestBearishPrice)
_highestBearishPrice = candle.HighPrice;
}
else
{
_bullishCount = 0;
_bearishCount = 0;
_lowestBullishPrice = null;
_highestBearishPrice = null;
}

if (_bullishCount >= 1)
_previousBullishPrice = _lowestBullishPrice;

if (_bearishCount >= 1)
_previousBearishPrice = _highestBearishPrice;

bool internalShiftBearish =
_previousBullishPrice.HasValue && _lowestBullishPrice.HasValue &&
candle.ClosePrice < _previousBullishPrice &&
candle.ClosePrice < _lowestBullishPrice;

bool internalShiftBullish =
_previousBearishPrice.HasValue && _highestBearishPrice.HasValue &&
candle.ClosePrice > _previousBearishPrice &&
candle.ClosePrice > _highestBearishPrice;

bool allowInternalShiftBearish = internalShiftBearish && _lastInternalShift != -1;
bool allowInternalShiftBullish = internalShiftBullish && _lastInternalShift != 1;

var bullishSignal = allowInternalShiftBullish && _touchedLowerLiquidityLine && !_lockedBullish;
var bearishSignal = allowInternalShiftBearish && _touchedUpperLiquidityLine && !_lockedBearish;

if (bullishSignal)
{
_lockedBullish = true;
_touchedLowerLiquidityLine = false;
_barSinceLiquidityTouch = 0;
_lastInternalShift = 1;
}

if (bearishSignal)
{
_lockedBearish = true;
_touchedUpperLiquidityLine = false;
_barSinceLiquidityTouch = 0;
_lastInternalShift = -1;
}

if (_barSinceLiquidityTouch.HasValue)
_barSinceLiquidityTouch++;

if (_barSinceLiquidityTouch >= 3)
{
_lockedBullish = false;
_lockedBearish = false;
_barSinceLiquidityTouch = null;
}

if (_touchedLowerLiquidityLine)
_lockedBullish = false;

if (_touchedUpperLiquidityLine)
_lockedBearish = false;

bool inTimeRange = candle.OpenTime >= StartDate && candle.OpenTime <= EndDate;

if (Mode != TradeMode.BearishOnly && bullishSignal && inTimeRange && _entryPrice is null)
{
BuyMarket();
_entryPrice = candle.ClosePrice;
_entryTime = candle.OpenTime;
_isLongPosition = true;
}

if (Mode != TradeMode.BullishOnly && bearishSignal && inTimeRange && _entryPrice is null)
{
SellMarket();
_entryPrice = candle.ClosePrice;
_entryTime = candle.OpenTime;
_isLongPosition = false;
}

if (_entryPrice.HasValue)
{
if (_isLongPosition && bearishSignal && candle.OpenTime > _entryTime)
{
SellMarket(Math.Abs(Position));
_entryPrice = null;
}
else if (!_isLongPosition && bullishSignal && candle.OpenTime > _entryTime)
{
BuyMarket(Math.Abs(Position));
_entryPrice = null;
}

var pip = Security?.PriceStep ?? 0.0001m;
if (_isLongPosition)
{
var stopLossPrice = _entryPrice.Value - StopLossPips * pip;
var takeProfitPrice = _entryPrice.Value + TakeProfitPips * pip;

if (candle.ClosePrice <= stopLossPrice)
{
SellMarket(Math.Abs(Position));
_entryPrice = null;
}
else if (EnableTakeProfit && candle.ClosePrice >= takeProfitPrice)
{
SellMarket(Math.Abs(Position));
_entryPrice = null;
}
}
else
{
var stopLossPrice = _entryPrice.Value + StopLossPips * pip;
var takeProfitPrice = _entryPrice.Value - TakeProfitPips * pip;

if (candle.ClosePrice >= stopLossPrice)
{
BuyMarket(Math.Abs(Position));
_entryPrice = null;
}
else if (EnableTakeProfit && candle.ClosePrice <= takeProfitPrice)
{
BuyMarket(Math.Abs(Position));
_entryPrice = null;
}
}
}
}
}
