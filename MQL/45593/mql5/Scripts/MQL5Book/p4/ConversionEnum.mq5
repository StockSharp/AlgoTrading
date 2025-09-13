//+------------------------------------------------------------------+
//|                                               ConversionEnum.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+

#define PRT(A) Print(#A, "=", (A))

#include <MQL5Book/EnumToArray.mqh>

//+------------------------------------------------------------------+
//| Helper function to get enum elements into array                  |
//+------------------------------------------------------------------+
template<typename E>
void process(E a)
{
   int result[];
   int n = EnumToArray(a, result, 0, USHORT_MAX);
   Print(typename(E), " Count=", n);
   for(int i = 0; i < n; i++)
   {
      Print(i, " ", EnumToString((E)result[i]), "=", result[i]);
   }
}

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   PRT(EnumToString(PRICE_CLOSE));            // PRICE_CLOSE
   PRT(EnumToString((ENUM_APPLIED_PRICE)10)); // ENUM_APPLIED_PRICE::10

   process((ENUM_APPLIED_PRICE)0);
   /* will ouput:
   ENUM_APPLIED_PRICE Count=7
   0 PRICE_CLOSE=1
   1 PRICE_OPEN=2
   2 PRICE_HIGH=3
   3 PRICE_LOW=4
   4 PRICE_MEDIAN=5
   5 PRICE_TYPICAL=6
   6 PRICE_WEIGHTED=7
   */
}
//+------------------------------------------------------------------+
