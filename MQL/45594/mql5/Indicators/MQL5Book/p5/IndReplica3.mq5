//+------------------------------------------------------------------+
//|                                                  IndReplica3.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property indicator_chart_window
#property indicator_buffers 1
#property indicator_plots 1

input uchar ArrowCode = 159;
input int ArrowPadding = 0;
input int TimeShift = 0;

double buffer[];

//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
int OnInit()
{
   // register the array as indicator buffer
   SetIndexBuffer(0, buffer);

   // adjust visual settings of the plot under index 0
   PlotIndexSetInteger(0, PLOT_DRAW_TYPE, DRAW_ARROW);
   PlotIndexSetInteger(0, PLOT_ARROW, ArrowCode);
   PlotIndexSetInteger(0, PLOT_ARROW_SHIFT, ArrowPadding);
   PlotIndexSetInteger(0, PLOT_SHIFT, TimeShift);
   PlotIndexSetInteger(0, PLOT_LINE_COLOR, clrBlue);
   
   return INIT_SUCCEEDED;
}

//+------------------------------------------------------------------+
//| Custom indicator iteration function                              |
//+------------------------------------------------------------------+
int OnCalculate(const int rates_total,
                const int prev_calculated,
                const int begin,
                const double &data[])
{
   // on every new bar or many new bars (including first event)
   if(prev_calculated != rates_total)
   {
      // update new bars
      ArrayCopy(buffer, data, prev_calculated, prev_calculated);
   }
   else // ticks on current bar
   {
      // update the latest bar
      buffer[rates_total - 1] = data[rates_total - 1];
   }

   return rates_total; // report number of processed bars for future calls
}
//+------------------------------------------------------------------+
