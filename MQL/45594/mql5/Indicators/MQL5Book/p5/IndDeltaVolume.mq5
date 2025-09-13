//+------------------------------------------------------------------+
//|                                               IndDeltaVolume.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
// indicator settings
#property indicator_separate_window
#property indicator_buffers 3
#property indicator_plots   3

// plot settings
#property indicator_type1   DRAW_HISTOGRAM
#property indicator_color1  clrBlue
#property indicator_width1  1
#property indicator_label1  "Buy"

#property indicator_type2   DRAW_HISTOGRAM
#property indicator_color2  clrRed
#property indicator_width2  1
#property indicator_label2  "Sell"

#property indicator_type3   DRAW_HISTOGRAM
#property indicator_color3  clrMagenta
#property indicator_width3  3
#property indicator_label3  "Delta"

// includes
#include <MQL5Book/IndCommon.mqh>
#include <MQL5Book/TickEnum.mqh>

// inputs
input int BarCount = 100;
input COPY_TICKS TickType = INFO_TICKS;
input bool ShowBuySell = true;

//+------------------------------------------------------------------+
//| Class for volume delta calculation                               |
//+------------------------------------------------------------------+
class CalcDeltaVolume
{
   const int limit;
   const COPY_TICKS tickType;
   
   ulong lasttime; // millisecond mark of the last online tick processed
   int lastcount;  // number of online ticks with the same mark
   
   // NB: indicator buffers must be of type double,
   // but volumes are ulong - this may produce discrepancines on very large values
   double buy[];
   double sell[];
   double delta[];
   
protected:
   // main tick processing on history and online (common part)
   void calc(const int i, const MqlTick &ticks[], const int skip = 0)
   {
      const int n = ArraySize(ticks);
      for(int j = skip; j < n; ++j)
      {
         // when real volumes are expected to be available, check them in the ticks
         if(tickType == TRADE_TICKS)
         {
            // accumulate volumes for buy and sell deals separately
            if((ticks[j].flags & TICK_FLAG_BUY) != 0)
            {
               buy[i] += (double)ticks[j].volume;
            }
            if((ticks[j].flags & TICK_FLAG_SELL) != 0)
            {
               sell[i] += (double)ticks[j].volume;
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
               if(d > 0) buy[i] += d;
               else sell[i] += d;
               delta[i] += d;
            }
         }
      }
   }

   // remember moment of recent online tick processing
   void updateLastTime(const int n, const MqlTick &ticks[])
   {
      lasttime = ticks[n - 1].time_msc;
      lastcount = 0;
      for(int k = n - 1; k >= 0; --k)
      {
         if(ticks[k].time_msc == ticks[n - 1].time_msc) ++lastcount;
      }
   }

public:
   CalcDeltaVolume(
      const int bars,
      const COPY_TICKS type)
      : limit(bars), tickType(type), lasttime(0), lastcount(0)
   {
      // register the internal arrays as indicator buffers
      SetIndexBuffer(0, buy);
      SetIndexBuffer(1, sell);
      SetIndexBuffer(2, delta);
   }
   
   // complete initialization
   void reset()
   {
      // most of array is feeded with empty value
      // except for given number of recent bars for calculations
      // where elements will accumulate volumes counted up from 0
      ArrayInitialize(buy, EMPTY_VALUE);
      ArrayFill(buy, ArraySize(buy) - limit, limit, 0);
      
      // replicate this initial state to other buffers
      ArrayCopy(sell, buy);
      ArrayCopy(delta, buy);
      
      // prepare variables for online tick monitoring
      lasttime = 0;
      lastcount = 0;
   }
   
   
   // get ticks for specific bar on the history
   int createDeltaBar(const int i, const datetime &time[])
   {
      const int size = ArraySize(time);
      if(i < 0 || i >= size)
      {
         return -1; // do nothing: self-protection from out of bound requests
      }
      
      delta[i] = buy[i] = sell[i] = 0;
      
      MqlTick ticks[];
      // prev and next are timestamps of the bar boundaries,
      // new function PeriodSeconds() will be covered in the chapter about charts
      const datetime prev = time[i];
      const datetime next = prev + PeriodSeconds();
      ResetLastError();
      const int n = CopyTicksRange(_Symbol, ticks, COPY_TICKS_ALL, prev * 1000, next * 1000 - 1);
      if(n > -1 && _LastError == 0)
      {
         if(i == size - 1) // last bar
         {
            updateLastTime(n, ticks);
         }
         calc(i, ticks);
      }
      else
      {
         return -_LastError;
      }
      
      return n;
   }

   // get online ticks on the latest bar, which are not yet processed
   int updateLastDelta(const int total)
   {
      MqlTick ticks[];
      ResetLastError();
      const int n = CopyTicksRange(_Symbol, ticks, COPY_TICKS_ALL, lasttime);
      if(n > -1 && _LastError == 0)
      {
         const int skip = lastcount;
         updateLastTime(n, ticks);
         calc(total - 1, ticks, skip);
         return n - skip;
      }
      return -_LastError;
   }
};

//+------------------------------------------------------------------+
//| Global variables                                                 |
//+------------------------------------------------------------------+
CalcDeltaVolume deltas(BarCount, TickType);
bool calcDone = false;
bool selfRefresh = false;

//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
int OnInit()
{
   if(_Period >= PERIOD_D1
   || _Period == PERIOD_M1)
   {
      Alert("Use intraday timeframe larger than M1 and smaller than D1, please");
      return INIT_FAILED;
   }
   
   // adjust splitted volumes visibility upon user request
   PlotIndexSetInteger(0, PLOT_DRAW_TYPE, ShowBuySell ? DRAW_HISTOGRAM : DRAW_NONE);
   PlotIndexSetInteger(1, PLOT_DRAW_TYPE, ShowBuySell ? DRAW_HISTOGRAM : DRAW_NONE);
   
   return INIT_SUCCEEDED;
}

//+------------------------------------------------------------------+
//| Custom indicator iteration function                              |
//+------------------------------------------------------------------+
int OnCalculate(ON_CALCULATE_STD_FULL_PARAM_LIST)
{
   if(prev_calculated == 0)
   {
      deltas.reset();
   }
   
   calcDone = false;
   
   // on every new bar or many new bars (including first event)
   if(prev_calculated != rates_total)
   {
      // process all or new bars
      for(int i = fmax(prev_calculated, fmax(1, rates_total - BarCount));
         i < rates_total && !IsStopped(); ++i)
      {
         if((deltas.createDeltaBar(i, time)) <= 0)
         {
            Print("No data on bar ", i, ", at ", TimeToString(time[i]),
               ". Setting up timer for refresh...");
            EventSetTimer(1); // ask terminal to call us in 1 second
            return 0; // no ticks, can't show
         }
      }
   }
   else // ticks on current bar
   {
      // update the latest bar
      if((deltas.updateLastDelta(rates_total)) <= 0)
      {
         return 0; // error/warning
      }
   }
   
   calcDone = true;
   if(selfRefresh)
   {
      Print("Refresh done");
      selfRefresh = false;
   }

   return rates_total; // report number of processed bars for next call
}

//+------------------------------------------------------------------+
//| Timer callback function                                          |
//+------------------------------------------------------------------+
void OnTimer()
{
   EventKillTimer();
   if(!calcDone)
   {
      Print("Refreshing...");
      selfRefresh = true;
      ChartSetSymbolPeriod(0, _Symbol, _Period); // refresh myself
   }
   else
   {
      Print("Ready before timer");
   }
}
//+------------------------------------------------------------------+
