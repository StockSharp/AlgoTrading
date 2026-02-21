using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Donchian Quest Research strategy.
/// Opens on breakout of a long Donchian channel and exits on a shorter channel.
/// </summary>
public class DonchianQuestResearchStrategy : Strategy
{
private readonly StrategyParam<int> _openPeriod;
private readonly StrategyParam<int> _closePeriod;
private readonly StrategyParam<DataType> _candleType;

public int OpenPeriod { get => _openPeriod.Value; set => _openPeriod.Value = value; }
public int ClosePeriod { get => _closePeriod.Value; set => _closePeriod.Value = value; }
public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

public DonchianQuestResearchStrategy()
{
_openPeriod = Param(nameof(OpenPeriod), 50)
.SetRange(10, 100)
.SetDisplay("Open Period", "Entry channel period", "Indicators");

_closePeriod = Param(nameof(ClosePeriod), 50)
.SetRange(5, 100)
.SetDisplay("Close Period", "Exit channel period", "Indicators");

_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
.SetDisplay("Candle Type", "Type of candles", "General");
}

public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
=> [(Security, CandleType)];

protected override void OnStarted2(DateTime time)
{
base.OnStarted2(time);

var openCh = new DonchianChannels { Length = OpenPeriod };
var closeCh = new DonchianChannels { Length = ClosePeriod };

var sub = SubscribeCandles(CandleType);
sub.BindEx(openCh, closeCh, Process).Start();

var area = CreateChartArea();
if (area != null)
{
DrawCandles(area, sub);
DrawIndicator(area, openCh);
DrawIndicator(area, closeCh);
DrawOwnTrades(area);
}
}

private void Process(ICandleMessage candle,
IIndicatorValue openValue, IIndicatorValue closeValue)
{
if (candle.State != CandleStates.Finished || !IsFormedAndOnlineAndAllowTrading())
return;

if (openValue is not DonchianChannelsValue openDc || closeValue is not DonchianChannelsValue closeDc)
return;

if (openDc.UpperBand is not decimal openHigh || openDc.LowerBand is not decimal openLow)
return;

if (closeDc.UpperBand is not decimal closeHigh || closeDc.LowerBand is not decimal closeLow)
return;

var price = candle.ClosePrice;

if (Position <= 0 && price >= openHigh)
{
var v = Volume + Math.Abs(Position);
BuyMarket(v);
}
else if (Position >= 0 && price <= openLow)
{
var v = Volume + Math.Abs(Position);
SellMarket(v);
}
else if (Position > 0 && price <= closeLow)
{
SellMarket(Position);
}
else if (Position < 0 && price >= closeHigh)
{
BuyMarket(Math.Abs(Position));
}
}
}
