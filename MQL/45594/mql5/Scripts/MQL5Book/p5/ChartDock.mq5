//+------------------------------------------------------------------+
//|                                                    ChartDock.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#include <MQL5Book/ChartModeMonitor.mqh>

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   const int flags[] =
   {
      CHART_IS_DOCKED,
      CHART_FLOAT_LEFT, CHART_FLOAT_TOP, CHART_FLOAT_RIGHT, CHART_FLOAT_BOTTOM
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
   [0]    51       1
   [1]    52       0
   [2]    53       0
   [3]    54       0
   [4]    55       0
                                  // undock from context menu
   CHART_IS_DOCKED 1 -> 0
   CHART_FLOAT_LEFT 0 -> 299
   CHART_FLOAT_TOP 0 -> 75
   CHART_FLOAT_RIGHT 0 -> 1263
   CHART_FLOAT_BOTTOM 0 -> 472    // vertical size changed
   CHART_FLOAT_BOTTOM 472 -> 500
   CHART_FLOAT_BOTTOM 500 -> 539
                                  // horizontal size changed
   CHART_FLOAT_RIGHT 1263 -> 1024
   CHART_FLOAT_RIGHT 1024 -> 1023
                                  // dock back
   CHART_IS_DOCKED 0 -> 1

*/
//+------------------------------------------------------------------+
