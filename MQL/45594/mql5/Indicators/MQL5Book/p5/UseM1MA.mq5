//+------------------------------------------------------------------+
//|                                                      UseM1MA.mq5 |
//|                               Copyright (c) 2020-2021, Marketeer |
//|                          https://www.mql5.com/en/users/marketeer |
//|                                                                  |
//| For simplified version look at UseM1MASimple.mq5                 |
//+------------------------------------------------------------------+
#property copyright "Copyright (c) 2020-2021 Marketeer"
#property link "https://www.mql5.com/en/users/marketeer"
#property version "1.1"
#property description "M1-based Moving Average provides more adequate estimation of average price per bar compared to standard price types (close, open, median, typical, weighted, etc)."

// drawing settings
#property indicator_chart_window
#property indicator_buffers 1
#property indicator_plots   1

#property indicator_type1 DRAW_LINE
#property indicator_color1 clrDodgerBlue
#property indicator_width1 2
#property indicator_style1 STYLE_SOLID

// inputs
input uint _BarLimit = 100; // BarLimit
input uint BarPeriod = 1;
input ENUM_APPLIED_PRICE M1Price = PRICE_CLOSE;

#include <MQL5Book/IndCommon.mqh>

// indicator buffer
double Buffer[];

// globals
int Handle;
int BarLimit;
bool PendingRefresh;

const string MyName = "M1MA (" + StringSubstr(EnumToString(M1Price), 6)
   + "," + (string)BarPeriod + "[" + (string)(PeriodSeconds() / 60) + "])";
const uint P = PeriodSeconds() / 60 * BarPeriod;

//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
int OnInit()
{
   IndicatorSetString(INDICATOR_SHORTNAME, MyName);
   IndicatorSetInteger(INDICATOR_DIGITS, _Digits);

   SetIndexBuffer(0, Buffer);
  
   // only simple MA makes sense to get average price of M1's within single bar of a higher timeframe
   Handle = iMA(_Symbol, PERIOD_M1, P, 0, MODE_SMA, M1Price);
  
   return Handle != INVALID_HANDLE ? INIT_SUCCEEDED : INIT_FAILED;
}

//+------------------------------------------------------------------+
//| Custom indicator iteration function                              |
//+------------------------------------------------------------------+
int OnCalculate(ON_CALCULATE_STD_FULL_PARAM_LIST)
{
   if(prev_calculated == 0)
   {
      Print("Start");
      // fresh start or history update
      ArrayInitialize(Buffer, EMPTY_VALUE);
      if(_BarLimit == 0
      || _BarLimit > (uint)rates_total)
      {
         BarLimit = rates_total;
         PlotIndexSetInteger(0, PLOT_DRAW_BEGIN, 0);
      }
      else
      {
         BarLimit = (int)_BarLimit;
         PlotIndexSetInteger(0, PLOT_DRAW_BEGIN, rates_total - BarLimit);
      }
   }
   else
   {
      // new bars initialization
      for(int i = fmax(prev_calculated - 1, (int)(rates_total - BarLimit)); i < rates_total; ++i)
      {
         Buffer[i] = EMPTY_VALUE;
      }
   }
   
   // wait for M1-indicator to get ready
   if(BarsCalculated(Handle) != iBars(_Symbol, PERIOD_M1))
   {
      if(prev_calculated == 0)
      {
         // if not, initiate a self-refersh
         Print("Requesting refresh");
         EventSetTimer(1);
         PendingRefresh = true;
      }
      return prev_calculated;
   }

   // clear pending requests for self-refresh (if any)
   PendingRefresh = false;
   
   // keep track of M1 data available history
   int noDataBar = -1;
   int trackDataBar = -1;
   
   // main calculation loop
   for(int i = fmax(prev_calculated - 1, (int)(rates_total - BarLimit)); i < rates_total; ++i)
   {
      static double result[1];
      static double exact[1];
      
      // get latest M1 bar corresponding to i-th bar of current timeframe
      const datetime dt = time[i] + PeriodSeconds() - 60;
      const int bar = iBarShift(_Symbol, PERIOD_M1, dt);
      
      // check if M1 timeseries exist on this bar of current timeframe
      ResetLastError();
      datetime x = iTime(_Symbol, PERIOD_M1, bar);

      // if M1 data is missing, remember bar number to prevent drawing
      if(bar == -1 || _LastError != 0 || x > dt)
      {
         if(trackDataBar != -1) // hole in near history (need a refresh)
         {
            Print("Hole detected, refresh");
            EventSetTimer(1);
            PendingRefresh = true;
            return prev_calculated;
         }
         noDataBar = i; // update on old bars without data
         continue;
      }
      else if(noDataBar != -1)
      {
         trackDataBar = i; // update on latest bars with data
      }
      
      // find the number of M1 bars mapped into unfinished bar 0
      // it's used to adjust value on the latest bar
      int prev = 0;
      if(bar == 0)
      {
         prev = iBarShift(_Symbol, PERIOD_M1, time[i]);
         if(CopyBuffer(Handle, 0, prev, 1, exact) != 1)
         {
            prev = 0;
         }
      }
      
      // request MA from M1
      if(CopyBuffer(Handle, 0, bar, 1, result) == 1)
      {
         // for all bars except 0-th use values intact
         if(bar > 0 || prev == 0)
         {
            Buffer[i] = result[0];
         }
         else
         {
            // for 0-th bar adjust the value for the part
            // of M1 bars extended into 1-st bar;
            // for example, with P=60 (H1 chart) the averaging at 15:15
            // is made of M1 bars from 14:15 to 15:15
            /*
                                 now
                                  V
             <--   exact  -->     V
                <--    result  -->.
            |       1        |    .  0        }
                               unfinished bar 
            */      
            
            // (P - prev) elements are common
            const double fix = (result[0] * P + exact[0] * prev * BarPeriod - exact[0] * P) / prev / BarPeriod;
            Buffer[i] = (fix * prev * BarPeriod + result[0] * (P - prev * BarPeriod)) / P;
         }
      }
      else
      {
         Print("CopyBuffer failed: ", _LastError);
         return prev_calculated;
      }
   }
   
   // if M1 history is shorter than current timeframe, limit drawing
   if(noDataBar != -1)
   {
      PlotIndexSetInteger(0, PLOT_DRAW_BEGIN, noDataBar + 1 + BarPeriod);
      Print("Limits: ", noDataBar, " ", rates_total);
   }
   
   return rates_total;
}

//+------------------------------------------------------------------+
//| Timer handler                                                    |
//+------------------------------------------------------------------+
void OnTimer()
{
   EventKillTimer();
   if(PendingRefresh)
   {
      Print("Refreshing");
      ChartSetSymbolPeriod(0, _Symbol, _Period);
   }
}
//+------------------------------------------------------------------+
