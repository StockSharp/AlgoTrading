//+------------------------------------------------------------------+
//|                                        SymbolFilterTradeMode.mq5 |
//|                                  Copyright 2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//| Print out trade mode permissions for selected symbols            |
//+------------------------------------------------------------------+
#property script_show_inputs

#include <MQL5Book/SymbolFilter.mqh>

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   SymbolFilter f;                      // filter object
   string symbols[];                    // array for resulting names
   long permissions[][2];               // array for output data
   
   // specify properties to read from symbols
   ENUM_SYMBOL_INFO_INTEGER modes[] =
   {
      SYMBOL_TRADE_MODE,
      SYMBOL_ORDER_MODE
   };

   // apply the filter and build symbol list
   f.let(SYMBOL_VISIBLE, true).select(true, modes, symbols, permissions);
   
   const int n = ArraySize(symbols);
   PrintFormat("===== Trade permissions for the symbols (%d) =====", n);
   for(int i = 0; i < n; ++i)
   {
      Print(symbols[i] + ":");
      for(int j = 0; j < ArraySize(modes); ++j)
      {
         // show properties as user-friendly names and "as is" (numbers)
         PrintFormat("  %s (%d)",
            SymbolMonitor::stringify(permissions[i][j], modes[j]),
            permissions[i][j]);
      }
   }
}
//+------------------------------------------------------------------+
/*

   example output (excerpt):
   
   ===== Trade permissions for the symbols (13) =====
   EURUSD:
     SYMBOL_TRADE_MODE_FULL (4)
     [ _SYMBOL_ORDER_MARKET _SYMBOL_ORDER_LIMIT _SYMBOL_ORDER_STOP _SYMBOL_ORDER_STOP_LIMIT _SYMBOL_ORDER_SL _SYMBOL_ORDER_TP _SYMBOL_ORDER_CLOSEBY ] (127)
   GBPUSD:
     SYMBOL_TRADE_MODE_FULL (4)
     [ _SYMBOL_ORDER_MARKET _SYMBOL_ORDER_LIMIT _SYMBOL_ORDER_STOP _SYMBOL_ORDER_STOP_LIMIT _SYMBOL_ORDER_SL _SYMBOL_ORDER_TP _SYMBOL_ORDER_CLOSEBY ] (127)
   ...
   SP500m:
     SYMBOL_TRADE_MODE_DISABLED (0)
     [ _SYMBOL_ORDER_MARKET _SYMBOL_ORDER_LIMIT _SYMBOL_ORDER_STOP _SYMBOL_ORDER_STOP_LIMIT _SYMBOL_ORDER_SL _SYMBOL_ORDER_TP ] (63)

*/
//+------------------------------------------------------------------+
