//+------------------------------------------------------------------+
//|                                                  IndReplica2.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property indicator_chart_window
#property indicator_buffers 1
#property indicator_plots 1

input ENUM_DRAW_TYPE DrawType = DRAW_LINE;
input ENUM_LINE_STYLE LineStyle = STYLE_SOLID;

double buffer[];

//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
int OnInit()
{
   // register the array as indicator buffer
   SetIndexBuffer(0, buffer);

   // adjust visual settings of the plot under index 0
   PlotIndexSetInteger(0, PLOT_DRAW_TYPE, DrawType);
   PlotIndexSetInteger(0, PLOT_LINE_STYLE, LineStyle);
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
