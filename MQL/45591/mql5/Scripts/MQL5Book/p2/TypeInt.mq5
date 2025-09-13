//+------------------------------------------------------------------+
//|                                                      TypeInt.mq5 |
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
   int x = -10;          // ok, signed integer x = -10
   uint y = -1;          // ok, but unsigned integer y = 4294967295
   int z = 1.23;         // warning: truncation of constant value, z = 1
   short h = 0x1000;     // ok, h = 4096 in decimal
   long p = 10000000000; // ok
   int w = 10000000000;  // warning, truncation…, w = 1410065408

   PRT(x);
   PRT(y);
   PRT(z);
   PRT(h);
   PRT(p);
   PRT(w);
}

//+------------------------------------------------------------------+
