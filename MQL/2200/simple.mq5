//+------------------------------------------------------------------+
//|                                                        roman.mq5 |
//|                                       Copyright 2013,Viktor Moss |
//|                                              http://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright "Copyright 2013,Viktor Moss"
#property link      "https://login.mql5.com/users/vicmos"
#property version   "1.00"

#include <Trade\Trade.mqh>
//--- input parameters
input double   Lot=0.1; // Lots
input int      TP=46;   // Take Profit
input int      SL=31;   // Stop Loss

CTrade trade;
bool   bs=true; //true-buy  false-sell
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
//---
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
//---
   double pr=0;
   if(!PositionSelect(_Symbol)) //no position
     {
      if(bs) trade.Buy(Lot);
      else   trade.Sell(Lot);
     }
   else // there is a position
     {
      if(PositionGetInteger(POSITION_TYPE)==POSITION_TYPE_BUY)
        {
         pr=(SymbolInfoDouble(_Symbol,SYMBOL_BID)-PositionGetDouble(POSITION_PRICE_OPEN))/_Point;
         if(pr>=TP)
           {
            trade.PositionClose(_Symbol);
            bs=true;//buy
           }
         if(pr<=-SL)
           {
            trade.PositionClose(_Symbol);
            bs=false;//sell
           }
        }
      if(PositionGetInteger(POSITION_TYPE)==POSITION_TYPE_SELL)
        {
         pr=(PositionGetDouble(POSITION_PRICE_OPEN)-SymbolInfoDouble(_Symbol,SYMBOL_ASK))/_Point;
         if(pr>=TP)
           {
            trade.PositionClose(_Symbol);
            bs=false;//sell
           }
         if(pr<=-SL)
           {
            trade.PositionClose(_Symbol);
            bs=true;//buy
           }
        }
     }
  }
//+------------------------------------------------------------------+
