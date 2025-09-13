//+------------------------------------------------------------------+
//|                                             ObjectTimeframes.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//|                                                                  |
//| The script creates labels for all timeframes,                    |
//| each one is visible on its own timeframe.                        |
//+------------------------------------------------------------------+
#include "ObjectPrefix.mqh"
#include <MQL5Book/EnumToArray.mqh>

//+------------------------------------------------------------------+
//| Get brief name of timeframe (drop PERIOD_ prefix)                |
//+------------------------------------------------------------------+
string GetPeriodName(const int tf)
{
   const static int PERIOD_ = StringLen("PERIOD_");
   return StringSubstr(EnumToString((ENUM_TIMEFRAMES)tf), PERIOD_);
}

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   // obtain full list of all timeframes
   ENUM_TIMEFRAMES tf = 0;
   int values[];
   const int n = EnumToArray(tf, values, 0, USHORT_MAX);

   // find center of the window
   const int centerX = (int)ChartGetInteger(0, CHART_WIDTH_IN_PIXELS) / 2;
   const int centerY = (int)ChartGetInteger(0, CHART_HEIGHT_IN_PIXELS) / 2;
   
   // loop through all timeframes
   for(int i = 1; i < n; ++i)
   {
      // create and setup text label for each timeframe
      const string name = ObjNamePrefix + (string)i;
      ObjectCreate(0, name, OBJ_LABEL, 0, 0, 0);
      ObjectSetInteger(0, name, OBJPROP_XDISTANCE, centerX);
      ObjectSetInteger(0, name, OBJPROP_YDISTANCE, centerY);
      ObjectSetInteger(0, name, OBJPROP_ANCHOR, ANCHOR_CENTER);
      ObjectSetString(0, name, OBJPROP_TEXT, GetPeriodName(values[i]));
      ObjectSetInteger(0, name, OBJPROP_FONTSIZE, fmin(centerY, centerX));
      ObjectSetInteger(0, name, OBJPROP_COLOR, clrLightGray);
      ObjectSetInteger(0, name, OBJPROP_BACK, true);
      
      // calculate the flag of i-th timeframe visibility
      const int flag = 1 << (i - 1);
      ObjectSetInteger(0, name, OBJPROP_TIMEFRAMES, flag);
   }
}
//+------------------------------------------------------------------+
