//+------------------------------------------------------------------+
//|                                         ObjectSimpleShowcase.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//|                                                                  |
//| The script creates all object types with single anchors point.   |
//+------------------------------------------------------------------+
#include "ObjectPrefix.mqh"

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   ENUM_OBJECT types[] =
   {
      // straigt lines
      OBJ_VLINE, OBJ_HLINE,
      // marks (arrows and other signs)
      OBJ_ARROW_THUMB_UP, OBJ_ARROW_THUMB_DOWN,
      OBJ_ARROW_UP, OBJ_ARROW_DOWN,
      OBJ_ARROW_STOP, OBJ_ARROW_CHECK,
      OBJ_ARROW_LEFT_PRICE, OBJ_ARROW_RIGHT_PRICE,
      OBJ_ARROW_BUY, OBJ_ARROW_SELL,
      // OBJ_ARROW, // see ObjectWingdings.mq5
      
      // text
      OBJ_TEXT,
      // event flag (calendar-like) at the bottom
      OBJ_EVENT,
   };
 
   const int n = ArraySize(types);
   for(int i = 0; i < n; ++i)
   {
      ObjectCreate(0, ObjNamePrefix + (string)iTime(_Symbol, _Period, i), types[i],
         0, iTime(_Symbol, _Period, i), iClose(_Symbol, _Period, i));
   }
   
   PrintFormat("%d objects of various types created", n);
}
//+------------------------------------------------------------------+
