//+------------------------------------------------------------------+
//|                                                 FuncCallback.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+

#property copyright "Copyright 2021, MetaQuotes Ltd."
#property link      "https://www.mql5.com"
#property version   "1.00"

typedef void (*ProgressCallback)(const float percent);

//+------------------------------------------------------------------+
//| Heavy computations demo                                          |
//+------------------------------------------------------------------+
void DoMath(double &bigdata[], ProgressCallback callback)
{
   const int N = 1000000;
   for(int i = 0; i < N; ++i)
   {
      if(i % 10000 == 0 && callback != NULL)
      {
         callback(i * 100.0f / N);
      }

      // ... lot of calculations
   }
}

//+------------------------------------------------------------------+
//| Callback for progress indication                                 |
//+------------------------------------------------------------------+
void MyCallback(const float percent)
{
   Print(percent);
}

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   double data[] = {0};
   DoMath(data, MyCallback);
}

//+------------------------------------------------------------------+
