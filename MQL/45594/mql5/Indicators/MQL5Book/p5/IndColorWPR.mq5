//+------------------------------------------------------------------+
//|                                                  IndColorWPR.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright   "2021, MetaQuotes Ltd."
#property link        "https://www.mql5.com"
#property description "Larry Williams Percent Range Colored"

// indicator settings
#property indicator_separate_window
#property indicator_maximum    0.0
#property indicator_minimum    -100.0

// indicator buffers and plots
#property indicator_buffers    2
#property indicator_plots      1
#property indicator_type1      DRAW_COLOR_LINE
#property indicator_color1     clrDodgerBlue,clrGreen,clrRed

// indicator levels
#property indicator_level1     -20.0
#property indicator_level2     -80.0
#property indicator_levelstyle STYLE_DOT
#property indicator_levelcolor clrSilver
#property indicator_levelwidth 1

#include <MQL5Book/IndCommon.mqh>

// input parameters
input int WPRPeriod = 14; // Period

// indicator buffers
double WPRBuffer[];
double WPRColors[];

//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
void OnInit()
{
   // check input value validity
   if(WPRPeriod < 1)
   {
      Alert(StringFormat("Incorrect Period value (%d). Should be 1 or larger", WPRPeriod));
   }
   
   // map indicator buffer
   SetIndexBuffer(0, WPRBuffer);
   SetIndexBuffer(1, WPRColors, INDICATOR_COLOR_INDEX);
   
   // notify other programs about empty first elements in our buffer
   PlotIndexSetInteger(0, PLOT_DRAW_BEGIN, WPRPeriod - 1);
   
   // set name for Data Window and indicator subwindow caption
   IndicatorSetString(INDICATOR_SHORTNAME, "%R" + "(" + (string)WPRPeriod + ")");
   IndicatorSetInteger(INDICATOR_DIGITS, 2);
}

//+------------------------------------------------------------------+
//| Williams Percent Range                                           |
//+------------------------------------------------------------------+
int OnCalculate(ON_CALCULATE_STD_FULL_PARAM_LIST)
{
   // skip calculations on insufficient data or invalid period
   if(rates_total < WPRPeriod || WPRPeriod < 1) return 0;

   // since we set PLOT_DRAW_BEGIN property
   // we don't need an explicit clean-up if this indicator is supposed
   // to provide data to other indicator via 'Apply to' feature,
   // but we still need it if this indicator is called from other MQL5 code
   if(prev_calculated == 0)
   {
      if(rates_total > TerminalInfoInteger(TERMINAL_MAXBARS))
      {
         PrintFormat("Warning: oldest historic bars are hidden (available=%d, visible=%d)",
            rates_total, TerminalInfoInteger(TERMINAL_MAXBARS));
      }
      ArrayFill(WPRBuffer, 0, WPRPeriod - 1, EMPTY_VALUE);
   }
   
   // main cycle of WPR calculation with update of the last bar
   for(int i = fmax(prev_calculated - 1, WPRPeriod - 1); i < rates_total && !IsStopped(); i++)
   {
      double max_high = high[fmax(ArrayMaximum(high, i - WPRPeriod + 1, WPRPeriod), 0)];
      double min_low = low[fmax(ArrayMinimum(low, i - WPRPeriod + 1, WPRPeriod), 0)];
      if(max_high != min_low)
      {
         WPRBuffer[i] = -(max_high - close[i]) * 100 / (max_high - min_low);
      }
      else
      {
         WPRBuffer[i] = WPRBuffer[i - 1];
      }
      
      // default color is clrDodgerBlue
      WPRColors[i] = 0;
      // if new value goes over upper level, color it with clrRed
      if(WPRBuffer[i] > -20) WPRColors[i] = 2;
      // otherwise if it goes below lower level, color it with clrGreen
      else if(WPRBuffer[i] < -80) WPRColors[i] = 1;

      /*
      // alternative (more strict) coloring with analysis of both adjacent points
      if(WPRBuffer[i] > -20 && WPRBuffer[i - 1] > -20) WPRColors[i] = 2;
      else if(WPRBuffer[i] < -80 && WPRBuffer[i - 1] < -80) WPRColors[i] = 1;
      */
   }
   return rates_total;
}
//+------------------------------------------------------------------+
