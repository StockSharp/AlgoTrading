//+------------------------------------------------------------------+
//|                                                   ARGBbitmap.mq5 |
//|                                  Copyright 2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright   "Copyright 2022, MetaQuotes Ltd."
#property link        "https://www.mql5.com"
#property description "Draw bitmap on a chart using different ARGB modes."
#property script_show_inputs

#resource "argb.bmp" as bitmap Data[][]

input ENUM_COLOR_FORMAT ColorFormat = COLOR_FORMAT_XRGB_NOALPHA;

const string BitmapObject = "BitmapObject";
const string ResName = "::image";

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   ResourceCreate(ResName, Data, ArrayRange(Data, 1), ArrayRange(Data, 0),
      0, 0, 0, ColorFormat);

   ObjectCreate(0, BitmapObject, OBJ_BITMAP_LABEL, 0, 0, 0);
   ObjectSetInteger(0, BitmapObject, OBJPROP_XDISTANCE, 50);
   ObjectSetInteger(0, BitmapObject, OBJPROP_YDISTANCE, 50);
   ObjectSetString(0, BitmapObject, OBJPROP_BMPFILE, ResName);
   
   Comment("Press ESC to stop the demo");
   const ulong start = TerminalInfoInteger(TERMINAL_KEYSTATE_ESCAPE);
   while(!IsStopped()           // wait for user command to stop the demo
   && TerminalInfoInteger(TERMINAL_KEYSTATE_ESCAPE) == start)
   {
      Sleep(1000);
   }
   
   Comment("");
   ObjectDelete(0, BitmapObject);
   ResourceFree(ResName);
}
//+------------------------------------------------------------------+
