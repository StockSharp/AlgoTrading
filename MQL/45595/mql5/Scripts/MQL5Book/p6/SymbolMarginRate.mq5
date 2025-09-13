//+------------------------------------------------------------------+
//|                                             SymbolMarginRate.mq5 |
//|                                  Copyright 2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//| Write a list of symbols and their margin rates to the log.       |
//+------------------------------------------------------------------+
#property script_show_inputs

#include <MQL5Book/MqlError.mqh>

input bool MarketWatchOnly = true;
input ENUM_ORDER_TYPE OrderType = ORDER_TYPE_BUY;

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   const int n = SymbolsTotal(MarketWatchOnly);
   // list all symbols in Market Watch or available in general
   PrintFormat("Margin rates per symbol for %s:", EnumToString(OrderType));
   for(int i = 0; i < n; ++i)
   {
      const string s = SymbolName(i, MarketWatchOnly);
      double initial = 1.0, maintenance = 1.0;
      if(!SymbolInfoMarginRate(s, OrderType, initial, maintenance))
      {
         PrintFormat("Error: %s(%d)", E2S(_LastError), _LastError);
      }
      PrintFormat("%4d %s = %f %f", i, s, initial, maintenance);
   }
}
//+------------------------------------------------------------------+
