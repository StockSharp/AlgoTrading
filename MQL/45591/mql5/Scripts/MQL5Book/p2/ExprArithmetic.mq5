//+------------------------------------------------------------------+
//|                                               ExprArithmetic.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+

#property copyright "Copyright 2021, MetaQuotes Ltd."
#property link      "https://www.mql5.com"
#property version   "1.00"

#define PRT(A) Print(#A, "=", (A))

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   double v = 3 + 4 * 5; // ok: v = 23
   int a = 24 / 7;       // ok: a = 3
   int b = 24 / 8;       // ok: b = 3
   double c = 24 / 7;    // ok: c = 3 (!)
   double d = 24.0 / 7;  // ok: d = 3.4285714285714284
   int x = 11 % 5;       // ok: x = 1
   double z = DBL_MAX / DBL_MIN - 1; // inf: Not A Number
   /*
   int y = 11 % 5.0;     // error: '%' - illegal operation use
   */
   
   PRT(v);
   PRT(a);
   PRT(b);
   PRT(c);
   PRT(d);
   PRT(x);
   PRT(z);
}
//+------------------------------------------------------------------+
