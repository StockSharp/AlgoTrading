//+------------------------------------------------------------------+
//|                                                   ObjectRays.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//|                                                                  |
//| The script creates a channel with right ray tirned ON.           |
//+------------------------------------------------------------------+
#include "ObjectPrefix.mqh"

//+------------------------------------------------------------------+
//| Create and setup a single channel object                         |
//+------------------------------------------------------------------+
void SetupChannel(const int length, const double deviation = 1.0,
   const bool right = false, const bool left = false,
   const color clr = clrRed)
{
   // create and setup a stddev channel
   const string name = ObjNamePrefix + "Channel"
      + (right ? "R" : "") + (left ? "L" : "");
   // NB: 0-th binding point must have time less than 1-st binding point,
   // otherwise the channel will collapse to a single point
   ObjectCreate(0, name, OBJ_STDDEVCHANNEL, 0, iTime(NULL, 0, length), 0);
   ObjectSetInteger(0, name, OBJPROP_TIME, 1, iTime(NULL, 0, 0));
   // deviation
   ObjectSetDouble(0, name, OBJPROP_DEVIATION, deviation);
   // color and description
   ObjectSetInteger(0, name, OBJPROP_COLOR, clr);
   ObjectSetString(0, name, OBJPROP_TEXT, StringFormat("%2.1", deviation)
      + ((!right && !left) ? " NO RAYS" : "")
      + (right ? " RIGHT RAY" : "") + (left ? " LEFT RAY" : ""));
   // ray properties go here
   ObjectSetInteger(0, name, OBJPROP_RAY_RIGHT, right);
   ObjectSetInteger(0, name, OBJPROP_RAY_LEFT, left);
   // highlight new object by selection (also it allows for easy deletion by user)
   ObjectSetInteger(0, name, OBJPROP_SELECTABLE, true);
   ObjectSetInteger(0, name, OBJPROP_SELECTED, true);
}

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   // create 3 channels with different ray settings
   SetupChannel(24, 1.0, true);
   SetupChannel(48, 2.0, false, true, clrBlue);
   SetupChannel(36, 3.0, false, false, clrGreen);
}
//+------------------------------------------------------------------+
