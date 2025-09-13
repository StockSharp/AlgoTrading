//+------------------------------------------------------------------+
//|                                          EA_OBJPROP_CHART_ID.mq5 |
//|                        Copyright 2011, MetaQuotes Software Corp. |
//|                                              http://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright "Copyright 2011, MetaQuotes Software Corp."
#property link      "http://www.mql5.com"
#property version   "1.00"

long daily_chart;
long H4_chart;
string D1="daily";
string H4="H4";

int subwindow_handle;
int subwindow_ID;
int indicator_handle;
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
//--- show foreground
   ChartSetInteger(0,CHART_FOREGROUND,1);

   indicator_handle=iCustom(_Symbol,_Period,"Examples\\Price_Channel");
   ChartIndicatorAdd(0,0,indicator_handle);
   subwindow_handle=iCustom(_Symbol,_Period,"Subwindow");
   subwindow_ID=(int)ChartGetInteger(0,CHART_WINDOWS_TOTAL);
   ChartIndicatorAdd(0,subwindow_ID,subwindow_handle);

//--- get subwindow height
   int height=(int)ChartGetInteger(0,CHART_HEIGHT_IN_PIXELS,subwindow_ID);

//--- create a daily (D1) chart in indicator's window
   CreateChartInSubwindow(1,800,0,400,height,D1,_Symbol,PERIOD_D1,clrLightCyan,daily_chart);
//--- create a chart (H4) in the in indicator's window
   CreateChartInSubwindow(1,400,0,400,height,H4,_Symbol,PERIOD_H4,clrHoneydew,H4_chart);
//---
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
//--- delete objects
//  ObjectDelete(0,D1);
//  ObjectDelete(0,H4);
//--- delete subwindow
   ChartIndicatorDelete(0,subwindow_ID,"Subwindow");
//--- delete PriceChannel indicator from the main window
//--- its short name with default parameters is "Price Channel(22)"
   ChartIndicatorDelete(0,0,"Price Channel(22)");
  }
//+------------------------------------------------------------------+
//|  Creates a OBJ_CHART object in the indicator's window            |
//+------------------------------------------------------------------+
bool CreateChartInSubwindow(int window,
                            int x,
                            int y,
                            int x_size,
                            int y_size,
                            string chartname,
                            string symbol,
                            ENUM_TIMEFRAMES timeframe,
                            color clr,
                            long  &chart_ID
                            )
  {
//--- if the object not found, create a new one
   if(ObjectFind(0,chartname)<0)
     {
      ResetLastError();
      //--- try to create OBJ_CHART object
      bool created=ObjectCreate(0,chartname,OBJ_CHART,window,0,0);
      if(!created)
        {
         PrintFormat("Error in creation of OBJ_CHART object with name %s. Error code=%d",
                     chartname,GetLastError());
         return(false);
        }
      else
        {
         // using chart operations to customize chart
         //--- center of coordinates is in the upper right corner of the chart
         ObjectSetInteger(0,chartname,OBJPROP_CORNER,CORNER_RIGHT_UPPER);
         //--- set distance in pixels along the X/Y axes from the binding corner
         ObjectSetInteger(0,chartname,OBJPROP_XDISTANCE,x);
         ObjectSetInteger(0,chartname,OBJPROP_YDISTANCE,y);

         //--- set chart width and height
         ObjectSetInteger(0,chartname,OBJPROP_XSIZE,x_size);
         ObjectSetInteger(0,chartname,OBJPROP_YSIZE,y_size);
         //--- set chart scale
         ObjectSetInteger(0,chartname,OBJPROP_CHART_SCALE,3);
         //--- hide price axis
         ObjectSetInteger(0,chartname,OBJPROP_PRICE_SCALE,0);

         //--- set period
         ObjectSetInteger(0,chartname,OBJPROP_PERIOD,timeframe);

         // using chart operations to customize chart
         //--- save chart handle
         chart_ID=ObjectGetInteger(0,chartname,OBJPROP_CHART_ID);

         //--- set background color
         ChartSetInteger(chart_ID,CHART_COLOR_BACKGROUND,clr);
         //--- foreground
         ChartSetInteger(chart_ID,CHART_FOREGROUND,1);

         //--- hide grid
         ChartSetInteger(chart_ID,CHART_SHOW_GRID,0);
         //--- set shift from the right border
         ChartSetInteger(chart_ID,CHART_SHIFT,1);
         //--- set shift size as 10% of the chart width
         ChartSetDouble(chart_ID,CHART_SHIFT_SIZE,10);

         ResetLastError();
         //--- create indicator handle
         int handle=iCustom(ChartSymbol(chart_ID),ChartPeriod(chart_ID),"Examples\\Price_Channel");
         if(handle!=INVALID_HANDLE)
           {
            //--- add indicator
            ChartIndicatorAdd(chart_ID,0,handle);
           }
         else
           {
            //--- print error message
            PrintFormat("Error in creation of the indicator handle. Error code=%d",GetLastError());
           }
         //--- redraw chart
         ChartRedraw(chart_ID);
        }

     }
   return(true);
  }
//+------------------------------------------------------------------+
