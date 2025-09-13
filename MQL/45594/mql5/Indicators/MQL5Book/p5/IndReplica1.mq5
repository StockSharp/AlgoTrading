//+------------------------------------------------------------------+
//|                                                  IndReplica1.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property indicator_chart_window
#property indicator_buffers 1
#property indicator_plots 1

#include <MQL5Book/PRTF.mqh>

double buffer[];

//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
int OnInit()
{
   // register the array as indicator buffer
   PRTF(SetIndexBuffer(0, buffer)); // true / ok
   // this 2-nd incorrect call made deliberetly to show the error
   PRTF(SetIndexBuffer(1, buffer)); // false / BUFFERS_WRONG_INDEX(4602)
   // check the size, it's still 0
   PRTF(ArraySize(buffer)); // 0
   
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
   // check just after start-up how the buffer size is automatically managed by the platform
   if(prev_calculated == 0)
   {
      PRTF(ArraySize(buffer));
   }
   
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
