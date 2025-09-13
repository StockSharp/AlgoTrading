//+------------------------------------------------------------------+
//|                                            EventTranslateKey.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright   "2021, MetaQuotes Ltd."
#property link        "https://www.mql5.com"
#property description "Translates virtual key codes to characters if possible and accumulates them in Comment."

#property indicator_chart_window
#property indicator_buffers 0
#property indicator_plots   0

#include <VirtualKeys.mqh>

string message = "";

//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
void OnInit()
{
   ChartSetInteger(0, CHART_QUICK_NAVIGATION, false);
}

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
   if(id == CHARTEVENT_KEYDOWN)
   {
      if(lparam == VK_RETURN)
      {
         message += "\n";
      }
      else if(lparam == VK_BACK)
      {
         StringSetLength(message, StringLen(message) - 1);
      }
      else
      {
         ResetLastError();
         const ushort c = TranslateKey((int)lparam);
         if(_LastError == 0)
         {
            message += ShortToString(c);
         }
      }
      Comment(message);
   }
}

//+------------------------------------------------------------------+
//| Finalization handler                                             |
//+------------------------------------------------------------------+
void OnDeinit(const int)
{
   Comment("");
   ChartSetInteger(0, CHART_QUICK_NAVIGATION, true);
}
//+------------------------------------------------------------------+
