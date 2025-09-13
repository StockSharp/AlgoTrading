//+------------------------------------------------------------------+
//|                                                    ArraySize.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+

#define PRT(A) Print(#A, "=", (A))

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   int dynamic[];
   int fixed[][4] = {{1, 2, 3, 4}, {5, 6, 7, 8}};
   
   PRT(sizeof(fixed) / sizeof(int));   // 8
   PRT(ArraySize(fixed));              // 8
   
   ArrayResize(dynamic, 10);
   
   PRT(sizeof(dynamic) / sizeof(int)); // 13 (incorrect, sizeof inapplicable)
   PRT(ArraySize(dynamic));            // 10
   
   PRT(ArrayRange(fixed, 0));          // 2
   PRT(ArrayRange(fixed, 1));          // 4
   
   PRT(ArrayRange(dynamic, 0));        // 10
   PRT(ArrayRange(dynamic, 1));        // 0
   
   int size = 1;
   for(int i = 0; i < 2; ++i)
   {
      size *= ArrayRange(fixed, i);
   }
   PRT(size == ArraySize(fixed));      // true
}
//+------------------------------------------------------------------+
