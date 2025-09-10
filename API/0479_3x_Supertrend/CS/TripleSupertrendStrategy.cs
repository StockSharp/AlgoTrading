using System;
using System.Collections.Generic;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Triple Supertrend strategy using three ATR-based bands.
/// </summary>
public class TripleSupertrendStrategy : Strategy
{
private readonly StrategyParam<DataType> _candle;
private readonly StrategyParam<int> _p1,_p2,_p3;
private readonly StrategyParam<decimal> _f1,_f2,_f3;
private bool _prev;

public DataType CandleType { get=>_candle.Value; set=>_candle.Value=value; }
public int AtrPeriod1 { get=>_p1.Value; set=>_p1.Value=value; }
public decimal Factor1 { get=>_f1.Value; set=>_f1.Value=value; }
public int AtrPeriod2 { get=>_p2.Value; set=>_p2.Value=value; }
public decimal Factor2 { get=>_f2.Value; set=>_f2.Value=value; }
public int AtrPeriod3 { get=>_p3.Value; set=>_p3.Value=value; }
public decimal Factor3 { get=>_f3.Value; set=>_f3.Value=value; }

public TripleSupertrendStrategy()
{
_candle=Param(nameof(CandleType),TimeSpan.FromMinutes(1).TimeFrame())
.SetDisplay("Candle Type","Type of candles","General");
_p1=Param(nameof(AtrPeriod1),11)
.SetDisplay("ATR1","Fast ATR","Supertrend");
_f1=Param(nameof(Factor1),1m)
.SetDisplay("Factor1","Fast factor","Supertrend");
_p2=Param(nameof(AtrPeriod2),12)
.SetDisplay("ATR2","Medium ATR","Supertrend");
_f2=Param(nameof(Factor2),2m)
.SetDisplay("Factor2","Medium factor","Supertrend");
_p3=Param(nameof(AtrPeriod3),13)
.SetDisplay("ATR3","Slow ATR","Supertrend");
_f3=Param(nameof(Factor3),3m)
.SetDisplay("Factor3","Slow factor","Supertrend");
}

public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
=> [(Security,CandleType)];

protected override void OnReseted(){base.OnReseted();_prev=false;}

protected override void OnStarted(DateTimeOffset time)
{
base.OnStarted(time);
var ma1=new ExponentialMovingAverage{Length=AtrPeriod1};
var ma2=new ExponentialMovingAverage{Length=AtrPeriod2};
var ma3=new ExponentialMovingAverage{Length=AtrPeriod3};
var atr1=new AverageTrueRange{Length=AtrPeriod1};
var atr2=new AverageTrueRange{Length=AtrPeriod2};
var atr3=new AverageTrueRange{Length=AtrPeriod3};
var sub=SubscribeCandles(CandleType);
sub.BindEx(ma1,ma2,ma3,atr1,atr2,atr3,Process).Start();
var area=CreateChartArea();
if(area!=null){DrawCandles(area,sub);DrawIndicator(area,ma1);DrawIndicator(area,ma2);DrawIndicator(area,ma3);DrawOwnTrades(area);}
}

private void Process(ICandleMessage c,IIndicatorValue m1,IIndicatorValue m2,IIndicatorValue m3,IIndicatorValue a1,IIndicatorValue a2,IIndicatorValue a3)
{
if(c.State!=CandleStates.Finished) return;
var close=c.ClosePrice;
var lower1=m1.ToDecimal()-a1.ToDecimal()*Factor1;
var lower2=m2.ToDecimal()-a2.ToDecimal()*Factor2;
var lower3=m3.ToDecimal()-a3.ToDecimal()*Factor3;
var up1=close>lower1;
var up2=close>lower2;
var up3=close>lower3;
if(up1&&up2&&up3&&!_prev&&Position<=0)
BuyMarket(Volume+Math.Abs(Position));
else if(!up1&&!up2&&!up3&&Position>0)
SellMarket(Position);
_prev=up1;
}
}
