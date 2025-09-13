//+------------------------------------------------------------------+
//|                                           SymbolFilterSpread.mq5 |
//|                                  Copyright 2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//| Print out spread and freeze levels for selected symbols          |
//+------------------------------------------------------------------+
#property script_show_inputs

#include <MQL5Book/SymbolFilter.mqh>

//+------------------------------------------------------------------+
//| Enum with part of elements of ENUM_SYMBOL_INFO_INTEGER           |
//+------------------------------------------------------------------+
enum ENUM_SYMBOL_INFO_INTEGER_PART
{
   SPREAD_FIXED = SYMBOL_SPREAD,
   SPREAD_FLOAT = SYMBOL_SPREAD_FLOAT,
   STOPS_LEVEL = SYMBOL_TRADE_STOPS_LEVEL,
   FREEZE_LEVEL = SYMBOL_TRADE_FREEZE_LEVEL
};

input bool UseMarketWatch = true;
input ENUM_SYMBOL_INFO_INTEGER_PART Property = SPREAD_FIXED;
input bool ShowPerSymbolDetails = true;

//+------------------------------------------------------------------+
//| Composite struct to hold symbol namd and its requested propery   |
//+------------------------------------------------------------------+
struct SymbolDistance
{
   string name;
   int value;
};

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   SymbolFilter f;                // filter object
   string symbols[];              // array for names
   long values[];                 // array for values
   SymbolDistance distances[];    // array for detailed printout
   MapArray<long,int> stats;      // counters per specific value of selected property

   // apply the filter and collect values for symbols, then sort
   f.select(UseMarketWatch, (ENUM_SYMBOL_INFO_INTEGER)Property, symbols, values, true);
   const int n = ArraySize(symbols);
   if(ShowPerSymbolDetails) ArrayResize(distances, n);
   
   // calculate stats and collect details
   for(int i = 0; i < n; ++i)
   {
      stats.inc(values[i]);
      if(ShowPerSymbolDetails)
      {
         distances[i].name = symbols[i];
         distances[i].value = (int)values[i];
      }
   }

   PrintFormat("===== Distances for %s symbols =====",
      (UseMarketWatch ? "Market Watch" : "all available"));
   PrintFormat("Total symbols: %d", n);
   
   PrintFormat("Stats per %s:", EnumToString((ENUM_SYMBOL_INFO_INTEGER)Property));
   stats.print();
   
   if(ShowPerSymbolDetails)
   {
      Print("Details per symbol:");
      ArrayPrint(distances);
   }
}
//+------------------------------------------------------------------+
/*

   example output (1-st run with default settings):
   
      ===== Distances for Market Watch symbols =====
      Total symbols: 13
      Stats per SYMBOL_SPREAD:
          [key] [value]
      [0]     0       2
      [1]     2       3
      [2]     3       1
      [3]     6       1
      [4]     7       1
      [5]     9       1
      [6]   151       1
      [7]   319       1
      [8]  3356       1
      [9]  3400       1
      Details per symbol:
             [name] [value]
      [ 0] "USDJPY"       0
      [ 1] "EURUSD"       0
      [ 2] "USDCHF"       2
      [ 3] "USDCAD"       2
      [ 4] "GBPUSD"       2
      [ 5] "AUDUSD"       3
      [ 6] "XAUUSD"       6
      [ 7] "SP500m"       7
      [ 8] "NZDUSD"       9
      [ 9] "USDCNH"     151
      [10] "USDSEK"     319
      [11] "BTCUSD"    3356
      [12] "USDRUB"    3400

   example output (2-nd run, Property=SPREAD_FLOAT, ShowPerSymbolDetails=false):
   
      ===== Distances for Market Watch symbols =====
      Total symbols: 13
      Stats per SYMBOL_SPREAD_FLOAT:
          [key] [value]
      [0]     1      13
   
*/
//+------------------------------------------------------------------+
