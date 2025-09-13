using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Multi-timeframe RSI SAR strategy with optional Bollinger Band trigger.
/// </summary>
public class MtfRsiSarStrategy : Strategy
{
private readonly StrategyParam<bool> _useRsi;
private readonly StrategyParam<bool> _useBollinger;
private readonly StrategyParam<bool> _useSar;
private readonly StrategyParam<int> _rsiPeriod;
private readonly StrategyParam<int> _bollingerPeriod;
private readonly StrategyParam<decimal> _bollingerWidth;
private readonly StrategyParam<decimal> _sarStep;
private readonly StrategyParam<decimal> _sarMax;
private readonly StrategyParam<DataType> _candleType;

private decimal? _rsi5;
private decimal? _rsi15;
private decimal? _rsi30;
private decimal? _rsi60;
private decimal? _sar5;
private decimal? _sar15;
private decimal? _sar30;
private decimal? _upperBand;
private decimal? _lowerBand;

public bool UseRsi { get => _useRsi.Value; set => _useRsi.Value = value; }
public bool UseBollinger { get => _useBollinger.Value; set => _useBollinger.Value = value; }
public bool UseSar { get => _useSar.Value; set => _useSar.Value = value; }
public int RsiPeriod { get => _rsiPeriod.Value; set => _rsiPeriod.Value = value; }
public int BollingerPeriod { get => _bollingerPeriod.Value; set => _bollingerPeriod.Value = value; }
public decimal BollingerWidth { get => _bollingerWidth.Value; set => _bollingerWidth.Value = value; }
public decimal SarStep { get => _sarStep.Value; set => _sarStep.Value = value; }
public decimal SarMax { get => _sarMax.Value; set => _sarMax.Value = value; }
public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

public MtfRsiSarStrategy()
{
_useRsi = Param(nameof(UseRsi), true)
.SetDisplay("Use RSI", "Enable RSI filter", "Switches");

_useBollinger = Param(nameof(UseBollinger), true)
.SetDisplay("Use Bollinger", "Enable Bollinger Band trigger", "Switches");

_useSar = Param(nameof(UseSar), true)
.SetDisplay("Use SAR", "Enable Parabolic SAR filter", "Switches");

_rsiPeriod = Param(nameof(RsiPeriod), 14)
.SetDisplay("RSI Period", "Period for RSI", "Indicators");

_bollingerPeriod = Param(nameof(BollingerPeriod), 20)
.SetDisplay("BB Period", "Bollinger Bands period", "Indicators");

_bollingerWidth = Param(nameof(BollingerWidth), 2m)
.SetDisplay("BB Width", "Bollinger Bands width", "Indicators");

_sarStep = Param(nameof(SarStep), 0.02m)
.SetDisplay("SAR Step", "Parabolic SAR acceleration factor", "Indicators");

_sarMax = Param(nameof(SarMax), 0.2m)
.SetDisplay("SAR Max", "Parabolic SAR maximum acceleration", "Indicators");

_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
.SetDisplay("Candle Type", "Base candle timeframe", "General");
}

public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
{
yield return (Security, CandleType);
yield return (Security, TimeSpan.FromMinutes(15).TimeFrame());
yield return (Security, TimeSpan.FromMinutes(30).TimeFrame());
yield return (Security, TimeSpan.FromMinutes(60).TimeFrame());
}

protected override void OnStarted(DateTimeOffset time)
{
base.OnStarted(time);

var bb = new BollingerBands { Length = BollingerPeriod, Width = BollingerWidth };
var rsi5 = new RSI { Length = RsiPeriod };
var sar5 = new ParabolicSar { Acceleration = SarStep, MaxAcceleration = SarMax };
var rsi15 = new RSI { Length = RsiPeriod };
var sar15 = new ParabolicSar { Acceleration = SarStep, MaxAcceleration = SarMax };
var rsi30 = new RSI { Length = RsiPeriod };
var sar30 = new ParabolicSar { Acceleration = SarStep, MaxAcceleration = SarMax };
var rsi60 = new RSI { Length = RsiPeriod };

var sub5 = SubscribeCandles(CandleType);
sub5.Bind(bb, rsi5, sar5, ProcessFive).Start();

var sub15 = SubscribeCandles(TimeSpan.FromMinutes(15).TimeFrame());
sub15.Bind(rsi15, sar15, ProcessFifteen).Start();

var sub30 = SubscribeCandles(TimeSpan.FromMinutes(30).TimeFrame());
sub30.Bind(rsi30, sar30, ProcessThirty).Start();

var sub60 = SubscribeCandles(TimeSpan.FromMinutes(60).TimeFrame());
sub60.Bind(rsi60, ProcessSixty).Start();
}

private void ProcessFive(ICandleMessage candle, decimal middle, decimal upper, decimal lower, decimal rsi, decimal sar)
{
if (candle.State != CandleStates.Finished)
return;

_upperBand = upper;
_lowerBand = lower;
_rsi5 = rsi;
_sar5 = sar;

TryTrade(candle);
}

private void ProcessFifteen(ICandleMessage candle, decimal rsi, decimal sar)
{
if (candle.State != CandleStates.Finished)
return;

_rsi15 = rsi;
_sar15 = sar;
}

private void ProcessThirty(ICandleMessage candle, decimal rsi, decimal sar)
{
if (candle.State != CandleStates.Finished)
return;

_rsi30 = rsi;
_sar30 = sar;
}

private void ProcessSixty(ICandleMessage candle, decimal rsi)
{
if (candle.State != CandleStates.Finished)
return;

_rsi60 = rsi;
}

private void TryTrade(ICandleMessage candle)
{
if (UseRsi && (!(_rsi5.HasValue && _rsi15.HasValue && _rsi30.HasValue && _rsi60.HasValue)))
return;
if (UseSar && (!(_sar5.HasValue && _sar15.HasValue && _sar30.HasValue)))
return;
if (UseBollinger && (!(_upperBand.HasValue && _lowerBand.HasValue)))
return;

var bullRsi = !UseRsi || (_rsi5 > 50 && _rsi15 > 50 && _rsi30 > 50 && _rsi60 > 50);
var bearRsi = !UseRsi || (_rsi5 < 50 && _rsi15 < 50 && _rsi30 < 50 && _rsi60 < 50);
var bullBb = !UseBollinger || candle.ClosePrice >= _upperBand;
var bearBb = !UseBollinger || candle.ClosePrice <= _lowerBand;
var bullSar = !UseSar || (_sar5 < candle.LowPrice && _sar15 < candle.LowPrice && _sar30 < candle.LowPrice);
var bearSar = !UseSar || (_sar5 > candle.HighPrice && _sar15 > candle.HighPrice && _sar30 > candle.HighPrice);

var buy = bullRsi && bullBb && bullSar;
var sell = bearRsi && bearBb && bearSar;

if (buy && Position <= 0)
BuyMarket();
else if (sell && Position >= 0)
SellMarket();
}
}
