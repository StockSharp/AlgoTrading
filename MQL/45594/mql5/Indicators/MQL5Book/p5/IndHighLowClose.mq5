//+------------------------------------------------------------------+
//|                                              IndHighLowClose.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property indicator_chart_window
#property indicator_buffers 3
#property indicator_plots 2

double highs[];
double lows[];
double closes[];

//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
int OnInit()
{
   // register arrays for prices as indicator buffers
   SetIndexBuffer(0, highs);
   SetIndexBuffer(1, lows);
   SetIndexBuffer(2, closes);

   // adjust visual settings of the high-low plot under index 0
   PlotIndexSetInteger(0, PLOT_DRAW_TYPE, DRAW_HISTOGRAM2);
   PlotIndexSetInteger(0, PLOT_LINE_WIDTH, 5);
   PlotIndexSetInteger(0, PLOT_LINE_COLOR, clrBlue);

   // adjust visual settings of the close plot under index 1
   PlotIndexSetInteger(1, PLOT_DRAW_TYPE, DRAW_LINE);
   PlotIndexSetInteger(1, PLOT_LINE_WIDTH, 2);
   PlotIndexSetInteger(1, PLOT_LINE_COLOR, clrRed);

   return INIT_SUCCEEDED;
}

//+------------------------------------------------------------------+
//| Custom indicator iteration function                              |
//+------------------------------------------------------------------+
int OnCalculate(const int rates_total,
                const int prev_calculated,
                const datetime &time[],
                const double &open[],
                const double &high[],
                const double &low[],
                const double &close[],
                const long &tick_volume[],
                const long &volume[],
                const int &spread[])
{
   // on every new bar or many new bars (including first event)
   if(prev_calculated != rates_total)
   {
      // update new bars
      ArrayCopy(highs, high, prev_calculated, prev_calculated);
      ArrayCopy(lows, low, prev_calculated, prev_calculated);
      ArrayCopy(closes, close, prev_calculated, prev_calculated);
   }
   else // ticks on current bar
   {
      // update the latest bar
      highs[rates_total - 1] = high[rates_total - 1];
      lows[rates_total - 1] = low[rates_total - 1];
      closes[rates_total - 1] = close[rates_total - 1];
   }

   return rates_total; // report number of processed bars for future calls
}
//+------------------------------------------------------------------+
