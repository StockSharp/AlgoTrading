//+------------------------------------------------------------------+
//|                                           GlobalsNoCondition.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property service

#include "PRTF.mqh"

// maximal number of concurrently running copies of the program
input int limit = 1;        // Limit
input int startPause = 100; // Delay (ms)

//+------------------------------------------------------------------+
//| Delay emulation                                                  |
//+------------------------------------------------------------------+
void Delay()
{
   if(startPause > 0)
   {
      Sleep(startPause);
   }
}

//+------------------------------------------------------------------+
//| Service program start function                                   |
//+------------------------------------------------------------------+
void OnStart()
{
   PrintFormat("\nParameters: Limit: %d, Delay: %d", limit, startPause);
   // create new temporary variable if it's not exist
   // keep current value otherwise
   PRTF(GlobalVariableTemp(__FILE__));

   // precondition guards
   int count = (int)GlobalVariableGet(__FILE__);
   if(count < 0)
   {
      Print("Negative count detected. Not allowed.");
      return;
   }
   
   if(count >= limit)
   {
      PrintFormat("Can't start more than %d copy(s)", limit);
      return;
   }
   
   // emulate slow execution in busy conditions,
   // where multiple programs run in parallel
   Delay();
   // since another instance could have already read the same count
   // from the gloval varibale and increment it,
   // out increment operates on outdated value and does not actually
   // count both instances (we have the same count as other program)
   PRTF(GlobalVariableSet(__FILE__, count + 1));
   
   // work cycle (mockup)
   int loop = 0;
   while(!IsStopped())
   {
      PrintFormat("Copy %d is working [%d]...", count, loop++);
      // ...
      Sleep(3000);
   }
   
   int last = (int)GlobalVariableGet(__FILE__);
   if(last > 0)
   {
      PrintFormat("Copy %d (out of %d) is stopping", count, last);
      Delay();
      PRTF(GlobalVariableSet(__FILE__, last - 1));
   }
   else
   {
      Print("Count underflow");
   }
}
//+------------------------------------------------------------------+
/*
   example output

   GlobalsNoCondition  	GlobalVariableTemp(GlobalsNoCondition.mq5)=true / ok
   GlobalsNoCondition 1	
   GlobalsNoCondition 1	GlobalVariableTemp(GlobalsNoCondition.mq5)=false / GLOBALVARIABLE_EXISTS(4502)
   GlobalsNoCondition  	GlobalVariableSet(GlobalsNoCondition.mq5,count+1)=2021.08.31 17:47:17 / ok
   GlobalsNoCondition  	Copy 0 is working [0]...
   GlobalsNoCondition 1	GlobalVariableSet(GlobalsNoCondition.mq5,count+1)=2021.08.31 17:47:17 / ok
   GlobalsNoCondition 1	Copy 0 is working [0]...
   GlobalsNoCondition  	Copy 0 is working [1]...
   GlobalsNoCondition 1	Copy 0 is working [1]...
   GlobalsNoCondition  	Copy 0 is working [2]...
   GlobalsNoCondition 1	Copy 0 is working [2]...
   GlobalsNoCondition  	Copy 0 is working [3]...
   GlobalsNoCondition 1	Copy 0 is working [3]...
   GlobalsNoCondition  	Copy 0 (out of 1) is stopping
   GlobalsNoCondition  	GlobalVariableSet(GlobalsNoCondition.mq5,last-1)=2021.08.31 17:47:26 / ok
   GlobalsNoCondition 1	Count underflow
*/
//+------------------------------------------------------------------+
