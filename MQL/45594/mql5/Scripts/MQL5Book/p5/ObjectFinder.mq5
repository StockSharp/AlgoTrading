//+------------------------------------------------------------------+
//|                                                 ObjectFinder.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//|                                                                  |
//| The script finds and lists all objects on all charts.            |
//+------------------------------------------------------------------+
#include <MQL5Book/Periods.mqh>

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   int count = 0;
   long id = ChartFirst();
   // loop through charts
   while(id != -1)
   {
      PrintFormat("%s %s (%lld)", ChartSymbol(id), PeriodToString(ChartPeriod(id)), id);
      const int win = (int)ChartGetInteger(id, CHART_WINDOWS_TOTAL);
      // loop through windows
      for(int k = 0; k < win; ++k)
      {
         PrintFormat("  Window %d", k);
         const int n = ObjectsTotal(id, k);
         // loop through objects
         for(int i = 0; i < n; ++i)
         {
            const string name = ObjectName(id, i, k);
            const ENUM_OBJECT type = (ENUM_OBJECT)ObjectGetInteger(id, name, OBJPROP_TYPE);
            PrintFormat("    %s %s", EnumToString(type), name);
            ++count;
         }
      }
      id = ChartNext(id);
   }
   
   PrintFormat("%d objects found", count);
}
//+------------------------------------------------------------------+
