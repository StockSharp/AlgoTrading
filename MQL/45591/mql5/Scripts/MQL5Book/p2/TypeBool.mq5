//+------------------------------------------------------------------+
//|                                                     TypeBool.mq5 |
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
   bool t = true;          // true
   bool f = false;         // false
   bool x = 100;           // x = true
   bool y = 0;             // y = false
   int i = true;           // i = 1
   int j = false;          // j = 0

   PRT(t);
   PRT(f);
   PRT(x);
   PRT(y);
   PRT(i);
   PRT(j);
}

//+------------------------------------------------------------------+
