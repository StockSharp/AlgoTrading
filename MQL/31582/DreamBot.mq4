//+------------------------------------------------------------------+
//|                                                     DreamBot.mq4 |
//|                                      Copyright 2020, cs software |
//|                                   https://cs-robots5.webnode.se/ |
//+------------------------------------------------------------------+
#property copyright   "Copyright 2020, cs software"
#property link        "https://cs-robots5.webnode.se/"
#property description "Any account type,time frame,leverage and deposit"
#property version     "1.03"
#property strict
#include <CSstd.mqh>
//---
int
Ticket=0;
double
iFOR[3];
string
dir;
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
   if(!tcheck())
     {
      ExpertRemove();
      return(INIT_PARAMETERS_INCORRECT);
     }
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
//---
  }
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
  {
   if(IsTesting())
     {
      if(TrailingStep>TrailingStart||TrailingStart>=TakeProfit)
         return;
     }
   if(IsNewBar())
     {
      if(Utr)
         Tfunc(TrailingStart,TrailingStep);
      EntrySignal();
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void EntrySignal()
  {
   for(int i=0; i<3; i++)
      iFOR[i]=iForce(NULL,PERIOD_H1,13,MODE_SMA,PRICE_CLOSE,i);
//---
   if(iFOR[1]>BullsPwr&&iFOR[2]<BullsPwr)
      if(CheckPositions()==0)
         OpBuy(TakeProfit,StopLoss);
   if(iFOR[1]<BearsPwr&&iFOR[2]>BearsPwr)
      if(CheckPositions()==0)
         OpSell(TakeProfit,StopLoss);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void OpBuy(int _take,int _stop)
  {
   double
   _SL = 0,
   _TP = 0,
   Contract = CheckVolumeValue(0.01);
   if(CheckMoneyForTrade(Symbol(),Contract,OP_BUY))
      Ticket = OrderSend(Symbol(),OP_BUY,Contract,Ask,5,0,0,NULL,1234567,0,Green);
   if(OrderSelect(Ticket, SELECT_BY_TICKET, MODE_TRADES))
     {
      _TP = Ask + SC(_take) * Point;
      _SL = Bid - SC(_stop) * Point;
      if(!OrderModify(OrderTicket(), OrderOpenPrice(), _SL, _TP, 0))
        {
         Print(GetLastError());
         return;
        }
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void OpSell(int _take,int _stop)
  {
   double
   _SL = 0,
   _TP = 0,
   Contract = CheckVolumeValue(0.01);
   if(CheckMoneyForTrade(Symbol(),Contract,OP_SELL))
      Ticket = OrderSend(Symbol(),OP_SELL,Contract,Bid,5,0,0,NULL,1234567,0,Green);
   if(OrderSelect(Ticket, SELECT_BY_TICKET, MODE_TRADES))
     {
      _TP = Bid - SC(_take) * Point;
      _SL = Ask + SC(_stop) * Point;
      if(!OrderModify(OrderTicket(), OrderOpenPrice(), _SL, _TP, 0))
        {
         Print(GetLastError());
         return;
        }
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void Tfunc(int _start,int _step)
  {
//---
   double
   stops=MarketInfo(Symbol(),MODE_STOPLEVEL)*Point,
   NewPoint,
   nsb,
   nss;
   int
   Tstart=0,
   Tstep=0;
//---
   NewPoint=Point;
   Tstart=_step;
   Tstep=_start;
//---
   for(int x=0; x<OrdersTotal(); x++)
     {
      if(!OrderSelect(x,SELECT_BY_POS,MODE_TRADES))
         break;
      if((OrderSymbol()!=Symbol()) && (OrderMagicNumber()!=1234567))
         continue;
      if((OrderSymbol()==Symbol()) && (OrderMagicNumber()==1234567))
        {
         if(OrderType()==OP_BUY && Bid-Tstart*NewPoint>OrderOpenPrice())
           {
            nsb=NormalizeDouble(Bid-Tstep*NewPoint,Digits);
            if(nsb>OrderStopLoss() || OrderStopLoss()==0)
              {
               if(nsb<Bid-stops*NewPoint)
                 {
                  if(!OrderModify(OrderTicket(),OrderOpenPrice(),nsb,OrderTakeProfit(),0,clrGreen))
                    {
                     Print(GetLastError());
                     return;
                    }
                 }
              }
           }
         if(OrderType()==OP_SELL && Ask+Tstart*NewPoint<OrderOpenPrice())
           {
            nss=NormalizeDouble(Ask+Tstep*NewPoint,Digits);
            if(nss<OrderStopLoss() || OrderStopLoss()==0)
              {
               if(Ask+stops*NewPoint<nss)
                 {
                  if(!OrderModify(OrderTicket(),OrderOpenPrice(),nss,OrderTakeProfit(),0,clrRed))
                    {
                     Print(GetLastError());
                     return;
                    }
                 }
              }
           }
        }
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int CheckPositions()
  {
   int val=0;
   for(int x = OrdersTotal() - 1; x >= 0; x--)
     {
      if(!OrderSelect(x, SELECT_BY_POS))
         break;
      if(OrderSymbol()!=Symbol() && OrderMagicNumber()!=1234567)
         continue;
      if((OrderCloseTime() == 0) && OrderSymbol()==Symbol() && OrderMagicNumber()==1234567)
        {
         if(OrderType() == OP_BUY||OrderType() == OP_SELL)
            val = 1;
         if(!(OrderType() == OP_BUY||OrderType() == OP_SELL))
            val = -1;
        }
     }
   return(val);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool IsNewBar()
  {
   static datetime BarLast;
   datetime BarCurrent = iTime(Symbol(),PERIOD_H1,0);
   if(BarLast!=BarCurrent)
     {
      BarLast=BarCurrent;
      return(true);
     }
   return(false);
  }
//+------------------------------------------------------------------+
