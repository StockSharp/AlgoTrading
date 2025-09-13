//+------------------------------------------------------------------+
//|                                                UseStochastic.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property indicator_separate_window
#property indicator_buffers 2
#property indicator_plots   2

// drawing settings
#property indicator_type1   DRAW_LINE
#property indicator_color1  clrBlue
#property indicator_width1  1
#property indicator_label1  "St'Main"

#property indicator_type2   DRAW_LINE
#property indicator_color2  clrChocolate
#property indicator_width2  1
#property indicator_label2  "St'Signal"
#property indicator_style2  STYLE_DOT

input int KPeriod = 5;
input int DPeriod = 3;
input int Slowing = 3;
input ENUM_MA_METHOD Method = MODE_SMA;
input ENUM_STO_PRICE StochasticPrice = STO_LOWHIGH;

// indicator buffer
double MainBuffer[];
double SignalBuffer[];

// global variable for subordinate indicator
int Handle;

//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
int OnInit()
{
   IndicatorSetString(INDICATOR_SHORTNAME,
      StringFormat("Stochastic(%d,%d,%d)", KPeriod, DPeriod, Slowing));
   // map arrays to indicator buffers
   SetIndexBuffer(0, MainBuffer);
   SetIndexBuffer(1, SignalBuffer);
   // obtain Stochastic handle
   Handle = iStochastic(_Symbol, _Period,
      KPeriod, DPeriod, Slowing, Method, StochasticPrice);
   
   // this is a trick to detect if we create indicator which is already running
   // if so, we can read data right away
   double array[];
   Print("This is very first copy of iStochastic with such settings=",
      !(CopyBuffer(Handle, 0, 0, 10, array) > 0));
   
   return Handle == INVALID_HANDLE ? INIT_FAILED : INIT_SUCCEEDED;
}

//+------------------------------------------------------------------+
//| Custom indicator iteration function                              |
//+------------------------------------------------------------------+
int OnCalculate(const int rates_total,
                const int prev_calculated,
                const int begin,
                const double &data[])
{
   // wait until the subindicator is calculated for all bars
   if(BarsCalculated(Handle) != rates_total)
   {
      return prev_calculated;
   }
   
   // copy data from subordinate indicator into our buffers
   const int n = CopyBuffer(Handle, 0, 0, rates_total - prev_calculated + 1, MainBuffer);
   const int m = CopyBuffer(Handle, 1, 0, rates_total - prev_calculated + 1, SignalBuffer);
   
   return n > -1 && m > -1 ? rates_total : 0;
}
//+------------------------------------------------------------------+
