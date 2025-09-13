//+------------------------------------------------------------------+
//|                                                    TypeFloat.mq5 |
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
   double a0 = 123;      // ok, a0 = 123.0
   double a1 = 123.0;    // ok, a1 = 123.0
   double a2 = 0.123E3;  // ok, a2 = 123.0
   double a3 = 12300E-2; // ok, a3 = 123.0
   double b = -.75;      // ok, b = -0.75
   double q = LONG_MAX;  // warning: truncation, q = 9.223372036854776e+18
   //               LONG_MAX = 9223372036854775807
   double d = 9007199254740992; // ok, maximal stable long in double

   double z = 0.12345678901234567890123456789; // ok, but truncated
   // to 16 digits: z = 0.1234567890123457
   double y1 = 1234.56789;  // ok, y1 = 1234.56789
   double y2 = 1234.56789f; // accuracy loss, y2 = 1234.56787109375
   float m = 1000000000.0;  // ok, stored as is
   float n =  999999975.0;  // warning: truncation, n = 1000000000.0

   PRT(a0);
   PRT(a1);
   PRT(a2);
   PRT(a3);
   PRT(b);
   PRT(q);
   PRT(d);
   PRT(z);
   PRT(y1);
   PRT(y2);
   PRT(m);
   PRT(n);
}

//+------------------------------------------------------------------+
