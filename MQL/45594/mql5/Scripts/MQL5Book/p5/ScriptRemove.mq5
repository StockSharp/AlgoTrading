//+------------------------------------------------------------------+
//|                                                 ScriptRemove.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+

//+------------------------------------------------------------------+
//| Example of a class which can produce errors inside               |
//+------------------------------------------------------------------+
class ProblemSource
{
public:
   ProblemSource()
   {
      // emulate failure during object creation, for example,
      // during some resource acquiring such as a file, etc.
      if(rand() > 20000)
      {
         ExpertRemove(); // set _StopFlag to true
      }
   }
};

ProblemSource global; // this object may fail

//+------------------------------------------------------------------+
//| Worker function stub                                             |
//+------------------------------------------------------------------+
void SubFunction()
{
   ProblemSource local; // this object may fail
   // emulate some work (with a valid object only)
   Sleep(1000);
}

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   int count = 0;
   // loop until stopped by user or programmatically
   while(!IsStopped())
   {
      // we could use 'break' to exit the loop, but sometimes
      // the condition for breaking is burried deep inside nested calls,
      // then setting the stop flag to true will also work in deferred manner
      // on the next time when while condition is checked
      
      SubFunction();
      Print(++count);
   }
   /*
      example output
      
      1
      2
      3
      ExpertRemove() function called
      4
   */
}
//+------------------------------------------------------------------+
