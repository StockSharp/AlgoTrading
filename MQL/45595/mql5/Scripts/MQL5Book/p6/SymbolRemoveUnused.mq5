//+------------------------------------------------------------------+
//|                                           SymbolRemoveUnused.mq5 |
//|                                  Copyright 2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//| Remove unused symbols from the Market Watch.                     |
//+------------------------------------------------------------------+
#include <MQL5Book/MqlError.mqh>

#define PUSH(A,V) (A[ArrayResize(A, ArraySize(A) + 1, ArraySize(A) * 2) - 1] = V)

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   // request user confirmation
   if(IDOK == MessageBox("This script will remove all unused symbols"
      " from the Market Watch. Proceed?", "Please, confirm", MB_OKCANCEL))
   {
      const int n = SymbolsTotal(true);
      ResetLastError();
      string removed[];
      // loop through the symbols backwards
      for(int i = n - 1; i >= 0; --i)
      {
         const string s = SymbolName(i, true);
         if(SymbolSelect(s, false))
         {
            // collect removed symbols
            PUSH(removed, s);
         }
         else
         {
            // show error description otherwise
            PrintFormat("Can't remove '%s': %s (%d)", s, E2S(_LastError), _LastError);
         }
      }
      const int r = ArraySize(removed);
      PrintFormat("%d out of %d symbols removed", r, n);
      if(r > 0)
      {
         ArrayPrint(removed);
         // if some symbols have been removed, we have an option to restore them
         // (at this moment Market Watch is already prunned)
         if(IDOK == MessageBox("Do you want to restore removed symbols"
            " in the Market Watch?", "Please, confirm", MB_OKCANCEL))
         {
            int restored = 0;
            for(int i = r - 1; i >= 0; --i)
            {
               restored += SymbolSelect(removed[i], true);
            }
            PrintFormat("%d symbols restored", restored);
         }
      }
   }
}
//+------------------------------------------------------------------+
