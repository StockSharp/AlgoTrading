//+------------------------------------------------------------------+
//|                                                    SubScaler.mq5 |
//|                               Copyright (c) 2021-2022, Marketeer |
//+------------------------------------------------------------------+
#property copyright "Copyright (c) 2021-2022, Marketeer"
#property link      "https://www.mql5.com/en/users/marketeer"
#property version   "1.1"
#property description "Subwindow scaler: use Up/Down arrow keys to scale up/down; hold Shift with arrow keys to move up/down.\n"
#property description "Scaling up means that picture becomes larger (zoom in, can be clipped), whereas scaling down means that picture becomes smaller (zoom out).\n"
#property description "SubScaler must be placed first or initiate creation of a subwindow; affected indicator must be the next and switched to 'Inherited scale'; chart must have keyboard focus for the keys to take effect.\n"

#property indicator_separate_window
#property indicator_buffers 0
#property indicator_plots   0

#define VK_UP   38
#define VK_DOWN 40

input double FixedMaximum = 1000;  // Initial Maximum
input double FixedMinimum = -1000; // Initial Minimum
input double _ScaleFactor = 0.1;   // Scale Factor [0.01 ... 0.5]
input bool Disabled = false;

double ScaleFactor;  // scale factor step (corrected for allowed range)
int w = -1, n = -1;  // subwindow number and number of indicators in it

//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
void OnInit()
{
   ScaleFactor = _ScaleFactor;
   if(ScaleFactor < 0.01 || ScaleFactor > 0.5)
   {
      PrintFormat("ScaleFactor %f is adjusted to default value 0.1, valid range is [0.01, 0.5]", ScaleFactor);
      ScaleFactor = 0.1;
   }
   w = ChartWindowFind();
   n = ChartIndicatorsTotal(0, w);
   PrintFormat("Starting in window %d with %d indicators", w, n);
}

//+------------------------------------------------------------------+
//| Main function to change subwindow scale upon key presses         |
//+------------------------------------------------------------------+
void Scale(const long cmd, const int shift)
{
   const double min = ChartGetDouble(0, CHART_PRICE_MIN, w);
   const double max = ChartGetDouble(0, CHART_PRICE_MAX, w);
  
   if((shift & 0x10000000) == 0) // Shift is released (not pressed)
   {
      if(cmd == VK_UP) // scale up (zoom in)
      {
         IndicatorSetDouble(INDICATOR_MINIMUM, min / (1.0 + ScaleFactor));
         IndicatorSetDouble(INDICATOR_MAXIMUM, max / (1.0 + ScaleFactor));
         ChartRedraw();
      }
      else if(cmd == VK_DOWN) // scale down (zoom out)
      {
         IndicatorSetDouble(INDICATOR_MINIMUM, min * (1.0 + ScaleFactor));
         IndicatorSetDouble(INDICATOR_MAXIMUM, max * (1.0 + ScaleFactor));
         ChartRedraw();
      }
   }
   else // Shift is pressed
   {
      if(cmd == VK_UP) // move graph up (slide from bottom to top)
      {
         const double d = (max - min) * ScaleFactor;
         IndicatorSetDouble(INDICATOR_MINIMUM, min - d);
         IndicatorSetDouble(INDICATOR_MAXIMUM, max - d);
         ChartRedraw();
      }
      else if(cmd == VK_DOWN) // move graph down (slide from top to bottom)
      {
         const double d = (max - min) * ScaleFactor;
         IndicatorSetDouble(INDICATOR_MINIMUM, min + d);
         IndicatorSetDouble(INDICATOR_MAXIMUM, max + d);
         ChartRedraw();
      }
      else if(cmd == '0') // move nearest bound to zero
      {
         if(fabs(max) > fabs(min))
         {
            IndicatorSetDouble(INDICATOR_MINIMUM, 0);
            IndicatorSetDouble(INDICATOR_MAXIMUM, max - min);
         }
         else
         {
            IndicatorSetDouble(INDICATOR_MINIMUM, min - max);
            IndicatorSetDouble(INDICATOR_MAXIMUM, 0);
         }
      }
   }
}

//+------------------------------------------------------------------+
//| Chart event handler                                              |
//+------------------------------------------------------------------+
void OnChartEvent(const int id, const long &lparam, const double &dparam, const string &sparam)
{
   switch(id)
   {
   case CHARTEVENT_KEYDOWN:
      if(!Disabled)
      {
         Scale(lparam, TerminalInfoInteger(TERMINAL_KEYSTATE_SHIFT));
      }
      break;
   case CHARTEVENT_CHART_CHANGE:
      if(ChartIndicatorsTotal(0, w) > n)
      {
         n = ChartIndicatorsTotal(0, w);
         const double min = ChartGetDouble(0, CHART_PRICE_MIN, w);
         const double max = ChartGetDouble(0, CHART_PRICE_MAX, w);
         PrintFormat("Change: %f %f %d", min, max, n);
         if(min == 0 && max == 0)
         {
            IndicatorSetDouble(INDICATOR_MINIMUM, FixedMinimum);
            IndicatorSetDouble(INDICATOR_MAXIMUM, FixedMaximum);
         }
      }
      break;
   }
}

//+------------------------------------------------------------------+
//| Custom indicator iteration function                              |
//+------------------------------------------------------------------+
int OnCalculate(const int rates_total,
                const int prev_calculated,
                const datetime &time[],
                const double &open[],
                const double &high[],
                const double &low[],
                const double &close[],
                const long &tick_volume[],
                const long &volume[],
                const int &spread[])
{
   return rates_total;
}
//+------------------------------------------------------------------+
