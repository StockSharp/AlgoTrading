//+------------------------------------------------------------------+
//|                                                   ExprAssign.mq5 |
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
   const double cx = 123.0;
   int x, a[5] = {1};
   string s;

   a[2] = 21;              // ok
   x = a[0] + a[1] + a[2]; // ok
   s = Symbol();           // ok
   /*
   int y;
   cx = 0;          // error: 'cx' - constant cannot be modified
   5 = y;           // error: '5' - l-value required
   x + y = 3;       // error: l-value required
   Symbol() = "GBPUSD"; // error: l-value required
   */
}
//+------------------------------------------------------------------+
