//+------------------------------------------------------------------+
//|                                                UseM1MASimple.mq5 |
//|                               Copyright (c) 2020-2021, Marketeer |
//|                          https://www.mql5.com/en/users/marketeer |
//|                                                                  |
//| For complete version with protection from EMPTY_VALUEs           |
//| and waiting for ongoing out-of-sync M1 look at UseM1MASimple.mq5 |
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
      }
      else
      {
         BarLimit = (int)_BarLimit;
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
   
   // main calculation loop
   for(int i = fmax(prev_calculated - 1, (int)(rates_total - BarLimit)); i < rates_total; ++i)
   {
      static double result[1];
      
      // get latest M1 bar corresponding to i-th bar of current timeframe
      const datetime dt = time[i] + PeriodSeconds() - 60;
      const int bar = iBarShift(_Symbol, PERIOD_M1, dt);
      
      if(bar > -1)
      {
         // request MA from M1
         if(CopyBuffer(Handle, 0, bar, 1, result) == 1)
         {
            Buffer[i] = result[0];
         }
         else
         {
            Print("CopyBuffer failed: ", _LastError);
            return prev_calculated;
         }
      }
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
