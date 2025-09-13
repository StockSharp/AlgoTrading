//+------------------------------------------------------------------+
//|                                            ObjectAnchorLabel.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//|                                                                  |
//| The script slowly moves a label around the chart and             |
//| changes anchor point on the label.                               |
//+------------------------------------------------------------------+
#property script_show_inputs

input ENUM_BASE_CORNER Corner = CORNER_LEFT_UPPER;

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   const int t = ChartWindowOnDropped();
   Comment(EnumToString(Corner));

   const string name = "ObjAnchorLabel";
   int h = (int)ChartGetInteger(0, CHART_HEIGHT_IN_PIXELS, t);
   int w = (int)ChartGetInteger(0, CHART_WIDTH_IN_PIXELS);
   int x = w / 2;
   int y = h / 2;
   
   // create and setup the label
   ObjectCreate(0, name, OBJ_LABEL, t, 0, 0);
   ObjectSetInteger(0, name, OBJPROP_SELECTABLE, true);
   ObjectSetInteger(0, name, OBJPROP_SELECTED, true);
   ObjectSetInteger(0, name, OBJPROP_CORNER, Corner);
      
   int px = 0, py = 0;
   int pass = 0;
   ENUM_ANCHOR_POINT anchor = 0;

   for( ;!IsStopped(); ++pass)
   {
      // once in a while change movement direction and anchor
      if(pass % 50 == 0)
      {
         h = (int)ChartGetInteger(0, CHART_HEIGHT_IN_PIXELS, t);
         w = (int)ChartGetInteger(0, CHART_WIDTH_IN_PIXELS);
         px = rand() * (w / 40) / 32768 - (w / 80);
         py = rand() * (h / 40) / 32768 - (h / 80);
         // ENUM_ANCHOR_POINT consists of 9 elements: get a random one
         anchor = (ENUM_ANCHOR_POINT)(rand() * 9 / 32768);
         ObjectSetInteger(0, name, OBJPROP_ANCHOR, anchor);
      }

      // bouncing from window endges, prevent overflow
      if(x + px > w || x + px < 0) px = -px;
      if(y + py > h || y + py < 0) py = -py;
      // update label position
      x += px;
      y += py;
      
      // update the label
      ObjectSetString(0, name, OBJPROP_TEXT, EnumToString(anchor)
         + "[" + (string)x + "," + (string)y + "]");
      ObjectSetInteger(0, name, OBJPROP_XDISTANCE, x);
      ObjectSetInteger(0, name, OBJPROP_YDISTANCE, y);

      ChartRedraw();
      Sleep(100);
   }
   
   ObjectDelete(0, name);
   Comment("");
}
//+------------------------------------------------------------------+
