//+------------------------------------------------------------------+
//|                                                   ObjectEdit.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//|                                                                  |
//| The script creates edit boxes with different properties.         |
//| User can click editable boxes and alter their content on chart.  |
//+------------------------------------------------------------------+
#include "ObjectPrefix.mqh"

//+------------------------------------------------------------------+
//| Create and setup a single bitmap label                           |
//+------------------------------------------------------------------+
void SetupEdit(const int x, const int y, const int dx, const int dy,
   const ENUM_ALIGN_MODE alignment = ALIGN_LEFT, const bool readonly = false)
{
   // create an object
   const string props = EnumToString(alignment) + (readonly ? " read-only" : " editable");
   const string name = ObjNamePrefix + "Edit" + props;
   ObjectCreate(0, name, OBJ_EDIT, 0, 0, 0);
   // position and size
   ObjectSetInteger(0, name, OBJPROP_XDISTANCE, x);
   ObjectSetInteger(0, name, OBJPROP_YDISTANCE, y);
   ObjectSetInteger(0, name, OBJPROP_XSIZE, dx);
   ObjectSetInteger(0, name, OBJPROP_YSIZE, dy);
   // specific properties for edit boxes
   ObjectSetInteger(0, name, OBJPROP_ALIGN, alignment);
   ObjectSetInteger(0, name, OBJPROP_READONLY, readonly);
   // colors
   ObjectSetInteger(0, name, OBJPROP_BGCOLOR, clrWhite);
   ObjectSetInteger(0, name, OBJPROP_COLOR, readonly ? clrRed : clrBlue);
   // text content
   ObjectSetString(0, name, OBJPROP_TEXT, props);
   // show a hint for editable boxes only 
   ObjectSetString(0, name, OBJPROP_TOOLTIP,
      (readonly ? "\n" : "Click me to edit"));
}

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   SetupEdit(100, 100, 200, 20);
   SetupEdit(100, 120, 200, 20, ALIGN_RIGHT);
   SetupEdit(100, 140, 200, 20, ALIGN_CENTER);
   SetupEdit(100, 160, 200, 20, ALIGN_CENTER, true);
}
//+------------------------------------------------------------------+
