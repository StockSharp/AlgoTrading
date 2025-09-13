//+------------------------------------------------------------------+
//|                                                   SymbolList.mq5 |
//|                                  Copyright 2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//| Write a list of symbols to the log.                              |
//+------------------------------------------------------------------+
#property script_show_inputs

#include <MQL5Book/PRTF.mqh>

input bool MarketWatchOnly = true;

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   const int n = SymbolsTotal(MarketWatchOnly);
   // list all symbols in Market Watch or available in general
   Print("Total symbol count: ", n);
   for(int i = 0; i < n; ++i)
   {
      PrintFormat("%4d %s", i, SymbolName(i, MarketWatchOnly));
   }
   // now incorrect (out of bound) request made intentionally to demo the error
   PRTF(SymbolName(n, MarketWatchOnly)); // MARKET_UNKNOWN_SYMBOL(4301)
}
//+------------------------------------------------------------------+
