//+------------------------------------------------------------------+
//|                                                ChartElements.mq5 |
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
      CHART_SHOW,
      CHART_SHOW_TICKER, CHART_SHOW_OHLC,
      CHART_SHOW_BID_LINE, CHART_SHOW_ASK_LINE, CHART_SHOW_LAST_LINE,
      CHART_SHOW_PERIOD_SEP, CHART_SHOW_GRID,
      CHART_SHOW_VOLUMES,
      CHART_SHOW_OBJECT_DESCR,
      CHART_SHOW_TRADE_LEVELS,
      CHART_SHOW_DATE_SCALE, CHART_SHOW_PRICE_SCALE,
      CHART_SHOW_ONE_CLICK
   };
   ChartModeMonitor m(flags);
   Print("Initial state:");
   m.print();
   m.backup();
   
   // artificially hide price and time scales
   // (don't worry - we'll restore them from backup below)
   ChartSetInteger(0, CHART_SHOW_DATE_SCALE, false); 
   ChartSetInteger(0, CHART_SHOW_PRICE_SCALE, false); 
   
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
   [ 0]    46       1
   [ 1]   118       1
   [ 2]    12       0
   [ 3]    13       1
   [ 4]    14       0
   [ 5]    15       0
   [ 6]    16       1
   [ 7]    17       1
   [ 8]    18       0
   [ 9]    19       0
   [10]    34       1
   [11]    36       1
   [12]    37       1
   [13]    44       0
   CHART_SHOW_DATE_SCALE 1 -> 0
   CHART_SHOW_PRICE_SCALE 1 -> 0
   CHART_SHOW_ONE_CLICK 0 -> 1
   CHART_SHOW_GRID 1 -> 0
   CHART_SHOW_VOLUMES 0 -> 2
   CHART_SHOW_VOLUMES 2 -> 1
   CHART_SHOW_TRADE_LEVELS 1 -> 0

*/
//+------------------------------------------------------------------+
