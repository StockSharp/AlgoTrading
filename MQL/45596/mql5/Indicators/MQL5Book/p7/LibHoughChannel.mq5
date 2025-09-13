//+------------------------------------------------------------------+
//|                                              LibHoughChannel.mq5 |
//|                               Copyright (c) 2015-2022, Marketeer |
//|                          https://www.mql5.com/en/users/marketeer |
//+------------------------------------------------------------------+
#property copyright "Copyright (c) 2015-2022, Marketeer"
#property link      "https://www.mql5.com/en/users/marketeer"
#property version   "1.0"
#property description "Create 2+ trend lines on highs and lows using Hough transform."

#property indicator_chart_window
#property indicator_buffers 0
#property indicator_plots   0

//#define LIB_HOUGH_IMPL_DEBUG
#include <MQL5Book/LibHoughTransform.mqh>
#include <MQL5Book/AutoPtr.mqh>

//+------------------------------------------------------------------+
//| I N P U T S                                                      |
//+------------------------------------------------------------------+
input int BarOffset = 0;
input int BarCount = 21;
input int MaxLines = 3;

//+------------------------------------------------------------------+
//| Custom implementation of HoughImage based on quotes              |
//+------------------------------------------------------------------+
class HoughQuotes: public HoughImage<int>
{
public:
   enum PRICE_LINE
   {
      HighLow = 0,   // Bar Range |High..Low|
      OpenClose = 1, // Bar Body |Open..Close|
      LowLow = 2,    // Bar Lows
      HighHigh = 3,  // Bar Highs
   };

protected:
   int size;
   int offset;
   int step;
   double base;
   PRICE_LINE type;

public:
   HoughQuotes(int startbar, int barcount, PRICE_LINE price)
   {
      offset = startbar;
      size = barcount;
      type = price;
      int hh = iHighest(NULL, 0, MODE_HIGH, size, startbar);
      int ll = iLowest(NULL, 0, MODE_LOW, size, startbar);
      int pp = (int)((iHigh(NULL, 0, hh) - iLow(NULL, 0, ll)) / _Point);
      step = pp / size;
      base = iLow(NULL, 0, ll);
   }

   virtual int getWidth() const override
   {
      return size;
   }

   virtual int getHeight() const override
   {
      return size;
   }

   virtual int get(int x, int y) const override
   {
      if(offset + x >= iBars(NULL, 0)) return 0;

      const double price = convert(y);
      if(type == HighLow)
      {
         if(price >= iLow(NULL, 0, offset + x) && price <= iHigh(NULL, 0, offset + x))
         {
            return 1;
         }
      }
      else if(type == OpenClose)
      {
         if(price >= fmin(iOpen(NULL, 0, offset + x), iClose(NULL, 0, offset + x))
         && price <= fmax(iOpen(NULL, 0, offset + x), iClose(NULL, 0, offset + x)))
         {
            return 1;
         }
      }
      else if(type == LowLow)
      {
         if(iLow(NULL, 0, offset + x) >= price - step * _Point / 2
         && iLow(NULL, 0, offset + x) <= price + step * _Point / 2)
         {
            return 1;
         }
      }
      else if(type == HighHigh)
      {
         if(iHigh(NULL, 0, offset + x) >= price - step * _Point / 2
         && iHigh(NULL, 0, offset + x) <= price + step * _Point / 2)
         {
            return 1;
         }
      }
      return 0;
   }

   // index by Y (quantized) -> continuous value (such as price)
   double convert(const double y) const
   {
      return base + y * step * _Point;
   }
};

//+------------------------------------------------------------------+
//| G L O B A L S                                                    |
//+------------------------------------------------------------------+
const string Prefix = "HoughChannel-";
HoughTransform *ht;

//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
int OnInit()
{
   ht = createHoughTransform(BarCount);
   HoughInfo info = getHoughInfo();
   Print(info.dimension, " per ", info.about);
   return CheckPointer(ht) != POINTER_INVALID ? INIT_SUCCEEDED : INIT_FAILED;
}

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
      HoughQuotes highs(BarOffset, BarCount, HoughQuotes::HighHigh);
      HoughQuotes lows(BarOffset, BarCount, HoughQuotes::LowLow);
      static double result[];
      int n;
      n = ht.transform(highs, result, fmin(MaxLines, 5));
      if(n)
      {
         for(int i = 0; i < n; ++i)
         {
            DrawLine(highs, Prefix + "Highs-" + (string)i, result[i * 2 + 0], result[i * 2 + 1], clrBlue, 5 - i);
         }
      }
      else
      {
         Print("No solution for Highs");
      }
      n = ht.transform(lows, result, fmin(MaxLines, 5));
      if(n)
      {
         for(int i = 0; i < n; ++i)
         {
            DrawLine(lows, Prefix + "Lows-" + (string)i, result[i * 2 + 0], result[i * 2 + 1], clrRed, 5 - i);
         }
      }
      else
      {
         Print("No solution for Lows");
      }
      now = iTime(NULL, 0, 0);
   }
   return rates_total;
}

//+------------------------------------------------------------------+
//| Single line visual presenter                                     |
//+------------------------------------------------------------------+
void DrawLine(HoughQuotes &quotes, const string name, const double a, const double b,
   const color clr, const int width)
{
   ObjectCreate(0, name, OBJ_TREND, 0, 0, 0);
   ObjectSetInteger(0, name, OBJPROP_TIME, 0, iTime(NULL, 0, BarOffset + BarCount - 1));
   ObjectSetDouble(0, name, OBJPROP_PRICE, 0, quotes.convert(a * (BarCount - 1) + b));

   ObjectSetInteger(0, name, OBJPROP_TIME, 1, iTime(NULL, 0, BarOffset));
   ObjectSetDouble(0, name, OBJPROP_PRICE, 1, quotes.convert(a * 0 + b));

   ObjectSetInteger(0, name, OBJPROP_COLOR, clr);
   ObjectSetInteger(0, name, OBJPROP_WIDTH, width);
}

//+------------------------------------------------------------------+
//| Finalization handler                                             |
//+------------------------------------------------------------------+
void OnDeinit(const int)
{
   AutoPtr<HoughTransform> destructor(ht);
   ObjectsDeleteAll(0, Prefix);
}
//+------------------------------------------------------------------+
