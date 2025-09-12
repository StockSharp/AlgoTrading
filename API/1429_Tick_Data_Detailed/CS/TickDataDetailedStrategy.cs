using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Collects detailed tick volume statistics across predefined ranges.
/// </summary>
public class TickDataDetailedStrategy : Strategy
{
private readonly StrategyParam<decimal> _volumeLessThan;
private readonly StrategyParam<decimal> _volume2From;
private readonly StrategyParam<decimal> _volume2To;
private readonly StrategyParam<decimal> _volume3From;
private readonly StrategyParam<decimal> _volume3To;
private readonly StrategyParam<decimal> _volume4From;
private readonly StrategyParam<decimal> _volume4To;
private readonly StrategyParam<decimal> _volume5From;
private readonly StrategyParam<decimal> _volume5To;
private readonly StrategyParam<decimal> _volume6From;
private readonly StrategyParam<decimal> _volume6To;
private readonly StrategyParam<decimal> _volumeGreaterThan;

private decimal _lastPrice;

private decimal _buyVol1, _buyVol2, _buyVol3, _buyVol4, _buyVol5, _buyVol6, _buyVol7;
private decimal _sellVol1, _sellVol2, _sellVol3, _sellVol4, _sellVol5, _sellVol6, _sellVol7;
private int _buyTick1, _buyTick2, _buyTick3, _buyTick4, _buyTick5, _buyTick6, _buyTick7;
private int _sellTick1, _sellTick2, _sellTick3, _sellTick4, _sellTick5, _sellTick6, _sellTick7;

/// <summary>
/// Volume less than this value belongs to the first bucket.
/// </summary>
public decimal VolumeLessThan
{
get => _volumeLessThan.Value;
set => _volumeLessThan.Value = value;
}

/// <summary>
/// Second bucket lower bound.
/// </summary>
public decimal Volume2From
{
get => _volume2From.Value;
set => _volume2From.Value = value;
}

/// <summary>
/// Second bucket upper bound.
/// </summary>
public decimal Volume2To
{
get => _volume2To.Value;
set => _volume2To.Value = value;
}

/// <summary>
/// Third bucket lower bound.
/// </summary>
public decimal Volume3From
{
get => _volume3From.Value;
set => _volume3From.Value = value;
}

/// <summary>
/// Third bucket upper bound.
/// </summary>
public decimal Volume3To
{
get => _volume3To.Value;
set => _volume3To.Value = value;
}

/// <summary>
/// Fourth bucket lower bound.
/// </summary>
public decimal Volume4From
{
get => _volume4From.Value;
set => _volume4From.Value = value;
}

/// <summary>
/// Fourth bucket upper bound.
/// </summary>
public decimal Volume4To
{
get => _volume4To.Value;
set => _volume4To.Value = value;
}

/// <summary>
/// Fifth bucket lower bound.
/// </summary>
public decimal Volume5From
{
get => _volume5From.Value;
set => _volume5From.Value = value;
}

/// <summary>
/// Fifth bucket upper bound.
/// </summary>
public decimal Volume5To
{
get => _volume5To.Value;
set => _volume5To.Value = value;
}

/// <summary>
/// Sixth bucket lower bound.
/// </summary>
public decimal Volume6From
{
get => _volume6From.Value;
set => _volume6From.Value = value;
}

/// <summary>
/// Sixth bucket upper bound.
/// </summary>
public decimal Volume6To
{
get => _volume6To.Value;
set => _volume6To.Value = value;
}

/// <summary>
/// Volume greater than this value belongs to the last bucket.
/// </summary>
public decimal VolumeGreaterThan
{
get => _volumeGreaterThan.Value;
set => _volumeGreaterThan.Value = value;
}

/// <summary>
/// Initializes a new instance of <see cref="TickDataDetailedStrategy"/>.
/// </summary>
public TickDataDetailedStrategy()
{
_volumeLessThan = Param(nameof(VolumeLessThan), 10000m)
.SetGreaterThanZero()
.SetDisplay("Volume <", "Upper bound for first bucket", "General");

_volume2From = Param(nameof(Volume2From), 10000m)
.SetGreaterThanZero()
.SetDisplay("Vol2 From", "Lower bound for second bucket", "General");

_volume2To = Param(nameof(Volume2To), 20000m)
.SetGreaterThanZero()
.SetDisplay("Vol2 To", "Upper bound for second bucket", "General");

_volume3From = Param(nameof(Volume3From), 20000m)
.SetGreaterThanZero()
.SetDisplay("Vol3 From", "Lower bound for third bucket", "General");

_volume3To = Param(nameof(Volume3To), 50000m)
.SetGreaterThanZero()
.SetDisplay("Vol3 To", "Upper bound for third bucket", "General");

_volume4From = Param(nameof(Volume4From), 50000m)
.SetGreaterThanZero()
.SetDisplay("Vol4 From", "Lower bound for fourth bucket", "General");

_volume4To = Param(nameof(Volume4To), 100000m)
.SetGreaterThanZero()
.SetDisplay("Vol4 To", "Upper bound for fourth bucket", "General");

_volume5From = Param(nameof(Volume5From), 100000m)
.SetGreaterThanZero()
.SetDisplay("Vol5 From", "Lower bound for fifth bucket", "General");

_volume5To = Param(nameof(Volume5To), 200000m)
.SetGreaterThanZero()
.SetDisplay("Vol5 To", "Upper bound for fifth bucket", "General");

_volume6From = Param(nameof(Volume6From), 200000m)
.SetGreaterThanZero()
.SetDisplay("Vol6 From", "Lower bound for sixth bucket", "General");

_volume6To = Param(nameof(Volume6To), 400000m)
.SetGreaterThanZero()
.SetDisplay("Vol6 To", "Upper bound for sixth bucket", "General");

_volumeGreaterThan = Param(nameof(VolumeGreaterThan), 400000m)
.SetGreaterThanZero()
.SetDisplay("Volume >", "Lower bound for last bucket", "General");
}

/// <inheritdoc />
public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
{
yield return (Security, DataType.Ticks);
}

/// <inheritdoc />
protected override void OnReseted()
{
base.OnReseted();
_lastPrice = 0m;
_buyVol1 = _buyVol2 = _buyVol3 = _buyVol4 = _buyVol5 = _buyVol6 = _buyVol7 = 0m;
_sellVol1 = _sellVol2 = _sellVol3 = _sellVol4 = _sellVol5 = _sellVol6 = _sellVol7 = 0m;
_buyTick1 = _buyTick2 = _buyTick3 = _buyTick4 = _buyTick5 = _buyTick6 = _buyTick7 = 0;
_sellTick1 = _sellTick2 = _sellTick3 = _sellTick4 = _sellTick5 = _sellTick6 = _sellTick7 = 0;
}

/// <inheritdoc />
protected override void OnStarted(DateTimeOffset time)
{
base.OnStarted(time);

SubscribeTicks()
.Bind(ProcessTrade)
.Start();
}

private void ProcessTrade(ExecutionMessage trade)
{
var value = trade.Volume * trade.Price;
var bucket = DetermineBucket(value);
var isBuy = trade.Price >= _lastPrice;

switch (bucket)
{
case 0:
if (isBuy)
{
_buyTick1++;
_buyVol1 += value;
}
else
{
_sellTick1++;
_sellVol1 += value;
}
break;
case 1:
if (isBuy)
{
_buyTick2++;
_buyVol2 += value;
}
else
{
_sellTick2++;
_sellVol2 += value;
}
break;
case 2:
if (isBuy)
{
_buyTick3++;
_buyVol3 += value;
}
else
{
_sellTick3++;
_sellVol3 += value;
}
break;
case 3:
if (isBuy)
{
_buyTick4++;
_buyVol4 += value;
}
else
{
_sellTick4++;
_sellVol4 += value;
}
break;
case 4:
if (isBuy)
{
_buyTick5++;
_buyVol5 += value;
}
else
{
_sellTick5++;
_sellVol5 += value;
}
break;
case 5:
if (isBuy)
{
_buyTick6++;
_buyVol6 += value;
}
else
{
_sellTick6++;
_sellVol6 += value;
}
break;
default:
if (isBuy)
{
_buyTick7++;
_buyVol7 += value;
}
else
{
_sellTick7++;
_sellVol7 += value;
}
break;
}

_lastPrice = trade.Price;
}

private int DetermineBucket(decimal value)
{
if (value <= VolumeLessThan)
return 0;
if (value > Volume2From && value <= Volume2To)
return 1;
if (value > Volume3From && value <= Volume3To)
return 2;
if (value > Volume4From && value <= Volume4To)
return 3;
if (value > Volume5From && value <= Volume5To)
return 4;
if (value > Volume6From && value <= Volume6To)
return 5;
return 6;
}
}
