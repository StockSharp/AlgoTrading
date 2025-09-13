//+------------------------------------------------------------------+
//|                                                 ChartFullSet.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#include <MQL5Book/ChartModeMonitor.mqh>

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   int flags[1000];
   // prepare array with values covering all properties
   for(int i = 0; i < ArraySize(flags); ++i)
   {
      flags[i] = i;
   }
   // only existing properies will be attached to the monitor
   ChartModeMonitor m(flags);
   // make a backup and show total number of properties
   PrintFormat("Total number of properties: %d", m.backup());
   // monitor changes utill the script is stopped
   while(!IsStopped())
   {
      m.snapshot();
      Sleep(500);
   }
   // restore properties from backup
   m.restore();
}
//+------------------------------------------------------------------+
