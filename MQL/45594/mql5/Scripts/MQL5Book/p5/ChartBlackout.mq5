//+------------------------------------------------------------------+
//|                                                ChartBlackout.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   Print("This script switches the entire chart between visible and hidden states."
         " Run it twice for recovery.");
   ChartSetInteger(0, CHART_SHOW, !ChartGetInteger(0, CHART_SHOW));
}
//+------------------------------------------------------------------+
