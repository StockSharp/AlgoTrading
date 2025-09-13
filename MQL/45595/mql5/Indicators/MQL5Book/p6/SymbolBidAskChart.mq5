//+------------------------------------------------------------------+
//|                                            SymbolBidAskChart.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
// indicator settings
#property indicator_chart_window
#property indicator_buffers 4
#property indicator_plots   1

// plot settings
#property indicator_type1   DRAW_BARS
#property indicator_color1  clrDodgerBlue
#property indicator_width1  2
#property indicator_label1  "Open;High;Low;Close;"

// includes
#include <MQL5Book/IndCommon.mqh>
#include <MQL5Book/TickEnum.mqh>
#include <MQL5Book/MqlError.mqh>

enum ENUM_SYMBOL_CHART_MODE_EXTENDED
{
   _SYMBOL_CHART_MODE_BID,  // SYMBOL_CHART_MODE_BID
   _SYMBOL_CHART_MODE_LAST, // SYMBOL_CHART_MODE_LAST
   _SYMBOL_CHART_MODE_ASK,  // SYMBOL_CHART_MODE_ASK*
};

// inputs
input int BarCount = 100;
input COPY_TICKS TickType = INFO_TICKS;
input ENUM_SYMBOL_CHART_MODE_EXTENDED ChartMode = _SYMBOL_CHART_MODE_BID;

//+------------------------------------------------------------------+
//| Class for calculation of bars based on specific price type       |
//+------------------------------------------------------------------+
class CalcCustomBars
{
   const int limit;
   const COPY_TICKS tickType;
   const ENUM_SYMBOL_CHART_MODE_EXTENDED chartMode;
   
   ulong lasttime; // millisecond mark of the last online tick processed
   int lastcount;  // number of online ticks with the same mark
   
   // indicator buffers
   double open[];
   double high[];
   double low[];
   double close[];
   
protected:

   double price(const MqlTick &t) const
   {
      switch(chartMode)
      {
      case _SYMBOL_CHART_MODE_BID:
         return t.bid;
      case _SYMBOL_CHART_MODE_LAST:
         return t.last;
      case _SYMBOL_CHART_MODE_ASK:
         return t.ask;
      }
      return 0; // error
   }

   // main tick processing on history and online (common part)
   void calc(const int i, const MqlTick &ticks[], const int skip = 0)
   {
      const int n = ArraySize(ticks);
      for(int j = skip; j < n; ++j)
      {
         const double p = price(ticks[j]);
         if(open[i] == EMPTY_VALUE)
         {
            open[i] = p;
         }
         
         if(p > high[i] || high[i] == EMPTY_VALUE)
         {
            high[i] = p;
         }
         
         if(p < low[i])
         {
            low[i] = p;
         }
         
         close[i] = p;
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
   CalcCustomBars(
      const int bars,
      const COPY_TICKS type,
      const ENUM_SYMBOL_CHART_MODE_EXTENDED mode)
      : limit(bars), tickType(type), chartMode(mode), lasttime(0), lastcount(0)
   {
      // register the internal arrays as indicator buffers
      SetIndexBuffer(0, open);
      SetIndexBuffer(1, high);
      SetIndexBuffer(2, low);
      SetIndexBuffer(3, close);
      const static string defTitle[] = {"Open;High;Low;Close;"}; // use array for compiler bugfix
      const static string types[] = {"Bid", "Last", "Ask"};
      string name = defTitle[0];
      StringReplace(name, ";", types[chartMode] + ";");
      PlotIndexSetString(0, PLOT_LABEL, name);
      IndicatorSetInteger(INDICATOR_DIGITS, _Digits);
   }
   
   // complete initialization
   void reset()
   {
      // most of array is feeded with empty value
      // except for given number of recent bars for calculations
      ArrayInitialize(open, EMPTY_VALUE);
      ArrayFill(open, ArraySize(open) - limit, limit, 0);
      
      // replicate this initial state to other buffers
      ArrayCopy(high, open);
      ArrayCopy(low, open);
      ArrayCopy(close, open);
      
      // prepare variables for online tick monitoring
      lasttime = 0;
      lastcount = 0;
   }
   
   
   // get ticks for specific bar on the history
   int createBar(const int i, const datetime &time[])
   {
      const int size = ArraySize(time);
      if(i < 0 || i >= size)
      {
         return -1; // do nothing: self-protection from out of bound requests
      }
      
      open[i] = high[i] = low[i] = close[i] = EMPTY_VALUE;
      
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
   int updateLastBar(const int total)
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
CalcCustomBars chart(BarCount, TickType, ChartMode);
bool calcDone = false;
bool selfRefresh = false;

//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
int OnInit()
{
   if(_Period >= PERIOD_D1)
   {
      Alert("Use intraday timeframe, please");
      return INIT_FAILED;
   }
   
   ENUM_SYMBOL_CHART_MODE mode =
      (ENUM_SYMBOL_CHART_MODE)SymbolInfoInteger(_Symbol, SYMBOL_CHART_MODE);
   Print("Chart mode: ", EnumToString(mode));
   
   if(mode == SYMBOL_CHART_MODE_BID
      && ChartMode == _SYMBOL_CHART_MODE_LAST)
   {
      Alert("Last price is not available for ", _Symbol);
   }
   
   return INIT_SUCCEEDED;
}

//+------------------------------------------------------------------+
//| Custom indicator iteration function                              |
//+------------------------------------------------------------------+
int OnCalculate(ON_CALCULATE_STD_FULL_PARAM_LIST)
{
   if(prev_calculated == 0)
   {
      chart.reset();
   }
   
   calcDone = false;
   
   // on every new bar or many new bars (including first event)
   if(prev_calculated != rates_total)
   {
      // process all or new bars
      for(int i = fmax(prev_calculated, fmax(1, rates_total - BarCount));
         i < rates_total && !IsStopped(); ++i)
      {
         const int e = chart.createBar(i, time);
         if(e <= 0)
         {
            PrintFormat("No data on bar %d at %s %s (%d). Refreshing...",
               i, TimeToString(time[i]), E2S(-e), -e);
            EventSetTimer(1); // ask terminal to call us in 1 second
            return 0; // no ticks, can't show
         }
      }
   }
   else // ticks on current bar
   {
      // update the latest bar
      const int e = chart.updateLastBar(rates_total);
      if(e < 0)
      {
         PrintFormat("Error, no ticks, %s (%d)", E2S(-e), -e);
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
