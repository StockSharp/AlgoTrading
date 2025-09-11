using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Dubic EMA strategy.
/// </summary>
public class DubicEmaStrategy : Strategy
{
private readonly StrategyParam<int> _emaLength;
private readonly StrategyParam<int> _rangeLength;
private readonly StrategyParam<decimal> _rangeThreshold;
private readonly StrategyParam<int> _minRangeBars;
private readonly StrategyParam<int> _atrLength;
private readonly StrategyParam<decimal> _atrMultiplier;
private readonly StrategyParam<decimal> _minAtrPercent;
private readonly StrategyParam<decimal> _takeProfitPercent;
private readonly StrategyParam<decimal> _stopLossOffset;
private readonly StrategyParam<bool> _useAtrStop;
private readonly StrategyParam<bool> _useVolatilityFilter;
private readonly StrategyParam<bool> _useParabolicSar;
private readonly StrategyParam<decimal> _sarStep;
private readonly StrategyParam<decimal> _sarMax;
private readonly StrategyParam<DataType> _candleType;
private EMA _emaHigh;
private EMA _emaLow;
private ATR _atr;
private Highest _highest;
private Lowest _lowest;
private ParabolicSar _sar;

private int _rangeBars;
private bool _isReady;
private decimal _longStop;
private decimal _longTake;
private decimal _shortStop;
private decimal _shortTake;

public int EmaLength { get => _emaLength.Value; set => _emaLength.Value = value; }
public int RangeLength { get => _rangeLength.Value; set => _rangeLength.Value = value; }
public decimal RangeThresholdPercent { get => _rangeThreshold.Value; set => _rangeThreshold.Value = value; }
public int MinRangeBars { get => _minRangeBars.Value; set => _minRangeBars.Value = value; }
public int AtrLength { get => _atrLength.Value; set => _atrLength.Value = value; }
public decimal AtrMultiplier { get => _atrMultiplier.Value; set => _atrMultiplier.Value = value; }
public decimal MinAtrPercent { get => _minAtrPercent.Value; set => _minAtrPercent.Value = value; }
public decimal TakeProfitPercent { get => _takeProfitPercent.Value; set => _takeProfitPercent.Value = value; }
public decimal StopLossOffsetPercent { get => _stopLossOffset.Value; set => _stopLossOffset.Value = value; }
public bool UseAtrStop { get => _useAtrStop.Value; set => _useAtrStop.Value = value; }
public bool UseVolatilityFilter { get => _useVolatilityFilter.Value; set => _useVolatilityFilter.Value = value; }
public bool UseParabolicSar { get => _useParabolicSar.Value; set => _useParabolicSar.Value = value; }
public decimal SarStep { get => _sarStep.Value; set => _sarStep.Value = value; }
public decimal SarMax { get => _sarMax.Value; set => _sarMax.Value = value; }
public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
public DubicEmaStrategy()
{
_emaLength = Param(nameof(EmaLength), 40).SetGreaterThanZero();
_rangeLength = Param(nameof(RangeLength), 20).SetGreaterThanZero();
_rangeThreshold = Param(nameof(RangeThresholdPercent), 2m).SetGreaterThanZero();
_minRangeBars = Param(nameof(MinRangeBars), 3).SetGreaterThanZero();
_atrLength = Param(nameof(AtrLength), 14).SetGreaterThanZero();
_atrMultiplier = Param(nameof(AtrMultiplier), 1.5m).SetGreaterThanZero();
_minAtrPercent = Param(nameof(MinAtrPercent), 0.5m).SetGreaterThanZero();
_takeProfitPercent = Param(nameof(TakeProfitPercent), 2m).SetGreaterThanZero();
_stopLossOffset = Param(nameof(StopLossOffsetPercent), 0.5m).SetGreaterThanZero();
_useAtrStop = Param(nameof(UseAtrStop), true);
_useVolatilityFilter = Param(nameof(UseVolatilityFilter), true);
_useParabolicSar = Param(nameof(UseParabolicSar), true);
_sarStep = Param(nameof(SarStep), 0.02m).SetGreaterThanZero();
_sarMax = Param(nameof(SarMax), 0.2m).SetGreaterThanZero();
_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame());
}
public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
{
return [(Security, CandleType)];
}

protected override void OnReseted()
{
base.OnReseted();
_rangeBars = 0;
_isReady = false;
_longStop = _longTake = _shortStop = _shortTake = 0m;
}
protected override void OnStarted(DateTimeOffset time)
{
base.OnStarted(time);

_emaHigh = new EMA { Length = EmaLength };
_emaLow = new EMA { Length = EmaLength };
_atr = new ATR { Length = AtrLength };
_highest = new Highest { Length = RangeLength };
_lowest = new Lowest { Length = RangeLength };
_sar = new ParabolicSar { AccelerationStep = SarStep, AccelerationMax = SarMax };

var sub = SubscribeCandles(CandleType);
sub.Bind(_emaHigh, _emaLow, _atr, _sar, _highest, _lowest, Process).Start();

var area = CreateChartArea();
if (area != null)
{
DrawCandles(area, sub);
DrawIndicator(area, _emaHigh);
DrawIndicator(area, _emaLow);
DrawIndicator(area, _sar);
DrawOwnTrades(area);
}
}
private void Process(ICandleMessage candle, decimal emaHigh, decimal emaLow, decimal atr, decimal sar, decimal high, decimal low)
{
if (candle.State != CandleStates.Finished)
return;

if (!_isReady)
{
if (_emaHigh.IsFormed && _emaLow.IsFormed && _atr.IsFormed && _highest.IsFormed && _lowest.IsFormed)
_isReady = true;
else
return;
}

if (!IsFormedAndOnlineAndAllowTrading())
return;

var range = high - low;
var rangePercent = candle.ClosePrice != 0 ? range / candle.ClosePrice * 100m : 0m;
_rangeBars = rangePercent <= RangeThresholdPercent ? _rangeBars + 1 : 0;
var inRange = _rangeBars >= MinRangeBars;

var atrPercent = candle.ClosePrice != 0 ? atr / candle.ClosePrice * 100m : 0m;
var volOk = !UseVolatilityFilter || atrPercent >= MinAtrPercent;

var buy = candle.ClosePrice > emaHigh && candle.ClosePrice > emaLow && !inRange && volOk;
var sell = candle.ClosePrice < emaHigh && candle.ClosePrice < emaLow && !inRange && volOk;
if (buy && Position <= 0)
{
BuyMarket(Volume + Math.Abs(Position));
_longStop = UseAtrStop ? candle.ClosePrice - atr * AtrMultiplier : emaLow * (1m - StopLossOffsetPercent / 100m);
_longTake = candle.ClosePrice * (1m + TakeProfitPercent / 100m);
}
else if (sell && Position >= 0)
{
SellMarket(Volume + Math.Abs(Position));
_shortStop = UseAtrStop ? candle.ClosePrice + atr * AtrMultiplier : emaHigh * (1m + StopLossOffsetPercent / 100m);
_shortTake = candle.ClosePrice * (1m - TakeProfitPercent / 100m);
}
if (Position > 0)
{
if (UseParabolicSar && candle.LowPrice <= sar || candle.LowPrice <= _longStop || candle.HighPrice >= _longTake)
SellMarket(Math.Abs(Position));
}
else if (Position < 0)
{
if (UseParabolicSar && candle.HighPrice >= sar || candle.HighPrice >= _shortStop || candle.LowPrice <= _shortTake)
BuyMarket(Math.Abs(Position));
}
}
}
