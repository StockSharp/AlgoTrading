//+------------------------------------------------------------------+
//|                                                 ObjectBitmap.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//|                                                                  |
//| The script creates a bitmap label.                               |
//+------------------------------------------------------------------+
#include "ObjectPrefix.mqh"

//+------------------------------------------------------------------+
//| Create and setup a single bitmap label                           |
//+------------------------------------------------------------------+
void SetupBitmap(const string button, const int x, const int y,
   const string imageOn, const string imageOff = NULL)
{
   // create an object
   const string name = ObjNamePrefix + "Bitmap";
   ObjectCreate(0, name, OBJ_BITMAP_LABEL, 0, 0, 0);
   // position
   ObjectSetInteger(0, name, OBJPROP_XDISTANCE, x);
   ObjectSetInteger(0, name, OBJPROP_YDISTANCE, y);
   // images
   ObjectSetString(0, name, OBJPROP_BMPFILE, 0, imageOn);
   if(imageOff != NULL) ObjectSetString(0, name, OBJPROP_BMPFILE, 1, imageOff);
}

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   SetupBitmap("image", 100, 100,
      "\\Images\\dollar.bmp", "\\Images\\euro.bmp");
}
//+------------------------------------------------------------------+
