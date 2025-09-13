//+------------------------------------------------------------------+
//|                                  ResourceTextAnchOrientation.mq5 |
//|                                  Copyright 2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright   "Copyright 2022, MetaQuotes Ltd."
#property link        "https://www.mql5.com"
#property description "Draw many text examples with different anchor point and orientation."
#property script_show_inputs

#include <MQL5Book/ColorMix.mqh>

//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
input string Font = "Arial";             // Font Name
input int    Size = -150;                // Size
input int    ExampleCount = 11;          // Number of examples
input color  Background = clrNONE;       // Background Color
input uint   Seconds = 10;               // Demo Time (seconds)

int Random(const int limit)
{
   return rand() % limit;
}

enum ENUM_TEXT_ANCHOR
{
   LEFT_TOP = TA_LEFT | TA_TOP,
   LEFT_VCENTER = TA_LEFT | TA_VCENTER,
   LEFT_BOTTOM = TA_LEFT | TA_BOTTOM,
   CENTER_TOP = TA_CENTER | TA_TOP,
   CENTER_VCENTER = TA_CENTER | TA_VCENTER,
   CENTER_BOTTOM = TA_CENTER | TA_BOTTOM,
   RIGHT_TOP = TA_RIGHT | TA_TOP,
   RIGHT_VCENTER = TA_RIGHT | TA_VCENTER,
   RIGHT_BOTTOM = TA_RIGHT | TA_BOTTOM,
};

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   const ENUM_TEXT_ANCHOR anchors[] =
   {
      LEFT_TOP,
      LEFT_VCENTER,
      LEFT_BOTTOM,
      CENTER_TOP,
      CENTER_VCENTER,
      CENTER_BOTTOM,
      RIGHT_TOP,
      RIGHT_VCENTER,
      RIGHT_BOTTOM,
   };
   
   const int na = sizeof(anchors) / sizeof(uint);
   
   const string name = "FONT";
   const int w = (int)ChartGetInteger(0, CHART_WIDTH_IN_PIXELS);
   const int h = (int)ChartGetInteger(0, CHART_HEIGHT_IN_PIXELS);

   // on-chart object to hold bitmap resource
   ObjectCreate(0, name, OBJ_BITMAP_LABEL, 0, 0, 0);
   // adjust the object dimensions
   ObjectSetInteger(0, name, OBJPROP_XSIZE, w);
   ObjectSetInteger(0, name, OBJPROP_YSIZE, h);
   
   // prepare empty buffer for resource for setting to the object
   uint data[];
   ArrayResize(data, w * h);
   // transparent background by default
   ArrayInitialize(data, Background == clrNONE ? 0 : ColorToARGB(Background));
   ResourceCreate(name, data, w, h, 0, 0, w, COLOR_FORMAT_ARGB_NORMALIZE);
   ObjectSetString(0, name, OBJPROP_BMPFILE, "::" + name);

   for(int i = 0; i < ExampleCount; ++i)
   {
      // apply random angle
      const int angle = Random(360);
      TextSetFont(Font, Size, 0, angle * 10);
      
      // get random coordinates and anchor point
      const ENUM_TEXT_ANCHOR anchor = anchors[Random(na)];
      const int x = Random(w / 2) + w / 4;
      const int y = Random(h / 2) + h / 4;
      const color clr = ColorMix::HSVtoRGB(angle);
      
      // draw the bullet just at the binding point of the text
      TextOut(ShortToString(0x2022), x, y, TA_CENTER | TA_VCENTER, data, w, h,
         ColorToARGB(clr), COLOR_FORMAT_ARGB_NORMALIZE);
      
      // generate altered text
      const string text =  EnumToString(anchor) + "(" + (string)angle + CharToString(0xB0) + ")";
   
      // draw the text
      TextOut(text, x, y, anchor, data, w, h,
         ColorToARGB(clr), COLOR_FORMAT_ARGB_NORMALIZE);
   }
   // update the resource and the chart
   ResourceCreate(name, data, w, h, 0, 0, w, COLOR_FORMAT_ARGB_NORMALIZE);
   ChartRedraw();
   
   const uint timeout = GetTickCount() + Seconds * 1000;
   while(!IsStopped() && GetTickCount() < timeout)
   {
      Sleep(1000);
   }
   
   ObjectDelete(0, name);
   ResourceFree("::" + name);
}
//+------------------------------------------------------------------+
