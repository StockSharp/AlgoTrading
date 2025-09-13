//+------------------------------------------------------------------+
//|                                            ObjectCornerLabel.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//|                                                                  |
//| The script slowly moves a label around the chart,                |
//| changing direction but preserving binding corner.                |
//+------------------------------------------------------------------+
#property script_show_inputs

input ENUM_BASE_CORNER Corner = CORNER_LEFT_UPPER;

//+------------------------------------------------------------------+
//| Helper function to concatenate strings from given array          |
//+------------------------------------------------------------------+
string StringImplode(const string &lines[], const string glue,
   const int start = 0, const int stop = -1)
{
   const int n = stop == -1 ? ArraySize(lines) : fmin(stop, ArraySize(lines));
   string result = "";
   for(int i = start; i < n; i++)
   {
      result += (i > start ? glue : "") + lines[i];
   }
   return result;
}

//+------------------------------------------------------------------+
//| Neat stringifier function for enumeration element                |
//+------------------------------------------------------------------+
template<typename E>
string GetEnumString(const E e)
{
   string words[];
   StringSplit(EnumToString(e), '_', words);
   for(int i = 0; i < ArraySize(words); ++i) StringToLower(words[i]);
   return StringImplode(words, " ", 1);
}

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   const int t = ChartWindowOnDropped();
   const string legend = GetEnumString(Corner);

   const string name = "ObjCornerLabel-" + legend;
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

   for( ;!IsStopped(); ++pass)
   {
      // once in a while change movement direction
      if(pass % 50 == 0)
      {
         h = (int)ChartGetInteger(0, CHART_HEIGHT_IN_PIXELS, t);
         w = (int)ChartGetInteger(0, CHART_WIDTH_IN_PIXELS);
         px = rand() * (w / 20) / 32768 - (w / 40);
         py = rand() * (h / 20) / 32768 - (h / 40);
      }

      // bouncing from window endges, prevent overflow
      if(x + px > w || x + px < 0) px = -px;
      if(y + py > h || y + py < 0) py = -py;
      // update label position
      x += px;
      y += py;
      
      // update the label
      ObjectSetString(0, name, OBJPROP_TEXT, legend
         + "[" + (string)x + "," + (string)y + "]");
      ObjectSetInteger(0, name, OBJPROP_XDISTANCE, x);
      ObjectSetInteger(0, name, OBJPROP_YDISTANCE, y);

      ChartRedraw();
      Sleep(100);
   }
   
   ObjectDelete(0, name);
}
//+------------------------------------------------------------------+
