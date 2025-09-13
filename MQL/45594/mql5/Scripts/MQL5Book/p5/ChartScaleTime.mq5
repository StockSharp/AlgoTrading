//+------------------------------------------------------------------+
//|                                               ChartScaleTime.mq5 |
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
      CHART_SCALE,
      CHART_VISIBLE_BARS,
      CHART_FIRST_VISIBLE_BAR,
      CHART_WIDTH_IN_BARS,
      CHART_WIDTH_IN_PIXELS
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
   [0]     5       4
   [1]   100      35
   [2]   104      34
   [3]   105      45
   [4]   106     715
                                     // 1) the chart is scaled down:
   CHART_SCALE 4 -> 3                // - the "scale" property changed
   CHART_VISIBLE_BARS 35 -> 69       // - number of visible bars increased
   CHART_FIRST_VISIBLE_BAR 34 -> 68  // - index of first visible bar increased
   CHART_WIDTH_IN_BARS 45 -> 90      // - potential chart capacity in bars increased
                                     // 2) right-side shift is switched off
   CHART_VISIBLE_BARS 69 -> 89       // - number of visible bars increased
   CHART_FIRST_VISIBLE_BAR 68 -> 88  // - index of first visible bar increased
                                     // 3) window is squeezed
   CHART_VISIBLE_BARS 89 -> 86       // - number of visible bars decreased
   CHART_WIDTH_IN_BARS 90 -> 86      // - potential chart capacity in bars decreased
   CHART_WIDTH_IN_PIXELS 715 -> 680  // - width in pixels decreased
                                     // 4) "End" key is pressed to go to latest quotes
   CHART_VISIBLE_BARS 86 -> 85       // - number of visible bars decreased
   CHART_FIRST_VISIBLE_BAR 88 -> 84  // - index of first visible bar decreased

*/
//+------------------------------------------------------------------+
