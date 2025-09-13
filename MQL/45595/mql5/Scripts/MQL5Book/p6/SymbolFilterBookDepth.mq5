//+------------------------------------------------------------------+
//|                                        SymbolFilterBookDepth.mq5 |
//|                                  Copyright 2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//| Print out book depth stats for selected symbols                  |
//+------------------------------------------------------------------+
#property script_show_inputs

#include <MQL5Book/SymbolFilter.mqh>

input bool UseMarketWatch = false;
input int ShowSymbolsWithDepth = -1;

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   SymbolFilter f;                // filter object
   string symbols[];              // array for resulting names
   long depths[];                 // array for output
   MapArray<long,int> stats;      // counters per depth
   
   if(ShowSymbolsWithDepth > -1)
   {
      f.let(SYMBOL_TICKS_BOOKDEPTH, ShowSymbolsWithDepth);
   }
   
   // apply the filter and collect depths for symbols
   f.select(UseMarketWatch, SYMBOL_TICKS_BOOKDEPTH, symbols, depths);
   const int n = ArraySize(symbols);
   
   PrintFormat("===== Book depths for %s symbols %s=====",
      (UseMarketWatch ? "Market Watch" : "all available"),
      (ShowSymbolsWithDepth > -1 ? "(filtered by depth="
      + (string)ShowSymbolsWithDepth + ") " : ""));
   PrintFormat("Total symbols: %d", n);

   if(ShowSymbolsWithDepth > -1) // if specific depth is given, just list
   {
      ArrayPrint(symbols);
      return;
   }

   for(int i = 0; i < n; ++i)    // otherwise calculate stats
   {
      stats.inc(depths[i]);
   }
   
   Print("Stats per depth:");
   stats.print();
   Print("Legend: key=depth, value=count");
}
//+------------------------------------------------------------------+
/*

   example output (1-st run with default settings):
   
      ===== Book depths for all available symbols =====
      Total symbols: 52357
      Stats per depth:
          [key] [value]
      [0]     0   52244
      [1]     5       3
      [2]    10      67
      [3]    16       5
      [4]    20      13
      [5]    32      25
      Legend: key=depth, value=count

   example output (2-nd run, UseMarketWatch=false, ShowSymbolsWithDepth=32):
   
      ===== Book depths for all available symbols (filtered by depth=32) =====
      Total symbols: 25
      [ 0] "USDCNH" "USDZAR" "USDHUF" "USDPLN" "EURHUF" "EURNOK" "EURPLN" "EURSEK" "EURZAR" "GBPNOK" "GBPPLN" "GBPSEK" "GBPZAR"
      [13] "NZDCAD" "NZDCHF" "USDMXN" "EURMXN" "GBPMXN" "CADMXN" "CHFMXN" "MXNJPY" "NZDMXN" "USDCOP" "USDARS" "USDCLP"
   
*/
//+------------------------------------------------------------------+
