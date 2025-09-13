//+------------------------------------------------------------------+
//|                                              ObjectWingdings.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//|                                                                  |
//| The script creates arrow objects with all Wingdings marks.       |
//+------------------------------------------------------------------+
#include "ObjectPrefix.mqh"

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   // loop though all characters in Wingdings font
   for(int i = 33; i < 256; ++i)
   {
      const int b = i - 33;
      const string name = ObjNamePrefix + "Wingdings-" + (string)iTime(_Symbol, _Period, b);
      ObjectCreate(0, name,
         OBJ_ARROW, 0, iTime(_Symbol, _Period, b), iOpen(_Symbol, _Period, b));
      ObjectSetInteger(0, name, OBJPROP_ARROWCODE, i);
   }
   
   PrintFormat("%d objects with arrows created", 256 - 33);
}
//+------------------------------------------------------------------+
