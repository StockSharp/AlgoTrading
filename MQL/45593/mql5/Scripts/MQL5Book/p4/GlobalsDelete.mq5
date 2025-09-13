//+------------------------------------------------------------------+
//|                                                GlobalsDelete.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#include "PRTF.mqh"

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   // try to delete nonexistent variable
   PRTF(GlobalVariableDel("#123%"));
   // try to delete a group of nonexistent variables
   PRTF(GlobalVariablesDeleteAll("#123%"));

   // the time limit is in long past,
   // so no variables with such time should exist, and no one be deleted
   PRTF(GlobalVariablesDeleteAll(NULL, D'2021.01.01'));

   const string abracadabra = "abracadabra";
   // make sure this variable exists (just for the test)
   PRTF(GlobalVariableSet(abracadabra, 0));
   // now delete it
   PRTF(GlobalVariableDel(abracadabra));
   
   // now try to delete variables from previous test scripts:
   // GlobalsRunCount.mq5 and GlobalsRunCheck.mq5
   // this should remove 2 variables if the tests have been run before,
   // because their variables starts from the given prefix
   PRTF(GlobalVariablesDeleteAll("GlobalsRun"));
   PRTF(GlobalVariablesTotal());
   /*
      example output
      
      GlobalVariableDel(#123%)=false / GLOBALVARIABLE_NOT_FOUND(4501)
      GlobalVariablesDeleteAll(#123%)=0 / ok
      GlobalVariablesDeleteAll(NULL,D'2021.01.01')=0 / ok
      GlobalVariableSet(abracadabra,0)=2021.08.30 14:02:32 / ok
      GlobalVariableDel(abracadabra)=true / ok
      GlobalVariablesDeleteAll(GlobalsRun)=2 / ok
      GlobalVariablesTotal()=0 / ok
      
   */
}
//+------------------------------------------------------------------+
