//+------------------------------------------------------------------+
//|                                              ObjectGroupEdit.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright "Copyright 2021, MetaQuotes Ltd."
#property link      "https://www.mql5.com"
#property version   "1.0"
#property description "Apply changes made in a single object properties dialog to all selected objects on current chart."

#property indicator_chart_window
#property indicator_buffers 0
#property indicator_plots   0

#include <MQL5Book/ObjectMonitor.mqh>

#define PUSH(A,V) (A[ArrayResize(A, ArraySize(A) + 1) - 1] = V)

int consts[2048];
string selected[];
ObjectMonitor *objects[];

//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
void OnInit()
{
   for(int i = 0; i < ArraySize(consts); ++i)
   {
      consts[i] = i;
   }
   
   EventSetTimer(1);
}

//+------------------------------------------------------------------+
//| Monitor properties of objects selected on the chart              |
//+------------------------------------------------------------------+
void TrackSelectedObjects()
{
   for(int j = 0; j < ArraySize(objects); ++j)
   {
      delete objects[j];
   }
   
   ArrayResize(objects, 0, ArraySize(selected));

   for(int i = 0; i < ArraySize(selected); ++i)
   {
      const string name = selected[i];
      PUSH(objects, new ObjectMonitor(name, consts)); // can make .backup() for undo
   }
}

//+------------------------------------------------------------------+
//| Timer event handler                                              |
//+------------------------------------------------------------------+
void OnTimer()
{
   // collect names of selected objects in the following array
   string updates[];
   const int n = ObjectsTotal(0);
   for(int i = 0; i < n; ++i)
   {
      const string name = ObjectName(0, i);
      if(ObjectGetInteger(0, name, OBJPROP_SELECTED))
      {
         PUSH(updates, name);
      }
   }
   
   if(ArraySize(selected) != ArraySize(updates))
   {
      ArraySwap(selected, updates);
      Comment("Selected objects: ", ArraySize(selected));
      TrackSelectedObjects();
   }
}

//+------------------------------------------------------------------+
//| Custom indicator iteration function                              |
//| (dummy here, required for indicator)                             |
//+------------------------------------------------------------------+
int OnCalculate(const int rates_total,
                const int prev_calculated,
                const int begin,
                const double &price[])
{
   return rates_total;
}

//+------------------------------------------------------------------+
//| Chart event handler                                              |
//+------------------------------------------------------------------+
void OnChartEvent(const int id, const long &lparam, const double &dparam, const string &sparam)
{
   if(id == CHARTEVENT_OBJECT_CHANGE)
   {
      Print("Object changed: ", sparam);
      for(int i = 0; i < ArraySize(selected); ++i)
      {
         if(sparam == selected[i])
         {
            const int changes = objects[i].snapshot();
            if(changes > 0)
            {
               for(int j = 0; j < ArraySize(objects); ++j)
               {
                  if(j != i)
                  {
                     objects[j].applyChanges(objects[i]);
                  }
               }
            }
            ChartRedraw();
            return;
         }
      }
   }
}

//+------------------------------------------------------------------+
//| Finalization handler                                             |
//+------------------------------------------------------------------+
void OnDeinit(const int)
{
   for(int j = 0; j < ArraySize(objects); ++j)
   {
      delete objects[j];
   }
   Comment("");
}
//+------------------------------------------------------------------+
