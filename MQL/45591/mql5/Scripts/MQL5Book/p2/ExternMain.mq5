//+------------------------------------------------------------------+
//|                                                   ExternMain.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+

#define PRT(A)  Print(#A, "=", (A))

// Uncomment the following directive
// to #include common header with 'int x' definition
// instead of extern declarations
// #define USE_INCLUDE_WORKAROUND

#include "ExternHeader1.mqh"
#include "ExternHeader2.mqh"

// the next definition is required for all and every extern variables declared ealier
// otherwise we'll get compilation errors
int x = 2; // if necessary, set specific initial value to 'x'
// NB: if USE_INCLUDE_WORKAROUND is enabled,
// the above definition becomes an error, because
// non-extern int x is defined in other place: ExternCommon.mqh

long y; // the type must correspond to previous extern declaration in ExternHeader1.mqh

// the next definition duplicates definition from ExternHeader1.mqh
// and since they both are not extern, we've got the error:
// short z; // variable already defined

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   inc();  // use x
   dec();  // use x
   PRT(x); // 2
   PRT(y); // 0
}
//+------------------------------------------------------------------+
