//+------------------------------------------------------------------+
//|                                               TypeConversion.mq5 |
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
   short s = 10;
   long n = 10;
   int p = s * n + 1.0;// possible loss of data due to type conversion
   
   double d = 1.0;
   int x = 1.0 / 10;   // truncation of constant value
   int y = d / 10;     // possible loss of data due to type conversion
   d = LONG_MAX;       // truncation of constant value
   
   long m1 = 1000000000;
   long m2 = m1 * m1;                 // ok: m2 = 1000000000000000000
   long m3 = 1000000000 * 1000000000; // integral constant overflow
                                      // m3 = -1486618624
   PRT(m2);
   PRT(m3);

   d = m1 * m1;        // possible loss of data due to type conversion
   
   char c = 3000;      // truncation of constant value
   PRT(c);             // -72
   uchar uc = 3000;    // truncation of constant value
   PRT(uc);            // 184

   char c55 = 55;
   char sm = c55 * c55;  // ok! 
   PRT(sm);              // 3025 -> -47
   uchar um = c55 * c55; // ok!
   PRT(um);              // 3025 -> 209
   
   uint u = 11;
   int i = -49;
   PRT(i + i);           // -98
   PRT(u + i);           // 4294967258
   
   double w = 100.0, v = 7.0;
   float f = (float)(w / v);
   p = (int)(w / v);
   PRT(w / v);           // 14.28571428571429
   PRT((int)(w / v));    // 14
   Print("Result:" + (string)(float)(w / v)); // Result:14.28571
}
//+------------------------------------------------------------------+
