//+------------------------------------------------------------------+
//|                                               ChartCloseIdle.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#include <MQL5Book/Periods.mqh>
#include <MQL5Book/MapArray.mqh>

#define PUSH(A,V) (A[ArrayResize(A, ArraySize(A) + 1) - 1] = V)

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   MapArray<string,int> chartCounts;
   ulong duplicateChartIDs[];
   // collect duplicated idle charts
   if(ChartIdleList(chartCounts, duplicateChartIDs))
   {
      for(int i = 0; i < ArraySize(duplicateChartIDs); ++i)
      {
         const ulong id = duplicateChartIDs[i];
         // request the next idle chart to bring to front
         ChartSetInteger(id, CHART_BRING_TO_TOP, true);
         // refresh the windows, so the chart is actually brought to front
         ChartRedraw(id);
         // ask user confirmation for deletion
         const int button = MessageBox(
            "Remove idle chart: "
            + ChartSymbol(id) + "/" + PeriodToString(ChartPeriod(id)) + "?",
            __FILE__, MB_YESNOCANCEL);
         if(button == IDCANCEL) break;   
         if(button == IDYES)
         {
            ChartClose(id);
         }
      }
   }
   else
   {
      Print("No idle charts.");
   }
}

//+------------------------------------------------------------------+
//| Main worker function to collect idle charts                      |
//+------------------------------------------------------------------+
int ChartIdleList(MapArray<string,int> &map, ulong &duplicateChartIDs[])
{
   // keep enumerating all charts until no more found
   for(long id = ChartFirst(); id != -1; id = ChartNext(id))
   {
      // skip objects with charts
      if(ChartGetInteger(id, CHART_IS_OBJECT)) continue;

      // obtain main properties of the chart
      const int win = (int)ChartGetInteger(id, CHART_WINDOWS_TOTAL);
      const string expert = ChartGetString(id, CHART_EXPERT_NAME);
      const string script = ChartGetString(id, CHART_SCRIPT_NAME);
      const int objectCount = ObjectsTotal(id);

      // calculate number of indicators (if any)
      int indicators = 0;
      for(int i = 0; i < win; ++i)      
      {
         indicators += ChartIndicatorsTotal(id, i);
      }
      
      const string key = ChartSymbol(id) + "/" + PeriodToString(ChartPeriod(id));
      
      if(map[key] == 0     // always count first time occurence of symbol/timeframe
                           // otherwise count only empty charts:
         || (indicators == 0           // no indicators
            && StringLen(expert) == 0  // no expert
            && StringLen(script) == 0  // no script
            && objectCount == 0))      // no objects
      {
         const int i = map.inc(key);
         if(map[i] > 1)                // duplicate
         {
            PUSH(duplicateChartIDs, id);
         }
      }
   }
   return map.getSize();
}
//+------------------------------------------------------------------+
