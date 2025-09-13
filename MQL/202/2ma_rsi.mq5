//+------------------------------------------------------------------+
//|                                                      2MA_RSI.mq5 |
//|                                             Copyright 2010, Alf. |
//|                                      http://forum.liteforex.org/ |
//+------------------------------------------------------------------+
#property copyright "Copyright 2010, Alf."
#property link      "http://forum.liteforex.org/"
#property version   "1.00"

#include <OnTesterFunctions.mqh>
#include <Martingail.mqh>
//--- input parameters
input double   DML=1000;
input int      Ud=1;
input int      Stop=500;
input int      Tp=1500;
input int      Slipage=50;
input int      PeriodFast=5;
input int      PeriodSlow=20;
input int      ShiftFast=0;
input int      ShiftSlow=0;
input int      RSI=14;
input double   Perekup=70;
input double   Pereprod=30;

int m1=0;
int m2=0;
int r=0;

Martingail lt;
//+------------------------------------------------------------------+
//| Open                                                             |
//+------------------------------------------------------------------+
void Open()
  {
   double ma1[2];
   double ma2[2];
   double rsi[1];
   if(CopyBuffer(m1,0,1,2,ma1)<0) return;
   if(CopyBuffer(m2,0,1,2,ma2)<0) return;
   if(CopyBuffer(r,0,1,1,rsi)<0)  return;
   MqlTick last_tick;
   SymbolInfoTick(_Symbol,last_tick);
   MqlTradeResult result;
   MqlTradeRequest request;
   ZeroMemory(request);

   request.symbol=_Symbol;
   request.magic=777;
   request.deviation=50;
   request.action=TRADE_ACTION_DEAL;
   request.type_filling=ORDER_FILLING_FOK;
   Comment("Ëîò=",lt.Lot());
   if(ma1[1]<ma2[1] && ma1[0]>ma2[0] && rsi[0]<Pereprod)
     {
      request.volume=lt.Lot();
      request.price=last_tick.ask;
      request.type=ORDER_TYPE_BUY;
      request.sl=last_tick.bid-Stop*Point();
      request.tp=last_tick.ask+Tp*Point();
      OrderSend(request,result);
     }
   if(ma1[1]>ma2[1] && ma1[0]<ma2[0] && rsi[0]>Perekup)
     {
      request.volume=lt.Lot();
      request.price=last_tick.bid;
      request.type=ORDER_TYPE_SELL;
      request.sl=last_tick.ask+Stop*Point();
      request.tp=last_tick.bid-Tp*Point();
      OrderSend(request,result);
     }
  }
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
//---
   m1=iMA(_Symbol,0,PeriodFast,ShiftFast,MODE_EMA,PRICE_MEDIAN);
   m2=iMA(_Symbol,0,PeriodSlow,ShiftSlow,MODE_EMA,PRICE_MEDIAN);
   r=iRSI(_Symbol,0,RSI,PRICE_MEDIAN);
   lt.GVarName="MG_1";
   lt.Shape=DML;
   lt.DoublingCount=Ud;
   lt.GVarGet();
//---
   return(0);
  }
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
//---
   lt.GVarSet();
  }
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
  {
//---
   MqlRates rt[1];
   if(CopyRates(Symbol(),_Period,0,1,rt)<0) return;
   if(rt[0].tick_volume>1) return;
   if(!PositionSelect(_Symbol))
     {
      Print("OK");
      Open();
     }
  }
//+------------------------------------------------------------------+
double OnTester()
  {
   double p=profitc_divide_lossc();
   double s=max_series_loss();
   if(Tp<Stop) return 0;
   s=s+1;
   return p/s;
  }
//+------------------------------------------------------------------+
