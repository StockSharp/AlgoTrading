//+------------------------------------------------------------------+
//|                                             EventWindowSizer.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright   "2021, MetaQuotes Ltd."
#property link        "https://www.mql5.com"
#property description "Calculates and shows current window size in pixels using notifications about chart events (CHARTEVENT_CHART_CHANGE).\n\n"
#property description "You may take a screenshot without scales using 'Picture' values, or 'Including scales'."

#property indicator_chart_window
#property indicator_buffers 0
#property indicator_plots   0

//+------------------------------------------------------------------+
//| Custom indicator iteration function                              |
//| (dummy here, required for indicator)                             |
//+------------------------------------------------------------------+
int OnCalculate(const int rates_total,
                const int prev_calculated,
                const int begin,
                const double &price[])
{
   return rates_total;
}

//+------------------------------------------------------------------+
//| Chart event handler                                              |
//+------------------------------------------------------------------+
void OnChartEvent(const int id, const long &lparam, const double &dparam, const string &sparam)
{
   if(id == CHARTEVENT_CHART_CHANGE)
   {
      const int w = (int)ChartGetInteger(0, CHART_WIDTH_IN_PIXELS);
      const int h = (int)ChartGetInteger(0, CHART_HEIGHT_IN_PIXELS);
      // To take a screenshot with borders, need to adjust Screen value (X,Y) by (-2,-1) - shown as Picture.
      // To take it with scales - additionally adjust by (-54,-22) - shown as Including scales.
      Comment(StringFormat("Screen: %d x %d\nPicture: %d x %d\nIncluding scales: %d x %d",
         w, h, w + 2, h + 1, w + 2 + 54, h + 1 + 22));
   }
}

//+------------------------------------------------------------------+
//| Finalization handler                                             |
//+------------------------------------------------------------------+
void OnDeinit(const int)
{
   Comment("");
}
//+------------------------------------------------------------------+
