//+------------------------------------------------------------------+
//|                                             StmtJumpContinue.mq5 |
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
   int a[10][10] = {0};

   for(int i = 0; i < 10; ++i)
   {
      for(int j = 0; j < 10; ++j)
      {
         if((j * i) % 2 == 1)
            continue;
         a[i][j] = (i + 1) * (j + 1);
      }
   }

   ArrayPrint(a);
   
   int b[10] = {1, -2, 3, 4, -5, -6, 7, 8, -9, 10};
   int sum = 0;
   
   for(int i = 0; i < 10; ++i)
   {
      if(b[i] < 0) continue;
      sum += b[i];
   }
   
   Print(sum); // 33
}
//+------------------------------------------------------------------+
