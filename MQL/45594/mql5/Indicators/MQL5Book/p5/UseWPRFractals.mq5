//+------------------------------------------------------------------+
//|                                               UseWPRFractals.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property indicator_chart_window
#property indicator_buffers 3
#property indicator_plots   3

// drawing settings
#property indicator_type1   DRAW_ARROW
#property indicator_color1  clrRed
#property indicator_width1  1
#property indicator_label1  "Sell"

#property indicator_type2   DRAW_ARROW
#property indicator_color2  clrBlue
#property indicator_width2  1
#property indicator_label2  "Buy"

#property indicator_type3   DRAW_NONE
#property indicator_color3  clrGreen
#property indicator_width3  1
#property indicator_label3  "Filter"

// inputs
input int PeriodWPR = 11;
input int PeriodEMA = 5;
input int FractalOrder = 1;
input int Offset = 0;
input double Threshold = 0.2;

// indicator buffers
double UpBuffer[];    // up side means overbought, hence sell
double DownBuffer[];  // down side means oversold, hence buy
double Filter[];      // direction filter according to fractals

// global variables for subordinate indicators
int handleWPR, handleEMA3, handleFractals;

//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
int OnInit()
{
   if(Offset < 0 || Offset > 1)
   {
      Alert("Offset should take index for signal bar: 0 or 1");
      return INIT_PARAMETERS_INCORRECT;
   }
   // indicator buffers mapping
   SetIndexBuffer(0, UpBuffer);
   SetIndexBuffer(1, DownBuffer);
   SetIndexBuffer(2, Filter, INDICATOR_DATA); // could be INDICATOR_CALCULATIONS
   ArraySetAsSeries(UpBuffer, true);
   ArraySetAsSeries(DownBuffer, true);
   ArraySetAsSeries(Filter, true);
   
   // set bullets
   PlotIndexSetInteger(0, PLOT_ARROW, 234);
   PlotIndexSetInteger(1, PLOT_ARROW, 233);

   // subordinate indicators
   handleWPR = iCustom(_Symbol, _Period, "IndWPR", PeriodWPR);
   handleEMA3 = iCustom(_Symbol, _Period, "IndTripleEMA", PeriodEMA, 0, handleWPR);
   handleFractals = iCustom(_Symbol, _Period, "IndFractals", FractalOrder);
   if(handleWPR == INVALID_HANDLE
   || handleEMA3 == INVALID_HANDLE
   || handleFractals == INVALID_HANDLE)
   {
      return INIT_FAILED;
   }
   
   return INIT_SUCCEEDED;
}

//+------------------------------------------------------------------+
//| Read indicator buffers and make decision for trade signals       |
//+------------------------------------------------------------------+
int MarkSignals(const int bar, const int offset, const double &data[])
{
   double wpr[2];
   double peaks[1], hollows[1];
   if(CopyBuffer(handleEMA3, 0, bar + offset, 2, wpr) == 2
   && CopyBuffer(handleFractals, 0, bar + offset + FractalOrder, 1, peaks) == 1
   && CopyBuffer(handleFractals, 1, bar + offset + FractalOrder, 1, hollows) == 1)
   {
      int filterdirection = (int)Filter[bar + 1]; // pick up previous direction
      
      // take latest fractal as indication of beginning of opposite movement
      if(peaks[0] != EMPTY_VALUE)
      {
         filterdirection = -1; // sell
      }
      if(hollows[0] != EMPTY_VALUE)
      {
         filterdirection = +1; // buy
      }

      Filter[bar] = filterdirection; // remember new direction

      // convert 2 last values from WPR into [-1,+1] range
      const double old = (wpr[0] + 50) / 50;     // +1.0 -1.0
      const double last = (wpr[1] + 50) / 50;    // +1.0 -1.0
      
      // bumps from up back to middle
      if(filterdirection == -1
      && old >= 1.0 - Threshold && last <= 1.0 - Threshold)
      {
         UpBuffer[bar] = data[bar];
         return -1; // sell
      }
      else
      {
         UpBuffer[bar] = EMPTY_VALUE;
      }
      
      // bumps from down back to middle
      if(filterdirection == +1
      && old <= -1.0 + Threshold && last >= -1.0 + Threshold)
      {
         DownBuffer[bar] = data[bar];
         return +1; // buy
      }
      else
      {
         DownBuffer[bar] = EMPTY_VALUE;
      }
   }
   return 0; // no signal
}

//+------------------------------------------------------------------+
//| Custom indicator iteration function                              |
//+------------------------------------------------------------------+
int OnCalculate(const int rates_total,
                const int prev_calculated,
                const int begin,
                const double &data[])
{
   // wait until the subindicators are calculated for all bars
   if(BarsCalculated(handleEMA3) != rates_total
   || BarsCalculated(handleFractals) != rates_total)
   {
      return prev_calculated;
   }
   
   ArraySetAsSeries(data, true);
   
   if(prev_calculated == 0)
   {
      ArrayInitialize(UpBuffer, EMPTY_VALUE);
      ArrayInitialize(DownBuffer, EMPTY_VALUE);
      ArrayInitialize(Filter, 0);
      
      // find signals on history
      for(int i = rates_total - FractalOrder - 1; i >= 0; --i)
      {
         MarkSignals(i, Offset, data);
      }
   }
   else
   {
      for(int i = 0; i < rates_total - prev_calculated; ++i)
      {
         UpBuffer[i] = EMPTY_VALUE;
         DownBuffer[i] = EMPTY_VALUE;
         Filter[i] = 0;
      }
      
      // find signals on new bar or on every tick (if Offset == 0)
      if(rates_total != prev_calculated
      || Offset == 0)
      {
         // copy data from subordinate indicators into our buffer
         MarkSignals(0, Offset, data);
      }
   }
   
   return rates_total;
}
//+------------------------------------------------------------------+
