//+------------------------------------------------------------------+
//|                                                 StmtLoopsFor.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+

#property copyright "Copyright 2021, MetaQuotes Ltd."
#property link      "https://www.mql5.com"
#property version   "1.00"

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   // simple single variable loop
   int a[] = {1, 2, 3, 4, 5, 6, 7};
   const int n = ArraySize(a);

   for(int i = 0; i < n; ++i)
      a[i] = a[i] * a[i];

   ArrayPrint(a);    // 1  4  9 16 25 36 49
   // Print(i);      // error: 'i' - undeclared identifier

   // pair of loop variables
   for(int i = 0, j = n - 1; i < n / 2; ++i, --j)
   {
      int temp = a[i];
      a[i] = a[j];
      a[j] = temp;
   }

   ArrayPrint(a);    // 49 36 25 16  9  4  1

   // partial loop header
   int k = 0, sum = 0;

   for( ; sum < 100; )
   {
      sum += a[k++];
   }

   Print(k, " ", sum - a[k]); // 3 94

   // nested loops
   int table[10][10] = {0};
   for(int i = 1; i <= 10; ++i)
   {
      for(int j = 1; j <= 10; ++j)
      {
         table[i - 1][j - 1] = i * j;
      }
   }
   ArrayPrint(table);

   // infinite (!) loop, empty header
   for( ; /*!IsStopped()*/; )
   {
      Comment(GetTickCount());
      Sleep(1000);

      // exit upon user request to remove the script
      // 'Abnormal termination' after 3 seconds of graceful timeout
   }
   Comment(""); // never reached till !IsStopped() is commented
}
//+------------------------------------------------------------------+
