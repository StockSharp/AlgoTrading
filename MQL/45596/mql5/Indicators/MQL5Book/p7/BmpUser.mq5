//+------------------------------------------------------------------+
//|                                                      BmpUser.mq5 |
//|                                  Copyright 2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property indicator_chart_window
#property indicator_buffers 0
#property indicator_plots   0

// The default value in 'ResourceOff' input below is equivalent to
// the path and name "\\Indicators\\MQL5Book\\p7\\BmpOwner.ex5::search1.bmp"
input string ResourceOff = "BmpOwner.ex5::search1.bmp";
input string ResourceOn = "BmpOwner.ex5::search2.bmp";
input int X = 25;
input int Y = 25;
input ENUM_BASE_CORNER Corner = CORNER_RIGHT_LOWER;

const string Prefix = "BMP_";
const ENUM_ANCHOR_POINT Anchors[] =
{
   ANCHOR_LEFT_UPPER,
   ANCHOR_LEFT_LOWER,
   ANCHOR_RIGHT_LOWER,
   ANCHOR_RIGHT_UPPER
};

//+------------------------------------------------------------------+
//| Indicator initialization function                                |
//+------------------------------------------------------------------+
void OnInit()
{
   const string name = Prefix + "search";
   ObjectCreate(0, name, OBJ_BITMAP_LABEL, 0, 0, 0);
   
   ObjectSetString(0, name, OBJPROP_BMPFILE, 0, ResourceOn);
   ObjectSetString(0, name, OBJPROP_BMPFILE, 1, ResourceOff);
   ObjectSetInteger(0, name, OBJPROP_XDISTANCE, X);
   ObjectSetInteger(0, name, OBJPROP_YDISTANCE, Y);
   ObjectSetInteger(0, name, OBJPROP_CORNER, Corner);
   ObjectSetInteger(0, name, OBJPROP_ANCHOR, Anchors[(int)Corner]);
}

//+------------------------------------------------------------------+
//| Indicator calculation function (dummy here)                      |
//+------------------------------------------------------------------+
int OnCalculate(const int rates_total,
                const int prev_calculated,
                const int begin,
                const double &price[])
{
   return rates_total;
}

//+------------------------------------------------------------------+
//| Indicator finalization function                                  |
//+------------------------------------------------------------------+
void OnDeinit(const int)
{
   ObjectsDeleteAll(0, Prefix);
}
//+------------------------------------------------------------------+
