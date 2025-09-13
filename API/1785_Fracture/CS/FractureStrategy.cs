using System;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Fracture breakout strategy using fractals and smoothed MAs.
/// </summary>
public class FractureStrategy : Strategy
{
private readonly StrategyParam<DataType> _candleType;
private readonly StrategyParam<int> _atrPeriod;
private readonly StrategyParam<int> _adxPeriod;
private readonly StrategyParam<decimal> _adxLine;
private readonly StrategyParam<int> _ma1;
private readonly StrategyParam<int> _ma2;
private readonly StrategyParam<int> _ma3;
private readonly StrategyParam<decimal> _rangeMult;
private readonly StrategyParam<decimal> _minProfit;

private decimal _h1,_h2,_h3,_h4,_h5;
private decimal _l1,_l2,_l3,_l4,_l5;
private decimal _up,_down;

public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
public int AtrPeriod { get => _atrPeriod.Value; set => _atrPeriod.Value = value; }
public int AdxPeriod { get => _adxPeriod.Value; set => _adxPeriod.Value = value; }
public decimal AdxLine { get => _adxLine.Value; set => _adxLine.Value = value; }
public int Ma1Period { get => _ma1.Value; set => _ma1.Value = value; }
public int Ma2Period { get => _ma2.Value; set => _ma2.Value = value; }
public int Ma3Period { get => _ma3.Value; set => _ma3.Value = value; }
public decimal RangingMultiplier { get => _rangeMult.Value; set => _rangeMult.Value = value; }
public decimal MinProfit { get => _minProfit.Value; set => _minProfit.Value = value; }

public FractureStrategy()
{
_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
.SetDisplay("Candle", "Candle type", "General");
_atrPeriod = Param(nameof(AtrPeriod), 14).SetGreaterThanZero().SetDisplay("ATR", "ATR period", "Params");
_adxPeriod = Param(nameof(AdxPeriod), 22).SetGreaterThanZero().SetDisplay("ADX", "ADX period", "Params");
_adxLine = Param(nameof(AdxLine), 40m).SetDisplay("ADX Line", "ADX threshold", "Params");
_ma1 = Param(nameof(Ma1Period), 5).SetGreaterThanZero().SetDisplay("MA1", "First SMMA", "MA");
_ma2 = Param(nameof(Ma2Period), 9).SetGreaterThanZero().SetDisplay("MA2", "Second SMMA", "MA");
_ma3 = Param(nameof(Ma3Period), 22).SetGreaterThanZero().SetDisplay("MA3", "Third SMMA", "MA");
_rangeMult = Param(nameof(RangingMultiplier), 0.5m).SetGreaterThanZero().SetDisplay("Range Mult", "ATR range factor", "Params");
_minProfit = Param(nameof(MinProfit), 1m).SetGreaterThanZero().SetDisplay("Min Profit", "ATR profit target", "Risk");
}

/// <inheritdoc />
protected override void OnReseted()
{
base.OnReseted();
_h1=_h2=_h3=_h4=_h5=0m;
_l1=_l2=_l3=_l4=_l5=0m;
_up=_down=0m;
}

/// <inheritdoc />
protected override void OnStarted(DateTimeOffset time)
{
base.OnStarted(time);
var atr = new AverageTrueRange{Length=AtrPeriod};
var adx = new AverageDirectionalIndex{Length=AdxPeriod};
var ma1 = new SmoothedMovingAverage{Length=Ma1Period};
var ma2 = new SmoothedMovingAverage{Length=Ma2Period};
var ma3 = new SmoothedMovingAverage{Length=Ma3Period};
var sub = SubscribeCandles(CandleType);
sub.Bind(atr,adx,ma1,ma2,ma3,Process).Start();
var area = CreateChartArea();
if(area!=null){
DrawCandles(area,sub);
DrawIndicator(area,ma1);
DrawIndicator(area,ma2);
DrawIndicator(area,ma3);
DrawOwnTrades(area);
}
}

private void Process(ICandleMessage c, decimal atr, decimal adx, decimal ma1, decimal ma2, decimal ma3)
{
_h1=_h2; _h2=_h3; _h3=_h4; _h4=_h5; _h5=c.HighPrice;
_l1=_l2; _l2=_l3; _l3=_l4; _l4=_l5; _l5=c.LowPrice;
if(_h3>_h1&&_h3>_h2&&_h3>_h4&&_h3>_h5)_up=_h3;
if(_l3<_l1&&_l3<_l2&&_l3<_l4&&_l3<_l5)_down=_l3;
if(c.State!=CandleStates.Finished)return;
var range=Math.Abs(ma2-ma3)<atr*RangingMultiplier;
if(!IsFormedAndOnlineAndAllowTrading())return;
if(adx<AdxLine){
if(Position<=0&&_up!=0m&&c.ClosePrice>=_up&&c.ClosePrice>=ma1)BuyMarket(Volume+Math.Abs(Position));
else if(Position>=0&&_down!=0m&&c.ClosePrice<=_down&&c.ClosePrice<=ma1)SellMarket(Volume+Math.Abs(Position));
}else if(!range){
if(Position<=0&&ma1>=ma2&&ma2>=ma3&&c.ClosePrice>ma1)BuyMarket(Volume+Math.Abs(Position));
else if(Position>=0&&ma1<=ma2&&ma2<=ma3&&c.ClosePrice<ma1)SellMarket(Volume+Math.Abs(Position));
}
if(Position>0&&c.ClosePrice-PositionAvgPrice>=atr*MinProfit)SellMarket(Position);
else if(Position<0&&PositionAvgPrice-c.ClosePrice>=atr*MinProfit)BuyMarket(-Position);
}
}
