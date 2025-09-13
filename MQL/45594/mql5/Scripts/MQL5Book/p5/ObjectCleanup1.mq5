//+------------------------------------------------------------------+
//|                                               ObjectCleanup1.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//|                                                                  |
//| The script deletes all objects with specific name prefix.        |
//+------------------------------------------------------------------+
#property script_show_inputs

input bool UseCustomDeleteAll = false;

#include "ObjectPrefix.mqh"

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   const int n = UseCustomDeleteAll ?
      CustomDeleteAllObjects(0, ObjNamePrefix) :
      ObjectsDeleteAll(0, ObjNamePrefix);
   
   PrintFormat("%d objects deleted", n);
}

//+------------------------------------------------------------------+
//| Self-made equivalent of ObjectsDeleteAll                         |
//+------------------------------------------------------------------+
int CustomDeleteAllObjects(const long chart, const string prefix,
   const int window = -1, const int type = -1)
{
   int count = 0;
   const int n = ObjectsTotal(chart, window, type);
   
   // NB: loop objects backwards in the chart internal list
   // to preserve numbering while deleting from the tail
   for(int i = n - 1; i >= 0; --i)
   {
      const string name = ObjectName(chart, i, window, type);
      
      // straightforward approach is not useful much...
      if(StringLen(prefix) == 0 || StringFind(name, prefix) == 0)
      // until we need to select objects by special properties,
      // such as coordinates, color, anchor point, etc,
      // which we don't know yet - here is just a clue (see ObjectCleanup2.mq5)
      // && ObjectGetInteger(0, name, OBJPROP_COLOR) == clrRed
      // && ObjectGetInteger(0, name, OBJPROP_ANCHOR) == ANCHOR_TOP)
      {
         count += ObjectDelete(chart, name);
      }
   }
   ChartRedraw();
   return count;
}
//+------------------------------------------------------------------+
