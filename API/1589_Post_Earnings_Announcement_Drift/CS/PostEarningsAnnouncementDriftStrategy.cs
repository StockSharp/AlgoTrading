using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Post-earnings announcement drift strategy.
/// </summary>
public class PostEarningsAnnouncementDriftStrategy : Strategy
{
private readonly StrategyParam<int> _holding;
private readonly StrategyParam<decimal> _surprise;
private readonly StrategyParam<DataType> _candleType;

private int _barsInTrade;

public int HoldingPeriod { get => _holding.Value; set => _holding.Value = value; }
public decimal SurpriseThreshold { get => _surprise.Value; set => _surprise.Value = value; }
public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

public PostEarningsAnnouncementDriftStrategy()
{
_holding = Param(nameof(HoldingPeriod), 8).SetGreaterThanZero().SetDisplay("Holding Bars", "Bars to hold position", "General");
_surprise = Param(nameof(SurpriseThreshold), 3m).SetDisplay("Surprise %", "Earnings surprise threshold", "General");
_candleType = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame()).SetDisplay("Candle Type", "Type of candles", "General");
}

/// <inheritdoc />
public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
=> [(Security, CandleType)];

/// <inheritdoc />
protected override void OnReseted()
{
base.OnReseted();
_barsInTrade = 0;
}

/// <inheritdoc />
protected override void OnStarted(DateTimeOffset time)
{
base.OnStarted(time);
SubscribeCandles(CandleType).Bind(Process).Start();
}

private void Process(ICandleMessage candle)
{
if (candle.State != CandleStates.Finished)
return;

if (Position == 0)
{
if (TryGetEarningsSurprise(candle.OpenTime, out var s))
{
if (s > SurpriseThreshold)
BuyMarket();
else if (s < -SurpriseThreshold)
SellMarket();
_barsInTrade = 0;
}
}
else
{
_barsInTrade++;
if (_barsInTrade >= HoldingPeriod)
{
if (Position > 0)
SellMarket(Position);
else if (Position < 0)
BuyMarket(-Position);
}
}
}

private bool TryGetEarningsSurprise(DateTimeOffset date, out decimal surprise)
{
surprise = 0m;
return false; // TODO: implement earnings data retrieval
}
}
