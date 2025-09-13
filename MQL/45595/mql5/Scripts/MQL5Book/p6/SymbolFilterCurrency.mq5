//+------------------------------------------------------------------+
//|                                         SymbolFilterCurrency.mq5 |
//|                                  Copyright 2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//| Build an array of symbols with specific currencies               |
//+------------------------------------------------------------------+
#property script_show_inputs

#include <MQL5Book/SymbolFilter.mqh>

input bool MarketWatchOnly = true;

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   // instantiate a filter object
   SymbolFilter f;
   // prepare an array for results
   string symbols[];

   // create a filter with condition on base currency and apply it
   f.let(SYMBOL_CURRENCY_BASE, "USD")
   .select(MarketWatchOnly, symbols);
   Print("===== Base is USD =====");
   ArrayPrint(symbols);
   
   // reset the array
   ArrayResize(symbols, 0);

   // add new condition on profit currency to the filter and apply it
   f.let(SYMBOL_CURRENCY_PROFIT, "USD", IS::NOT_EQUAL)
   .select(MarketWatchOnly, symbols);

   Print("===== Base is USD and Profit is not USD =====");
   ArrayPrint(symbols);
}
//+------------------------------------------------------------------+
/*

   example output:
   ===== Base is USD =====
   "USDCHF" "USDJPY" "USDCNH" "USDRUB" "USDCAD" "USDSEK" "SP500m" "Brent" 
   ===== Base is USD and Profit is not USD =====
   "USDCHF" "USDJPY" "USDCNH" "USDRUB" "USDCAD" "USDSEK"

*/
//+------------------------------------------------------------------+
