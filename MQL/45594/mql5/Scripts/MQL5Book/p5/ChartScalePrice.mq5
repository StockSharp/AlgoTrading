//+------------------------------------------------------------------+
//|                                              ChartScalePrice.mq5 |
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
      CHART_SCALEFIX, CHART_SCALEFIX_11,
      CHART_SCALE_PT_PER_BAR, CHART_POINTS_PER_BAR,
      CHART_FIXED_MAX, CHART_FIXED_MIN,
      CHART_PRICE_MIN, CHART_PRICE_MAX,
      CHART_HEIGHT_IN_PIXELS, CHART_WINDOW_YDISTANCE
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
	    [key] [value]   // ENUM_CHART_PROPERTY_INTEGER
	[0]     6       0
	[1]     7       0
	[2]    10       0
	[3]   107     357
	[4]   110       0
	    [key]  [value]  // ENUM_CHART_PROPERTY_DOUBLE
	[0]    11 10.00000
	[1]     8  1.13880
	[2]     9  1.12330
	[3]   108  1.12330
	[4]   109  1.13880
	// shrink window vertically
	CHART_HEIGHT_IN_PIXELS 357 -> 370
	CHART_HEIGHT_IN_PIXELS 370 -> 408
	CHART_FIXED_MAX 1.1389 -> 1.1388
	CHART_FIXED_MIN 1.1232 -> 1.1233
	CHART_PRICE_MIN 1.1232 -> 1.1233
	CHART_PRICE_MAX 1.1389 -> 1.1388
	// squeeze horizontal scale, so price range is increased
	CHART_FIXED_MAX 1.1388 -> 1.139
	CHART_FIXED_MIN 1.1233 -> 1.1183
	CHART_PRICE_MIN 1.1233 -> 1.1183
	CHART_PRICE_MAX 1.1388 -> 1.139
	CHART_FIXED_MAX 1.139 -> 1.1406
	CHART_FIXED_MIN 1.1183 -> 1.1167
	CHART_PRICE_MIN 1.1183 -> 1.1167
	CHART_PRICE_MAX 1.139 -> 1.1406
	// drag'n'drop price scale to show larger range (quotes became more "flat") 
	CHART_FIXED_MAX 1.1406 -> 1.1454
	CHART_FIXED_MIN 1.1167 -> 1.1119
	CHART_PRICE_MIN 1.1167 -> 1.1119
	CHART_PRICE_MAX 1.1406 -> 1.1454

*/
//+------------------------------------------------------------------+
