//+------------------------------------------------------------------+
//|                                                 ResourceFont.mq5 |
//|                                  Copyright 2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright   "Copyright 2022, MetaQuotes Ltd."
#property link        "https://www.mql5.com"
#property description "Display text with custom font in dynamically changing orientation."
#property script_show_inputs

#resource "a_LCDNova3DCmObl.ttf"

//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
input string Message = "Hello world!";   // Message
input uint   Seconds = 10;               // Demo Time (seconds)

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   const string name = "FONT";
   const int w = (int)ChartGetInteger(0, CHART_WIDTH_IN_PIXELS);
   const int h = (int)ChartGetInteger(0, CHART_HEIGHT_IN_PIXELS);

   // on-chart object to hold bitmap resource
   ObjectCreate(0, name, OBJ_BITMAP_LABEL, 0, 0, 0);
   ObjectSetInteger(0, name, OBJPROP_XDISTANCE, w / 2);
   ObjectSetInteger(0, name, OBJPROP_YDISTANCE, h / 2);
   ObjectSetInteger(0, name, OBJPROP_ANCHOR, ANCHOR_CENTER);
   
   uint data[], width, height;
   
   // empty resource for binding with the object
   ArrayResize(data, 1);
   ResourceCreate(name, data, 1, 1, 0, 0, 1, COLOR_FORMAT_ARGB_RAW);
   ObjectSetString(0, name, OBJPROP_BMPFILE, "::" + name);
   
   const uint timeout = GetTickCount() + Seconds * 1000;
   int angle = 0;
   int remain = 10;
   
   while(!IsStopped() && GetTickCount() < timeout)
   {
      // apply new angle
      TextSetFont("::a_LCDNova3DCmObl.ttf", -240, 0, angle * 10);

      // generate altered text
      const string text = Message + " (" + (string)remain-- + ")";
      
      // obtain dimensions of the text, allocate array
      TextGetSize(text, width, height);
      ArrayResize(data, width * height);
      ArrayInitialize(data, 0);            // transparent background
      
      // on vertical orientation exchange the sizes
      if((bool)(angle / 90 & 1))
      {
         const uint t = width;
         width = height;
         height = t;
      }
      
      // adjust the object dimensions
      ObjectSetInteger(0, name, OBJPROP_XSIZE, width);
      ObjectSetInteger(0, name, OBJPROP_YSIZE, height);

      // draw the text
      TextOut(text, width / 2, height / 2, TA_CENTER | TA_VCENTER, data, width, height,
         ColorToARGB(clrBlue), COLOR_FORMAT_ARGB_RAW);

      // update the resource and the chart
      ResourceCreate(name, data, width, height, 0, 0, width, COLOR_FORMAT_ARGB_RAW);
      ChartRedraw();

      // update the counter
      angle += 90;
      
      Sleep(1000);
   }
   
   ObjectDelete(0, name);
   ResourceFree("::" + name);
}
//+------------------------------------------------------------------+
