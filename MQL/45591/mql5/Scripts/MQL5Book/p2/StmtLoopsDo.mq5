//+------------------------------------------------------------------+
//|                                                  StmtLoopsDo.mq5 |
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
   double d = 1.0;
   do
   {
      Print(d);
      d *= M_SQRT2;
   }
   while(d < 100.0);
}
//+------------------------------------------------------------------+
