//+------------------------------------------------------------------+
//|                                                   ObjectFont.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//|                                                                  |
//| The script creates and periodically updates objects with text:   |
//| font name and size are modified.                                 |
//+------------------------------------------------------------------+
#include <MQL5Book/ObjectMonitor.mqh>
#include <MQL5Book/AutoPtr.mqh>

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
   
   ~ObjectBuilder()
   {
      ObjectDelete(host, id);
   }
   
   bool isExisting() const
   {
      return ObjectFind(host, id) == window;
   }
   
   // changing name and chart is prohibited in the builder
   virtual void name(const string _id) override = delete;
   virtual void chart(const long _chart) override = delete;
};

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   const string name = "ObjFont-";
   
   // calculate center of current chart, both in pixels and time/price
   const int bars = (int)ChartGetInteger(0, CHART_WIDTH_IN_BARS);
   const int first = (int)ChartGetInteger(0, CHART_FIRST_VISIBLE_BAR);
   
   const datetime centerTime = iTime(NULL, 0, first - bars / 2);
   const double centerPrice =
      (ChartGetDouble(0, CHART_PRICE_MIN)
      + ChartGetDouble(0, CHART_PRICE_MAX)) / 2;
   
   const int centerX = (int)ChartGetInteger(0, CHART_WIDTH_IN_PIXELS) / 2;
   const int centerY = (int)ChartGetInteger(0, CHART_HEIGHT_IN_PIXELS) / 2;
   
   // for objects without text content (such as lines), we enable
   // descriptions on chart - please note how they will use default font ever
   ChartSetInteger(0, CHART_SHOW_OBJECT_DESCR, true);

   // array of objects types to test
   ENUM_OBJECT types[] =
   {
      OBJ_HLINE, // NB: font will not have an effect
      OBJ_VLINE, // NB: font will not have an effect
      OBJ_TEXT,
      OBJ_LABEL,
      OBJ_BUTTON,
      OBJ_EDIT,
   };

   int t = 0; // type cursor
   
   // some most popular fonts among standard Windows fonts
   string fonts[] =
   {
      "Comic Sans MS",
      "Consolas",
      "Courier New",
      "Lucida Console",
      "Microsoft Sans Serif",
      "Segoe UI",
      "Tahoma",
      "Times New Roman",
      "Trebuchet MS",
      "Verdana"
   };
   
   int f = 0; // font cursor

   const int key = TerminalInfoInteger(TERMINAL_KEYSTATE_SCRLOCK);
   AutoPtr<ObjectBuilder> guard;
   
   // show objects of different types with different fonts
   while(!IsStopped())
   {
      Sleep(1000);
      if(TerminalInfoInteger(TERMINAL_KEYSTATE_SCRLOCK) != key)
      {
         continue;
      }

      // create and make common setup of new object
      const string str = EnumToString(types[t]);
      ObjectBuilder *object = guard = new ObjectBuilder(name + str, types[t]);
      object.set(OBJPROP_TIME, centerTime);
      object.set(OBJPROP_PRICE, centerPrice);
      object.set(OBJPROP_XDISTANCE, centerX);
      object.set(OBJPROP_YDISTANCE, centerY);
      object.set(OBJPROP_XSIZE, centerX / 3 * 2);
      object.set(OBJPROP_YSIZE, centerY / 3 * 2);

      // adjust font name and size (random choice)
      const int size = rand() * 15 / 32767 + 8;
      Comment(str + " " + fonts[f] + " " + (string)size);
      object.set(OBJPROP_TEXT, fonts[f] + " " + (string)size);
      object.set(OBJPROP_FONT, fonts[f]);
      object.set(OBJPROP_FONTSIZE, size);
      
      // switch to next object type and font name
      t = ++t % ArraySize(types);
      f = ++f % ArraySize(fonts);
      
      ChartRedraw();
   }
   Comment("");
}
//+------------------------------------------------------------------+
