using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Ichimoku Clouds strategy allowing long or short modes with customizable signal strength filters and optional take-profit / stop-loss levels.
/// Entry is triggered by Tenkan-sen crossing Kijun-sen and classified as strong, neutral or weak depending on the cloud position.
/// </summary>
public class IchimokuCloudsStrategyLongAndShortStrategy : Strategy
{
private readonly StrategyParam<int> _tenkanPeriod;
private readonly StrategyParam<int> _kijunPeriod;
private readonly StrategyParam<int> _senkouSpanPeriod;
private readonly StrategyParam<decimal> _takeProfitPct;
private readonly StrategyParam<decimal> _stopLossPct;
private readonly StrategyParam<string> _tradingMode;
private readonly StrategyParam<string> _entrySignalOptionsLong;
private readonly StrategyParam<string> _exitSignalOptionsLong;
private readonly StrategyParam<string> _entrySignalOptionsShort;
private readonly StrategyParam<string> _exitSignalOptionsShort;
private readonly StrategyParam<DataType> _candleType;

private decimal _prevTenkan;
private decimal _prevKijun;

/// <summary>
/// Tenkan-sen period.
/// </summary>
public int TenkanPeriod
{
get => _tenkanPeriod.Value;
set => _tenkanPeriod.Value = value;
}

/// <summary>
/// Kijun-sen period.
/// </summary>
public int KijunPeriod
{
get => _kijunPeriod.Value;
set => _kijunPeriod.Value = value;
}

/// <summary>
/// Senkou Span B period.
/// </summary>
public int SenkouSpanPeriod
{
get => _senkouSpanPeriod.Value;
set => _senkouSpanPeriod.Value = value;
}

/// <summary>
/// Take profit percentage.
/// </summary>
public decimal TakeProfitPct
{
get => _takeProfitPct.Value;
set => _takeProfitPct.Value = value;
}

/// <summary>
/// Stop loss percentage.
/// </summary>
public decimal StopLossPct
{
get => _stopLossPct.Value;
set => _stopLossPct.Value = value;
}

/// <summary>
/// Trading mode (Long or Short).
/// </summary>
public string TradingMode
{
get => _tradingMode.Value;
set => _tradingMode.Value = value;
}

/// <summary>
/// Entry signal options when trading long.
/// </summary>
public string EntrySignalOptionsLong
{
get => _entrySignalOptionsLong.Value;
set => _entrySignalOptionsLong.Value = value;
}

/// <summary>
/// Exit signal options when trading long.
/// </summary>
public string ExitSignalOptionsLong
{
get => _exitSignalOptionsLong.Value;
set => _exitSignalOptionsLong.Value = value;
}

/// <summary>
/// Entry signal options when trading short.
/// </summary>
public string EntrySignalOptionsShort
{
get => _entrySignalOptionsShort.Value;
set => _entrySignalOptionsShort.Value = value;
}

/// <summary>
/// Exit signal options when trading short.
/// </summary>
public string ExitSignalOptionsShort
{
get => _exitSignalOptionsShort.Value;
set => _exitSignalOptionsShort.Value = value;
}

/// <summary>
/// Candle type used for the strategy.
/// </summary>
public DataType CandleType
{
get => _candleType.Value;
set => _candleType.Value = value;
}

/// <summary>
/// Initialize Ichimoku Clouds Long/Short strategy.
/// </summary>
public IchimokuCloudsStrategyLongAndShortStrategy()
{
_tenkanPeriod = Param(nameof(TenkanPeriod), 9)
.SetGreaterThanZero()
.SetDisplay("Tenkan Period", "Tenkan-sen period", "Indicators");

_kijunPeriod = Param(nameof(KijunPeriod), 26)
.SetGreaterThanZero()
.SetDisplay("Kijun Period", "Kijun-sen period", "Indicators");

_senkouSpanPeriod = Param(nameof(SenkouSpanPeriod), 52)
.SetGreaterThanZero()
.SetDisplay("Senkou Span Period", "Senkou Span B period", "Indicators");

_takeProfitPct = Param(nameof(TakeProfitPct), 0m)
.SetDisplay("Take Profit %", "Take profit percentage (0 - disabled)", "Risk Management");

_stopLossPct = Param(nameof(StopLossPct), 0m)
.SetDisplay("Stop Loss %", "Stop loss percentage (0 - disabled)", "Risk Management");

_tradingMode = Param(nameof(TradingMode), "Long")
.SetDisplay("Trading Mode", "Trade direction: Long or Short", "General");

_entrySignalOptionsLong = Param(nameof(EntrySignalOptionsLong), "Bullish All")
.SetDisplay("Entry Signal (Long)", "Entry signal filter for long mode", "Long Mode Signals");

_exitSignalOptionsLong = Param(nameof(ExitSignalOptionsLong), "Bearish Weak")
.SetDisplay("Exit Signal (Long)", "Exit signal filter for long mode", "Long Mode Signals");

_entrySignalOptionsShort = Param(nameof(EntrySignalOptionsShort), "None")
.SetDisplay("Entry Signal (Short)", "Entry signal filter for short mode", "Short Mode Signals");

_exitSignalOptionsShort = Param(nameof(ExitSignalOptionsShort), "None")
.SetDisplay("Exit Signal (Short)", "Exit signal filter for short mode", "Short Mode Signals");

_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
.SetDisplay("Candle Type", "Candle type for the strategy", "General");
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
_prevTenkan = 0;
_prevKijun = 0;
}

/// <inheritdoc />
protected override void OnStarted(DateTimeOffset time)
{
base.OnStarted(time);

var ichimoku = new Ichimoku
{
Tenkan = { Length = TenkanPeriod },
Kijun = { Length = KijunPeriod },
SenkouB = { Length = SenkouSpanPeriod }
};

var subscription = SubscribeCandles(CandleType);
subscription
.BindEx(ichimoku, ProcessCandle)
.Start();

var area = CreateChartArea();
if (area != null)
{
DrawCandles(area, subscription);
DrawIndicator(area, ichimoku);
DrawOwnTrades(area);
}
}

private enum SignalStrength
{
Strong,
Neutral,
Weak
}

private static bool IsSignalAllowed(string option, SignalStrength strength, bool bullish)
{
if (option == "None")
return false;

if (bullish)
{
return option switch
{
"Bullish Strong" => strength == SignalStrength.Strong,
"Bullish Neutral" => strength == SignalStrength.Neutral,
"Bullish Weak" => strength == SignalStrength.Weak,
"Bullish Strong and Neutral" => strength is SignalStrength.Strong or SignalStrength.Neutral,
"Bullish Neutral and Weak" => strength is SignalStrength.Neutral or SignalStrength.Weak,
"Bullish Strong and Weak" => strength is SignalStrength.Strong or SignalStrength.Weak,
"Bullish All" => true,
_ => false
};
}

return option switch
{
"Bearish Strong" => strength == SignalStrength.Strong,
"Bearish Neutral" => strength == SignalStrength.Neutral,
"Bearish Weak" => strength == SignalStrength.Weak,
"Bearish Strong and Neutral" => strength is SignalStrength.Strong or SignalStrength.Neutral,
"Bearish Neutral and Weak" => strength is SignalStrength.Neutral or SignalStrength.Weak,
"Bearish Strong and Weak" => strength is SignalStrength.Strong or SignalStrength.Weak,
"Bearish All" => true,
_ => false
};
}

private void ProcessCandle(ICandleMessage candle, IIndicatorValue ichimokuValue)
{
if (candle.State != CandleStates.Finished)
return;

if (!IsFormedAndOnlineAndAllowTrading())
return;

var ichimoku = (IchimokuValue)ichimokuValue;

if (ichimoku.Tenkan is not decimal tenkan)
return;

if (ichimoku.Kijun is not decimal kijun)
return;

if (ichimoku.SenkouA is not decimal senkouA)
return;

if (ichimoku.SenkouB is not decimal senkouB)
return;

var upperCloud = Math.Max(senkouA, senkouB);
var lowerCloud = Math.Min(senkouA, senkouB);

var crossUp = tenkan > kijun && _prevTenkan <= _prevKijun;
var crossDown = tenkan < kijun && _prevTenkan >= _prevKijun;

if (crossUp)
{
var strength = tenkan > upperCloud ? SignalStrength.Strong : tenkan < lowerCloud ? SignalStrength.Weak : SignalStrength.Neutral;

if (TradingMode == "Long" && IsSignalAllowed(EntrySignalOptionsLong, strength, true) && Position <= 0)
{
var volume = Volume + Math.Abs(Position);
BuyMarket(volume);
}
else if (TradingMode == "Short" && IsSignalAllowed(ExitSignalOptionsShort, strength, true) && Position < 0)
{
BuyMarket(Math.Abs(Position));
}
}
else if (crossDown)
{
var strength = tenkan < lowerCloud ? SignalStrength.Strong : tenkan > upperCloud ? SignalStrength.Weak : SignalStrength.Neutral;

if (TradingMode == "Short" && IsSignalAllowed(EntrySignalOptionsShort, strength, false) && Position >= 0)
{
var volume = Volume + Math.Abs(Position);
SellMarket(volume);
}
else if (TradingMode == "Long" && IsSignalAllowed(ExitSignalOptionsLong, strength, false) && Position > 0)
{
SellMarket(Position);
}
}

if (Position > 0)
{
if (TakeProfitPct > 0 && candle.ClosePrice >= PositionPrice * (1 + TakeProfitPct / 100m))
SellMarket(Position);
else if (StopLossPct > 0 && candle.ClosePrice <= PositionPrice * (1 - StopLossPct / 100m))
SellMarket(Position);
}
else if (Position < 0)
{
var shortPos = Math.Abs(Position);
if (TakeProfitPct > 0 && candle.ClosePrice <= PositionPrice * (1 - TakeProfitPct / 100m))
BuyMarket(shortPos);
else if (StopLossPct > 0 && candle.ClosePrice >= PositionPrice * (1 + StopLossPct / 100m))
BuyMarket(shortPos);
}

_prevTenkan = tenkan;
_prevKijun = kijun;
}
}
