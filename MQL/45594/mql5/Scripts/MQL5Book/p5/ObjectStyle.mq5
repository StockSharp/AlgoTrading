//+------------------------------------------------------------------+
//|                                                  ObjectStyle.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//|                                                                  |
//| The script creates and periodically updates rectangular objects: |
//| modifications affect color, style, width, filling and back props.|
//+------------------------------------------------------------------+
#include <MQL5Book/ObjectMonitor.mqh>
#include <MQL5Book/AutoPtr.mqh>

#define OBJECT_NUMBER 5

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
   const string name = "ObjStyle-";
   const int bars = (int)ChartGetInteger(0, CHART_VISIBLE_BARS);
   const int first = (int)ChartGetInteger(0, CHART_FIRST_VISIBLE_BAR);
   const int rectsize = bars / OBJECT_NUMBER;

   AutoPtr<ObjectBuilder> objects[OBJECT_NUMBER];
   
   color colors[OBJECT_NUMBER] = {clrRed, clrGreen, clrBlue, clrMagenta, clrOrange};
   
   // create and setup rectangle objects
   for(int i = 0; i < OBJECT_NUMBER; ++i)
   {
      const int h = iHighest(NULL, 0, MODE_HIGH, rectsize, i * rectsize);
      const int l = iLowest(NULL, 0, MODE_LOW, rectsize, i * rectsize);

      ObjectBuilder *object = new ObjectBuilder(name + (string)(i + 1), OBJ_RECTANGLE);
      object.set(OBJPROP_TIME, iTime(NULL, 0, i * rectsize), 0);
      object.set(OBJPROP_TIME, iTime(NULL, 0, (i + 1) * rectsize), 1);
      object.set(OBJPROP_PRICE, iHigh(NULL, 0, h), 0);
      object.set(OBJPROP_PRICE, iLow(NULL, 0, l), 1);
      object.set(OBJPROP_COLOR, colors[i]);
      object.set(OBJPROP_WIDTH, i + 1);
      object.set(OBJPROP_STYLE, (ENUM_LINE_STYLE)i);
      objects[i] = object;
   }
   
   const int key = TerminalInfoInteger(TERMINAL_KEYSTATE_SCRLOCK);
   int pass = 0;
   int offset = 0;

   for( ;!IsStopped(); ++pass)
   {
      Sleep(200);
      if(TerminalInfoInteger(TERMINAL_KEYSTATE_SCRLOCK) != key)
      {
         continue;
      }
      
      // once in a while change color/style/width/fill/back props
      if(pass % 5 == 0)
      {
         ++offset;
         for(int i = 0; i < OBJECT_NUMBER; ++i)
         {
            objects[i][].set(OBJPROP_COLOR, colors[(i + offset) % OBJECT_NUMBER]);
            objects[i][].set(OBJPROP_WIDTH, (i + offset) % OBJECT_NUMBER + 1);
            objects[i][].set(OBJPROP_FILL, rand() > 32768 / 2);
            objects[i][].set(OBJPROP_BACK, rand() > 32768 / 2);
         }
      }
      ChartRedraw();
   }
}
//+------------------------------------------------------------------+
