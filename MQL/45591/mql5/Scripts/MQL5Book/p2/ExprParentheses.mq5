//+------------------------------------------------------------------+
//|                                              ExprParentheses.mq5 |
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
   int offset = 8;
   int coefficient = 10, flags = 0;

   // warning: expression not boolean
   int result1 = coefficient * flags | 1 << offset > 0 ? offset : 1;
   int result2 = coefficient * flags | 1 << (offset > 0 ? offset : 1);
   int result3 = coefficient * (flags | 1 << (offset > 0 ? offset : 1));

   PRT(coefficient * flags | 1 << offset > 0 ? offset : 1);     // 8
   PRT(coefficient * flags | 1 << (offset > 0 ? offset : 1));   // 256
   PRT(coefficient * (flags | 1 << (offset > 0 ? offset : 1))); // 2560
}
//+------------------------------------------------------------------+
