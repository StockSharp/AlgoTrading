//+------------------------------------------------------------------+
//|                                              VolatilityPivot.mq5 |
//|                                         Copyright © 2005, S.B.T. |
//|                                                 excelf@gmail.com |
//+------------------------------------------------------------------+
//--- Copyright
#property copyright "Copyright © 2005, S.B.T."
//--- a link to the website of the author
#property link "excelf@gmail.com"
//--- indicator version
#property version   "1.00"
//--- Drawing the indicator in the main window
#property indicator_chart_window
//--- 4 buffers are used for calculation and drawing the indicator
#property indicator_buffers 4
//--- 4 plots are used
#property indicator_plots   4
//+----------------------------------------------+
//|  Bullish indicator drawing parameters        |
//+----------------------------------------------+
//--- drawing indicator 1 as a line
#property indicator_type1   DRAW_LINE
//--- blue color is used for the indicator line
#property indicator_color1  clrBlue
//--- the indicator 1 line is a dot-dash one
#property indicator_style1  STYLE_DASHDOTDOT
//--- indicator 1 line width is equal to 2
#property indicator_width1  2
//---- displaying of the the indicator label
#property indicator_label1  "Upper VolatilityPivot"
//+----------------------------------------------+
//| Parameters of drawing a bearish indicator    |
//+----------------------------------------------+
//--- drawing indicator 2 as a line
#property indicator_type2   DRAW_LINE
//--- HotPink color is used for the indicator line
#property indicator_color2  clrHotPink
//--- the indicator 2 line is a dot-dash one
#property indicator_style2  STYLE_DASHDOTDOT
//--- indicator 2 line width is equal to 2
#property indicator_width2  2
//---- displaying of the the indicator label
#property indicator_label2  "Lower VolatilityPivot"
//+----------------------------------------------+
//| Bullish indicator drawing parameters         |
//+----------------------------------------------+
//--- drawing the indicator 3 as a label
#property indicator_type3   DRAW_ARROW
//--- DeepSkyBlue color is used for the indicator
#property indicator_color3  clrDeepSkyBlue
//--- indicator 3 width is equal to 4
#property indicator_width3  4
//--- displaying the indicator label
#property indicator_label3  "Buy VolatilityPivot"
//+----------------------------------------------+
//| Parameters of drawing a bearish indicator    |
//+----------------------------------------------+
//--- drawing the indicator 4 as a label
#property indicator_type4   DRAW_ARROW
//--- red color is used for the indicator
#property indicator_color4  clrRed
//--- indicator 4 width is equal to 4
#property indicator_width4  4
//--- displaying the indicator label
#property indicator_label4  "Sell VolatilityPivot"
//+----------------------------------------------+
//| declaration of constants                     |
//+----------------------------------------------+
#define RESET 0  // A constant for returning the indicator recalculation command to the terminal
//+----------------------------------------------+
//| Declaration of enumeration                   |
//+----------------------------------------------+
enum Mode_
  {
   Mode_ATR=0,   //ATR
   Mode_Price    //Price deviation
  };
//+----------------------------------------------+
//| Indicator input parameters                   |
//+----------------------------------------------+
input uint   atr_range=100;
input uint   ima_range=10;
input double atr_factor=3;
input Mode_  Mode=Mode_ATR;
input  uint  DeltaPrice=200;
input int    Shift=0;          // Horizontal shift of the indicator in bars
//+----------------------------------------------+
//--- declaration of dynamic arrays that
//--- will be used as indicator buffers
double ExtMapBufferUp[];
double ExtMapBufferDown[];
double ExtMapBufferUp1[];
double ExtMapBufferDown1[];
//---
double dDeltaPrice;
//--- declaration of integer variables for the indicators handles
int ATR_Handle;
//--- declaration of integer variables for the start of data calculation
int min_rates_total;
//+------------------------------------------------------------------+
//| CMoving_Average class description                                  |
//+------------------------------------------------------------------+
#include <SmoothAlgorithms.mqh>
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+  
int OnInit()
  {
//--- Getting the handle of the ATR indicator
   if(Mode==Mode_ATR)
     {
      min_rates_total=int(atr_range+ima_range)+1;
      ATR_Handle=iATR(NULL,0,atr_range);
      if(ATR_Handle==INVALID_HANDLE)
        {
         Print(" Failed to get handle of the ATR indicator");
         return(INIT_FAILED);
        }
     }
   else
     {
      min_rates_total=3;
      dDeltaPrice=DeltaPrice*_Point;
     }
//--- set ExtMapBufferUp[] dynamic array as an indicator buffer
   SetIndexBuffer(0,ExtMapBufferUp,INDICATOR_DATA);
//---- shifting the indicator 1 horizontally by Shift
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//--- shifting the start of drawing the indicator 1
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//--- indexing buffer elements as timeseries   
   ArraySetAsSeries(ExtMapBufferUp,true);
//--- setting the indicator values that won't be visible on a chart
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//--- set ExtMapBufferDown[] dynamic array as an indicator buffer
   SetIndexBuffer(1,ExtMapBufferDown,INDICATOR_DATA);
//---- shifting the indicator 2 horizontally by Shift
   PlotIndexSetInteger(1,PLOT_SHIFT,Shift);
//--- shifting the starting point of calculation of drawing of the indicator 2
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,min_rates_total);
//--- indexing buffer elements as timeseries   
   ArraySetAsSeries(ExtMapBufferDown,true);
//--- setting the indicator values that won't be visible on a chart
   PlotIndexSetDouble(1,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//--- Set the ExtMapBufferUp1[] dynamic array as an indicator buffer
   SetIndexBuffer(2,ExtMapBufferUp1,INDICATOR_DATA);
//---- shifting the indicator 1 horizontally by Shift
   PlotIndexSetInteger(2,PLOT_SHIFT,Shift);
//--- shifting the start of drawing of the indicator 3
   PlotIndexSetInteger(2,PLOT_DRAW_BEGIN,min_rates_total);
//--- indexing buffer elements as timeseries   
   ArraySetAsSeries(ExtMapBufferUp1,true);
//--- setting the indicator values that won't be visible on a chart
   PlotIndexSetDouble(2,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//--- indicator symbol
   PlotIndexSetInteger(2,PLOT_ARROW,118);
//--- Set the ExtMapBufferDown1[] dynamic array as an indicator buffer
   SetIndexBuffer(3,ExtMapBufferDown1,INDICATOR_DATA);
//---- shifting the indicator 2 horizontally by Shift
   PlotIndexSetInteger(3,PLOT_SHIFT,Shift);
//--- shifting the start of drawing of the indicator 4
   PlotIndexSetInteger(3,PLOT_DRAW_BEGIN,min_rates_total);
//--- indexing buffer elements as timeseries   
   ArraySetAsSeries(ExtMapBufferDown1,true);
//--- setting the indicator values that won't be visible on a chart
   PlotIndexSetDouble(3,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//--- indicator symbol
   PlotIndexSetInteger(3,PLOT_ARROW,118);
//--- creation of the name to be displayed in a separate sub-window and in a pop up help
   IndicatorSetString(INDICATOR_SHORTNAME,"VolatilityPivot");
//--- determining the accuracy of the indicator values
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits);
//--- initialization end
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+
//| Custom indicator iteration function                              |
//+------------------------------------------------------------------+
int OnCalculate(const int rates_total,    // number of bars in history at the current tick
                const int prev_calculated,// amount of history in bars at the previous tick
                const datetime &time[],
                const double &open[],
                const double& high[],     // price array of price maximums for the indicator calculation
                const double& low[],      // price array of minimums of price for the indicator calculation
                const double &close[],
                const long &tick_volume[],
                const long &volume[],
                const int &spread[])
  {
//--- checking if the number of bars is enough for the calculation
   if((BarsCalculated(ATR_Handle)<rates_total && Mode==Mode_ATR) || rates_total<min_rates_total) return(RESET);
//--- declarations of local variables 
   double DeltaStop,Stop;
   static double PrevStop;
   int limit,bar;
//--- indexing elements in arrays as in timeseries  
   ArraySetAsSeries(close,true);
//--- calculation of the 'limit' starting index for the bars recalculation loop
   if(prev_calculated>rates_total || prev_calculated<=0) // checking for the first start of calculation of an indicator
     {
      limit=rates_total-min_rates_total-1;               // starting index for calculation of all bars
      PrevStop=close[limit+1];
     }
   else limit=rates_total-prev_calculated;               // starting index for calculation of new bars
//---
   if(Mode==Mode_Price)
     {
      //--- main indicator calculation loop
      for(bar=limit; bar>=0; bar--)
        {
         ExtMapBufferUp1[bar]=EMPTY_VALUE;
         ExtMapBufferDown1[bar]=EMPTY_VALUE;
         DeltaStop=dDeltaPrice;
         //---
         if(close[bar]==PrevStop) Stop=PrevStop;
         else
           {
            if(close[bar+1]<PrevStop && close[bar]<PrevStop) Stop=MathMin(PrevStop,close[bar]+DeltaStop);
            else
              {
               if(close[bar+1]>PrevStop && close[bar]>PrevStop) Stop=MathMax(PrevStop,close[bar]-DeltaStop);
               else
                 {
                  if(close[bar]>PrevStop) Stop=close[bar]-DeltaStop;
                  else Stop=close[bar]+DeltaStop;
                 }
              }
           }
         //---
         if(close[bar]>Stop) ExtMapBufferUp[bar]=Stop; else ExtMapBufferUp[bar]=EMPTY_VALUE;
         if(close[bar]<Stop) ExtMapBufferDown[bar]=Stop; else ExtMapBufferDown[bar]=EMPTY_VALUE;
         if(ExtMapBufferUp[bar+1]==EMPTY_VALUE && ExtMapBufferUp[bar]!=EMPTY_VALUE) ExtMapBufferUp1[bar]=ExtMapBufferUp[bar];
         if(ExtMapBufferDown[bar+1]==EMPTY_VALUE && ExtMapBufferDown[bar]!=EMPTY_VALUE) ExtMapBufferDown1[bar]=ExtMapBufferDown[bar];
         if(bar) PrevStop=Stop;
        }
     }
//---
   if(Mode==Mode_ATR)
     {
      //--- declarations of local variables 
      double ATR[];
      int to_copy,maxbar;
      //--- declaration of the CMoving_Average class variables from the SmoothAlgorithms.mqh file 
      static CMoving_Average EMA;
      //--- indexing elements in arrays as in timeseries  
      ArraySetAsSeries(ATR,true);
      to_copy=limit+1;
      if(CopyBuffer(ATR_Handle,0,0,to_copy,ATR)<=0) return(RESET);
      maxbar=rates_total-min_rates_total-1;
      //--- main indicator calculation loop
      for(bar=limit; bar>=0; bar--)
        {
         ExtMapBufferUp1[bar]=EMPTY_VALUE;
         ExtMapBufferDown1[bar]=EMPTY_VALUE;
         DeltaStop=atr_factor*EMA.EMASeries(maxbar,prev_calculated,rates_total,ima_range,ATR[bar],bar,true);
         //---
         if(close[bar]==PrevStop) Stop=PrevStop;
         else
           {
            if(close[bar+1]<PrevStop && close[bar]<PrevStop) Stop=MathMin(PrevStop,close[bar]+DeltaStop);
            else
              {
               if(close[bar+1]>PrevStop && close[bar]>PrevStop) Stop=MathMax(PrevStop,close[bar]-DeltaStop);
               else
                 {
                  if(close[bar]>PrevStop) Stop=close[bar]-DeltaStop;
                  else Stop=close[bar]+DeltaStop;
                 }
              }
           }
         //---
         if(close[bar]>Stop) ExtMapBufferUp[bar]=Stop; else ExtMapBufferUp[bar]=EMPTY_VALUE;
         if(close[bar]<Stop) ExtMapBufferDown[bar]=Stop; else ExtMapBufferDown[bar]=EMPTY_VALUE;
         //---
         if(ExtMapBufferUp[bar+1]==EMPTY_VALUE && ExtMapBufferUp[bar]!=EMPTY_VALUE) ExtMapBufferUp1[bar]=ExtMapBufferUp[bar];
         if(ExtMapBufferDown[bar+1]==EMPTY_VALUE && ExtMapBufferDown[bar]!=EMPTY_VALUE) ExtMapBufferDown1[bar]=ExtMapBufferDown[bar];
         //---
         if(bar) PrevStop=Stop;
        }
     }
//---     
   return(rates_total);
  }
//+------------------------------------------------------------------+
