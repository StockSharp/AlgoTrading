//+------------------------------------------------------------------+
//|                                                    EasyRobot.mq4 |
//|                                      Copyright 2020, cs software |
//|                             https://www.mql5.com/en/users/kenpar |
//+------------------------------------------------------------------+
#property copyright "Copyright 2020, cs software"
#property link      "https://www.mql5.com/en/users/kenpar"
#property version   "1.05"
#property strict
#include <CSer_std.mqh>
//---
double
NewPoint;
//---
int
ticket=0;
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
   NewPoint=PointValue();
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {

  }
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
  {
   if(IsTesting())
      if(Tstep>Tstart)
         return;
   if(IsNewBar())
     {
      if(UseTstop&&GetPosition()!=0)
         Trail();
      Entry();
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void Entry()
  {
   if(iOpen(Symbol(),PERIOD_H1,1) < iClose(Symbol(),PERIOD_H1,1))
      if(GetPosition()==0)
         SendBuy(TakeFactor,StopFactor);
   if(iOpen(Symbol(),PERIOD_H1,1) > iClose(Symbol(),PERIOD_H1,1))
      if(GetPosition()==0)
         SendSell(TakeFactor,StopFactor);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void SendBuy(double _tf,double _sf)
  {
   double
   _otf = (ATR() * (Zd(_tf,NewPoint))),
   _osf = (ATR() * (Zd(_sf,NewPoint))),
   _tp = Ask + SC(_otf * NewPoint),
   _sl = Bid - SC(_osf * NewPoint),
   Contract=CheckVolumeValue(Symbol(),0.01);
   if(CheckMoneyForTrade(Symbol(),OP_BUY,Contract))
      ticket = OrderSend(Symbol(),OP_BUY,Contract,Ask,5,_sl,_tp,NULL,1234567,0,Green);
   if(ticket<0)
      return;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void SendSell(double _tf,double _sf)
  {
   double
   _otf = (ATR() * (Zd(_tf,NewPoint))),
   _osf = (ATR() * (Zd(_sf,NewPoint))),
   _tp = Bid - SC(_otf * NewPoint),
   _sl = Ask + SC(_osf * NewPoint),
   Contract=CheckVolumeValue(Symbol(),0.01);
   if(CheckMoneyForTrade(Symbol(),OP_SELL,Contract))
      ticket = OrderSend(Symbol(),OP_SELL,Contract,Bid,5,_sl,_tp,NULL,1234567,0,Green);
   if(ticket<0)
      return;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double ATR()
  {
   return(iATR(Symbol(),PERIOD_H1,14,1));
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int GetPosition()
  {
   int posval=0;
   for(int e = OrdersTotal() - 1; e >= 0; e--)
     {
      if(!OrderSelect(e, SELECT_BY_POS))
         break;
      if(OrderSymbol()!=Symbol() && OrderMagicNumber()!=1234567)
         continue;
      if(OrderCloseTime() == 0 && OrderSymbol()==Symbol() && OrderMagicNumber()==1234567)
        {
         if(OrderType() == OP_BUY||OrderType() == OP_SELL)
            posval = 1;
         if(!(OrderType() == OP_BUY||OrderType() == OP_SELL))
            posval = -1;
        }
     }
   return(posval);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void Trail()
  {
   double
   stops=MarketInfo(Symbol(),MODE_STOPLEVEL)*NewPoint;
   for(int u=0; u<OrdersTotal(); u++)
     {
      if(!OrderSelect(u,SELECT_BY_POS,MODE_TRADES))
         break;
      if(OrderSymbol()!=Symbol() && OrderMagicNumber()!=1234567)
         continue;
      if(OrderSymbol()==Symbol() && OrderMagicNumber()==1234567)
        {
         if(OrderType()==OP_BUY && Bid-Tstart*NewPoint>OrderOpenPrice())
           {
            double nsb=NormalizeDouble(Bid-Tstep*NewPoint,Digits);
            if(nsb>OrderStopLoss() || OrderStopLoss()==0)
              {
               if(nsb<Bid-stops)
                 {
                  if(!OrderModify(OrderTicket(),OrderOpenPrice(),nsb,OrderTakeProfit(),0,Blue))
                    {
                     Print(GetLastError());
                     return;
                    }
                 }
              }
           }
         if(OrderType()==OP_SELL && Ask+Tstart*NewPoint<OrderOpenPrice())
           {
            double nss=NormalizeDouble(Ask+Tstep*NewPoint,Digits);
            if(nss<OrderStopLoss() || OrderStopLoss()==0)
              {
               if(Ask+stops<nss)
                 {
                  if(!OrderModify(OrderTicket(),OrderOpenPrice(),nss,OrderTakeProfit(),0,Blue))
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
bool IsNewBar()
  {
   static datetime lastbar;
   datetime curbar = iTime(Symbol(),PERIOD_H1,0);
   if(lastbar!=curbar)
     {
      lastbar=curbar;
      return (true);
     }
   return(false);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double CheckVolumeValue(string sym,double vol)
  {
   double min_volume=SymbolInfoDouble(sym,SYMBOL_VOLUME_MIN);
   if(vol<min_volume)
      return(min_volume);
   double max_volume=SymbolInfoDouble(sym,SYMBOL_VOLUME_MAX);
   if(vol>max_volume)
      return(max_volume);
   double volume_step=SymbolInfoDouble(sym,SYMBOL_VOLUME_STEP);
   int ratio=(int)MathRound(vol/volume_step);
   if(MathAbs(ratio*volume_step-vol)>0.0000001)
      return(ratio*volume_step);
   return(vol);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CheckMoneyForTrade(string symb,int type,double lots)
  {
   double free_margin=AccountFreeMarginCheck(symb,type,lots);
   if(free_margin<0)
     {
      string oper=(type==OP_BUY)? "Buy":"Sell";
      Print("Not enough money for ",oper," ",lots," ",symb," Error code=",GetLastError());
      return(false);
     }
//--- checking successful
   return(true);
  }
//+------------------------------------------------------------------+
