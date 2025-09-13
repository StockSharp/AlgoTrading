//+------------------------------------------------------------------+
//|                                                  ObjectChart.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright "Copyright 2021, MetaQuotes Ltd."
#property link      "https://www.mql5.com"
#property version   "1.0"
#property description "Create 2 micro-chart objects in a subwindow to show higher and lower timeframes."

#define SUBCHART_HEIGHT 150
#define SUBCHART_WIDTH  200

// bufferless indicator in a subwindow
#property indicator_separate_window
#property indicator_height SUBCHART_HEIGHT
#property indicator_buffers 0
#property indicator_plots   0
// without exact range below indicator title is not shown in the subwindow
#property indicator_maximum 1.0
#property indicator_minimum 0.0

#include <MQL5Book/Periods.mqh>

#define PeriodUp(P) PeriodRelative(P, +1)
#define PeriodDown(P) PeriodRelative(P, -1)

const string Prefix = "ObjectChart-";

//+------------------------------------------------------------------+
//| Find higher or lower timeframe against specified one             |
//+------------------------------------------------------------------+
ENUM_TIMEFRAMES PeriodRelative(const ENUM_TIMEFRAMES tf, const int step)
{
   static const ENUM_TIMEFRAMES stdtfs[] =
   {
      PERIOD_M1,  // =1 (1)
      PERIOD_M2,  // =2 (2)
      PERIOD_M3,  // =3 (3)
      PERIOD_M4,  // =4 (4)
      PERIOD_M5,  // =5 (5)
      PERIOD_M6,  // =6 (6)
      PERIOD_M10, // =10 (A)
      PERIOD_M12, // =12 (C)
      PERIOD_M15, // =15 (F)
      PERIOD_M20, // =20 (14)
      PERIOD_M30, // =30 (1E)
      PERIOD_H1,  // =16385 (4001)
      PERIOD_H2,  // =16386 (4002)
      PERIOD_H3,  // =16387 (4003)
      PERIOD_H4,  // =16388 (4004)
      PERIOD_H6,  // =16390 (4006)
      PERIOD_H8,  // =16392 (4008)
      PERIOD_H12, // =16396 (400C)
      PERIOD_D1,  // =16408 (4018)
      PERIOD_W1,  // =32769 (8001)
      PERIOD_MN1, // =49153 (C001)
   };
   const int x = ArrayBsearch(stdtfs, tf == PERIOD_CURRENT ? _Period : tf);
   const int needle = x + step;
   if(needle >= 0 && needle < ArraySize(stdtfs))
   {
      return stdtfs[needle];
   }
   return tf;
}

//+------------------------------------------------------------------+
//| Create and setup a single bitmap label                           |
//+------------------------------------------------------------------+
long SetupSubChart(const int n, const int dx, const int dy,
   ENUM_TIMEFRAMES tf = PERIOD_CURRENT, const string symbol = NULL)
{
   // create an object in the subwindow
   const string name = Prefix + "Chart-"
      + (symbol == NULL ? _Symbol : symbol) + PeriodToString(tf);
   ObjectCreate(0, name, OBJ_CHART, ChartWindowFind(), 0, 0);
   
   // binding to the corner
   ObjectSetInteger(0, name, OBJPROP_CORNER, CORNER_RIGHT_UPPER);

   // position and size
   ObjectSetInteger(0, name, OBJPROP_XSIZE, dx);
   ObjectSetInteger(0, name, OBJPROP_YSIZE, dy);
   ObjectSetInteger(0, name, OBJPROP_XDISTANCE, (n + 1) * dx);
   ObjectSetInteger(0, name, OBJPROP_YDISTANCE, 0);
   
   // specific chart settings
   if(symbol != NULL)
   {
      ObjectSetString(0, name, OBJPROP_SYMBOL, symbol);
   }
   
   if(tf != PERIOD_CURRENT)
   {
      ObjectSetInteger(0, name, OBJPROP_PERIOD, tf);
   }
   
   ObjectSetInteger(0, name, OBJPROP_DATE_SCALE, false);
   ObjectSetInteger(0, name, OBJPROP_PRICE_SCALE, false);
   
   const long id = ObjectGetInteger(0, name, OBJPROP_CHART_ID);
   ChartIndicatorAdd(id, 0, iMA(NULL, tf, 10, 0, MODE_EMA, PRICE_CLOSE));
   return id;
}

//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
void OnInit()
{
   Print(SetupSubChart(0, SUBCHART_WIDTH, SUBCHART_HEIGHT, PeriodUp(_Period)));
   Print(SetupSubChart(1, SUBCHART_WIDTH, SUBCHART_HEIGHT, PeriodDown(_Period)));
}

//+------------------------------------------------------------------+
//| Custom indicator iteration function (dummy here)                 |
//+------------------------------------------------------------------+
int OnCalculate(const int rates_total,
                const int prev_calculated,
                const int begin,
                const double &price[])
{
   return rates_total;
}

//+------------------------------------------------------------------+
//| Finalization handler                                             |
//+------------------------------------------------------------------+
void OnDeinit(const int)
{
   ObjectsDeleteAll(0, Prefix);
}
//+------------------------------------------------------------------+
