//+------------------------------------------------------------------+
//|                                            FuncFiboRecursive.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+

#property copyright "Copyright 2021, MetaQuotes Ltd."
#property link      "https://www.mql5.com"
#property version   "1.00"

//+------------------------------------------------------------------+
//| Recursive Calculation of Fibonacci number at given position n    |
//+------------------------------------------------------------------+
int Fibo(const int n)
{
   if(n <= 1) return 1;
   
   return Fibo(n - 1) + Fibo(n - 2);
}

//+------------------------------------------------------------------+
//| BUG: Endless recursive calculations of Fibonacci number          |
//+------------------------------------------------------------------+
int FiboEndless(const int n)
{
   // if(n <= 1) return 1;
   
   return FiboEndless(n - 1) + FiboEndless(n - 2);
}

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   int f = Fibo(10); // 89
   Print(f);
   
   // runtime error(!)
   // FiboEndless(10); // Stack overflow
}
