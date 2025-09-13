//+------------------------------------------------------------------+
//|                                                   MathMaxMin.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+

#define PRT(A)  Print(#A, "=", (A))

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   int i = 10, j = 11;
   double x = 5.5, y = -5.5;
   string s = "abc";

   // numbers   
   PRT(MathMax(i, j)); // 11
   PRT(MathMax(i, x)); // 10
   PRT(MathMax(x, y)); // 5.5
   PRT(MathMax(i, s)); // 10

   // type conversion
   PRT(typename(MathMax(i, j))); // int
   PRT(typename(MathMax(i, x))); // double
   PRT(typename(MathMax(i, s))); // string
}
//+------------------------------------------------------------------+
