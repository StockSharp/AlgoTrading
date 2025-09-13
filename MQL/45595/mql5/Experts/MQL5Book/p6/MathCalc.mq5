//+------------------------------------------------------------------+
//|                                                     MathCalc.mq5 |
//|                              Copyright (c) 2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright "Copyright (c) 2022, MetaQuotes Ltd."
#property link "https://www.mql5.com"
#property description "Math calculations demo for the tester."

#property tester_set "MathCalc.set"
#property tester_no_cache

input double X1;
input double X2;

//+------------------------------------------------------------------+
//| Tester event handler                                             |
//+------------------------------------------------------------------+
double OnTester()
{
   const double r = 1 + sqrt(X1 * X1 + X2 * X2);
   return sin(r) / r;
}
//+------------------------------------------------------------------+
