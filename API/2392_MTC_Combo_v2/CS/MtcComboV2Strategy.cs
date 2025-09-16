using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Simplified MTC Combo v2 strategy.
/// </summary>
public class MtcComboV2Strategy : Strategy
{
private readonly StrategyParam<int> _maPeriod;
private readonly StrategyParam<int> _p2;
private readonly StrategyParam<int> _p3;
private readonly StrategyParam<int> _p4;
private readonly StrategyParam<int> _pass;
private readonly StrategyParam<decimal> _sl1;
private readonly StrategyParam<decimal> _tp1;
private readonly StrategyParam<decimal> _sl2;
private readonly StrategyParam<decimal> _tp2;
private readonly StrategyParam<decimal> _sl3;
private readonly StrategyParam<decimal> _tp3;
private readonly StrategyParam<DataType> _candleType;

private SimpleMovingAverage _ma;
private decimal? _prevMa;
private readonly Queue<decimal> _opens = new();
private decimal _entry;
private decimal _sl;
private decimal _tp;

public int MaPeriod { get => _maPeriod.Value; set => _maPeriod.Value = value; }
public int P2 { get => _p2.Value; set => _p2.Value = value; }
public int P3 { get => _p3.Value; set => _p3.Value = value; }
public int P4 { get => _p4.Value; set => _p4.Value = value; }
public int Pass { get => _pass.Value; set => _pass.Value = value; }
public decimal Sl1 { get => _sl1.Value; set => _sl1.Value = value; }
public decimal Tp1 { get => _tp1.Value; set => _tp1.Value = value; }
public decimal Sl2 { get => _sl2.Value; set => _sl2.Value = value; }
public decimal Tp2 { get => _tp2.Value; set => _tp2.Value = value; }
public decimal Sl3 { get => _sl3.Value; set => _sl3.Value = value; }
public decimal Tp3 { get => _tp3.Value; set => _tp3.Value = value; }
public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

public MtcComboV2Strategy()
{
_maPeriod = Param(nameof(MaPeriod), 2).SetGreaterThanZero();
_p2 = Param(nameof(P2), 20).SetGreaterThanZero();
_p3 = Param(nameof(P3), 20).SetGreaterThanZero();
_p4 = Param(nameof(P4), 20).SetGreaterThanZero();
_pass = Param(nameof(Pass), 10);
_sl1 = Param(nameof(Sl1), 50m);
_tp1 = Param(nameof(Tp1), 50m);
_sl2 = Param(nameof(Sl2), 50m);
_tp2 = Param(nameof(Tp2), 50m);
_sl3 = Param(nameof(Sl3), 50m);
_tp3 = Param(nameof(Tp3), 50m);
_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame());
Volume = 1m;
}

public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities() =>
[(Security, CandleType)];

protected override void OnReseted()
{
base.OnReseted();
_ma = default;
_prevMa = null;
_opens.Clear();
_entry = 0m;
_sl = 0m;
_tp = 0m;
}

protected override void OnStarted(DateTimeOffset time)
{
base.OnStarted(time);
StartProtection();
_ma = new SimpleMovingAverage { Length = MaPeriod };
var sub = SubscribeCandles(CandleType);
sub.Bind(_ma, ProcessCandle).Start();
var area = CreateChartArea();
if (area != null)
{
DrawCandles(area, sub);
DrawIndicator(area, _ma);
DrawOwnTrades(area);
}
}

private void ProcessCandle(ICandleMessage candle, decimal maValue)
{
if (candle.State != CandleStates.Finished)
return;

_opens.Enqueue(candle.OpenPrice);
var max = Math.Max(Math.Max(P2, P3), P4) * 4 + 5;
while (_opens.Count > max)
_opens.Dequeue();

if (Position != 0)
{
var step = Security.PriceStep ?? 1m;
var stop = Position > 0 ? _entry - _sl * step : _entry + _sl * step;
var take = Position > 0 ? _entry + _tp * step : _entry - _tp * step;
if ((Position > 0 && candle.LowPrice <= stop) || (Position < 0 && candle.HighPrice >= stop))
{
ClosePos();
return;
}
if ((Position > 0 && candle.HighPrice >= take) || (Position < 0 && candle.LowPrice <= take))
{
ClosePos();
return;
}
}

var slope = _prevMa is null ? 0m : maValue - _prevMa.Value;
_prevMa = maValue;

if (Position != 0)
return;

_sl = Sl1;
_tp = Tp1;
var dir = Supervisor(slope);
if (dir > 0m)
{
BuyMarket(Volume);
_entry = candle.ClosePrice;
}
else if (dir < 0m)
{
SellMarket(Volume);
_entry = candle.ClosePrice;
}
}

private void ClosePos()
{
if (Position > 0) SellMarket(Position);
else if (Position < 0) BuyMarket(-Position);
}

private decimal Supervisor(decimal slope)
{
if (Pass == 4)
{
if (Perceptron(P4) > 0m && Perceptron(P3) > 0m)
{ _sl = Sl3; _tp = Tp3; return 1m; }
if (Perceptron(P4) <= 0m && Perceptron(P2) < 0m)
{ _sl = Sl2; _tp = Tp2; return -1m; }
}
else if (Pass == 3)
{
if (Perceptron(P3) > 0m)
{ _sl = Sl3; _tp = Tp3; return 1m; }
}
else if (Pass == 2)
{
if (Perceptron(P2) < 0m)
{ _sl = Sl2; _tp = Tp2; return -1m; }
}
return slope;
}

private decimal Perceptron(int p)
{
if (_opens.Count <= p * 4)
return 0m;

var arr = _opens.ToArray();
var a1 = arr[^1] - arr[^1 - p];
var a2 = arr[^1 - p] - arr[^1 - p * 2];
var a3 = arr[^1 - p * 2] - arr[^1 - p * 3];
var a4 = arr[^1 - p * 3] - arr[^1 - p * 4];

return a1 + a2 + a3 + a4;
}
