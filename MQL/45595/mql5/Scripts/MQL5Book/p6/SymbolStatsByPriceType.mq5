//+------------------------------------------------------------------+
//|                                       SymbolStatsByPriceType.mq5 |
//|                                  Copyright 2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//| Print stats on symbols whose charts are built using bid/last.    |
//+------------------------------------------------------------------+
const bool MarketWatchOnly = false;

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   const int n = SymbolsTotal(MarketWatchOnly);
   int k = 0;
   // run through all available symbols
   for(int i = 0; i < n; ++i)
   {
      if(SymbolInfoInteger(SymbolName(i, MarketWatchOnly), SYMBOL_CHART_MODE)
          == SYMBOL_CHART_MODE_LAST)
      {
         k++;
      }
   }
   PrintFormat("Symbols in total: %d", n);
   PrintFormat("Symbols using price types: Bid=%d, Last=%d", n - k, k);
}
//+------------------------------------------------------------------+
/*

   example output:
   Symbols in total: 52304
   Symbols using price types: Bid=229, Last=52075

*/
//+------------------------------------------------------------------+
