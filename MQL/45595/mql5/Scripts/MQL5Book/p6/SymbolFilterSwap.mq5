//+------------------------------------------------------------------+
//|                                             SymbolFilterSwap.mq5 |
//|                                  Copyright 2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//| Print out swaps stats or per symbol settings                     |
//+------------------------------------------------------------------+
#property script_show_inputs

#include <MQL5Book/SymbolFilter.mqh>

input bool UseMarketWatch = true;
input bool ShowPerSymbolDetails = false;
input ENUM_SYMBOL_SWAP_MODE Mode = SYMBOL_SWAP_MODE_POINTS;

//+------------------------------------------------------------------+
//| Composite struct to hold symbol name and its requested property  |
//+------------------------------------------------------------------+
struct SymbolSwap
{
   string name;
   double value;
};

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   SymbolFilter f;                // filter object
   PrintFormat("===== Swap modes for %s symbols =====",
      (UseMarketWatch ? "Market Watch" : "all available"));

   if(ShowPerSymbolDetails)
   {
      // prepare table of swaps for symbols with Mode
      string buyers[], sellers[];    // arrays for names
      double longs[], shorts[];      // arrays for swap values
      SymbolSwap swaps[];            // array for detailed printout

      f.let(SYMBOL_SWAP_MODE, Mode);
      // apply the filter and collect values 2 times for longs and shorts
      f.select(UseMarketWatch, SYMBOL_SWAP_LONG, buyers, longs, true);
      f.select(UseMarketWatch, SYMBOL_SWAP_SHORT, sellers, shorts, true);
      const int l = ArraySize(longs);
      const int s = ArraySize(shorts);
      const int n = ArrayResize(swaps, l + s); // should be l == s
      PrintFormat("Total symbols with %s: %d", EnumToString(Mode), l);
      
      // merge two arrays into one array of structs
      if(n > 0)
      {
         int i = l - 1, j = s - 1, k = 0;
         while(k < n)
         {
            const double swapLong = i >= 0 ? longs[i] : -DBL_MAX;
            const double swapShort = j >= 0 ? shorts[j] : -DBL_MAX;
            
            if(swapLong >= swapShort)
            {
               swaps[k].name = "+" + buyers[i];
               swaps[k].value = longs[i];
               --i;
               ++k;
            }
            else
            {
               swaps[k].name = "-" + sellers[j];
               swaps[k].value = shorts[j];
               --j;
               ++k;
            }
         }
         Print("Swaps per symbols (ordered):");
         ArrayPrint(swaps);
      }
   }
   else
   {
      // collect stats of swap mode usage among symbols
      string symbols[];
      long values[];
      MapArray<ENUM_SYMBOL_SWAP_MODE,int> stats; // counters per specific mode
      // apply the filter and collect values for symbols
      f.select(UseMarketWatch, SYMBOL_SWAP_MODE, symbols, values);
      const int n = ArraySize(symbols);
      for(int i = 0; i < n; ++i)
      {
         stats.inc((ENUM_SYMBOL_SWAP_MODE)values[i]);
      }
      PrintFormat("Total symbols: %d", n);
      Print("Stats per swap mode:");
      stats.print();
      Print("Legend: key=swap mode, value=count");
      for(int i = 0; i < stats.getSize(); ++i)
      {
         PrintFormat("%d -> %s", stats.getKey(i), EnumToString(stats.getKey(i)));
      }
   }
}
//+------------------------------------------------------------------+
/*

   example output (1-st run with default settings):
   
      ===== Swap modes for Market Watch symbols =====
      Total symbols: 13
      Stats per swap mode:
          [key] [value]
      [0]     1      10
      [1]     0       2
      [2]     2       1
      Legend: key=swap mode, value=count
      1 -> SYMBOL_SWAP_MODE_POINTS
      0 -> SYMBOL_SWAP_MODE_DISABLED
      2 -> SYMBOL_SWAP_MODE_CURRENCY_SYMBOL

   example output (2-nd run, ShowPerSymbolDetails=true):
   
      ===== Swap modes for Market Watch symbols =====
      Total symbols with SYMBOL_SWAP_MODE_POINTS: 10
      Swaps per symbols (ordered):
              [name]   [value]
      [ 0] "+AUDUSD"   6.30000
      [ 1] "+NZDUSD"   2.80000
      [ 2] "+USDCHF"   0.10000
      [ 3] "+USDRUB"   0.00000
      [ 4] "-USDRUB"   0.00000
      [ 5] "+USDJPY"  -0.10000
      [ 6] "+GBPUSD"  -0.20000
      [ 7] "-USDCAD"  -0.40000
      [ 8] "-USDJPY"  -0.60000
      [ 9] "+EURUSD"  -0.70000
      [10] "+USDCAD"  -0.80000
      [11] "-EURUSD"  -1.00000
      [12] "-USDCHF"  -1.00000
      [13] "-GBPUSD"  -2.20000
      [14] "+USDSEK"  -4.50000
      [15] "-XAUUSD"  -4.60000
      [16] "-USDSEK"  -4.90000
      [17] "-NZDUSD"  -6.70000
      [18] "+XAUUSD" -12.60000
      [19] "-AUDUSD" -14.80000

   example output (3-rd run, ShowPerSymbolDetails=true, Mode=SYMBOL_SWAP_MODE_CURRENCY_SYMBOL):

      ===== Swap modes for Market Watch symbols =====
      Total symbols with SYMBOL_SWAP_MODE_CURRENCY_SYMBOL: 1
      Swaps per symbols (ordered):
             [name]   [value]
      [0] "-SP500m" -35.00000
      [1] "+SP500m" -41.41000
   
*/
//+------------------------------------------------------------------+
