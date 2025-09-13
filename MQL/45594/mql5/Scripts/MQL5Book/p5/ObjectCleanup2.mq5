//+------------------------------------------------------------------+
//|                                               ObjectCleanup2.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//|                                                                  |
//| The script deletes all objects with specific name prefix         |
//| or custom properties (color and anchor point).                   |
//+------------------------------------------------------------------+
#property script_show_inputs

input bool UseCustomDeleteAll = false;
input color CustomColor = clrRed;
input ENUM_ARROW_ANCHOR CustomAnchor = ANCHOR_TOP;

#include "ObjectPrefix.mqh"
#include <MQL5Book/ObjectMonitor.mqh>

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   const int n = UseCustomDeleteAll ?
      CustomDeleteAllObjects(0, ObjNamePrefix, CustomColor, CustomAnchor) :
      ObjectsDeleteAll(0, ObjNamePrefix);
   
   PrintFormat("%d objects deleted", n);
}

//+------------------------------------------------------------------+
//| Self-made equivalent of ObjectsDeleteAll                         |
//+------------------------------------------------------------------+
int CustomDeleteAllObjects(const long chart, const string prefix,
   color clr, ENUM_ARROW_ANCHOR anchor,
   const int window = -1, const int type = -1)
{
   int count = 0;
   const int n = ObjectsTotal(chart, window, type);
   
   // NB: loop objects backwards in the chart internal list
   // to preserve numbering while deleting from the tail
   for(int i = n - 1; i >= 0; --i)
   {
      const string name = ObjectName(chart, i, window, type);
      
      ObjectSelector s(name);
      ResetLastError();
      if((StringLen(prefix) == 0 || StringFind(s.get(OBJPROP_NAME), prefix) == 0)
      && (s.get(OBJPROP_COLOR) == clr || clr == clrNONE)
      && s.get(OBJPROP_ANCHOR) == anchor
      && _LastError != 4203) // OBJECT_WRONG_PROPERTY
      {
         count += ObjectDelete(chart, name);
      }
   }
   ChartRedraw();
   return count;
}
//+------------------------------------------------------------------+
