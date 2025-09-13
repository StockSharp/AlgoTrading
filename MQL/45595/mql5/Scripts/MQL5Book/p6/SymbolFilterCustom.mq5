//+------------------------------------------------------------------+
//|                                           SymbolFilterCustom.mq5 |
//|                                  Copyright 2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//| Print out custom symbols                                         |
//+------------------------------------------------------------------+
#property script_show_inputs

#include <MQL5Book/SymbolFilter.mqh>

input bool UseMarketWatch = false;

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   SymbolFilter f;                // filter object
   string symbols[];              // array for resulting names
   string formulae[];             // array for formulae

   // apply the filter and collect custom symbols
   f.let(SYMBOL_CUSTOM, true)
   .select(UseMarketWatch, SYMBOL_FORMULA, symbols, formulae);
   const int n = ArraySize(symbols);
   
   PrintFormat("===== %s custom symbols =====",
      (UseMarketWatch ? "Market Watch" : "All available"));
   PrintFormat("Total symbols: %d", n);

   for(int i = 0; i < n; ++i)
   {
      Print(symbols[i], " ", formulae[i]);
   }
}
//+------------------------------------------------------------------+
/*

   example output:
   
      ===== All available custom symbols =====
      Total symbols: 1
      xxx SP500m/UK100
   
*/
//+------------------------------------------------------------------+
