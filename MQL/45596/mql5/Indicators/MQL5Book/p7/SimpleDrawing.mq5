//+------------------------------------------------------------------+
//|                                                SimpleDrawing.mq5 |
//|                              Copyright (c) 2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property indicator_chart_window
#property indicator_buffers 0
#property indicator_plots   0

#include "SimpleDrawing.mqh"

input int Size = 25;
input ENUM_BASE_CORNER Corner = CORNER_RIGHT_LOWER;
input DRAW::ORIENTATION Orientation = DRAW::VERTICAL;

AutoPtr<DRAW::SimpleDrawing> ShapesDrawing;

//+------------------------------------------------------------------+
//| Indicator initialization function                                |
//+------------------------------------------------------------------+
void OnInit()
{
   ChartSetInteger(0, CHART_MOUSE_SCROLL, false);
   ChartSetInteger(0, CHART_EVENT_MOUSE_MOVE, true);
   ChartSetInteger(0, CHART_SHOW_DATE_SCALE, false);
   ChartSetInteger(0, CHART_SHOW_PRICE_SCALE, false);
   ShapesDrawing = new DRAW::SimpleDrawing("DRAW_", Size, Corner, Orientation);
}

//+------------------------------------------------------------------+
//| Indicator calculation function (dummy here)                      |
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
   if(id == CHARTEVENT_OBJECT_CLICK)
   {
      ShapesDrawing[].onObjectClick(id, lparam, dparam, sparam);
   }
   else if(id == CHARTEVENT_MOUSE_MOVE)
   {
      ShapesDrawing[].onMouseMove(id, lparam, dparam, sparam);
   }
   else if(id == CHARTEVENT_CHART_CHANGE)
   {
      ShapesDrawing[].onChartChange();
   }
}

//+------------------------------------------------------------------+
//| Indicator finalization function                                  |
//+------------------------------------------------------------------+
void OnDeinit(const int)
{
   ChartSetInteger(0, CHART_MOUSE_SCROLL, true);
   ChartSetInteger(0, CHART_EVENT_MOUSE_MOVE, false);
   ChartSetInteger(0, CHART_SHOW_DATE_SCALE, true);
   ChartSetInteger(0, CHART_SHOW_PRICE_SCALE, true);
}
//+------------------------------------------------------------------+
