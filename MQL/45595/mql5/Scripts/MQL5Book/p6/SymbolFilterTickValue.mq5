//+------------------------------------------------------------------+
//|                                        SymbolFilterTickValue.mq5 |
//|                                  Copyright 2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//| Build an array of symbols ordered by their tick value            |
//+------------------------------------------------------------------+
#property script_show_inputs

#include <MQL5Book/SymbolFilter.mqh>

input bool MarketWatchOnly = true;

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   SymbolFilter f;      // filter object
   string symbols[];    // symbol names
   double tickValues[]; // array for results

   // apply the filter without conditions, just fill the array and sort it
   f.select(MarketWatchOnly, SYMBOL_TRADE_TICK_VALUE, symbols, tickValues, true);
   
   PrintFormat("===== Tick values of the symbols (%d) =====",
      ArraySize(tickValues));
   ArrayPrint(symbols);
   ArrayPrint(tickValues, 5);
}
//+------------------------------------------------------------------+
/*

   example output:
   ===== Tick values of the symbols (13) =====
   "BTCUSD" "USDRUB" "XAUUSD" "USDSEK" "USDCNH" "USDCAD" "USDJPY" "NZDUSD" "AUDUSD" "EURUSD" "GBPUSD" "USDCHF" "SP500m"
    0.00100  0.01309  0.10000  0.10955  0.15744  0.80163  0.87319  1.00000  1.00000  1.00000  1.00000  1.09212 10.00000

*/
//+------------------------------------------------------------------+
