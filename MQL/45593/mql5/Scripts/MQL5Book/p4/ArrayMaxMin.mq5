//+------------------------------------------------------------------+
//|                                                  ArrayMaxMin.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+

#define PRTS(A)  Print(#A, "=", (string)(A) + " / status:" + (string)GetLastError())
#define LIMIT 10

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   // generate random data
   int array[];
   ArrayResize(array, LIMIT);
   
   for(int i = 0; i < LIMIT; ++i)
   {
      array[i] = rand();
   }
   
   ArrayPrint(array);
   // by default new array is not a timeserie
   PRTS(ArrayMaximum(array));
   PRTS(ArrayMinimum(array));
   // switch timeseries mode on
   PRTS(ArraySetAsSeries(array, true));
   PRTS(ArrayMaximum(array));
   PRTS(ArrayMinimum(array));
   
   /*
      example output (will differ on every run due to randomization)
   22242  5909 21570  5850 18026 24740 10852  2631 24549 14635
   ArrayMaximum(array)=5 / status:0
   ArrayMinimum(array)=7 / status:0
   ArraySetAsSeries(array,true)=true / status:0
   ArrayMaximum(array)=4 / status:0
   ArrayMinimum(array)=2 / status:0
   */
   
   const int zeroone[] = {1, 1, 1, 2, 2, 2};
   PRTS(ArrayMaximum(zeroone)); // 3
   PRTS(ArrayMinimum(zeroone)); // 0
}
//+------------------------------------------------------------------+
