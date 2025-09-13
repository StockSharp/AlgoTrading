//+------------------------------------------------------------------+
//|                                                    ChartDrop.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#include <MQL5Book/PRTF.mqh>

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   const int w = PRTF(ChartWindowOnDropped());
   const datetime t = PRTF(ChartTimeOnDropped());
   const double p = PRTF(ChartPriceOnDropped());
   PRTF(ChartXOnDropped());
   PRTF(ChartYOnDropped());
   
   // for subwindows, map y coordinate to specific subwindow
   if(w > 0)
   {
      const int y = (int)PRTF(ChartGetInteger(0, CHART_WINDOW_YDISTANCE, w));
      PRTF(ChartYOnDropped() - y);
   }
}
//+------------------------------------------------------------------+
/*
   Example output (dropped on first subwindow with WPR indicator:
   note that 'price' value is -50, because WPR range is between 0 and -100)
   
   ChartWindowOnDropped()=1 / ok
   ChartTimeOnDropped()=2021.11.30 03:52:30 / ok
   ChartPriceOnDropped()=-50.0 / ok
   ChartXOnDropped()=217 / ok
   ChartYOnDropped()=312 / ok
   ChartGetInteger(0,CHART_WINDOW_YDISTANCE,w)=282 / ok
   ChartYOnDropped()-y=30 / ok

*/
//+------------------------------------------------------------------+
