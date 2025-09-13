//+------------------------------------------------------------------+
//|                                              GlobalsRunCheck.mq5 |
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
   // check existence of persistent variable using 2 different function;
   // on every run except for very first run these will show true (exist)
   // and updated time from previous run
   PRTF(GlobalVariableCheck(gv));
   PRTF(GlobalVariableTime(gv));
   // try to read the persistent counter if it exists,
   // if it's not exist we got 0, and start from beginning
   int count = (int)PRTF(GlobalVariableGet(gv));
   count++;
   // save incremented value in the persistent global variable
   PRTF(GlobalVariableSet(gv, count));
   Print("This script run count: ", count);
   /*
      example output after 3 runs:
      
      GlobalVariableCheck(gv)=false / ok
      GlobalVariableTime(gv)=1970.01.01 00:00:00 / GLOBALVARIABLE_NOT_FOUND(4501)
      GlobalVariableGet(gv)=0.0 / GLOBALVARIABLE_NOT_FOUND(4501)
      GlobalVariableSet(gv,count)=2021.08.29 16:59:35 / ok
      This script run count: 1
      GlobalVariableCheck(gv)=true / ok
      GlobalVariableTime(gv)=2021.08.29 16:59:35 / ok
      GlobalVariableGet(gv)=1.0 / ok
      GlobalVariableSet(gv,count)=2021.08.29 16:59:45 / ok
      This script run count: 2
      GlobalVariableCheck(gv)=true / ok
      GlobalVariableTime(gv)=2021.08.29 16:59:45 / ok
      GlobalVariableGet(gv)=2.0 / ok
      GlobalVariableSet(gv,count)=2021.08.29 16:59:56 / ok
      This script run count: 3
   */
}
//+------------------------------------------------------------------+
