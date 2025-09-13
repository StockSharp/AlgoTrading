//+------------------------------------------------------------------+
//|                                       SymbolFilterExpiration.mq5 |
//|                                  Copyright 2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//| Print out expiration modes for selected symbols                  |
//+------------------------------------------------------------------+
#property script_show_inputs

#include <MQL5Book/SymbolFilter.mqh>

input bool UseMarketWatch = false;
input bool ShowPerSymbolDetails = false;

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   SymbolFilter f;                // filter object
   string symbols[];              // array for names
   long flags[][2];               // array of int/long tuples as output
   
   MapArray<SYMBOL_EXPIRATION,int> stats;        // counters per mode
   MapArray<ENUM_SYMBOL_ORDER_GTC_MODE,int> gtc; // counters per GTC
   
   // integer properties to read from symbols
   ENUM_SYMBOL_INFO_INTEGER ints[] =
   {
      SYMBOL_EXPIRATION_MODE,
      SYMBOL_ORDER_GTC_MODE
   };
   
   // apply the filter and collect flags for symbols
   f.select(UseMarketWatch, ints, symbols, flags);
   const int n = ArraySize(symbols);
   
   for(int i = 0; i < n; ++i)
   {
      if(ShowPerSymbolDetails)
      {
         Print(symbols[i] + ":");
         for(int j = 0; j < ArraySize(ints); ++j)
         {
            // show properties as user-friendly names and "as is" (numbers)
            PrintFormat("  %s (%d)",
               SymbolMonitor::stringify(flags[i][j], ints[j]),
               flags[i][j]);
         }
      }
      
      const SYMBOL_EXPIRATION mode = (SYMBOL_EXPIRATION)flags[i][0];
      for(int j = 0; j < 4; ++j)
      {
         const SYMBOL_EXPIRATION bit = (SYMBOL_EXPIRATION)(1 << j);
         if((mode & bit) != 0)
         {
            stats.inc(bit);
         }

         if(bit == SYMBOL_EXPIRATION_GTC)
         {
            gtc.inc((ENUM_SYMBOL_ORDER_GTC_MODE)flags[i][1]);
         }
      }
   }

   PrintFormat("===== Expiration modes for %s symbols =====",
      (UseMarketWatch ? "Market Watch" : "all available"));
   PrintFormat("Total symbols: %d", n);
   
   Print("Stats per expiration mode:");
   stats.print();
   Print("Legend: key=expiration mode, value=count");
   for(int i = 0; i < stats.getSize(); ++i)
   {
      PrintFormat("%d -> %s", stats.getKey(i), EnumToString(stats.getKey(i)));
   }
   Print("Stats per GTC mode:");
   gtc.print();
   Print("Legend: key=GTC mode, value=count");
   for(int i = 0; i < gtc.getSize(); ++i)
   {
      PrintFormat("%d -> %s", gtc.getKey(i), EnumToString(gtc.getKey(i)));
   }
}
//+------------------------------------------------------------------+
/*

   example output (1-st run with default settings):
   
      ===== Expiration modes for all available symbols =====
      Total symbols: 52357
      Stats per expiration mode:
          [key] [value]
      [0]     1   52357
      [1]     2   52357
      [2]     4   52357
      [3]     8   52303
      Legend: key=expiration mode, value=count
      1 -> _SYMBOL_EXPIRATION_GTC
      2 -> _SYMBOL_EXPIRATION_DAY
      4 -> _SYMBOL_EXPIRATION_SPECIFIED
      8 -> _SYMBOL_EXPIRATION_SPECIFIED_DAY
      Stats per GTC mode:
          [key] [value]
      [0]     0   52357
      Legend: key=GTC mode, value=count
      0 -> SYMBOL_ORDERS_GTC

   example excerpt (2-nd run, UseMarketWatch=true, ShowPerSymbolDetails=true):
   
      EURUSD:
        [ _SYMBOL_EXPIRATION_GTC _SYMBOL_EXPIRATION_DAY _SYMBOL_EXPIRATION_SPECIFIED _SYMBOL_EXPIRATION_SPECIFIED_DAY ] (15)
        SYMBOL_ORDERS_GTC (0)
      GBPUSD:
        [ _SYMBOL_EXPIRATION_GTC _SYMBOL_EXPIRATION_DAY _SYMBOL_EXPIRATION_SPECIFIED ] (7)
        SYMBOL_ORDERS_GTC (0)
      USDCHF:
        [ _SYMBOL_EXPIRATION_GTC _SYMBOL_EXPIRATION_DAY _SYMBOL_EXPIRATION_SPECIFIED ] (7)
        SYMBOL_ORDERS_GTC (0)
      USDJPY:
        [ _SYMBOL_EXPIRATION_GTC _SYMBOL_EXPIRATION_DAY _SYMBOL_EXPIRATION_SPECIFIED ] (7)
        SYMBOL_ORDERS_GTC (0)
      ...
      XAUUSD:
        [ _SYMBOL_EXPIRATION_GTC _SYMBOL_EXPIRATION_DAY _SYMBOL_EXPIRATION_SPECIFIED _SYMBOL_EXPIRATION_SPECIFIED_DAY ] (15)
        SYMBOL_ORDERS_GTC (0)
      SP500m:
        [ _SYMBOL_EXPIRATION_GTC _SYMBOL_EXPIRATION_DAY _SYMBOL_EXPIRATION_SPECIFIED _SYMBOL_EXPIRATION_SPECIFIED_DAY ] (15)
        SYMBOL_ORDERS_GTC (0)
      UK100:
        [ _SYMBOL_EXPIRATION_GTC _SYMBOL_EXPIRATION_DAY _SYMBOL_EXPIRATION_SPECIFIED _SYMBOL_EXPIRATION_SPECIFIED_DAY ] (15)
        SYMBOL_ORDERS_GTC (0)
      ===== Expiration modes for Market Watch symbols =====
      Total symbols: 15
      Stats per expiration mode:
          [key] [value]
      [0]     1      15
      [1]     2      15
      [2]     4      15
      [3]     8       6
      Legend: key=expiration mode, value=count
      1 -> _SYMBOL_EXPIRATION_GTC
      2 -> _SYMBOL_EXPIRATION_DAY
      4 -> _SYMBOL_EXPIRATION_SPECIFIED
      8 -> _SYMBOL_EXPIRATION_SPECIFIED_DAY
      Stats per GTC mode:
          [key] [value]
      [0]     0      15
      Legend: key=GTC mode, value=count
      0 -> SYMBOL_ORDERS_GTC
   
*/
//+------------------------------------------------------------------+
