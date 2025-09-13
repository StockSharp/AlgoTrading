//+------------------------------------------------------------------+
//|                                                   ExprModify.mq5 |
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
   int a[] = {1, 2, 3, 4, 5};
   int b[] = {1, 2, 3, 4, 5};
   int i = 0, j = 0;
   
   a[++i] *= i + 1;           // {1, 4, 3, 4, 5}, i = 1
                              // not an equivalent!
   b[++j] = b[++j] * (j + 1); // {1, 2, 4, 4, 5}, j = 2

   PRT(i);
   PRT(j);
   ArrayPrint(a);
   ArrayPrint(b);

   ushort x = 0;
   x |= 1 << 10;              // 1024
   PRT(x);

   x &= ~(1 << 10);           // 0
   PRT(x);
}
//+------------------------------------------------------------------+
