//+------------------------------------------------------------------+
//|                                           DeltaVolumeProfile.mq5 |
//|                                  Copyright 2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
// indicator settings
#property indicator_chart_window
#property indicator_buffers 0
#property indicator_plots   0

// includes
#include <MQL5Book/IndCommon.mqh>
#include <MQL5Book/TickEnum.mqh>

// inputs
input bool ShowSplittedDelta = true;
input COPY_TICKS TickType = INFO_TICKS; // TickType (use TRADE_TICKS if real volumes available)

//+------------------------------------------------------------------+
//| Class for volume delta calculation                               |
//+------------------------------------------------------------------+
class DeltaVolumeProfile
{
   const COPY_TICKS tickType;
   const ENUM_SYMBOL_CHART_MODE barType;
   const bool delta;
   
   static const string prefix;
   
protected:
   double price(const MqlTick &tick)
   {
      return barType == SYMBOL_CHART_MODE_LAST ? tick.last : tick.bid;
   }

   // main tick processing (on history only)
   void calcProfile(const int b, const datetime time, const MqlTick &ticks[])
   {
      const string name = prefix + (string)(ulong)time;
      const double high = iHigh(_Symbol, _Period, b);
      const double low = iLow(_Symbol, _Period, b);
      const double range = high - low;
      
      if(fabs(range) < DBL_EPSILON) return;
      if(ArraySize(ticks) == 0) return;
      
      ObjectCreate(0, name, OBJ_BITMAP, 0, time, high);

      int x1, y1, x2, y2;
      ChartTimePriceToXY(0, 0, time, high, x1, y1);
      ChartTimePriceToXY(0, 0, time, low, x2, y2);
      
      const int h = y2 - y1 + 1;
      const int w = (int)(ChartGetInteger(0, CHART_WIDTH_IN_PIXELS)
         / ChartGetInteger(0, CHART_WIDTH_IN_BARS));

      if(h <= 0)
      {
         Print("Bad data: ", high, " ", low, " ", y1, " ", y2);
         DebugBreak();
         return;
      }

      uint data[];
      ArrayResize(data, w * h);
      ArrayInitialize(data, 0);
      ResourceCreate(name + (string)ChartID(), data, w, h, 0, 0, w, COLOR_FORMAT_ARGB_NORMALIZE);
         
      ObjectSetString(0, name, OBJPROP_BMPFILE, "::" + name + (string)ChartID());
      ObjectSetInteger(0, name, OBJPROP_XSIZE, w);
      ObjectSetInteger(0, name, OBJPROP_YSIZE, h);
      ObjectSetInteger(0, name, OBJPROP_ANCHOR, ANCHOR_UPPER);
      
      long plus[], minus[], max = 0;
      ArrayResize(plus, h);
      ArrayResize(minus, h);
      ArrayInitialize(plus, 0);
      ArrayInitialize(minus, 0);
      
      const int n = ArraySize(ticks);
      for(int j = 0; j < n; ++j)
      {
         const double p1 = price(ticks[j]);
         /*const*/ int index = (int)((high - p1) / range * (h - 1));
         if(index >= h)
         {
            Print("Correction: index=", index, " ", high, " ", p1, " ", h);
            index = h - 1;
            DebugBreak();
         }
         if(index < 0)
         {
            Print("Correction: index=", index, " ", high, " ", p1, " ", h);
            index = 0;
            DebugBreak();
         }
         // when real volumes are expected to be available, check them in the ticks
         if(tickType == TRADE_TICKS)
         {
            // accumulate volumes for buy and sell deals separately
            if((ticks[j].flags & TICK_FLAG_BUY) != 0)
            {
               plus[index] += (long)ticks[j].volume;
            }
            if((ticks[j].flags & TICK_FLAG_SELL) != 0)
            {
               minus[index] += (long)ticks[j].volume;
            }
         }
         else // tickType == INFO_TICKS or tickType == ALL_TICKS
         if(j > 0)
         {
            // when real volumes are unavailable, use price moves up/down to estimate volume change
            if((ticks[j].flags & (TICK_FLAG_ASK | TICK_FLAG_BID)) != 0)
            {
               const double d = (((ticks[j].ask + ticks[j].bid)
                              - (ticks[j - 1].ask + ticks[j - 1].bid)) / _Point);
               if(d > 0)
                  plus[index] += (long)d;
               else
                  minus[index] -= (long)d;
            }
         }
         
         if(delta)
         {
            if(plus[index] > max) max = plus[index];
            if(minus[index] > max) max = minus[index];
         }
         else
         {
            if(fabs(plus[index] - minus[index]) > max) max = fabs(plus[index] - minus[index]);
         }
      }
      
      if(max == 0)
      {
         Print("No tick volumes for ", (string)time);
         return;
      }
      
      for(int i = 0; i < h; i++)
      {
         if(delta)
         {
            const int dp = (int)(plus[i] * w / 2 / max);
            const int dm = (int)(minus[i] * w / 2 / max);
            for(int j = 0; j < dp; j++)
            {
               data[i * w + w / 2 + j] = ColorToARGB(clrBlue);
            }
            for(int j = 0; j < dm; j++)
            {
               data[i * w + w / 2 - j] = ColorToARGB(clrRed);
            }
         }
         else
         {
            const int d = (int)((plus[i] - minus[i]) * w / 2 / max);
            const int sign = d > 0 ? +1 : -1;
            for(int j = 0; j < fabs(d); j++)
            {
               data[i * w + w / 2 + j * sign] = ColorToARGB(clrGreen);
            }
         }
      }
      ResourceCreate(name + (string)ChartID(), data, w, h, 0, 0, w, COLOR_FORMAT_ARGB_NORMALIZE);
   }

public:
   DeltaVolumeProfile(const COPY_TICKS type, const bool d) :
      tickType(type), delta(d),
      barType((ENUM_SYMBOL_CHART_MODE)SymbolInfoInteger(_Symbol, SYMBOL_CHART_MODE))
   {
   }
   
   ~DeltaVolumeProfile()
   {
      // resources are not deleted with objects,
      // so calling ObjectsDeleteAll(0, prefix, 0); is not enough
      const int n = ObjectsTotal(0, 0);
      for(int i = n - 1; i >= 0; --i)
      {
         const string name = ObjectName(0, i, 0);
         if(StringFind(name, prefix) == 0)
         {
            ObjectDelete(0, name);
            ResourceFree("::" + name + (string)ChartID());
         }
      }
   }
   
   // get ticks for specific bar on the history
   int createProfileBar(const int i)
   {
      MqlTick ticks[];
      const datetime time = iTime(_Symbol, _Period, i);
      // prev and next are timestamps of the bar boundaries
      const datetime prev = time;
      const datetime next = prev + PeriodSeconds();
      ResetLastError();
      const int n = CopyTicksRange(_Symbol, ticks, COPY_TICKS_ALL, prev * 1000, next * 1000 - 1);
      if(n > -1 && _LastError == 0)
      {
         calcProfile(i, time, ticks);
      }
      else
      {
         return -_LastError;
      }
      
      return n;
   }
   
   // update bars where objects with histograms located
   void updateProfileBars()
   {
      const int n = ObjectsTotal(0, 0);
      for(int i = n - 1; i >= 0; --i)
      {
         const string name = ObjectName(0, i, 0);
         if(StringFind(name, prefix) == 0)
         {
            const datetime dt = (datetime)ObjectGetInteger(0, name, OBJPROP_TIME, 0);
            const int bar = iBarShift(_Symbol, _Period, dt, true);
            if(createProfileBar(bar) < 0)
            {
               ObjectDelete(0, name);
               ResourceFree("::" + name + (string)ChartID());
            }
         }
      }
   }
};

static const string DeltaVolumeProfile::prefix = "DVP";

//+------------------------------------------------------------------+
//| Global variables                                                 |
//+------------------------------------------------------------------+
DeltaVolumeProfile deltas(TickType, ShowSplittedDelta);

//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
int OnInit()
{
   if(_Period >= PERIOD_D1)
   {
      Alert("Use intraday timeframe smaller than D1, please");
      return INIT_FAILED;
   }
   
   return INIT_SUCCEEDED;
}

//+------------------------------------------------------------------+
//| Custom indicator iteration function                              |
//+------------------------------------------------------------------+
int OnCalculate(ON_CALCULATE_STD_FULL_PARAM_LIST)
{
   return rates_total;
}

//+------------------------------------------------------------------+
//| Helper data function                                             |
//+------------------------------------------------------------------+

#define TRY_AGAIN 0xAAA

void RequestData(const int b, const datetime time, const int count = 0)
{
   Comment("Requesting ticks for ", time);
   if(deltas.createProfileBar(b) <= 0)
   {
      Print("No data on bar ", b, ", at ", TimeToString(time),
         ". Sending event for refresh...");
      ChartSetSymbolPeriod(0, _Symbol, _Period); // refresh myself
      EventChartCustom(0, TRY_AGAIN, b, count + 1, NULL);
   }
   Comment("");
}

//+------------------------------------------------------------------+
//| Chart event handler                                              |
//+------------------------------------------------------------------+
void OnChartEvent(const int id, const long &lparam, const double &dparam, const string &sparam)
{
   if(id == CHARTEVENT_CLICK)
   {
      datetime time;
      double price;
      int window;
      ChartXYToTimePrice(0, (int)lparam, (int)dparam, window, time, price);
      time += PeriodSeconds() / 2;
      const int b = iBarShift(_Symbol, _Period, time, true);
      if(b != -1 && window == 0)
      {
         RequestData(b, iTime(_Symbol, _Period, b));
      }
   }
   else if(id == CHARTEVENT_CHART_CHANGE)
   {
      deltas.updateProfileBars();
   }
   else if(id == CHARTEVENT_CUSTOM + TRY_AGAIN)
   {
      Print("Refreshing... ", (int)dparam);
      const int b = (int)lparam;
      if((int)dparam < 5)
      {
         RequestData(b, iTime(_Symbol, _Period, b), (int)dparam);
      }
      else
      {
         Print("Give up. Check tick history manually, please, then click the bar again");
      }
   }
}
//+------------------------------------------------------------------+
