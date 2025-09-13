//+------------------------------------------------------------------+
//|                                                    ChartMode.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+

#include <MQL5Book/ChartModeMonitor.mqh>

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   int flags[] =
   {
      CHART_MODE, CHART_FOREGROUND, CHART_SHIFT, CHART_AUTOSCROLL
   };
   ChartModeMonitor m(flags);
   Print("Initial state:");
   m.print();
   m.backup();
   
   while(!IsStopped())
   {
      m.snapshot();
      Sleep(500);
   }
   m.restore();
}
//+------------------------------------------------------------------+
/*

   Initial state:
       [key] [value]
   [0]     0       1
   [1]     1       0
   [2]     2       0
   [3]     4       0
   CHART_MODE 1 -> 0
   CHART_MODE 0 -> 2
   CHART_MODE 2 -> 1
   CHART_SHIFT 0 -> 1
   CHART_AUTOSCROLL 0 -> 1

*/
//+------------------------------------------------------------------+
