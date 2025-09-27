namespace StockSharp.Samples.Strategies;

using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

using StockSharp.Algo;

/// <summary>
/// FXF Safe Trend Scalp V1 strategy translated from the MetaTrader 4 expert advisor.
/// Trades when price approaches ZigZag-based trendlines and confirms the move with moving averages.
/// </summary>
public class FXFSafeTrendScalpV1Strategy : Strategy
{
private enum SignalDirections
{
None,
Buy,
Sell
}

private sealed record ZigZagPivot(int BarIndex, decimal Price);

private readonly StrategyParam<DataType> _candleType;
private readonly StrategyParam<decimal> _volume;
private readonly StrategyParam<int> _zigZagDepth;
private readonly StrategyParam<decimal> _zigZagDeviationPoints;
private readonly StrategyParam<int> _zigZagBackstep;
private readonly StrategyParam<decimal> _trendOffsetPoints;
private readonly StrategyParam<int> _fastMaLength;
private readonly StrategyParam<int> _slowMaLength;
private readonly StrategyParam<decimal> _maxSpreadPoints;
private readonly StrategyParam<decimal> _stopLossPoints;
private readonly StrategyParam<decimal> _takeProfitPoints;
private readonly StrategyParam<decimal> _profitTargetPerLot;
private readonly StrategyParam<int> _trendAnchorIndex;
private readonly StrategyParam<int> _maxStoredPivots;

private SimpleMovingAverage _fastMa;
private SimpleMovingAverage _slowMa;
private readonly List<ZigZagPivot> _highPivots = new();
private readonly List<ZigZagPivot> _lowPivots = new();
private SignalDirections _signal;
private int _barIndex;
private decimal _pipSize;
private int _searchDirection;
private decimal? _currentExtreme;
private int _currentExtremeBar;
private int _barsSinceExtreme;
private decimal? _bestBid;
private decimal? _bestAsk;
private bool _hasBestBid;
private bool _hasBestAsk;

/// <summary>
/// Initializes a new instance of the <see cref="FXFSafeTrendScalpV1Strategy"/> class.
/// </summary>
public FXFSafeTrendScalpV1Strategy()
{
_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
.SetDisplay("Candle Type", "Timeframe used for analysis", "General");

_volume = Param(nameof(VolumePerTrade), 0.1m)
.SetGreaterThanZero()
.SetDisplay("Volume", "Order volume submitted per trade", "Trading");

_zigZagDepth = Param(nameof(ZigZagDepth), 2)
.SetGreaterThanZero()
.SetDisplay("ZigZag Depth", "Minimal bars between pivots", "ZigZag");

_zigZagDeviationPoints = Param(nameof(ZigZagDeviationPoints), 3m)
.SetGreaterThanZero()
.SetDisplay("ZigZag Deviation (pts)", "Minimum price move in points", "ZigZag");

_zigZagBackstep = Param(nameof(ZigZagBackstep), 1)
.SetGreaterThanZero()
.SetDisplay("ZigZag Backstep", "Bars to wait before switching direction", "ZigZag");

_trendOffsetPoints = Param(nameof(TrendOffsetPoints), 10m)
.SetGreaterThanZero()
.SetDisplay("Trend Offset (pts)", "Distance from trendline that triggers a signal", "Trading");

_fastMaLength = Param(nameof(FastMaLength), 2)
.SetGreaterThanZero()
.SetDisplay("Fast MA Length", "Length of the fast simple moving average", "Indicators");

_slowMaLength = Param(nameof(SlowMaLength), 50)
.SetGreaterThanZero()
.SetDisplay("Slow MA Length", "Length of the slow simple moving average", "Indicators");

_maxSpreadPoints = Param(nameof(MaxSpreadPoints), 20m)
.SetGreaterThanOrEqual(0m)
.SetDisplay("Max Spread (pts)", "Maximum allowed spread in points", "Trading");

_stopLossPoints = Param(nameof(StopLossPoints), 500m)
.SetGreaterThanOrEqual(0m)
.SetDisplay("Stop Loss (pts)", "Stop-loss distance in points", "Risk");

_takeProfitPoints = Param(nameof(TakeProfitPoints), 500m)
.SetGreaterThanOrEqual(0m)
.SetDisplay("Take Profit (pts)", "Take-profit distance in points", "Risk");

_profitTargetPerLot = Param(nameof(ProfitTargetPerLot), 50m)
.SetGreaterThanOrEqual(0m)
.SetDisplay("Profit Target per Lot", "Floating profit required to close positions", "Risk");

_trendAnchorIndex = Param(nameof(TrendAnchorIndex), 3)
.SetGreaterOrEqual(1)
.SetDisplay("Trend Anchor Index", "Older pivot index used to build the trendline", "ZigZag");

_maxStoredPivots = Param(nameof(MaxStoredPivots), 10)
.SetGreaterOrEqual(4)
.SetDisplay("Stored Pivots", "Maximum number of ZigZag pivots kept for analysis", "ZigZag");
}

/// <summary>
/// Candle type used for indicator calculations.
/// </summary>
public DataType CandleType
{
get => _candleType.Value;
set => _candleType.Value = value;
}

/// <summary>
/// Trade volume submitted with each entry.
/// </summary>
public decimal VolumePerTrade
{
get => _volume.Value;
set => _volume.Value = value;
}

/// <summary>
/// Minimum number of bars between ZigZag pivots.
/// </summary>
public int ZigZagDepth
{
get => _zigZagDepth.Value;
set => _zigZagDepth.Value = value;
}

/// <summary>
/// Minimum price move in points required to confirm a new pivot.
/// </summary>
public decimal ZigZagDeviationPoints
{
get => _zigZagDeviationPoints.Value;
set => _zigZagDeviationPoints.Value = value;
}

/// <summary>
/// Bars that must pass before a pivot in the opposite direction is accepted.
/// </summary>
public int ZigZagBackstep
{
get => _zigZagBackstep.Value;
set => _zigZagBackstep.Value = value;
}

/// <summary>
/// Offset from the trendline that arms a new entry signal.
/// </summary>
public decimal TrendOffsetPoints
{
get => _trendOffsetPoints.Value;
set => _trendOffsetPoints.Value = value;
}

/// <summary>
/// Fast moving average length.
/// </summary>
public int FastMaLength
{
get => _fastMaLength.Value;
set => _fastMaLength.Value = value;
}

/// <summary>
/// Slow moving average length.
/// </summary>
public int SlowMaLength
{
get => _slowMaLength.Value;
set => _slowMaLength.Value = value;
}

/// <summary>
/// Maximum allowed spread expressed in points.
/// </summary>
public decimal MaxSpreadPoints
{
get => _maxSpreadPoints.Value;
set => _maxSpreadPoints.Value = value;
}

/// <summary>
/// Stop-loss distance in points.
/// </summary>
public decimal StopLossPoints
{
get => _stopLossPoints.Value;
set => _stopLossPoints.Value = value;
}

/// <summary>
/// Take-profit distance in points.
/// </summary>
public decimal TakeProfitPoints
{
get => _takeProfitPoints.Value;
set => _takeProfitPoints.Value = value;
}

/// <summary>
/// Floating profit target per lot before closing the position.
/// </summary>
public decimal ProfitTargetPerLot
{
get => _profitTargetPerLot.Value;
set => _profitTargetPerLot.Value = value;
}

public int TrendAnchorIndex
{
get => _trendAnchorIndex.Value;
set => _trendAnchorIndex.Value = value;
}

public int MaxStoredPivots
{
get => _maxStoredPivots.Value;
set => _maxStoredPivots.Value = value;
}

/// <inheritdoc />
protected override void OnReseted()
{
base.OnReseted();

_fastMa = null;
_slowMa = null;
_highPivots.Clear();
_lowPivots.Clear();
_signal = SignalDirections.None;
_barIndex = -1;
_pipSize = 0m;
_searchDirection = 1;
_currentExtreme = null;
_currentExtremeBar = 0;
_barsSinceExtreme = 0;
_bestBid = null;
_bestAsk = null;
_hasBestBid = false;
_hasBestAsk = false;
}

/// <inheritdoc />
protected override void OnStarted(DateTimeOffset time)
{
base.OnStarted(time);

_pipSize = GetPipSize();
_fastMa = new SimpleMovingAverage { Length = FastMaLength };
_slowMa = new SimpleMovingAverage { Length = SlowMaLength };
_searchDirection = 1;
_barIndex = -1;

if (MaxStoredPivots <= TrendAnchorIndex)
throw new InvalidOperationException("Stored pivots must exceed the trend anchor index.");

var subscription = SubscribeCandles(CandleType);
subscription
.Bind(_fastMa, _slowMa, ProcessCandle)
.Start();

var area = CreateChartArea();
if (area != null)
{
DrawCandles(area, subscription);
DrawIndicator(area, _fastMa);
DrawIndicator(area, _slowMa);
DrawOwnTrades(area);
}
}

private void ProcessCandle(ICandleMessage candle, decimal fastMaValue, decimal slowMaValue)
{
if (candle.State != CandleStates.Finished)
return;

_barIndex++;

UpdateZigZag(candle);

if (CheckTotalProfitTarget(candle))
return;

TryExecuteSignal(candle);
UpdateSignal(candle, fastMaValue, slowMaValue);
}

private void UpdateSignal(ICandleMessage candle, decimal fastMaValue, decimal slowMaValue)
{
if (_fastMa == null || _slowMa == null)
return;

if (!_fastMa.IsFormed || !_slowMa.IsFormed)
return;

if (Position != 0)
return;

var offset = GetPointsValue(TrendOffsetPoints);
var highLine = GetTrendlineValue(_highPivots);
var lowLine = GetTrendlineValue(_lowPivots);

if (highLine.HasValue && _signal != SignalDirections.Sell)
{
var trigger = highLine.Value - offset;
if (fastMaValue < slowMaValue && candle.ClosePrice >= trigger)
_signal = SignalDirections.Sell;
}

if (lowLine.HasValue && _signal != SignalDirections.Buy)
{
var trigger = lowLine.Value + offset;
if (fastMaValue > slowMaValue && candle.ClosePrice <= trigger)
_signal = SignalDirections.Buy;
}
}

private void TryExecuteSignal(ICandleMessage candle)
{
if (_signal == SignalDirections.None)
return;

if (!IsFormedAndOnlineAndAllowTrading())
return;

if (_fastMa == null || _slowMa == null)
return;

if (!_fastMa.IsFormed || !_slowMa.IsFormed)
return;

if (Position != 0)
return;

var spreadLimit = GetPointsValue(MaxSpreadPoints);
if (spreadLimit > 0m)
{
var spread = GetCurrentSpread();
if (spread <= 0m || spread > spreadLimit)
return;
}

var volume = AlignVolume(VolumePerTrade);
if (volume <= 0m)
return;

var resultingPosition = _signal == SignalDirections.Buy ? Position + volume : Position - volume;

if (_signal == SignalDirections.Buy)
BuyMarket(volume);
else
SellMarket(volume);

ApplyProtection(candle.ClosePrice, resultingPosition);
}

private bool CheckTotalProfitTarget(ICandleMessage candle)
{
var targetPerLot = ProfitTargetPerLot;
if (targetPerLot <= 0m)
return false;

var currentPosition = Position;
if (currentPosition == 0m)
return false;

var entryPrice = PositionAvgPrice;
if (entryPrice == 0m)
return false;

var unrealized = (candle.ClosePrice - entryPrice) * currentPosition;
var required = targetPerLot * Math.Abs(currentPosition);

if (unrealized < required)
return false;

ClosePosition();
return true;
}

private void ClosePosition()
{
if (!IsFormedAndOnlineAndAllowTrading())
return;

var volume = Math.Abs(Position);
if (volume <= 0m)
return;

if (Position > 0)
SellMarket(volume);
else
BuyMarket(volume);
}

private void ApplyProtection(decimal price, decimal resultingPosition)
{
if (resultingPosition == 0m)
return;

var stopDistance = GetPointsValue(StopLossPoints);
if (stopDistance > 0m)
SetStopLoss(stopDistance, price, resultingPosition);

var takeDistance = GetPointsValue(TakeProfitPoints);
if (takeDistance > 0m)
SetTakeProfit(takeDistance, price, resultingPosition);
}

private void UpdateZigZag(ICandleMessage candle)
{
var deviation = GetPointsValue(ZigZagDeviationPoints);
if (deviation <= 0m)
deviation = _pipSize > 0m ? _pipSize : 1m;

var minBars = Math.Max(1, Math.Max(ZigZagDepth, ZigZagBackstep));

if (_currentExtreme is null)
{
_currentExtreme = _searchDirection > 0 ? candle.HighPrice : candle.LowPrice;
_currentExtremeBar = _barIndex;
_barsSinceExtreme = 0;
return;
}

if (_searchDirection > 0)
{
if (candle.HighPrice >= _currentExtreme.Value)
{
_currentExtreme = candle.HighPrice;
_currentExtremeBar = _barIndex;
_barsSinceExtreme = 0;
}
else
{
_barsSinceExtreme++;
}

var drop = _currentExtreme.Value - candle.LowPrice;
if (drop >= deviation && _barsSinceExtreme >= minBars)
{
AddPivot(_highPivots, _currentExtreme.Value, _currentExtremeBar);
_searchDirection = -1;
_currentExtreme = candle.LowPrice;
_currentExtremeBar = _barIndex;
_barsSinceExtreme = 0;
}
}
else
{
if (candle.LowPrice <= _currentExtreme.Value)
{
_currentExtreme = candle.LowPrice;
_currentExtremeBar = _barIndex;
_barsSinceExtreme = 0;
}
else
{
_barsSinceExtreme++;
}

var rise = candle.HighPrice - _currentExtreme.Value;
if (rise >= deviation && _barsSinceExtreme >= minBars)
{
AddPivot(_lowPivots, _currentExtreme.Value, _currentExtremeBar);
_searchDirection = 1;
_currentExtreme = candle.HighPrice;
_currentExtremeBar = _barIndex;
_barsSinceExtreme = 0;
}
}
}

private void AddPivot(List<ZigZagPivot> pivots, decimal price, int barIndex)
{
pivots.Insert(0, new ZigZagPivot(barIndex, price));
if (pivots.Count > MaxStoredPivots)
pivots.RemoveRange(MaxStoredPivots, pivots.Count - MaxStoredPivots);
}

private decimal? GetTrendlineValue(List<ZigZagPivot> pivots)
{
if (pivots.Count <= TrendAnchorIndex)
return null;

var recent = pivots[0];
var anchor = pivots[TrendAnchorIndex];

var barDistance = recent.BarIndex - anchor.BarIndex;
if (barDistance == 0)
return recent.Price;

var slope = (recent.Price - anchor.Price) / barDistance;
var barsAhead = _barIndex - recent.BarIndex;
return recent.Price + slope * barsAhead;
}

private decimal GetPointsValue(decimal points)
{
if (points <= 0m)
return 0m;

var step = _pipSize > 0m ? _pipSize : GetPipSize();
return step > 0m ? points * step : points;
}

private decimal GetPipSize()
{
var step = Security?.PriceStep ?? 0m;
if (step <= 0m)
step = Security?.MinPriceStep ?? 0m;
return step > 0m ? step : 1m;
}

private decimal AlignVolume(decimal volume)
{
if (volume <= 0m)
return 0m;

var security = Security;
if (security == null)
return volume;

var step = security.VolumeStep ?? 0m;
var min = security.VolumeMin ?? 0m;
var max = security.VolumeMax ?? decimal.MaxValue;

if (step > 0m)
volume = Math.Round(volume / step) * step;

if (min > 0m && volume < min)
volume = min;

if (max > 0m && volume > max)
volume = max;

return volume;
}

private decimal GetCurrentSpread()
{
if (!_hasBestBid || !_hasBestAsk || _bestBid is null || _bestAsk is null)
return 0m;

var spread = _bestAsk.Value - _bestBid.Value;
return spread > 0m ? spread : 0m;
}

/// <inheritdoc />
protected override void OnLevel1(Security security, Level1ChangeMessage message)
{
base.OnLevel1(security, message);

if (security != Security)
return;

if (message.Changes.TryGetValue(Level1Fields.BestBidPrice, out var bid) && bid is decimal bidPrice)
{
_bestBid = bidPrice;
_hasBestBid = true;
}

if (message.Changes.TryGetValue(Level1Fields.BestAskPrice, out var ask) && ask is decimal askPrice)
{
_bestAsk = askPrice;
_hasBestAsk = true;
}
}
}

