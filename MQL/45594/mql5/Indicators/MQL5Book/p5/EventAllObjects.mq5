//+------------------------------------------------------------------+
//|                                              EventAllObjects.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright   "2021, MetaQuotes Ltd."
#property link        "https://www.mql5.com"
#property description "This indicator creates a bunch of objects and takes some actions on them.\n"
                      "Also it traces and prints all events to the log."

#property indicator_chart_window
#property indicator_buffers 0
#property indicator_plots   0

#include <MQL5Book/ObjectMonitor.mqh>

const string ObjNamePrefix = "EventShow-";
const string ButtonName = ObjNamePrefix + "Button";
const string EditBoxName = ObjNamePrefix + "EditBox";
const string VLineName = ObjNamePrefix + "VLine";

bool objectCreate, objectDelete;

//+------------------------------------------------------------------+
//| Helper class for object creation and setup                       |
//+------------------------------------------------------------------+
class ObjectBuilder: public ObjectSelector
{
protected:
   const ENUM_OBJECT type;
   const int window;
public:
   ObjectBuilder(const string _id, const ENUM_OBJECT _type,
      const long _chart = 0, const int _win = 0):
      ObjectSelector(_id, _chart), type(_type), window(_win)
   {
      ObjectCreate(host, id, type, window, 0, 0);
   }
   // changing name and chart is prohibited in the builder
   virtual void name(const string _id) override = delete;
   virtual void chart(const long _chart) override = delete;
};

//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
void OnInit()
{
   // remember default settings
   objectCreate = ChartGetInteger(0, CHART_EVENT_OBJECT_CREATE);
   objectDelete = ChartGetInteger(0, CHART_EVENT_OBJECT_DELETE);

   // assign new settings   
   ChartSetInteger(0, CHART_EVENT_OBJECT_CREATE, true);
   ChartSetInteger(0, CHART_EVENT_OBJECT_DELETE, true);
   
   // create and setup demo objects
   ObjectBuilder button(ButtonName, OBJ_BUTTON);
   button.set(OBJPROP_XDISTANCE, 100).set(OBJPROP_YDISTANCE, 100)
   .set(OBJPROP_XSIZE, 200).set(OBJPROP_TEXT, "Click Me");

   ObjectBuilder line(VLineName, OBJ_VLINE);
   line.set(OBJPROP_TIME, iTime(NULL, 0, 0))
   .set(OBJPROP_SELECTABLE, true).set(OBJPROP_SELECTED, true)
   .set(OBJPROP_TEXT, "Drag Me").set(OBJPROP_TOOLTIP, "Drag Me");
   
   ChartRedraw();
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
   ENUM_CHART_EVENT evt = (ENUM_CHART_EVENT)id;
   PrintFormat("%s %lld %f '%s'", EnumToString(evt), lparam, dparam, sparam);
   if(id == CHARTEVENT_OBJECT_CLICK && sparam == ButtonName)
   {
      if(ObjectGetInteger(0, ButtonName, OBJPROP_STATE))
      {
         ObjectBuilder edit(EditBoxName, OBJ_EDIT);
         edit.set(OBJPROP_XDISTANCE, 100).set(OBJPROP_YDISTANCE, 150)
         .set(OBJPROP_BGCOLOR, clrWhite)
         .set(OBJPROP_XSIZE, 200).set(OBJPROP_TEXT, "Edit Me");
      }
      else
      {
         ObjectDelete(0, EditBoxName);
      }
      
      ChartRedraw();
   }
   else if(id == CHARTEVENT_OBJECT_ENDEDIT && sparam == EditBoxName)
   {
      Print(ObjectGetString(0, EditBoxName, OBJPROP_TEXT));
   }
   else if(id == CHARTEVENT_OBJECT_DRAG && sparam == VLineName)
   {
      Print(TimeToString((datetime)ObjectGetInteger(0, VLineName, OBJPROP_TIME)));
   }
}

//+------------------------------------------------------------------+
//| Finalization handler                                             |
//+------------------------------------------------------------------+
void OnDeinit(const int)
{
   // restore initial settings
   ChartSetInteger(0, CHART_EVENT_OBJECT_CREATE, objectCreate);
   ChartSetInteger(0, CHART_EVENT_OBJECT_DELETE, objectDelete);
   ObjectsDeleteAll(0, ObjNamePrefix);
   ChartRedraw();
}
//+------------------------------------------------------------------+
