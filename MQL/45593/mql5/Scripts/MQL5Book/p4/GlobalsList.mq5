//+------------------------------------------------------------------+
//|                                                  GlobalsList.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#include "PRTF.mqh"

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   // try to request a name for a variable with too large index
   // (it's assumed you don't have 1 million variables)
   PRTF(GlobalVariableName(1000000));
   // then get the total number of variables and enumerate them
   int n = PRTF(GlobalVariablesTotal());
   for(int i = 0; i < n; ++i)
   {
      const string name = GlobalVariableName(i);
      PrintFormat("%d %s=%f", i, name, GlobalVariableGet(name));
   }
   /*
      example output
      
      GlobalVariableName(1000000)= / GLOBALVARIABLE_NOT_FOUND(4501)
      GlobalVariablesTotal()=3 / ok
      0 GlobalsRunCheck.mq5=3.000000
      1 GlobalsRunCount.mq5=4.000000
      2 abracadabra=0.000000
   
   */
}
//+------------------------------------------------------------------+
