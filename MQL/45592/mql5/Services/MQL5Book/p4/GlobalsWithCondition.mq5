//+------------------------------------------------------------------+
//|                                         GlobalsWithCondition.mq5 |
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
   
   const int maxRetries = 5;
   int retry = 0;
   
   while(count < limit && retry < maxRetries)
   {
      // emulate slow execution in busy conditions,
      // where multiple programs run in parallel
      Delay();
      if(PRTF(GlobalVariableSetOnCondition(__FILE__, count + 1, count))) break;
      // conditional assignment failed, try again with updated base value
      count = (int)GlobalVariableGet(__FILE__);
      PrintFormat("Counter is already altered by other instance: %d", count);
      retry++;
   }
   
   if(count == limit || retry == maxRetries)
   {
      PrintFormat("Start failed: count: %d, retries: %d", count, retry);
      return;
   }
      
   // work cycle (mockup)
   int loop = 0;
   while(!IsStopped())
   {
      PrintFormat("Copy %d is working [%d]...", count, loop++);
      // ...
      Sleep(3000);
   }
   
   retry = 0;
   int last = (int)GlobalVariableGet(__FILE__);
   while(last > 0 && retry < maxRetries)
   {
      PrintFormat("Copy %d (out of %d) is stopping", count, last);
      Delay();
      if(PRTF(GlobalVariableSetOnCondition(__FILE__, last - 1, last))) break;
      last = (int)GlobalVariableGet(__FILE__);
      retry++;
   }

   if(last <= 0)
   {
      PrintFormat("Unexpected exit: %d", last);
   }
   else
   {
      PrintFormat("Stopped copy %d: count: %d, retries: %d", count, last, retry);
   }
}
//+------------------------------------------------------------------+
/*
   example output
   
   GlobalsWithCondition 2	GlobalVariableTemp(GlobalsWithCondition.mq5)=false / GLOBALVARIABLE_EXISTS(4502)
   GlobalsWithCondition 1	GlobalVariableTemp(GlobalsWithCondition.mq5)=false / GLOBALVARIABLE_EXISTS(4502)
   GlobalsWithCondition  	GlobalVariableTemp(GlobalsWithCondition.mq5)=true / ok
   GlobalsWithCondition  	GlobalVariableSetOnCondition(GlobalsWithCondition.mq5,count+1,count)=true / ok
   GlobalsWithCondition 1	GlobalVariableSetOnCondition(GlobalsWithCondition.mq5,count+1,count)=false / GLOBALVARIABLE_NOT_FOUND(4501)
   GlobalsWithCondition 2	GlobalVariableSetOnCondition(GlobalsWithCondition.mq5,count+1,count)=false / GLOBALVARIABLE_NOT_FOUND(4501)
   GlobalsWithCondition 1	Counter is already altered by other instance: 1
   GlobalsWithCondition  	Copy 0 is working [0]...
   GlobalsWithCondition 2	Counter is already altered by other instance: 1
   GlobalsWithCondition 1	GlobalVariableSetOnCondition(GlobalsWithCondition.mq5,count+1,count)=true / ok
   GlobalsWithCondition 1	Copy 1 is working [0]...
   GlobalsWithCondition 2	GlobalVariableSetOnCondition(GlobalsWithCondition.mq5,count+1,count)=false / GLOBALVARIABLE_NOT_FOUND(4501)
   GlobalsWithCondition 2	Counter is already altered by other instance: 2
   GlobalsWithCondition 2	Start failed: count: 2, retries: 2
   GlobalsWithCondition  	Copy 0 is working [1]...
   GlobalsWithCondition 1	Copy 1 is working [1]...
   GlobalsWithCondition  	Copy 0 is working [2]...
   GlobalsWithCondition 1	Copy 1 is working [2]...
   GlobalsWithCondition  	Copy 0 is working [3]...
   GlobalsWithCondition 1	Copy 1 is working [3]...
   GlobalsWithCondition  	Copy 0 (out of 2) is stopping
   GlobalsWithCondition  	GlobalVariableSetOnCondition(GlobalsWithCondition.mq5,last-1,last)=true / ok
   GlobalsWithCondition  	Stopped copy 0: count: 2, retries: 0
   GlobalsWithCondition 1	Copy 1 (out of 1) is stopping
   GlobalsWithCondition 1	GlobalVariableSetOnCondition(GlobalsWithCondition.mq5,last-1,last)=true / ok
   GlobalsWithCondition 1	Stopped copy 1: count: 1, retries: 0
*/
//+------------------------------------------------------------------+
