//+------------------------------------------------------------------+
//|                                              EventMouseWheel.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright   "2021, MetaQuotes Ltd."
#property link        "https://www.mql5.com"
#property description "Intercept and log mouse wheel events."

#property indicator_chart_window
#property indicator_buffers 0
#property indicator_plots   0

#define KEY_FLAG_NUMBER 7

const string keyNameByBit[KEY_FLAG_NUMBER] =
{
   "[Left Mouse] ",
   "[Right Mouse] ",
   "(Shift) ",
   "(Ctrl) ",
   "[Middle Mouse] ",
   "[Ext1 Mouse] ",
   "[Ext2 Mouse] ",
};

bool mouseWheel;

//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
void OnInit()
{
   mouseWheel = (bool)ChartGetInteger(0, CHART_EVENT_MOUSE_WHEEL);
   ChartSetInteger(0, CHART_EVENT_MOUSE_WHEEL, true);
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
   if(id == CHARTEVENT_MOUSE_WHEEL)
   {
      const int keymask = (int)(lparam >> 32);
      const short x = (short)lparam;
      const short y = (short)(lparam >> 16);
      const short delta = (short)dparam;
      string message = "";
      
      for(int i = 0; i < KEY_FLAG_NUMBER; ++i)
      {
         if(((1 << i) & keymask) != 0)
         {
            message += keyNameByBit[i];
         }
      }
      
      PrintFormat("X=%d Y=%d D=%d %s", x, y, delta, message);
   }
}

//+------------------------------------------------------------------+
//| Finalization handler                                             |
//+------------------------------------------------------------------+
void OnDeinit(const int)
{
   ChartSetInteger(0, CHART_EVENT_MOUSE_WHEEL, mouseWheel);
}
//+------------------------------------------------------------------+
