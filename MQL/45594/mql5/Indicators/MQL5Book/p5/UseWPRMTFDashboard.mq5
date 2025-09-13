//+------------------------------------------------------------------+
//|                                           UseWPRMTFDashboard.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property indicator_separate_window
#property indicator_buffers 2
#property indicator_plots   1

// adjust these bounds to get enough vertical space for multiple instances
// of indicator in the same subwindow: every instance shows a row of marks
#property indicator_maximum    150.0
#property indicator_minimum     50.0

// drawing settings
#property indicator_type1   DRAW_COLOR_ARROW
#property indicator_color1  clrGray
#property indicator_width1  1
#property indicator_label1  "WPR"

// inputs
input int WPRPeriod = 14;
input string WorkSymbol = ""; // Symbol
input int Mark = 110 /* big box */;
input double Level = 100;
input double LabelPadding = -10;

// NB. Level should be unique in each instance and differ from next Levels
// sufficiently to hold visual marks. For example, set Level as 80, 100, 120
// in 3 instances of this indicator for different symbols.
// Use LabelPadding to move text captions under corresponding row of marks.
// For example, -10 seems ok for above scales and if distance between rows
// is 20, as in the example above.

#include <MQL5Book/ColorMix.mqh>

#define TFS 21

// array of all timeframes is constant here (can be requested from user)
ENUM_TIMEFRAMES TF[TFS] =
{
   PERIOD_M1,
   PERIOD_M2,
   PERIOD_M3,
   PERIOD_M4,
   PERIOD_M5,
   PERIOD_M6,
   PERIOD_M10,
   PERIOD_M12,
   PERIOD_M15,
   PERIOD_M20,
   PERIOD_M30,
   PERIOD_H1,
   PERIOD_H2,
   PERIOD_H3,
   PERIOD_H4,
   PERIOD_H6,
   PERIOD_H8,
   PERIOD_H12,
   PERIOD_D1,
   PERIOD_W1,
   PERIOD_MN1,
};

// indicator buffers (data and color indices)
double WPRBuffer[];
double Colors[];

// global variable for subordinate indicator
int Handle[TFS];

// actual symbol variable
const string _WorkSymbol = (WorkSymbol == "" ? _Symbol : WorkSymbol);

// prevent refresh on timer if already calculated by tick
bool CalculationDone = false;

//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
int OnInit()
{
   SetIndexBuffer(0, WPRBuffer);
   SetIndexBuffer(1, Colors, INDICATOR_COLOR_INDEX);
   ArraySetAsSeries(WPRBuffer, true);
   ArraySetAsSeries(Colors, true);
   PlotIndexSetString(0, PLOT_LABEL, _WorkSymbol + " WPR");
   
   if(Mark != 0)
   {
      PlotIndexSetInteger(0, PLOT_ARROW, Mark);
   }

   // generate 64 specific tints for entire WPR range [0,1] scale
   PlotIndexSetInteger(0, PLOT_COLOR_INDEXES, 64); // use max colors possible
   for(int i = 0; i < 64; ++i)
   {
      // color palette is calculated based on 2 bounding colors
      PlotIndexSetInteger(0, PLOT_LINE_COLOR, i, ColorMix::RotateColors(clrBlue, clrRed, 64, i));
   }
   
   // indicator level is used to subscribe row of marks
   IndicatorSetInteger(INDICATOR_LEVELS, 1);
   IndicatorSetDouble(INDICATOR_LEVELVALUE, Level + LabelPadding);
   IndicatorSetString(INDICATOR_LEVELTEXT, 0, _WorkSymbol + " WPR");
   
   for(int i = 0; i < TFS; ++i)
   {
      Handle[i] = iCustom(_WorkSymbol, TF[i], "IndWPR", WPRPeriod);
      if(Handle[i] == INVALID_HANDLE) return INIT_FAILED;
   }

   IndicatorSetInteger(INDICATOR_DIGITS, 2);
   IndicatorSetString(INDICATOR_SHORTNAME, "%Rmtf" + "(" + _WorkSymbol + "/starting...)");
   
   CalculationDone = false;
   return INIT_SUCCEEDED;
}

//+------------------------------------------------------------------+
//| Check all handles for 'data ready' state                         |
//+------------------------------------------------------------------+
bool IsDataReady()
{
   static bool ready = false;
   for(int i = 0; i < TFS; ++i)
   {
      if(BarsCalculated(Handle[i]) != iBars(_WorkSymbol, TF[i]))
      {
         ready = false;
         Print("Waiting for ", _WorkSymbol, " ", EnumToString(TF[i]), " ", BarsCalculated(Handle[i]), " ", iBars(_WorkSymbol, TF[i]));
         IndicatorSetString(INDICATOR_SHORTNAME, "%Rmtf" + "(" + _WorkSymbol + "/building...)");
         return false;
      }
   }
   
   if(!ready)
   {
      ready = true;
      IndicatorSetString(INDICATOR_SHORTNAME, "%Rmtf" + "(" + _WorkSymbol + "/" + (string)WPRPeriod + ")");
   }
   return true;
}

//+------------------------------------------------------------------+
//| Read current values from WPRs                                    |
//+------------------------------------------------------------------+
void FillData()
{
   for(int i = 0; i < TFS; ++i)
   {
      double data[1];
      if(CopyBuffer(Handle[i], 0, 0, 1, data) == 1)
      {
         WPRBuffer[i] = Level + (data[0] + 100) / 100;  // WPR value itself (biassed with Level)
         Colors[i] = (int)((data[0] + 100) / 100 * 64); // color index
      }
   }
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
   if(!IsDataReady())
   {
      CalculationDone = false;
      EventSetTimer(1);
      return prev_calculated;
   }
   
   // init on fresh start or history update
   if(prev_calculated == 0)
   {
      ArrayInitialize(WPRBuffer, EMPTY_VALUE);
      ArrayInitialize(Colors, EMPTY_VALUE);
      // persistent colors of the last TFS bars
      for(int i = 0; i < TFS; ++i)
      {
         Colors[i] = 0;
      }
   }
   else
   {
      for(int i = prev_calculated; i < rates_total; ++i)
      {
         WPRBuffer[i] = EMPTY_VALUE;
         Colors[i] = 0;
      }
   }

   if(prev_calculated != rates_total) // new bar
   {
      // clear most outdated bar pushed to the left
      WPRBuffer[TFS] = EMPTY_VALUE;
      // wipe out colors
      for(int i = 0; i < TFS; ++i)
      {
         Colors[i] = 0;
      }
   }
   
   // copy data from subordinate indicators into our buffer
   FillData();
   
   CalculationDone = true;
   
   return rates_total;
}

//+------------------------------------------------------------------+
//| Timer handler                                                    |
//+------------------------------------------------------------------+
void OnTimer()
{
   if(!CalculationDone)
   {
      ChartSetSymbolPeriod(0, _Symbol, _Period);
   }
   EventKillTimer();
}
//+------------------------------------------------------------------+
