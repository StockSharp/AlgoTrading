//+------------------------------------------------------------------+
//|                                              GlobalsRunCount.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#include "PRTF.mqh"

const string gv = __FILE__;

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   // try to read the persistent counter if it exists,
   // if it's not exist we got 0, and start from beginning
   int count = (int)PRTF(GlobalVariableGet(gv));
   count++;
   // save incremented value in the persistent global variable
   PRTF(GlobalVariableSet(gv, count));
   Print("This script run count: ", count);
   /*
      example output after 3 runs:
      
      GlobalVariableGet(gv)=0.0 / GLOBALVARIABLE_NOT_FOUND(4501)
      GlobalVariableSet(gv,count)=2021.08.29 16:04:40 / ok
      This script run count: 1
      GlobalVariableGet(gv)=1.0 / ok
      GlobalVariableSet(gv,count)=2021.08.29 16:05:00 / ok
      This script run count: 2
      GlobalVariableGet(gv)=2.0 / ok
      GlobalVariableSet(gv,count)=2021.08.29 16:05:21 / ok
      This script run count: 3
   */
}
//+------------------------------------------------------------------+
