//+------------------------------------------------------------------+
//|                                      SymbolFilterMarginStats.mq5 |
//|                                  Copyright 2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//| Print out margin settings stats and per symbol                   |
//+------------------------------------------------------------------+
#property script_show_inputs

#include <MQL5Book/SymbolFilter.mqh>

input bool UseMarketWatch = false;
input bool ShowPerSymbolDetails = false;
input bool ExcludeZeroInitMargin = false;
input bool ExcludeZeroMainMargin = false;
input bool ExcludeZeroHedgeMargin = false;

//+------------------------------------------------------------------+
//| Composite struct to hold all requested properties per symbol     |
//+------------------------------------------------------------------+
struct MarginSettings
{
   string name;
   ENUM_SYMBOL_CALC_MODE calcMode;
   bool hedgeLeg;
   double initial;
   double maintenance;
   double hedged;
};

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   SymbolFilter f;                // filter object
   string symbols[];              // array for names
   long flags[][2];               // array of int/long tuples as output
   double values[][3];            // array of double tuples as output
   MarginSettings margins[];      // composite output
   
   MapArray<ENUM_SYMBOL_CALC_MODE,int> stats; // counters per mode
   int hedgeLeg = 0;                          // and other counters
   int zeroInit = 0;                          // ...
   int zeroMaintenance = 0;
   int zeroHedged = 0;
   
   // integer properties to read from symbols
   ENUM_SYMBOL_INFO_INTEGER ints[] =
   {
      SYMBOL_TRADE_CALC_MODE,
      SYMBOL_MARGIN_HEDGED_USE_LEG
   };
   
   // double properties to read from symbols
   ENUM_SYMBOL_INFO_DOUBLE doubles[] =
   {
      SYMBOL_MARGIN_INITIAL,
      SYMBOL_MARGIN_MAINTENANCE,
      SYMBOL_MARGIN_HEDGED
   };
   
   // add conditions if some specified
   if(ExcludeZeroInitMargin) f.let(SYMBOL_MARGIN_INITIAL, 0, IS::GREATER);
   if(ExcludeZeroMainMargin) f.let(SYMBOL_MARGIN_MAINTENANCE, 0, IS::GREATER);
   if(ExcludeZeroHedgeMargin) f.let(SYMBOL_MARGIN_HEDGED, 0, IS::GREATER);
   
   // apply the filter and collect integer flags for symbols
   f.select(UseMarketWatch, ints, symbols, flags);
   const int n = ArraySize(symbols);
   ArrayResize(symbols, 0, n);
   // apply the filter and collect double flags for symbols
   f.select(UseMarketWatch, doubles, symbols, values);

   if(ShowPerSymbolDetails) ArrayResize(margins, n);
   
   // calculate statistics and assemble integer and double properties
   // per symbol into common struct with symbol name
   for(int i = 0; i < n; ++i)
   {
      stats.inc((ENUM_SYMBOL_CALC_MODE)flags[i][0]);
      hedgeLeg += (int)flags[i][1];
      if(values[i][0] == 0) zeroInit++;
      if(values[i][1] == 0) zeroMaintenance++;
      if(values[i][2] == 0) zeroHedged++;
      
      if(ShowPerSymbolDetails)
      {
         margins[i].name = symbols[i];
         margins[i].calcMode = (ENUM_SYMBOL_CALC_MODE)flags[i][0];
         margins[i].hedgeLeg = (bool)flags[i][1];
         margins[i].initial = values[i][0];
         margins[i].maintenance = values[i][1];
         margins[i].hedged = values[i][2];
      }
   }
   PrintFormat("===== Margin calculation modes for %s symbols %s=====",
      (UseMarketWatch ? "Market Watch" : "all available"),
      (ExcludeZeroInitMargin || ExcludeZeroMainMargin || ExcludeZeroHedgeMargin
         ? "(with conditions) " : ""));
   PrintFormat("Total symbols: %d", n);
   PrintFormat("Hedge leg used in: %d", hedgeLeg);
   PrintFormat("Zero margin counts: initial=%d, maintenance=%d, hedged=%d",
      zeroInit, zeroMaintenance, zeroHedged);
   
   Print("Stats per calculation mode:");
   stats.print();
   Print("Legend: key=calculation mode, value=count");
   for(int i = 0; i < stats.getSize(); ++i)
   {
      PrintFormat("%d -> %s", stats.getKey(i), EnumToString(stats.getKey(i)));
   }
   
   if(ShowPerSymbolDetails)
   {
      Print("Settings per symbol:");
      ArrayPrint(margins);
   }
}
//+------------------------------------------------------------------+
/*

   example output (1-st run with default settings):
   
      ===== Margin calculation modes for all available symbols =====
      Total symbols: 131
      Hedge leg used in: 14
      Zero margin counts: initial=123, maintenance=130, hedged=32
      Stats per calculation mode:
          [key] [value]
      [0]     0     101
      [1]     4      16
      [2]     1       1
      [3]     2      11
      [4]     5       2
      Legend: key=calculation mode, value=count
      0 -> SYMBOL_CALC_MODE_FOREX
      4 -> SYMBOL_CALC_MODE_CFDLEVERAGE
      1 -> SYMBOL_CALC_MODE_FUTURES
      2 -> SYMBOL_CALC_MODE_CFD
      5 -> SYMBOL_CALC_MODE_FOREX_NO_LEVERAGE

   example output (2-nd run, ShowPerSymbolDetails=true, ExcludeZeroInitMargin=true):
   
      ===== Margin calculation modes for all available symbols (with conditions) =====
      Total symbols: 8
      Hedge leg used in: 0
      Zero margin counts: initial=0, maintenance=7, hedged=0
      Stats per calculation mode:
          [key] [value]
      [0]     0       5
      [1]     1       1
      [2]     5       2
      Legend: key=calculation mode, value=count
      0 -> SYMBOL_CALC_MODE_FOREX
      1 -> SYMBOL_CALC_MODE_FUTURES
      5 -> SYMBOL_CALC_MODE_FOREX_NO_LEVERAGE
      Settings per symbol:
            [name] [calcMode] [hedgeLeg]    [initial] [maintenance]    [hedged]
      [0] "XAUEUR"          0      false    100.00000       0.00000    50.00000
      [1] "XAUAUD"          0      false    100.00000       0.00000   100.00000
      [2] "XAGEUR"          0      false   1000.00000       0.00000  1000.00000
      [3] "USDGEL"          0      false 100000.00000  100000.00000 50000.00000
      [4] "SP500m"          1      false   6600.00000       0.00000  6600.00000
      [5] "XBRUSD"          5      false    100.00000       0.00000    50.00000
      [6] "XNGUSD"          0      false  10000.00000       0.00000 10000.00000
      [7] "XTIUSD"          5      false    100.00000       0.00000    50.00000
   
*/
//+------------------------------------------------------------------+
