//+------------------------------------------------------------------+
//|                                                     EventAll.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright   "2021, MetaQuotes Ltd."
#property link        "https://www.mql5.com"
#property description "Intercepts all events and prints them to the log\n\n"

#property indicator_chart_window
#property indicator_buffers 0
#property indicator_plots   0

input bool ShowMouseMove = false;
input bool ShowMouseWheel = false;
input bool ShowObjectCreate = false;
input bool ShowObjectDelete = false;

#include <MQL5Book/PRTF.mqh>

bool mouseMove, mouseWheel, objectCreate, objectDelete;

//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
void OnInit()
{
   // show and remember default settings
   mouseMove = PRTF(ChartGetInteger(0, CHART_EVENT_MOUSE_MOVE));
   mouseWheel = PRTF(ChartGetInteger(0, CHART_EVENT_MOUSE_WHEEL));
   objectCreate = PRTF(ChartGetInteger(0, CHART_EVENT_OBJECT_CREATE));
   objectDelete = PRTF(ChartGetInteger(0, CHART_EVENT_OBJECT_DELETE));

   // assign new settings   
   ChartSetInteger(0, CHART_EVENT_MOUSE_MOVE, ShowMouseMove);
   ChartSetInteger(0, CHART_EVENT_MOUSE_WHEEL, ShowMouseWheel);
   ChartSetInteger(0, CHART_EVENT_OBJECT_CREATE, ShowObjectCreate);
   ChartSetInteger(0, CHART_EVENT_OBJECT_DELETE, ShowObjectDelete);
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
   ENUM_CHART_EVENT evt = (ENUM_CHART_EVENT)id;
   PrintFormat("%s %lld %f '%s'", EnumToString(evt), lparam, dparam, sparam);
}

//+------------------------------------------------------------------+
//| Finalization handler                                             |
//+------------------------------------------------------------------+
void OnDeinit(const int)
{
   // restore initial settings
   ChartSetInteger(0, CHART_EVENT_MOUSE_MOVE, mouseMove);
   ChartSetInteger(0, CHART_EVENT_MOUSE_WHEEL, mouseWheel);
   ChartSetInteger(0, CHART_EVENT_OBJECT_CREATE, objectCreate);
   ChartSetInteger(0, CHART_EVENT_OBJECT_DELETE, objectDelete);
}
//+------------------------------------------------------------------+
