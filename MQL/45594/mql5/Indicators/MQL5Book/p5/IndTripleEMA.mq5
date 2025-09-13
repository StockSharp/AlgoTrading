//+------------------------------------------------------------------+
//|                                                 IndTripleEMA.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright   "2021, MetaQuotes Ltd."
#property link        "https://www.mql5.com"
#property description "Triple Exponential Moving Average"

// indicator settings
#property indicator_chart_window
#property indicator_buffers 4
#property indicator_plots   1

// drawing settings
#property indicator_type1   DRAW_LINE
#property indicator_color1  Orange
#property indicator_width1  1
#property indicator_label1  "EMA³"
#property indicator_applied_price PRICE_CLOSE

//+------------------------------------------------------------------+
//| How to handle 'begin' parameter in OnCalculate function          |
//+------------------------------------------------------------------+
enum BEGIN_POLICY
{
   STRICT, // strict
   CUSTOM, // custom
   NONE,   // no
};

// inputs
input int InpPeriodEMA = 14;                 // EMA period:
input BEGIN_POLICY InpHandleBegin = STRICT;  // Handle 'begin' parameter:

// indicator buffers
double TemaBuffer[];
double Ema[];
double EmaOfEma[];
double EmaOfEmaOfEma[];

//+------------------------------------------------------------------+
//| EMA internal settings                                            |
//+------------------------------------------------------------------+
// 'Offset' is reserved to define indeterminate region of warming up,
// where available number of elements is less than smoothing 'period'.
// But it's not actually needed for EMA, because unlike other MAs
// EMA's 'period' is not used directly to process 'period' elements,
// but affects inertial part of accumulation, which implicitly involves
// much more previous data trails (over 'period' elements).
// For triple EMA it's sometimes set to '3 * InpPeriodEMA - 3'.
// In this source code, being a part of the book, Offset = 0,
// which makes it easier to analize where source data begins.
const int Offset = 0;
const double K = 2.0 / (InpPeriodEMA + 1);

//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
void OnInit()
{
   // indicator setting
   const string caption = StringFormat("EMA³(%d)%s", InpPeriodEMA,
      StringSubstr(EnumToString(InpHandleBegin), 0, 1));
   IndicatorSetString(INDICATOR_SHORTNAME, caption);
   IndicatorSetInteger(INDICATOR_DIGITS, _Digits);

   // indicator buffers mapping
   SetIndexBuffer(0, TemaBuffer, INDICATOR_DATA);
   SetIndexBuffer(1, Ema, INDICATOR_CALCULATIONS);
   SetIndexBuffer(2, EmaOfEma, INDICATOR_CALCULATIONS);
   SetIndexBuffer(3, EmaOfEmaOfEma, INDICATOR_CALCULATIONS);

   // plot setup
   PlotIndexSetString(0, PLOT_LABEL, caption);
}

//+------------------------------------------------------------------+
//| Triple Exponential Moving Average                                |
//+------------------------------------------------------------------+
int OnCalculate(const int rates_total,
                const int prev_calculated,
                const int begin,
                const double &price[])
{
   if(rates_total < Offset) return 0;

   const int _begin = InpHandleBegin == STRICT ? (begin < rates_total ? begin : fmax(rates_total - 1, 0)) : 0;
   
   // fresh start or refresh
   if(prev_calculated == 0)
   {
      Print("begin=", _begin, " ", EnumToString(InpHandleBegin));
      
      // we can adjust plot settings dynamically
      PlotIndexSetInteger(0, PLOT_DRAW_BEGIN, _begin + Offset);
      
      // prepare arrays
      ArrayInitialize(Ema, EMPTY_VALUE);
      ArrayInitialize(EmaOfEma, EMPTY_VALUE);
      ArrayInitialize(EmaOfEmaOfEma, EMPTY_VALUE);
      ArrayInitialize(TemaBuffer, EMPTY_VALUE);
      Ema[_begin] = EmaOfEma[_begin] = EmaOfEmaOfEma[_begin] = price[_begin];
   }

   // main loop with respect to _begin
   for(int i = fmax(prev_calculated - 1, _begin);
      i < rates_total && !IsStopped(); i++)
   {
      EMA(price, Ema, i, _begin);
      EMA(Ema, EmaOfEma, i, _begin);
      EMA(EmaOfEma, EmaOfEmaOfEma, i, _begin);
      
      if(InpHandleBegin == CUSTOM) // empty data guard
      {
         if(Ema[i] == EMPTY_VALUE
         || EmaOfEma[i] == EMPTY_VALUE
         || EmaOfEmaOfEma[i] == EMPTY_VALUE)
            continue;
      }
      TemaBuffer[i] = 3 * Ema[i] - 3 * EmaOfEma[i] + EmaOfEmaOfEma[i];
   }
   return rates_total;
}

//+------------------------------------------------------------------+
//| Exponential Moving Average                                       |
//| Precondition: pos is in bounds of both arrays                    |
//+------------------------------------------------------------------+
void EMA(const double &source[], double &result[], const int pos, const int begin = 0)
{
   if(InpHandleBegin == CUSTOM) // empty data guard
   {
      if(source[pos] == EMPTY_VALUE || !MathIsValidNumber(source[pos]))
      {
         result[pos] = EMPTY_VALUE;
         return;
      }
      else
      if(pos > 0 && result[pos - 1] == EMPTY_VALUE)
      {
         result[pos] = source[pos];
         return;
      }
   }
   
   if(pos <= begin)
   {
      result[pos] = source[pos];
   }   
   else
   {
      result[pos] = source[pos] * K + result[pos - 1] * (1 - K);
   }
}
//+------------------------------------------------------------------+
