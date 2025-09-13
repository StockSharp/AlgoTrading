//+------------------------------------------------------------------+
//|                                                 ObjectZorder.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//|                                                                  |
//| The script creates 12 rectangle labels with random colors        |
//| and Z-order. Creation order and visible overlapping occures      |
//| according to clockwise order: 1, 2, ... 12.                      |
//+------------------------------------------------------------------+
#include "ObjectPrefix.mqh"
#include <MQL5Book/ObjectMonitor.mqh>

#define OBJECT_NUMBER 12

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
   const int t = ChartWindowOnDropped();

   int h = (int)ChartGetInteger(0, CHART_HEIGHT_IN_PIXELS, t);
   int w = (int)ChartGetInteger(0, CHART_WIDTH_IN_PIXELS);
   int x = w / 2;
   int y = h / 2;
   const int dx = w / 4;
   const int dy = h / 4;
   
   // create and setup 12 labels in clockwise order
   for(int i = 0; i < OBJECT_NUMBER; ++i)
   {
      const int px = (int)(MathSin((i + 1) * 30 * M_PI / 180) * dx) - dx / 2;
      const int py = -(int)(MathCos((i + 1) * 30 * M_PI / 180) * dy) - dy / 2;
      
      const int z = rand();
      const string text = StringFormat("%02d - %d", i + 1, z);

      ObjectBuilder *builder = new ObjectBuilder(ObjNamePrefix + text, OBJ_RECTANGLE_LABEL);
      builder.set(OBJPROP_XDISTANCE, x + px).set(OBJPROP_YDISTANCE, y + py)
      .set(OBJPROP_XSIZE, dx).set(OBJPROP_YSIZE, dy)
      .set(OBJPROP_TOOLTIP, text)
      .set(OBJPROP_ZORDER, z)
      .set(OBJPROP_BGCOLOR, (rand() << 8) ^ rand());
      delete builder;
   }
   // 12 objects are left on chart after this script execution,
   // you may use ObjectCleanup1.mq5 script to remove them.
}
//+------------------------------------------------------------------+
