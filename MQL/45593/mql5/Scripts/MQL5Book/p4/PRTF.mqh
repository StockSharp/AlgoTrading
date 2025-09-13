//+------------------------------------------------------------------+
//|                                                         PRTF.mqh |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+

#include <MQL5Book/MqlError.mqh>

#define PRTF(A) ResultPrint(#A, (A))

//+------------------------------------------------------------------+
//| Helper printer returning result and checking errors              |
//+------------------------------------------------------------------+
template<typename T>
T ResultPrint(const string s, const T retval = NULL)
{
   const string err = E2S(_LastError) + "(" + (string)_LastError + ")";
   Print(s, "=", retval, " / ", (_LastError == 0 ? "ok" : err));
   ResetLastError(); // cleanup for next execution
   return retval;
}
//+------------------------------------------------------------------+
