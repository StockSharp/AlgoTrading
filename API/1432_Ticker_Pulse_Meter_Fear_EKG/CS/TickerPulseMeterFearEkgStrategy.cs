using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Ticker Pulse Meter + Fear EKG strategy.
/// Combines short and long lookbacks to locate oversold zones and profit takes.
/// Enters long when percent above long and short crosses the upper threshold.
/// Exits when profit threshold is crossed downward.
/// </summary>
public class TickerPulseMeterFearEkgStrategy : Strategy
{
private readonly StrategyParam<int> _lookbackShort;
private readonly StrategyParam<int> _lookbackLong;
private readonly StrategyParam<int> _profitTake;
private readonly StrategyParam<int> _entryThresholdHigh;
private readonly StrategyParam<int> _entryThresholdLow;
private readonly StrategyParam<int> _orangeEntryThreshold;
private readonly StrategyParam<bool> _enableYellowSignals;
private readonly StrategyParam<bool> _enableExitSignal;
private readonly StrategyParam<DataType> _candleType;

private Highest _shortHigh;
private Lowest _shortLow;
private Highest _longHigh;
private Lowest _longLow;

private decimal _prevPctAboveLongAboveShort;
private decimal _prevPctBelowLongBelowShort;

/// <summary>
/// Short lookback period.
/// </summary>
public int LookbackShort
{
get => _lookbackShort.Value;
set => _lookbackShort.Value = value;
}

/// <summary>
/// Long lookback period.
/// </summary>
public int LookbackLong
{
get => _lookbackLong.Value;
set => _lookbackLong.Value = value;
}

/// <summary>
/// Profit take level.
/// </summary>
public int ProfitTake
{
get => _profitTake.Value;
set => _profitTake.Value = value;
}

/// <summary>
/// Cross above threshold.
/// </summary>
public int EntryThresholdHigh
{
get => _entryThresholdHigh.Value;
set => _entryThresholdHigh.Value = value;
}

/// <summary>
/// Cross below threshold.
/// </summary>
public int EntryThresholdLow
{
get => _entryThresholdLow.Value;
set => _entryThresholdLow.Value = value;
}

/// <summary>
/// Orange entry threshold.
/// </summary>
public int OrangeEntryThreshold
{
get => _orangeEntryThreshold.Value;
set => _orangeEntryThreshold.Value = value;
}

/// <summary>
/// Include orange signals.
/// </summary>
public bool EnableYellowSignals
{
get => _enableYellowSignals.Value;
set => _enableYellowSignals.Value = value;
}

/// <summary>
/// Allow exit signal.
/// </summary>
public bool EnableExitSignal
{
get => _enableExitSignal.Value;
set => _enableExitSignal.Value = value;
}

/// <summary>
/// Candle type to process.
/// </summary>
public DataType CandleType
{
get => _candleType.Value;
set => _candleType.Value = value;
}

/// <summary>
/// Initializes a new instance of <see cref="TickerPulseMeterFearEkgStrategy"/>.
/// </summary>
public TickerPulseMeterFearEkgStrategy()
{
_lookbackShort = Param(nameof(LookbackShort), 50)
.SetGreaterThanZero()
.SetDisplay("Short Lookback", "Short lookback period", "General")
.SetCanOptimize(true);

_lookbackLong = Param(nameof(LookbackLong), 200)
.SetGreaterThanZero()
.SetDisplay("Long Lookback", "Long lookback period", "General")
.SetCanOptimize(true);

_profitTake = Param(nameof(ProfitTake), 95)
.SetGreaterThanZero()
.SetDisplay("Profit Take", "Exit level", "General")
.SetCanOptimize(true);

_entryThresholdHigh = Param(nameof(EntryThresholdHigh), 20)
.SetGreaterThanZero()
.SetDisplay("Entry Threshold High", "Upper trigger", "General")
.SetCanOptimize(true);

_entryThresholdLow = Param(nameof(EntryThresholdLow), 40)
.SetGreaterThanZero()
.SetDisplay("Entry Threshold Low", "Lower trigger", "General")
.SetCanOptimize(true);

_orangeEntryThreshold = Param(nameof(OrangeEntryThreshold), 95)
.SetGreaterThanZero()
.SetDisplay("Orange Entry Threshold", "Irrational selling trigger", "General")
.SetCanOptimize(true);

_enableYellowSignals = Param(nameof(EnableYellowSignals), true)
.SetDisplay("Enable Yellow", "Include orange signals", "General");

_enableExitSignal = Param(nameof(EnableExitSignal), true)
.SetDisplay("Enable Exit", "Allow exit signal", "General");

_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
.SetDisplay("Candle Type", "Type of candles", "General");
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

_shortHigh = default;
_shortLow = default;
_longHigh = default;
_longLow = default;
_prevPctAboveLongAboveShort = default;
_prevPctBelowLongBelowShort = default;
}

/// <inheritdoc />
protected override void OnStarted(DateTimeOffset time)
{
base.OnStarted(time);

_shortHigh = new Highest { Length = LookbackShort };
_shortLow = new Lowest { Length = LookbackShort };
_longHigh = new Highest { Length = LookbackLong };
_longLow = new Lowest { Length = LookbackLong };

var subscription = SubscribeCandles(CandleType);
subscription.WhenNew(ProcessCandle).Start();
}

private void ProcessCandle(ICandleMessage candle)
{
if (candle.State != CandleStates.Finished)
return;

var shortHighVal = _shortHigh.Process(candle.HighPrice);
var shortLowVal = _shortLow.Process(candle.LowPrice);
var longHighVal = _longHigh.Process(candle.HighPrice);
var longLowVal = _longLow.Process(candle.LowPrice);

if (!shortHighVal.IsFinal || !shortLowVal.IsFinal || !longHighVal.IsFinal || !longLowVal.IsFinal)
return;

var shortHigh = shortHighVal.ToDecimal();
var shortLow = shortLowVal.ToDecimal();
var longHigh = longHighVal.ToDecimal();
var longLow = longLowVal.ToDecimal();

var denomShort = shortHigh - shortLow;
var denomLong = longHigh - longLow;
if (denomShort == 0 || denomLong == 0)
return;

var pctAboveShort = (candle.ClosePrice - shortLow) / denomShort;
var pctAboveLong = (candle.ClosePrice - longLow) / denomLong;

var pctAboveLongAboveShort = pctAboveLong * pctAboveShort * 100m;
var pctBelowLongBelowShort = (1m - pctAboveLong) * (1m - pctAboveShort) * 100m;

var crossPctAbove = _prevPctAboveLongAboveShort <= EntryThresholdHigh && pctAboveLongAboveShort > EntryThresholdHigh;
var crossUpper2 = _prevPctBelowLongBelowShort >= OrangeEntryThreshold && pctBelowLongBelowShort < OrangeEntryThreshold;
var tpm = crossPctAbove && _prevPctBelowLongBelowShort < EntryThresholdLow;
var longEntry = tpm || (crossUpper2 && EnableYellowSignals);
var longExit = _prevPctAboveLongAboveShort >= ProfitTake && pctAboveLongAboveShort < ProfitTake;

if (!IsFormedAndOnlineAndAllowTrading())
{
_prevPctAboveLongAboveShort = pctAboveLongAboveShort;
_prevPctBelowLongBelowShort = pctBelowLongBelowShort;
return;
}

if (longEntry && Position <= 0)
{
BuyMarket();
}
else if (EnableExitSignal && longExit && Position > 0)
{
SellMarket(Position);
}

_prevPctAboveLongAboveShort = pctAboveLongAboveShort;
_prevPctBelowLongBelowShort = pctBelowLongBelowShort;
}
}
