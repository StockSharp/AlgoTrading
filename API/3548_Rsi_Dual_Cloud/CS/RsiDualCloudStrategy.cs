using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Port of the MetaTrader strategy "RSI Dual Cloud EA".
/// Uses fast and slow RSI clouds to detect zone entries, exits, and line crossings.
/// Supports reversing the signals and restricting trading to long or short direction.
/// </summary>
public class RsiDualCloudStrategy : Strategy
{
private readonly StrategyParam<DataType> _candleType;
private readonly StrategyParam<int> _fastLength;
private readonly StrategyParam<int> _slowLength;
private readonly StrategyParam<decimal> _levelUp;
private readonly StrategyParam<decimal> _levelDown;
private readonly StrategyParam<decimal> _orderVolume;
private readonly StrategyParam<bool> _useEntranceSignal;
private readonly StrategyParam<bool> _useBeingSignal;
private readonly StrategyParam<bool> _useLeavingSignal;
private readonly StrategyParam<bool> _useCrossingSignal;
private readonly StrategyParam<bool> _useClosedCandles;
private readonly StrategyParam<bool> _reverseSignals;
private readonly StrategyParam<TradeModeOption> _tradeMode;

private RelativeStrengthIndex _fastRsi = null!;
private RelativeStrengthIndex _slowRsi = null!;

private decimal? _previousFastValue;
private decimal? _previousSlowValue;
private DateTimeOffset _lastProcessedTime;
private bool _isInitialized;

/// <summary>
/// Defines how the strategy may open positions.
/// </summary>
public enum TradeModeOption
{
Both,
LongOnly,
ShortOnly,
}

/// <summary>
/// Gets or sets the candle type used for calculations.
/// </summary>
public DataType CandleType
{
get => _candleType.Value;
set => _candleType.Value = value;
}

/// <summary>
/// Gets or sets the fast RSI period.
/// </summary>
public int FastLength
{
get => _fastLength.Value;
set => _fastLength.Value = value;
}

/// <summary>
/// Gets or sets the slow RSI period.
/// </summary>
public int SlowLength
{
get => _slowLength.Value;
set => _slowLength.Value = value;
}

/// <summary>
/// Upper RSI level used for short signals.
/// </summary>
public decimal LevelUp
{
get => _levelUp.Value;
set => _levelUp.Value = value;
}

/// <summary>
/// Lower RSI level used for long signals.
/// </summary>
public decimal LevelDown
{
get => _levelDown.Value;
set => _levelDown.Value = value;
}

/// <summary>
/// Trading volume used for market orders.
/// </summary>
public decimal OrderVolume
{
get => _orderVolume.Value;
set => _orderVolume.Value = value;
}

/// <summary>
/// Enables the "Entrance" condition (price enters the RSI zone).
/// </summary>
public bool UseEntranceSignal
{
get => _useEntranceSignal.Value;
set => _useEntranceSignal.Value = value;
}

/// <summary>
/// Enables the "Being" condition (price stays inside the RSI zone).
/// </summary>
public bool UseBeingSignal
{
get => _useBeingSignal.Value;
set => _useBeingSignal.Value = value;
}

/// <summary>
/// Enables the "Leaving" condition (price leaves the RSI zone).
/// </summary>
public bool UseLeavingSignal
{
get => _useLeavingSignal.Value;
set => _useLeavingSignal.Value = value;
}

/// <summary>
/// Enables the fast/slow RSI crossing condition.
/// </summary>
public bool UseCrossingSignal
{
get => _useCrossingSignal.Value;
set => _useCrossingSignal.Value = value;
}

/// <summary>
/// Determines whether only finished candles should be processed.
/// </summary>
public bool UseClosedCandles
{
get => _useClosedCandles.Value;
set => _useClosedCandles.Value = value;
}

/// <summary>
/// Reverses entry direction when enabled.
/// </summary>
public bool ReverseSignals
{
get => _reverseSignals.Value;
set => _reverseSignals.Value = value;
}

/// <summary>
/// Defines the allowed trade direction.
/// </summary>
public TradeModeOption TradeMode
{
get => _tradeMode.Value;
set => _tradeMode.Value = value;
}

/// <summary>
/// Initializes strategy parameters with default values and optimization ranges.
/// </summary>
public RsiDualCloudStrategy()
{
_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
.SetDisplay("Candle Type", "Primary candle series", "General");

_fastLength = Param(nameof(FastLength), 5)
.SetGreaterThanZero()
.SetDisplay("Fast RSI", "Fast RSI period", "RSI")
.SetCanOptimize(true)
.SetOptimize(3, 15, 2);

_slowLength = Param(nameof(SlowLength), 15)
.SetGreaterThanZero()
.SetDisplay("Slow RSI", "Slow RSI period", "RSI")
.SetCanOptimize(true)
.SetOptimize(10, 30, 5);

_levelUp = Param(nameof(LevelUp), 75m)
.SetDisplay("Upper Level", "RSI upper threshold", "Levels")
.SetCanOptimize(true)
.SetOptimize(60m, 90m, 5m);

_levelDown = Param(nameof(LevelDown), 25m)
.SetDisplay("Lower Level", "RSI lower threshold", "Levels")
.SetCanOptimize(true)
.SetOptimize(10m, 40m, 5m);

_orderVolume = Param(nameof(OrderVolume), 1m)
.SetGreaterThanZero()
.SetDisplay("Order Volume", "Volume for market orders", "Trading");

_useEntranceSignal = Param(nameof(UseEntranceSignal), false)
.SetDisplay("Use Entrance", "Trigger when RSI enters the zone", "Signals");

_useBeingSignal = Param(nameof(UseBeingSignal), true)
.SetDisplay("Use Being", "Trigger while RSI stays in the zone", "Signals");

_useLeavingSignal = Param(nameof(UseLeavingSignal), false)
.SetDisplay("Use Leaving", "Trigger when RSI leaves the zone", "Signals");

_useCrossingSignal = Param(nameof(UseCrossingSignal), false)
.SetDisplay("Use Crossing", "Trigger on fast/slow RSI cross", "Signals");

_useClosedCandles = Param(nameof(UseClosedCandles), true)
.SetDisplay("Closed Candles", "Process only finished candles", "General");

_reverseSignals = Param(nameof(ReverseSignals), false)
.SetDisplay("Reverse", "Invert the signal direction", "Trading");

_tradeMode = Param(nameof(TradeMode), TradeModeOption.Both)
.SetDisplay("Trade Mode", "Allowed trade direction", "Trading");
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

_previousFastValue = null;
_previousSlowValue = null;
_lastProcessedTime = default;
_isInitialized = false;
}

/// <inheritdoc />
protected override void OnStarted(DateTimeOffset time)
{
base.OnStarted(time);

// Initialize indicators for fast and slow RSI clouds.
_fastRsi = new RelativeStrengthIndex { Length = FastLength };
_slowRsi = new RelativeStrengthIndex { Length = SlowLength };

// Subscribe to candle data and bind indicators.
var subscription = SubscribeCandles(CandleType);
subscription
.Bind(_fastRsi, _slowRsi, ProcessCandle)
.Start();

// Set default strategy volume for convenience.
Volume = OrderVolume;
}

private void ProcessCandle(ICandleMessage candle, decimal fastValue, decimal slowValue)
{
// Skip unfinished candles when the user wants closed bars only.
if (UseClosedCandles && candle.State != CandleStates.Finished)
return;

// Ensure trading is allowed and indicators are ready.
if (!IsFormedAndOnlineAndAllowTrading())
return;

if (!_fastRsi.IsFormed || !_slowRsi.IsFormed)
return;

var previousFast = _previousFastValue;
var previousSlow = _previousSlowValue;

// Initialize previous values once indicators are formed.
if (!_isInitialized || previousFast is null || previousSlow is null)
{
_previousFastValue = fastValue;
_previousSlowValue = slowValue;
_lastProcessedTime = candle.OpenTime;
_isInitialized = true;
return;
}

// Detect whether we are working on a new candle to avoid multiple resets.
if (candle.OpenTime != _lastProcessedTime && UseClosedCandles)
{
previousFast = _previousFastValue;
previousSlow = _previousSlowValue;
}

var buySignal = CalculateBuySignal(previousFast.Value, previousSlow.Value, fastValue, slowValue);
var sellSignal = CalculateSellSignal(previousFast.Value, previousSlow.Value, fastValue, slowValue);

if (ReverseSignals)
{
(buySignal, sellSignal) = (sellSignal, buySignal);
}

if (buySignal)
{
HandleLongEntry();
}

if (sellSignal)
{
HandleShortEntry();
}

_previousFastValue = fastValue;
_previousSlowValue = slowValue;
_lastProcessedTime = candle.OpenTime;
}

private bool CalculateBuySignal(decimal previousFast, decimal previousSlow, decimal currentFast, decimal currentSlow)
{
// "Entrance" - RSI crosses from above the lower level.
if (UseEntranceSignal && previousFast > LevelDown && currentFast < LevelDown)
return true;

// "Being" - RSI stays below the lower level.
if (UseBeingSignal && currentFast < LevelDown)
return true;

// "Leaving" - RSI crosses from below to above the lower level.
if (UseLeavingSignal && previousFast < LevelDown && currentFast > LevelDown)
return true;

// "Crossing" - fast RSI crosses above slow RSI.
if (UseCrossingSignal && previousFast < previousSlow && currentFast > currentSlow)
return true;

return false;
}

private bool CalculateSellSignal(decimal previousFast, decimal previousSlow, decimal currentFast, decimal currentSlow)
{
// "Entrance" - RSI crosses from below the upper level.
if (UseEntranceSignal && previousFast < LevelUp && currentFast > LevelUp)
return true;

// "Being" - RSI stays above the upper level.
if (UseBeingSignal && currentFast > LevelUp)
return true;

// "Leaving" - RSI crosses from above to below the upper level.
if (UseLeavingSignal && previousFast > LevelUp && currentFast < LevelUp)
return true;

// "Crossing" - fast RSI crosses below slow RSI.
if (UseCrossingSignal && previousFast > previousSlow && currentFast < currentSlow)
return true;

return false;
}

private void HandleLongEntry()
{
// Respect trade mode restrictions.
if (TradeMode == TradeModeOption.ShortOnly)
return;

// Close a short position before opening a long one.
if (Position < 0)
ClosePosition();

if (Position <= 0)
BuyMarket(OrderVolume);
}

private void HandleShortEntry()
{
// Respect trade mode restrictions.
if (TradeMode == TradeModeOption.LongOnly)
return;

// Close a long position before opening a short one.
if (Position > 0)
ClosePosition();

if (Position >= 0)
SellMarket(OrderVolume);
}
}
