//+------------------------------------------------------------------+
//|                                                    TimeCheck.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#include "PRTF.mqh"

//+------------------------------------------------------------------+
//| Server time exact approximation                                  |
//| (keeps minutes and seconds in the shift)                         |
//+------------------------------------------------------------------+
datetime TimeTradeServerExact()
{
   enum LOCATION
   {
      LOCAL,
      SERVER,
   };
   static datetime now[2] = {}, then[2] = {};
   static int shiftInHours = 0;
   static long shiftInSeconds = 0;
   
   // keep track of 2 last execution times: now and then
   then[LOCAL] = now[LOCAL];
   then[SERVER] = now[SERVER];
   now[LOCAL] = TimeLocal();
   now[SERVER] = TimeCurrent();
   
   // during first call we don't have 2 time marks yet
   if(then[LOCAL] == 0 && then[SERVER] == 0) return 0;
   
   // when time marks differs equally on local and server computer
   // and server time is not paused due to weekend/holidays,
   // we can calculate the shift between them
   if(now[LOCAL] - now[SERVER] == then[LOCAL] - then[SERVER]
   && now[SERVER] != then[SERVER])
   {
      shiftInSeconds = now[LOCAL] - now[SERVER];
      shiftInHours = (int)MathRound(shiftInSeconds / 3600.0);
      PrintFormat("Shift update: hours: %d; seconds: %lld", shiftInHours, shiftInSeconds);
   }
   
   // NB: built-in TimeTradeServer function calculates:
   //                TimeLocal() - shiftInHours * 3600
   return (datetime)(TimeLocal() - shiftInSeconds);
}

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   // infinite loop until user interrupt it
   // to show all kinds of time
   while(!IsStopped())
   {
      PRTF(TimeLocal());
      PRTF(TimeCurrent());
      PRTF(TimeTradeServer());
      PRTF(TimeTradeServerExact());
      Sleep(1000);
   }
}
//+------------------------------------------------------------------+
/*
   example output
   
   TimeLocal()=2021.09.02 16:03:34 / ok
   TimeCurrent()=2021.09.02 15:59:39 / ok
   TimeTradeServer()=2021.09.02 16:03:34 / ok
   TimeTradeServerExact()=1970.01.01 00:00:00 / ok
   TimeLocal()=2021.09.02 16:03:35 / ok
   TimeCurrent()=2021.09.02 15:59:40 / ok
   TimeTradeServer()=2021.09.02 16:03:35 / ok
   Shift update: hours: 0; seconds: 235
   TimeTradeServerExact()=2021.09.02 15:59:40 / ok
   TimeLocal()=2021.09.02 16:03:36 / ok
   TimeCurrent()=2021.09.02 15:59:41 / ok
   TimeTradeServer()=2021.09.02 16:03:36 / ok
   Shift update: hours: 0; seconds: 235
   TimeTradeServerExact()=2021.09.02 15:59:41 / ok
   TimeLocal()=2021.09.02 16:03:37 / ok
   TimeCurrent()=2021.09.02 15:59:41 / ok
   TimeTradeServer()=2021.09.02 16:03:37 / ok
   TimeTradeServerExact()=2021.09.02 15:59:42 / ok
   TimeLocal()=2021.09.02 16:03:38 / ok
   TimeCurrent()=2021.09.02 15:59:43 / ok
   TimeTradeServer()=2021.09.02 16:03:38 / ok
   TimeTradeServerExact()=2021.09.02 15:59:43 / ok
*/
//+------------------------------------------------------------------+
