//+------------------------------------------------------------------+
//|                                                      UseWPR3.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property indicator_separate_window
#property indicator_buffers 1
#property indicator_plots   1

// drawing settings
#property indicator_type1   DRAW_LINE
#property indicator_color1  clrBlue
#property indicator_width1  1
#property indicator_label1  "WPR"

input int WPRPeriod = 14;

// indicator buffer
double WPRBuffer[];

// global variable for subordinate indicator
int handle;

//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
int OnInit()
{
   SetIndexBuffer(0, WPRBuffer);
   handle = iCustom(_Symbol, _Period, "IndWPR", WPRPeriod);
   return handle == INVALID_HANDLE ? INIT_FAILED : INIT_SUCCEEDED;
}

//+------------------------------------------------------------------+
//| Custom indicator iteration function                              |
//+------------------------------------------------------------------+
int OnCalculate(const int rates_total,
                const int prev_calculated,
                const int begin,
                const double &data[])
{
   // wait until the subindicator is calculated for all bars
   if(BarsCalculated(handle) != rates_total)
   {
      return prev_calculated;
   }
   
   // copy data from subordinate indicator into our buffer
   const int n = CopyBuffer(handle, 0, 0, rates_total - prev_calculated + 1, WPRBuffer);
   
   return n > -1 ? rates_total : 0;
}
//+------------------------------------------------------------------+
