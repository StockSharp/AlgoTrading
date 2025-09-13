//+------------------------------------------------------------------+
//|                                                     FuncFibo.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+

#property copyright "Copyright 2021, MetaQuotes Ltd."
#property link      "https://www.mql5.com"
#property version   "1.00"

//+------------------------------------------------------------------+
//| Calculate Fibonacci sequence number at given position n          |
//+------------------------------------------------------------------+
int Fibo(const int n)
{
   int prev = 0;
   int result = 1;

   for(int i = 0; i < n; ++i)
   {
      int temp = result;
      result = result + prev;
      prev = temp;
   }

   return result;
}

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   int f = Fibo(10); // 89
   Print(f);

   /*
   // WARNINGS
   double d = 5.5;
   Fibo(d);          // possible loss of data due to type conversion
   Fibo(5.5);        // truncation of constant value
   Fibo("10");       // implicit conversion from 'string' to 'number'

   // ERRORS
   Fibo();           // wrong parameters count
   Fibo(0, 10);      // wrong parameters count
   */
}