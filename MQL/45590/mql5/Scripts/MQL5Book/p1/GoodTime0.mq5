//+------------------------------------------------------------------+
//|                                                    GoodTime0.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+

#property copyright "Copyright 2021, MetaQuotes Ltd."
#property link      "https://www.mql5.com"
#property version   "1.00"

//+------------------------------------------------------------------+
//| Return greeting for given time of day (hour)                     |
//+------------------------------------------------------------------+
string Greeting(int hour)
{
   string messages[3] = {"Good morning", "Good day", "Good evening"};
   return messages[hour / 8];
}

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   Print(Greeting(0), ", ", Symbol());
}

//+------------------------------------------------------------------+
