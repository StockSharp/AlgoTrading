//+------------------------------------------------------------------+
//|                                                 GlobalsFlush.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   // enforce writing global variables to disk if they are not yet
   // saved in their current state
   GlobalVariablesFlush();
}
//+------------------------------------------------------------------+
