using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Closes existing positions when profit, loss or time limit is reached.
/// </summary>
public class ClosePositionsStrategy : Strategy
{
	private readonly StrategyParam<int> _profitPips;
	private readonly StrategyParam<int> _lossPips;
	private readonly StrategyParam<int> _timeLimit;
	private readonly StrategyParam<int> _closeHour;
	private readonly StrategyParam<int> _closeMinute;
	
	private decimal _entry;
	private DateTimeOffset _entryTime;
	
public int ProfitPips { get => _profitPips.Value; set => _profitPips.Value = value; }
public int LossPips { get => _lossPips.Value; set => _lossPips.Value = value; }
public int TimeLimit { get => _timeLimit.Value; set => _timeLimit.Value = value; }
public int CloseHour { get => _closeHour.Value; set => _closeHour.Value = value; }
public int CloseMinute { get => _closeMinute.Value; set => _closeMinute.Value = value; }

public ClosePositionsStrategy()
{
_profitPips = Param(nameof(ProfitPips), 100);
_lossPips = Param(nameof(LossPips), -200);
_timeLimit = Param(nameof(TimeLimit), 60);
_closeHour = Param(nameof(CloseHour), 15);
_closeMinute = Param(nameof(CloseMinute), 0);
}

public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
=> [(Security, DataType.Ticks)];

/// <inheritdoc />
protected override void OnStarted(DateTimeOffset time)
{
base.OnStarted(time);
SubscribeTrades().Bind(ProcessTrade).Start();
}

private void ProcessTrade(ExecutionMessage trade)
{
var price = trade.TradePrice ?? 0m;

if (Position == 0)
{
_entry = 0m;
_entryTime = default;
return;
}

if (_entry == 0m)
{
_entry = price;
_entryTime = trade.ServerTime;
}

var age = (trade.ServerTime - _entryTime).TotalMinutes;
var step = Security.PriceStep ?? 1m;

if (trade.ServerTime.Hour >= CloseHour && trade.ServerTime.Minute >= CloseMinute)
ClosePosition();
else if (age > TimeLimit)
ClosePosition();
else if (Position > 0)
{
var diff = price - _entry;
if (diff >= ProfitPips * step || diff <= LossPips * step)
ClosePosition();
}
else if (Position < 0)
{
var diff = _entry - price;
if (diff >= ProfitPips * step || diff <= LossPips * step)
ClosePosition();
}
}

private void ClosePosition()
{
if (Position > 0)
SellMarket();
else if (Position < 0)
BuyMarket();
}
}
