//+------------------------------------------------------------------+
//|                                                   MartGreg_1.mq5 |
//|                                             Copyright 2010, Alf. |
//|        http://forum.liteforex.org/showthread.php?p=6210#post6210 |
//+------------------------------------------------------------------+
#property copyright "Copyright 2010, Alf."
#property link      "http://forum.liteforex.org/showthread.php?p=6210#post6210"
#property version   "1.00"

#include <OnTesterFunctions.mqh>
#include <Martingail.mqh>
//--- input parameters
input double   DML=1000;
input int      Ud=1;
input int      Stop=500;
input int      Tp=1500;
input int      Slipage=50;
input int      MACD1Fast=5;
input int      MACD1Slow=20;
input int      MACD2Fast=10;
input int      MACD2Slow=15;

//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int m1=0;
int m2=0;


Martingail lt;
//+------------------------------------------------------------------+
//| Open                                                             |
//+------------------------------------------------------------------+
void Open()
  {
   double t[3];
   double k[2];
   int poz=0;
   if(CopyBuffer(m1,0,1,3,t)<0) return;
   if(CopyBuffer(m2,0,1,2,k)<0) return;

   MqlTick last_tick;
   SymbolInfoTick(_Symbol,last_tick);
   MqlTradeResult result;
   MqlTradeRequest request;
   ZeroMemory(result);
   ZeroMemory(request);
   
   HistorySelect(0,TimeCurrent());
   if(HistoryOrdersTotal()==0)
     {
      if(t[0]>t[1] && t[1]<t[2] && k[1]>k[0]) poz=1;
      if(t[0]<t[1] && t[1]>t[2] && k[1]<k[0]) poz=-1;
     }
   else
     {
      double cl=HistoryOrderGetDouble(HistoryOrderGetTicket(HistoryOrdersTotal()-1),ORDER_PRICE_OPEN);
      double op=HistoryOrderGetDouble(HistoryOrderGetTicket(HistoryOrdersTotal()-2),ORDER_PRICE_OPEN);
      long ot=HistoryOrderGetInteger(HistoryOrderGetTicket(HistoryOrdersTotal()-2),ORDER_TYPE);
      if(ot==ORDER_TYPE_BUY)
        {
         if(cl-op>0)
           {
            if(t[0]>t[1] && t[1]<t[2] && k[1]>k[0]) poz=1;
            if(t[0]<t[1] && t[1]>t[2] && k[1]<k[0]) poz=-1;
           }
         else poz=-1;

        }
      else
        {
         if(op-cl>0)
           {
            if(t[0]>t[1] && t[1]<t[2] && k[1]>k[0]) poz=1;
            if(t[0]<t[1] && t[1]>t[2] && k[1]<k[0]) poz=-1;
           }
         else poz=1;
        }
     }
   Print("Poz=",poz);
   request.symbol=_Symbol;
   request.magic=777;
   request.deviation=50;
   request.action=TRADE_ACTION_DEAL;
   request.type_filling=ORDER_FILLING_FOK;
   if(poz==1)
     {
      request.volume=lt.Lot();
      request.price=last_tick.ask;
      request.type=ORDER_TYPE_BUY;
      request.sl=last_tick.bid-Stop*Point();
      request.tp=last_tick.ask+Tp*Point();
      OrderSend(request,result);

     }
   if(poz==-1)
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
   m1=iMACD(_Symbol,0,MACD1Fast,MACD1Slow,3,PRICE_MEDIAN);
   m2=iMACD(_Symbol,0,MACD2Fast,MACD2Slow,3,PRICE_MEDIAN);
   lt.GVarName="PMG_1";
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
   if(!PositionSelect(_Symbol))Open();

  }
//+------------------------------------------------------------------+
//| Expert OnTester function                                         |
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
