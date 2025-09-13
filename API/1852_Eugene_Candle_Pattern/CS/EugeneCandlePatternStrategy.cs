namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

public class EugeneCandlePatternStrategy : Strategy
{
private readonly StrategyParam<decimal> _vol;
private readonly StrategyParam<int> _sl;
private readonly StrategyParam<int> _tp;
private readonly StrategyParam<bool> _inv;
private readonly StrategyParam<DataType> _cType;

private readonly ICandleMessage[] _r = new ICandleMessage[4];
private decimal _stop, _take;

public decimal Volume { get => _vol.Value; set => _vol.Value = value; }
public int StopLossPoints { get => _sl.Value; set => _sl.Value = value; }
public int TakeProfitPoints { get => _tp.Value; set => _tp.Value = value; }
public bool InvertSignals { get => _inv.Value; set => _inv.Value = value; }
public DataType CandleType { get => _cType.Value; set => _cType.Value = value; }

public EugeneCandlePatternStrategy()
{
_vol = Param(nameof(Volume),1m).SetGreaterThanZero().SetDisplay("Volume","Order volume","Trading");
_sl = Param(nameof(StopLossPoints),0).SetDisplay("Stop Loss (points)","Stop loss in price steps, 0 - disabled","Risk");
_tp = Param(nameof(TakeProfitPoints),0).SetDisplay("Take Profit (points)","Take profit in price steps, 0 - disabled","Risk");
_inv = Param(nameof(InvertSignals),false).SetDisplay("Invert Signals","Swap buy and sell signals","General");
_cType = Param(nameof(CandleType),TimeSpan.FromMinutes(1).TimeFrame()).SetDisplay("Candle Type","Type of candles","General");
}

public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities() => [(Security,CandleType)];

protected override void OnReseted()
{
base.OnReseted();
Array.Clear(_r);
_stop = _take = 0m;
}

protected override void OnStarted(DateTimeOffset time)
{
base.OnStarted(time);
var s = SubscribeCandles(CandleType);
s.Bind(Process).Start();
var a = CreateChartArea();
if (a!=null){ DrawCandles(a,s); DrawOwnTrades(a); }
StartProtection();
}

private void Process(ICandleMessage c)
{
if (c.State!=CandleStates.Finished) return;
CheckStops(c);
_r[3]=_r[2];_r[2]=_r[1];_r[1]=_r[0];_r[0]=c;
if (_r[3] is null) return;
Compute(out var ob,out var os,out var cb,out var cs);
if (InvertSignals){(ob,os)=(os,ob);(cb,cs)=(cs,cb);}
if (Position>0 && cb) ClosePosition();
else if (Position<0 && cs) ClosePosition();
if (Position<=0 && ob && !os && !cb){ BuyMarket(Volume); SetStops(c.ClosePrice,true); }
else if (Position>=0 && os && !ob && !cs){ SellMarket(Volume); SetStops(c.ClosePrice,false); }
}

private void Compute(out bool ob,out bool os,out bool cb,out bool cs)
{
var r0=_r[0];var r1=_r[1];var r2=_r[2];
ob=os=cb=cs=false;
var bi=r1.HighPrice<=r2.HighPrice && r1.LowPrice>=r2.LowPrice && r1.ClosePrice<=r1.OpenPrice;
var wi=r1.HighPrice<=r2.HighPrice && r1.LowPrice>=r2.LowPrice && r1.ClosePrice>r1.OpenPrice;
var wb=wi && r2.ClosePrice>r2.OpenPrice;
var bb=bi && r2.ClosePrice<r2.OpenPrice;
var zb=r1.OpenPrice<r1.ClosePrice? r1.ClosePrice-(r1.ClosePrice-r1.OpenPrice)/3m : r1.ClosePrice-(r1.ClosePrice-r1.LowPrice)/3m;
var zs=r1.OpenPrice>r1.ClosePrice? r1.ClosePrice+(r1.OpenPrice-r1.ClosePrice)/3m : r1.ClosePrice+(r1.HighPrice-r1.ClosePrice)/3m;
var h=r0.OpenTime.UtcDateTime.Hour;
var cB=(r0.LowPrice<=zb || h>=8) && !bb && !wi;
var cS=(r0.HighPrice>=zs || h>=8) && !wb && !bi;
var bSig=r0.HighPrice>r1.HighPrice;
var sSig=r0.LowPrice<r1.LowPrice;
if (bSig && cB && r0.LowPrice>r1.LowPrice && r1.LowPrice<r2.HighPrice) ob=true;
if (sSig && cS && r0.HighPrice<r1.HighPrice && r1.HighPrice>r2.LowPrice) os=true;
if (sSig && cS && r0.HighPrice<r1.HighPrice) cb=true;
if (bSig && cB && r0.LowPrice>r1.LowPrice) cs=true;
}

private void SetStops(decimal price,bool longPos)
{
var step=Security.PriceStep??1m;
if (longPos){ _stop=StopLossPoints>0?price-step*StopLossPoints:0m; _take=TakeProfitPoints>0?price+step*TakeProfitPoints:0m; }
else { _stop=StopLossPoints>0?price+step*StopLossPoints:0m; _take=TakeProfitPoints>0?price-step*TakeProfitPoints:0m; }
}

private void CheckStops(ICandleMessage c)
{
if (Position>0)
{
if ((_stop!=0m && c.LowPrice<=_stop) || (_take!=0m && c.HighPrice>=_take))
ClosePosition();
}
else if (Position<0)
{
if ((_stop!=0m && c.HighPrice>=_stop) || (_take!=0m && c.LowPrice<=_take))
ClosePosition();
}
}
}
