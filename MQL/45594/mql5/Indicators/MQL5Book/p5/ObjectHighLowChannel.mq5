//+------------------------------------------------------------------+
//|                                         ObjectHighLowChannel.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright "Copyright 2021, MetaQuotes Ltd."
#property link      "https://www.mql5.com"
#property version   "1.0"
#property description "Create 2 trend lines on highs and lows of specified bar range."

#property indicator_chart_window
#property indicator_buffers 0
#property indicator_plots   0

input int BarOffset = 0;
input int BarCount = 10;

const string Prefix = "HighLowChannel-";

//+------------------------------------------------------------------+
//| Custom indicator iteration function                              |
//+------------------------------------------------------------------+
int OnCalculate(const int rates_total,
                const int prev_calculated,
                const int begin,
                const double &price[])
{
   static datetime now = 0;
   if(now != iTime(NULL, 0, 0))
   {
      const int hh = iHighest(NULL, 0, MODE_HIGH, BarCount, BarOffset);
      const int lh = iLowest(NULL, 0, MODE_HIGH, BarCount, BarOffset);
      const int ll = iLowest(NULL, 0, MODE_LOW, BarCount, BarOffset);
      const int hl = iHighest(NULL, 0, MODE_LOW, BarCount, BarOffset);

      datetime t[2] = {iTime(NULL, 0, BarOffset + BarCount), iTime(NULL, 0, BarOffset)};
      double ph[2] = {iHigh(NULL, 0, fmax(hh, lh)), iHigh(NULL, 0, fmin(hh, lh))};
      double pl[2] = {iLow(NULL, 0, fmax(ll, hl)), iLow(NULL, 0, fmin(ll, hl))};
    
      DrawFigure(Prefix + "Highs", t, ph, clrBlue);
      DrawFigure(Prefix + "Lows", t, pl, clrRed);

      now = iTime(NULL, 0, 0);
   }
   return rates_total;
}

//+------------------------------------------------------------------+
//| Helper function for object creation and setup                    |
//+------------------------------------------------------------------+
bool DrawFigure(const string name, const datetime &t[], const double &p[], const color clr)
{
   if(ArraySize(t) != ArraySize(p)) return false;
   
   ObjectCreate(0, name, OBJ_TREND, 0, 0, 0);
   
   for(int i = 0; i < ArraySize(t); ++i)
   {
      ObjectSetInteger(0, name, OBJPROP_TIME, i, t[i]);
      ObjectSetDouble(0, name, OBJPROP_PRICE, i, p[i]);
   }

   ObjectSetInteger(0, name, OBJPROP_COLOR, clr);
   return true;
}

//+------------------------------------------------------------------+
//| Finalization handler                                             |
//+------------------------------------------------------------------+
void OnDeinit(const int)
{
   ObjectsDeleteAll(0, Prefix);
}
//+------------------------------------------------------------------+
