//+------------------------------------------------------------------+
//|                                           TrailingTakeProfit.mq5 |
//|                                  Copyright 2023, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright "Copyright 2023, MetaQuotes Ltd."
#property link      "https://www.mql5.com"
#property version   "1.00"
#include <Trade\Trade.mqh>
#include <Trade\PositionInfo.mqh>
#include <Trade\SymbolInfo.mqh>

//Introducing input variables

input bool TrailingTP_Mode = true;   
input string Symbol_Name = "GBPUSD";

input double TrailingTPStart = 200;   
input double TrailingTPDistance = 200;

CTrade trading;
CPositionInfo positioning;
CSymbolInfo symbolling;

//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
//---

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
if(TrailingTP_Mode)  // If true The EA runs
 TrailingTakeProfit(TrailingTPStart,TrailingTPDistance); 
// TrailingTPStart is the points from current price which Trailing take profit starts to trail price.
// TrailingTPDistance is the maximum of distance between price and take profit. 

  }
//+------------------------------------------------------------------+




//+------------------------------------------------------------------+
//|              trailing takeprofit                                 |
//+------------------------------------------------------------------+
void TrailingTakeProfit(double STtp, double Distp)
  {

   for(int i = PositionsTotal()-1 ; i>=0 ; i--)
     {
      if(positioning.SelectByIndex(i))
        {
         if(positioning.Symbol()== Symbol_Name)
           {

            if(positioning.PositionType() == POSITION_TYPE_BUY)
              {
               if(positioning.PriceOpen() - positioning.PriceCurrent() > STtp * Point())
                 {
                  if(positioning.TakeProfit() > positioning.PriceCurrent() + Distp * Point())

                    {
                     trading.PositionModify(PositionGetTicket(i),positioning.StopLoss(),positioning.PriceCurrent() + Distp *Point());
                     symbolling.RefreshRates();
                    }
                 }
              }

            else
               if(positioning.PositionType() == POSITION_TYPE_SELL)
                 {
                  if(positioning.PriceCurrent() - positioning.PriceOpen()  > STtp * Point())
                    {
                     if(positioning.TakeProfit() < positioning.PriceCurrent() - Distp * Point())
                       {
                        trading.PositionModify(PositionGetTicket(i),positioning.StopLoss(),positioning.PriceCurrent() - Distp * Point());
                        symbolling.RefreshRates();
                       }

                    }

                 }
           }

        }
     }
  }
//+------------------------------------------------------------------+
//+------------------------------------------------------------------+
