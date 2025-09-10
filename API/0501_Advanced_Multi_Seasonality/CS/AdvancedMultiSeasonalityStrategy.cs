using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that trades during predefined seasonal periods.
/// Up to four periods can be configured with individual entry date,
/// holding duration and trade direction.
/// </summary>
public class AdvancedMultiSeasonalityStrategy : Strategy
{
private readonly StrategyParam<DataType> _candleType;
private readonly StrategyParam<bool>[] _enabled = new StrategyParam<bool>[4];
private readonly StrategyParam<int>[] _entryMonth = new StrategyParam<int>[4];
private readonly StrategyParam<int>[] _entryDay = new StrategyParam<int>[4];
private readonly StrategyParam<int>[] _holdingDays = new StrategyParam<int>[4];
private readonly StrategyParam<string>[] _direction = new StrategyParam<string>[4];

private readonly bool[] _inTrade = new bool[4];
private readonly bool[] _isLong = new bool[4];
private readonly int[] _barsSinceEntry = new int[4];

/// <summary>
/// Candle type for calculations.
/// </summary>
public DataType CandleType
{
get => _candleType.Value;
set => _candleType.Value = value;
}

/// <summary>
/// Initialize <see cref="AdvancedMultiSeasonalityStrategy"/>.
/// </summary>
public AdvancedMultiSeasonalityStrategy()
{
_candleType = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame())
.SetDisplay("Candle Type", "Type of candles", "General");

for (var i = 0; i < 4; i++)
{
var group = $"Period {i + 1}";
_enabled[i] = Param($"Period{i + 1}Enabled", i == 0)
.SetDisplay("Activate", $"Use period {i + 1}", group);
_entryMonth[i] = Param($"EntryMonth{i + 1}", new[] {12,1,6,9}[i])
.SetDisplay("Entry Month", "Entry month", group);
_entryDay[i] = Param($"EntryDay{i + 1}", new[] {1,15,1,15}[i])
.SetDisplay("Entry Day", "Entry day", group);
_holdingDays[i] = Param($"HoldingDays{i + 1}", new[] {20,10,15,10}[i])
.SetDisplay("Holding Days", "Bars to hold", group);
_direction[i] = Param($"TradeDirection{i + 1}", "Long")
.SetDisplay("Direction", "Long or Short", group);
}
}

/// <inheritdoc />
public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
=> [(Security, CandleType)];

/// <inheritdoc />
protected override void OnReseted()
{
base.OnReseted();
for (var i = 0; i < 4; i++)
{
_inTrade[i] = false;
_barsSinceEntry[i] = 0;
}
}

/// <inheritdoc />
protected override void OnStarted(DateTimeOffset time)
{
base.OnStarted(time);

var subscription = SubscribeCandles(CandleType);
subscription.Bind(ProcessCandle).Start();

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

if (!IsFormedAndOnlineAndAllowTrading())
return;

for (var i = 0; i < 4; i++)
{
if (_inTrade[i])
{
_barsSinceEntry[i]++;
if (_barsSinceEntry[i] >= _holdingDays[i].Value)
{
if (_isLong[i] && Position > 0)
SellMarket(Math.Abs(Position));
else if (!_isLong[i] && Position < 0)
BuyMarket(Math.Abs(Position));

_inTrade[i] = false;
}
}
}

var month = candle.OpenTime.Month;
var day = candle.OpenTime.Day;

if (Position != 0)
return;

for (var i = 0; i < 4; i++)
{
if (_enabled[i].Value && !_inTrade[i] && month == _entryMonth[i].Value && day == _entryDay[i].Value)
{
var volume = Volume + Math.Abs(Position);
if (_direction[i].Value.Equals("Long", StringComparison.OrdinalIgnoreCase))
{
BuyMarket(volume);
_isLong[i] = true;
}
else
{
SellMarket(volume);
_isLong[i] = false;
}

_inTrade[i] = true;
_barsSinceEntry[i] = 0;
break;
}
}
}
}

