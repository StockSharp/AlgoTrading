//+------------------------------------------------------------------+
//|                                                    UseWPRMTF.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property indicator_separate_window
#property indicator_buffers 2
#property indicator_plots   1

// set the scale with some additional margins to exact range [-1,+1]
#property indicator_maximum    +1.2
#property indicator_minimum    -1.2

// indicator levels
#property indicator_level1     +0.6
#property indicator_level2     -0.6
#property indicator_levelstyle STYLE_DOT
#property indicator_levelcolor clrSilver
#property indicator_levelwidth 1

// drawing settings
#property indicator_type1   DRAW_COLOR_ARROW
#property indicator_color1  clrRed,clrGreen,clrBlue
#property indicator_width1  3
#property indicator_label1  "WPR"

// inputs
input int WPRPeriod = 14;
input string WorkSymbol = ""; // Symbol
input int Mark = 0;

#define TFS 21

// array of all timeframes is constant here (can be requested from user)
ENUM_TIMEFRAMES TF[TFS] =
{
   // minutes -> Red
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
   // hours -> Green
   PERIOD_H1,
   PERIOD_H2,
   PERIOD_H3,
   PERIOD_H4,
   PERIOD_H6,
   PERIOD_H8,
   PERIOD_H12,
   // daily and up -> Blue
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
         Print("Waiting for ", _WorkSymbol, " ", EnumToString(TF[i]));
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
         WPRBuffer[i] = (data[0] + 50) / 50;
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
         Colors[i] = i < 11 ? 0 : (i < 18 ? 1 : 2);
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
      // update colors
      for(int i = 0; i < TFS; ++i)
      {
         Colors[i] = i < 11 ? 0 : (i < 18 ? 1 : 2);
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
