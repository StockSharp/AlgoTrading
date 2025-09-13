//+------------------------------------------------------------------+
//|                                                ObjectButtons.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//|                                                                  |
//| The script creates 2 buttons with pressed and released states.   |
//+------------------------------------------------------------------+
#include "ObjectPrefix.mqh"

//+------------------------------------------------------------------+
//| Create and setup a single button object                          |
//+------------------------------------------------------------------+
void SetupButton(const string button,
   const int x, const int y,
   const int dx, const int dy,
   const bool state = false)
{
   // create and setup a button
   const string name = ObjNamePrefix + button;
   ObjectCreate(0, name, OBJ_BUTTON, 0, 0, 0);
   // position and size
   ObjectSetInteger(0, name, OBJPROP_XDISTANCE, x);
   ObjectSetInteger(0, name, OBJPROP_YDISTANCE, y);
   ObjectSetInteger(0, name, OBJPROP_XSIZE, dx);
   ObjectSetInteger(0, name, OBJPROP_YSIZE, dy);

   ObjectSetString(0, name, OBJPROP_TEXT, button);

   // pressed (true), released (false)
   ObjectSetInteger(0, name, OBJPROP_STATE, state);
}

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   // create 2 buttons
   SetupButton("Pressed", 100, 100, 100, 20, true);
   SetupButton("Normal", 100, 150, 100, 20);
}
//+------------------------------------------------------------------+
