//+------------------------------------------------------------------+
//|                                                ObjectListing.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//|                                                                  |
//| The script logs all objects and their main props on current chart|
//+------------------------------------------------------------------+
#include "ObjectPrefix.mqh"

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   // make sure at least 1 object is created on the chart
   const string vline = ObjNamePrefix + "current";
   // this line will be re-created at latest bar
   ObjectCreate(0, vline, OBJ_VLINE, 0, iTime(NULL, 0, 0), 0);
   ObjectSetString(0, vline, OBJPROP_TEXT, "Latest Bar At The Moment");
   
   int count = 0;
   const long id = ChartID();
   
   const int win = (int)ChartGetInteger(id, CHART_WINDOWS_TOTAL);
   // loop through windows
   for(int k = 0; k < win; ++k)
   {
      PrintFormat("  Window %d", k);
      const int n = ObjectsTotal(id, k);
      // loop through objects
      for(int i = 0; i < n; ++i)
      {
         const string name = ObjectName(id, i, k);
         const ENUM_OBJECT type = (ENUM_OBJECT)ObjectGetInteger(id, name, OBJPROP_TYPE);
         const datetime created = (datetime)ObjectGetInteger(id, name, OBJPROP_CREATETIME);
         const string description = ObjectGetString(id, name, OBJPROP_TEXT);
         const string hint = ObjectGetString(id, name, OBJPROP_TOOLTIP);
         PrintFormat("    %s %s %s %s %s", EnumToString(type), name, TimeToString(created), description, hint);
         ++count;
      }
   }
   
   PrintFormat("%d objects found", count);
}
//+------------------------------------------------------------------+
