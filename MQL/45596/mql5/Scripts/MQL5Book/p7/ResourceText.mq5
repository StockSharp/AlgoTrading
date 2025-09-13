//+------------------------------------------------------------------+
//|                                                 ResourceText.mq5 |
//|                                  Copyright 2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright   "Copyright 2022, MetaQuotes Ltd."
#property link        "https://www.mql5.com"
#property description "Display text in different styles."
#property script_show_inputs

//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
input string Font = "Arial";             // Font Name
input int    Size = -240;                // Size
input color  Color = clrBlue;            // Font Color
input color  Background = clrNONE;       // Background Color
input uint   Seconds = 10;               // Demo Time (seconds)

enum ENUM_FONT_WEIGHTS
{
   _DONTCARE = FW_DONTCARE,
   _THIN = FW_THIN,
   _EXTRALIGHT = FW_EXTRALIGHT,
   _LIGHT = FW_LIGHT,
   _NORMAL = FW_NORMAL,
   _MEDIUM = FW_MEDIUM,
   _SEMIBOLD = FW_SEMIBOLD,
   _BOLD = FW_BOLD,
   _EXTRABOLD = FW_EXTRABOLD,
   _HEAVY = FW_HEAVY,
};

int Random(const int limit)
{
   return rand() % limit;
}

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   const uint weights[] =
   {
      FW_DONTCARE,
      FW_THIN,
      FW_EXTRALIGHT, // FW_ULTRALIGHT,
      FW_LIGHT,
      FW_NORMAL,     // FW_REGULAR,
      FW_MEDIUM,
      FW_SEMIBOLD,   // FW_DEMIBOLD,
      FW_BOLD,
      FW_EXTRABOLD,  // FW_ULTRABOLD,
      FW_HEAVY,      // FW_BLACK
   };
   
   const int nw = sizeof(weights) / sizeof(uint);
   
   const uint rendering[] =
   {
      FONT_ITALIC,
      FONT_UNDERLINE,
      FONT_STRIKEOUT
   };
   
   const int nr = sizeof(rendering) / sizeof(uint);

   const string name = "FONT";
   const int w = (int)ChartGetInteger(0, CHART_WIDTH_IN_PIXELS);
   const int h = (int)ChartGetInteger(0, CHART_HEIGHT_IN_PIXELS);

   // on-chart object to hold bitmap resource
   ObjectCreate(0, name, OBJ_BITMAP_LABEL, 0, 0, 0);
   // adjust the object dimensions
   ObjectSetInteger(0, name, OBJPROP_XSIZE, w);
   ObjectSetInteger(0, name, OBJPROP_YSIZE, h);
   
   uint data[];
   
   // empty resource for binding with the object
   ArrayResize(data, w * h);
   // transparent background by default
   ArrayInitialize(data, Background == clrNONE ? 0 : ColorToARGB(Background));
   ResourceCreate(name, data, w, h, 0, 0, w, COLOR_FORMAT_ARGB_RAW);
   ObjectSetString(0, name, OBJPROP_BMPFILE, "::" + name);
   
   const int step = h / (ArraySize(weights) + 2);
   int cursor = 0;
   
   for(int weight = 0; weight < ArraySize(weights); ++weight)
   {
      // apply new style
      const int r = Random(8);
      uint render = 0;
      for(int j = 0; j < 3; ++j)
      {
         if((bool)(r & (1 << j))) render |= rendering[j];
      }
      TextSetFont(Font, Size, weights[weight] | render);

      // generate font description
      const string text = Font + EnumToString((ENUM_FONT_WEIGHTS)weights[weight]);

      // draw the text on a separate line
      cursor += step;
      TextOut(text, w / 2, cursor, TA_CENTER | TA_TOP, data, w, h,
         ColorToARGB(Color), COLOR_FORMAT_ARGB_RAW);
   }

   // update the resource and the chart
   ResourceCreate(name, data, w, h, 0, 0, w, COLOR_FORMAT_ARGB_RAW);
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
