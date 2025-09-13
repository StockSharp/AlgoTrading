//+------------------------------------------------------------------+
//|                                                     BmpOwner.mq5 |
//|                                  Copyright 2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property indicator_chart_window
#property indicator_buffers 0
#property indicator_plots   0

// shared image (accessible from other programs)
#resource "search1.bmp"
// private image (declared only to demontrate that such resource is not shared)
#resource "search2.bmp" as bitmap image[]
// private text (used for alert below)
#resource "message.txt" as string Message

//+------------------------------------------------------------------+
//| Indicator initialization function                                |
//+------------------------------------------------------------------+
int OnInit()
{
   Alert(Message); // this is equivalent of the following line
   // Alert("This indicator is not intended to run, it holds a bitmap resource");
   
   // remove indicator explicitly because it remains hanging uninitialized on the chart
   ChartIndicatorDelete(0, 0, MQLInfoString(MQL_PROGRAM_NAME));
   return INIT_FAILED;
}

//+------------------------------------------------------------------+
//| Indicator calculation function (dummy here)                      |
//+------------------------------------------------------------------+
int OnCalculate(const int rates_total,
                const int prev_calculated,
                const int begin,
                const double &price[])
{
   return 0;
}
//+------------------------------------------------------------------+
