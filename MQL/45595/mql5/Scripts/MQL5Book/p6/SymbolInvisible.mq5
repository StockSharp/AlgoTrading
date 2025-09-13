//+------------------------------------------------------------------+
//|                                              SymbolInvisible.mq5 |
//|                                  Copyright 2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//| Print a list of symbols selected for Market Watch implicitly.    |
//+------------------------------------------------------------------+

#define PUSH(A,V) (A[ArrayResize(A, ArraySize(A) + 1, ArraySize(A) * 2) - 1] = V)

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   const int n = SymbolsTotal(false);
   int selected = 0;
   string invisible[];
   // list all available symbols
   for(int i = 0; i < n; ++i)
   {
      const string s = SymbolName(i, false);
      if(SymbolInfoInteger(s, SYMBOL_SELECT))
      {
         selected++;
         if(!SymbolInfoInteger(s, SYMBOL_VISIBLE))
         {
            // collect selected symbols, which are invisible
            PUSH(invisible, s);
         }
      }
   }
   PrintFormat("Symbols: total=%d, selected=%d, implicit=%d", n, selected, ArraySize(invisible));
   if(ArraySize(invisible))
   {
      ArrayPrint(invisible);
   }
}
//+------------------------------------------------------------------+
