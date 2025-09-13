//+------------------------------------------------------------------+
//|                                         SymbolFilterExecMode.mq5 |
//|                                  Copyright 2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//| Print out execution and filling modes for selected symbols       |
//+------------------------------------------------------------------+
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
      SYMBOL_TRADE_EXEMODE,
      SYMBOL_FILLING_MODE
   };

   // apply the filter and build symbol list
   f.select(true, modes, symbols, permissions);
   
   const int n = ArraySize(symbols);
   PrintFormat("===== Trade execution and filling modes for the symbols (%d) =====", n);
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

   ===== Trade execution and filling modes for the symbols (13) =====
   EURUSD:
     SYMBOL_TRADE_EXECUTION_INSTANT (1)
     [_SYMBOL_FILLING_FOK ] (1)
   GBPUSD:
     SYMBOL_TRADE_EXECUTION_INSTANT (1)
     [ _SYMBOL_FILLING_FOK ] (1)
   ...
   USDCNH:
     SYMBOL_TRADE_EXECUTION_INSTANT (1)
     [ _SYMBOL_FILLING_FOK _SYMBOL_FILLING_IOC ] (3)
   USDRUB:
     SYMBOL_TRADE_EXECUTION_INSTANT (1)
     [ _SYMBOL_FILLING_IOC ] (2)
   AUDUSD:
     SYMBOL_TRADE_EXECUTION_INSTANT (1)
     [ _SYMBOL_FILLING_FOK ] (1)
   NZDUSD:
     SYMBOL_TRADE_EXECUTION_INSTANT (1)
     [ _SYMBOL_FILLING_FOK _SYMBOL_FILLING_IOC ] (3)
   ...
   XAUUSD:
     SYMBOL_TRADE_EXECUTION_INSTANT (1)
     [ _SYMBOL_FILLING_FOK _SYMBOL_FILLING_IOC ] (3)
   BTCUSD:
     SYMBOL_TRADE_EXECUTION_INSTANT (1)
     [(_SYMBOL_FILLING_RETURN)] (0)
   SP500m:
     SYMBOL_TRADE_EXECUTION_MARKET (2)
     [ _SYMBOL_FILLING_FOK ] (1)
   
*/
//+------------------------------------------------------------------+
