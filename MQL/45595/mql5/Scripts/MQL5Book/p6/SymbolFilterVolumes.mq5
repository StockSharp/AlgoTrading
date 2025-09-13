//+------------------------------------------------------------------+
//|                                          SymbolFilterVolumes.mq5 |
//|                                  Copyright 2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//| Print out volume related limits for selected symbols             |
//+------------------------------------------------------------------+
#property script_show_inputs

#include <MQL5Book/SymbolFilter.mqh>

input bool MarketWatchOnly = true;
input double MinimalContractSize = 0;

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   SymbolFilter f;                      // filter object
   string symbols[];                    // array for resulting names
   double volumeLimits[][4];            // array for output data
   
   // specify properties to read from symbols
   ENUM_SYMBOL_INFO_DOUBLE volumeIds[] =
   {
      SYMBOL_VOLUME_MIN,
      SYMBOL_VOLUME_STEP,
      SYMBOL_VOLUME_MAX,
      SYMBOL_VOLUME_LIMIT
   };

   // apply filter by contract size (if larger than provided input)
   // and request volume-related specs for matched symbols
   f.let(SYMBOL_TRADE_CONTRACT_SIZE, MinimalContractSize, IS::GREATER)
   .select(MarketWatchOnly, volumeIds, symbols, volumeLimits);
   
   const int n = ArraySize(symbols);
   PrintFormat("===== Volume limits of the symbols (%d) =====", n);
   string title = "";
   for(int i = 0; i < ArraySize(volumeIds); ++i)
   {
      title += "\t" + EnumToString(volumeIds[i]);
   }
   Print(title);
   for(int i = 0; i < n; ++i)
   {
      Print(symbols[i]);
      ArrayPrint(volumeLimits, 3, NULL, i, 1, 0);
   }
}
//+------------------------------------------------------------------+
/*

   example output:
   
   ===== Volume limits of the symbols (13) =====
   SYMBOL_VOLUME_MIN SYMBOL_VOLUME_STEP SYMBOL_VOLUME_MAX SYMBOL_VOLUME_LIMIT
   EURUSD
     0.010   0.010 500.000   0.000
   GBPUSD
     0.010   0.010 500.000   0.000
   USDCHF
     0.010   0.010 500.000   0.000
   USDJPY
     0.010   0.010 500.000   0.000
   USDCNH
      0.010    0.010 1000.000    0.000
   USDRUB
      0.010    0.010 1000.000    0.000
   AUDUSD
     0.010   0.010 500.000   0.000
   NZDUSD
     0.010   0.010 500.000   0.000
   USDCAD
     0.010   0.010 500.000   0.000
   USDSEK
     0.010   0.010 500.000   0.000
   XAUUSD
     0.010   0.010 100.000   0.000
   BTCUSD
      0.010    0.010 1000.000    0.000
   SP500m
    0.100  0.100  5.000 15.000

*/
//+------------------------------------------------------------------+
