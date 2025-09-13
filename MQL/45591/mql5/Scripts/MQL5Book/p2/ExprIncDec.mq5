//+------------------------------------------------------------------+
//|                                                   ExprIncDec.mq5 |
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
   int i = 0, j;
   j = ++i;       // j = 1, i = 1
   j = i++;       // j = 1, i = 2

   PRT(i);
   PRT(j);
   
   int x = 0, y = 5;
   int z = x +++ y; // "x++ + y" : z = 5, x = 1
   
   PRT(z);
   
   double v = 0.5;
   --v;           // v = -0.5

   PRT(v);
   
   int k = 0;
   int a[] = {1, 2, 3, 0, 5};
   
   while((a[k++] = -a[k]) != 0){}

   ArrayPrint(a); // {-1, -2, -3, 0, 5}
}
//+------------------------------------------------------------------+
