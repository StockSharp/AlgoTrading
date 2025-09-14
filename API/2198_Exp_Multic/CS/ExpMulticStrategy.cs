using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Multi-currency strategy without technical indicators.
/// Increases position size on profit and flips direction on loss.
/// </summary>
public class ExpMulticStrategy : Strategy
{
private readonly StrategyParam<decimal> _loss;
private readonly StrategyParam<decimal> _profit;
private readonly StrategyParam<decimal> _margin;
private readonly StrategyParam<decimal> _minVolume;
private readonly StrategyParam<decimal> _kChange;
private readonly StrategyParam<decimal> _kClose;

private readonly List<Security> _securities =
[
new Security { Id = "EURUSD" },
new Security { Id = "GBPUSD" },
new Security { Id = "USDJPY" },
new Security { Id = "USDCHF" },
new Security { Id = "USDCAD" },
new Security { Id = "AUDUSD" },
new Security { Id = "EURGBP" },
new Security { Id = "EURJPY" },
new Security { Id = "EURAUD" },
new Security { Id = "GBPJPY" },
];

private readonly Dictionary<Security, bool> _direction = new();
private readonly Dictionary<Security, decimal> _volume = new();
private readonly Dictionary<Security, decimal> _entryPrice = new();
private readonly Dictionary<Security, decimal> _lastPrice = new();

private bool _isActive;
private decimal _initialBalance;

/// <summary>Maximum permitted drawdown.</summary>
public decimal Loss { get => _loss.Value; set => _loss.Value = value; }
/// <summary>Target profit to stop trading.</summary>
public decimal Profit { get => _profit.Value; set => _profit.Value = value; }
/// <summary>Required free margin to open new positions.</summary>
public decimal Margin { get => _margin.Value; set => _margin.Value = value; }
/// <summary>Base volume for all trades.</summary>
public decimal MinVolume { get => _minVolume.Value; set => _minVolume.Value = value; }
/// <summary>Profit threshold for adding to position.</summary>
public decimal KChange { get => _kChange.Value; set => _kChange.Value = value; }
/// <summary>Profit threshold for closing position.</summary>
public decimal KClose { get => _kClose.Value; set => _kClose.Value = value; }

/// <summary>
/// Initialize parameters.
/// </summary>
public ExpMulticStrategy()
{
_loss = Param(nameof(Loss), 1900m).SetDisplay("Max Loss", "Maximum drawdown before reset", "Risk");
_profit = Param(nameof(Profit), 4000m).SetDisplay("Profit Target", "Profit to stop trading", "Risk");
_margin = Param(nameof(Margin), 5000m).SetDisplay("Margin", "Minimum balance to open trades", "Risk");
_minVolume = Param(nameof(MinVolume), 0.01m).SetDisplay("Min Volume", "Initial trade volume", "Trading");
_kChange = Param(nameof(KChange), 2100m).SetDisplay("Add Threshold", "Profit to increase volume", "Trading");
_kClose = Param(nameof(KClose), 4600m).SetDisplay("Close Threshold", "Profit to close position", "Trading");
}

/// <inheritdoc />
public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
{
foreach (var sec in _securities)
yield return (sec, DataType.Ticks);
}

/// <inheritdoc />
protected override void OnStarted(DateTimeOffset time)
{
base.OnStarted(time);

_initialBalance = Portfolio.CurrentValue;
_isActive = true;

foreach (var sec in _securities)
{
_direction[sec] = true;
_volume[sec] = MinVolume;
_entryPrice[sec] = 0m;
_lastPrice[sec] = 0m;

var subscription = SubscribeTrades(sec);
subscription.Bind(trade => ProcessTrade(sec, trade)).Start();
}
}

private void ProcessTrade(Security sec, ExecutionMessage trade)
{
_lastPrice[sec] = trade.TradePrice ?? 0m;
Process(sec);
}

private void Process(Security sec)
{
var balance = Portfolio.CurrentValue;
var totalPnL = balance - _initialBalance;

if (!_isActive)
{
CloseAllPositions();
_isActive = true;
return;
}

if (-totalPnL > Loss || totalPnL > Profit)
{
CloseAllPositions();
_isActive = true;
return;
}

var pos = GetPositionValue(sec, Portfolio) ?? 0m;
var price = _lastPrice[sec];

if (pos != 0m)
{
var entry = _entryPrice[sec];
var pnl = (price - entry) * pos;

if (pnl > _volume[sec] * KChange)
{
_volume[sec] += MinVolume;
if (_direction[sec])
BuyMarket(MinVolume, sec);
else
SellMarket(MinVolume, sec);
}
if (pnl < -_volume[sec] * KChange)
{
_direction[sec] = !_direction[sec];
ClosePosition(sec);
_entryPrice[sec] = 0m;
}
if (pnl > MinVolume * KClose)
{
ClosePosition(sec);
_entryPrice[sec] = 0m;
}
}
else if (balance > Margin)
{
_volume[sec] = MinVolume;
if (_direction[sec])
{
BuyMarket(MinVolume, sec);
}
else
{
SellMarket(MinVolume, sec);
}
_entryPrice[sec] = price;
}
}

private void CloseAllPositions()
{
foreach (var sec in _securities)
{
var pos = GetPositionValue(sec, Portfolio) ?? 0m;
if (pos > 0m)
SellMarket(pos, sec);
else if (pos < 0m)
BuyMarket(-pos, sec);

_volume[sec] = MinVolume;
_entryPrice[sec] = 0m;
}
}
}
