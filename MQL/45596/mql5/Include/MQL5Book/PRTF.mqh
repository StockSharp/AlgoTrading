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
   const int snapshot = _LastError; // required because _LastError is volatile
   const string err = E2S(snapshot) + "(" + (string)snapshot + ")";
   Print(s, "=", retval, " / ", (snapshot == 0 ? "ok" : err));
   ResetLastError(); // cleanup for next execution
   return retval;
}
//+------------------------------------------------------------------+
