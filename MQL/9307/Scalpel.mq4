//+------------------------------------------------------------------+
//|                                                      scalpel.mq4 |
//|                                           Copyright © 2009, ryaz |
//|                                               ryaz://outta.here/ |
//+------------------------------------------------------------------+
#property copyright "ryaz"
#property link      "outta@here"
#define EA "Scalpel"
#define VER "1_01"

extern double Lots = -5; //if Lots are negative means the percent of free margin used
extern int TakeProfit = 40; 
extern int StopLoss = 340;
extern int TrailingStop = 25;
extern int cciPeriod = 14;
extern double cciLimit = 75;
extern int MaxPos=1; //open positions allowed in one dir.
extern int Interval=0;//Minutes before adding a position (0=not used) 
extern int Reduce=600;//Minutes before reducing TP by one pip (0=not used)
extern int Live=0;//Minutes before closing an order regardless profit (0=not used)
extern int Volatility=100;//volatility bars (positive>directional or negative>non dir.)
extern int Threshold=1;//pip threshold for volatility
extern int FridayClose=22;//At what time to close all orders on Friday (0=not used)
extern int slippage = 3;
extern double spreadlimit = 5.5;
extern int magic = 123581321;
double high4, high1, high30, low4, low1, low30, high4s, high1s, high30s, low4s, low1s, low30s;
double cci, volu, vold, vol0u, vol1u=1, vol0d, vol1d=1, min, max, lNorm, thresh;
int tim=0, timm=0, tim30=0, tim1=0, tim4=0, lDigits, dig, pip;
bool ccib, ccis;
int init() {
    min=MarketInfo(Symbol(),MODE_MINLOT);
    max=MarketInfo(Symbol(),MODE_MAXLOT);
    lNorm=MarketInfo(Symbol(),MODE_LOTSTEP);
    if (lNorm==0.1) lDigits=1; else
    if (lNorm==0.01) lDigits=2; else
    if (lNorm==0.001) lDigits=3; else
    lDigits=0;
    lNorm=-MarketInfo(Symbol(),MODE_MARGINREQUIRED)*AccountLeverage()/100;
    pip=1;
    while (Lots<=-1) Lots/=100;
    dig=Digits;
    if (dig==3 || dig==5) {
      TakeProfit*=10;
      StopLoss*=10;
      TrailingStop*=10;
      slippage*=10;
      pip=10;
      dig--;
    }
    Interval*=60; 
    thresh=pip*Point*MathAbs(Threshold);
    spreadlimit*=pip*Point;
  }

double UseLots() {
  if (Lots>0) return(Lots);
  double lots=MathMin(NormalizeDouble(AccountFreeMargin()*Lots/lNorm,lDigits),max);
  if (lots<min) return(0);
  return(lots);
}

void start() {
   int i,buys=0,sells=0,life=0;
   double lots, h, l;
   bool close;
   if(Bars<cciPeriod || !IsTradeAllowed()) return;
   for(i=OrdersTotal()-1;i>=0;i--) {
      if (!OrderSelect(i,SELECT_BY_POS,MODE_TRADES)) continue;
      if (OrderMagicNumber()!=magic) continue;
      if (OrderSymbol()!=Symbol()) continue;
      life=(TimeCurrent()-OrderOpenTime())/60;
      if (Live>0) close=life>Live; else close=false;
      if (!close && FridayClose>0) close=DayOfWeek()==5 && Hour()>FridayClose;
      if (OrderType()==OP_BUY)  {
        if (!close && Reduce>0) close=Bid>OrderTakeProfit()-pip*(life/Reduce)*Point;
        if (close) {
          close=OrderClose(OrderTicket(),OrderLots(),Bid,slippage,Aqua);
          RefreshRates();
          if (close) {life=0;continue;}
        }
        buys++;
        if (TrailingStop>0) 
          if (Bid-OrderOpenPrice() > Point*TrailingStop) 
            if (NormalizeDouble(OrderStopLoss() - Bid+Point*TrailingStop,dig)<0) 
               OrderModify(OrderTicket(), OrderOpenPrice(), NormalizeDouble(Bid-Point*TrailingStop,Digits), OrderTakeProfit(), 0, Blue);
      } else {
        if (!close && Reduce>0) close=Ask<OrderTakeProfit()+pip*(life/Reduce)*Point;
        if (close) {
          close=OrderClose(OrderTicket(),OrderLots(),Ask,slippage,Pink);
          RefreshRates();
          if (close) {life=0;continue;}
        }
        sells++;
        if (TrailingStop>0) 
          if (OrderOpenPrice()-Ask > Point * TrailingStop)
            if (OrderStopLoss()==0 || NormalizeDouble(OrderStopLoss()- Ask-Point*TrailingStop,dig)>0)
              OrderModify(OrderTicket(), OrderOpenPrice(), NormalizeDouble(Ask+Point*TrailingStop,Digits), OrderTakeProfit(), 0, Red);
      }  
     }
   if (MathAbs(buys-sells)>=MaxPos) return;
   if (DayOfWeek()==5) return;
   if (Interval>0 && life>Interval) return;
   if (spreadlimit>0 && Ask-Bid>spreadlimit) return;
   if (tim!=Time[0]) {
     cci=iCCI(0,0,cciPeriod,PRICE_MEDIAN,1);
     if (cciLimit>0) {
       ccib=cci>0 && cci< cciLimit;
       ccis=cci<0 && cci>-cciLimit;
     } else {
       ccib=cci>-cciLimit;
       ccib=cci< cciLimit;
     }
     tim=Time[0];
   }
   if (!ccib && !ccis) return;
   high4=iHigh(0,PERIOD_H4,0);
   low4=iLow(0,PERIOD_H4,0);
   if (tim4!=iTime(0,PERIOD_H4,0)) {
     high4s=iHigh(0,PERIOD_H4,1);
     low4s=iLow(0,PERIOD_H4,1);
     tim4=iTime(0,PERIOD_H4,0);
   }
   if (tim1!=iTime(0,PERIOD_H1,0)) {
     high1s=iHigh(0,PERIOD_H1,1);
     low1s=iLow(0,PERIOD_H1,1);
     tim1=iTime(0,PERIOD_H1,0);
   }
   high1=iHigh(0,PERIOD_H1,0);
   low1=iLow(0,PERIOD_H1,0);
   if (tim30!=iTime(0,PERIOD_M30,0)) {
     high30s=iHigh(0,PERIOD_M30,1);
     low30s=iLow(0,PERIOD_M30,1);
     tim30=iTime(0,PERIOD_M30,0);
   }
   high30=iHigh(0,PERIOD_M30,0);
   low30=iLow(0,PERIOD_M30,0);
   if (Volatility>0) { 
     if (timm!=iTime(0,PERIOD_M1,0)) {
       vol1u=0;
       vol1d=0;
       for (i=Volatility; i<Volatility<<1; i++) {
         h=iClose(0,PERIOD_M1,i);
         l=iOpen(0,PERIOD_M1,i);
         if (h>l+thresh)
           vol1u+=iVolume(0,PERIOD_M1,i);
         else
         if (l>h+thresh)
           vol1d+=iVolume(0,PERIOD_M1,i);  
       }  
       volu=0;
       vold=0;
       for (i=1; i<Volatility; i++) {
         h=iClose(0,PERIOD_M1,i);
         l=iOpen(0,PERIOD_M1,i);
         if (h>l+thresh)
           volu+=iVolume(0,PERIOD_M1,i);
         else
         if (l>h+thresh)
           vold+=iVolume(0,PERIOD_M1,i);  
       }  
       if (Threshold<0) {
         l=vol1u;
         vol1u=vol1d;
         vol1d=l;
       } 
       timm=iTime(0,PERIOD_M1,0); 
     }
     h=iClose(0,PERIOD_M1,0);
     l=iOpen(0,PERIOD_M1,0);
     if (h>l+thresh) {
       vol0u=volu+iVolume(0,PERIOD_M1,0);
       vol0d=vold;
     } else
     if (l>h+thresh) {
       vol0d=vold+iVolume(0,PERIOD_M1,0);
       vol0u=volu;
     } else {
       vol0u=volu;
       vol0d=vold;
     }  
     if (Threshold<0) {
       l=vol0u;
       vol0u=vol0d;
       vol0d=l;
     } 
   } else 
   if (Volatility<0) {
     if (timm!=iTime(0,PERIOD_M1,0)) {
       vol1u=0;
       for (i=-Volatility; i<(-Volatility)<<1; i++) { 
         h=iClose(0,PERIOD_M1,i);
         l=iOpen(0,PERIOD_M1,i);
         if (MathAbs(h-l)>thresh)
           vol1u+=iVolume(0,PERIOD_M1,i);
       }  
       volu=0;
       for (i=1; i<-Volatility; i++)
         h=iClose(0,PERIOD_M1,i);
         l=iOpen(0,PERIOD_M1,i);
         if (MathAbs(h-l)>=thresh)
           volu+=iVolume(0,PERIOD_M1,i);
       vol1d=vol1u;
       vold=volu;  
       timm=iTime(0,PERIOD_M1,0); 
     }
     h=iClose(0,PERIOD_M1,0);
     l=iOpen(0,PERIOD_M1,0);
     if (MathAbs(h-l)>=thresh)
       vol0u=volu+iVolume(0,PERIOD_M1,0);
     vol0d=vol0u;
   } else {
     h=iClose(0,PERIOD_M1,0);
     l=iOpen(0,PERIOD_M1,0);
     if (MathAbs(h-l)>=thresh)
       vol0u=iVolume(0,PERIOD_M1,0);
     else
       vol0u=0;  
     vol0d=vol0u;  
   }
   if (ccib) { 
    if(low4>low4s && low1>low1s && low30>low30s && Ask >High[1] && vol0u>vol1u && vol1u>0 && High[2]>High[1] && High[3]>High[2]) {
      lots=UseLots();
      if (lots==0) return(0);
      i=OrderSend(Symbol(),OP_BUY,UseLots(),Ask,slippage,Ask-StopLoss*Point,Ask+TakeProfit*Point,EA,magic,0,Blue);
    }  
  } else
    if(high4<high4s && high1<high1s && high30<high30s && Ask <Low[1] && vol0d>vol1d && vol1d>0 && Low[2]<Low[1] && Low[3]<Low[2] && ((cciLimit>0 && cci<0 && cci>-cciLimit) || (cciLimit<0 && cci<cciLimit))) {
      lots=UseLots();
      if (lots==0) return(0);
      i=OrderSend(Symbol(),OP_SELL,UseLots(),Bid,slippage,Bid+StopLoss*Point,Bid-TakeProfit*Point,EA,magic,0,Red);
    }
  return(0);    
}

