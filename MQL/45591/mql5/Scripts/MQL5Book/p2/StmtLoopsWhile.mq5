//+------------------------------------------------------------------+
//|                                               StmtLoopsWhile.mq5 |
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
   int i = 5;

   // while(--i)  // warning: expression not boolean
   while(--i > 0) // condition is updated in place
   {
      Print(i);
   }
   
   while(i < 10)  // condition check is separated from...
      Print(++i); // ...affecting variable update

   // loop until user command
   while(!IsStopped())
   {
      Comment(GetTickCount());
      Sleep(1000);
   }
   Comment("");
}
//+------------------------------------------------------------------+
