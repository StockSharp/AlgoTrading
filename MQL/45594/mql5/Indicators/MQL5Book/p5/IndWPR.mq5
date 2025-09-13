//+------------------------------------------------------------------+
//|                                                       IndWPR.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright   "2021, MetaQuotes Ltd."
#property link        "https://www.mql5.com"
#property description "Larry Williams Percent Range"

// indicator settings
#property indicator_separate_window
#property indicator_maximum    0.0
#property indicator_minimum    -100.0

// indicator buffers and plots
#property indicator_buffers    1
#property indicator_plots      1
#property indicator_type1      DRAW_LINE
#property indicator_color1     clrDodgerBlue

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
}

//+------------------------------------------------------------------+
//| Williams Percent Range                                           |
//+------------------------------------------------------------------+
int OnCalculate(ON_CALCULATE_STD_FULL_PARAM_LIST)
{
   // skip calculations on insufficient data or invalid period
   if(rates_total < WPRPeriod || WPRPeriod < 1) return 0;

   // fill meaningless beginning with empty value
   if(prev_calculated == 0)
   {
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
   }
   return rates_total;
}
//+------------------------------------------------------------------+
