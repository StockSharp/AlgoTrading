//+------------------------------------------------------------------+
//|                                                      EnvStop.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+

//+------------------------------------------------------------------+
//| Check if given number is prime                                   |
//+------------------------------------------------------------------+
bool isPrime(int n)
{
   if(n < 1) return false;
   if(n <= 3) return true;
   if(n % 2 == 0) return false;
   const int p = (int)sqrt(n);
   int i = 3;
   for( ; i <= p; i += 2)
   {
      if(n % i == 0) return false;
   }

   return true;
}

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   int count = 0;
   int candidate = 1;
   
   while(!IsStopped()) // try to change the condition to just 'true'
   {
      // lengthy calculations emulation
      if(isPrime(candidate))
      {
         Comment("Count:", ++count, ", Prime:", candidate);
      }
      ++candidate;
      Sleep(10);
   }
   Comment("");
   Print("Total found:", count);
}
//+------------------------------------------------------------------+
