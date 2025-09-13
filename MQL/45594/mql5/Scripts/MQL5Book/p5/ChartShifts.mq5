//+------------------------------------------------------------------+
//|                                                  ChartShifts.mq5 |
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
      CHART_SHIFT_SIZE, CHART_FIXED_POSITION
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
       [key]  [value]
   [0]     3 21.78771
   [1]    41 17.87709
   CHART_FIXED_POSITION 17.87709497206704 -> 26.53631284916201
   CHART_FIXED_POSITION 26.53631284916201 -> 27.93296089385475
   CHART_FIXED_POSITION 27.93296089385475 -> 28.77094972067039
   CHART_FIXED_POSITION 28.77094972067039 -> 50.0

*/
//+------------------------------------------------------------------+
