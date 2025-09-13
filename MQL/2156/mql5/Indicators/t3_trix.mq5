//+---------------------------------------------------------------------+
//|                                                         T3_TRIX.mq5 |
//|                                       Copyright © 2006, FinGeR Alex | 
//|                                                                     | 
//+---------------------------------------------------------------------+ 
//| Place the SmoothAlgorithms.mqh file                                 |
//| in the directory: terminal_data_folder\MQL5\Include                 |
//+---------------------------------------------------------------------+
//--- copyright
#property copyright "Copyright © 2006, FinGeR Alex"
//--- link to the website of the author
#property link "" 
#property description ""
//--- indicator version
#property version   "1.00"
//--- drawing the indicator in a separate window
#property indicator_separate_window
//--- four buffers are used for the indicator calculation and drawing
#property indicator_buffers 4
//--- two plots are used
#property indicator_plots   2
//+----------------------------------------------+
//| Indicator 1 drawing parameters               |
//+----------------------------------------------+
//--- drawing the indicator as a colored cloud
#property indicator_type1   DRAW_FILLING
//--- the following colors are used as the indicator colors
#property indicator_color1  clrAqua,clrViolet
//--- displaying the indicator label
#property indicator_label1  "T3_TRIX Signal"
//+----------------------------------------------+
//| Indicator 2 drawing parameters               |
//+----------------------------------------------+
//--- drawing the indicator as a four-color histogram
#property indicator_type2 DRAW_COLOR_HISTOGRAM
//--- colors of the five-color histogram are as follows
#property indicator_color2 clrRed,clrPurple,clrGray,clrTeal,clrLimeGreen
//--- Indicator line is a solid one
#property indicator_style2 STYLE_SOLID
//--- indicator line width is 2
#property indicator_width2 2
//--- displaying the indicator label
#property indicator_label2  "T3_TRIX"
//+----------------------------------------------+
//|  CXMA class description                      |
//+----------------------------------------------+
#include <SmoothAlgorithms.mqh> 
//+----------------------------------------------+
//--- declaration of the CXMA class variables from the SmoothAlgorithms.mqh file
CXMA XMA1,XMA2;
//+----------------------------------------------+
//|  declaration of enumerations                 |
//+----------------------------------------------+
enum Applied_price_      // type of constant
  {
   PRICE_CLOSE_ = 1,     // Close
   PRICE_OPEN_,          // Open
   PRICE_HIGH_,          // High
   PRICE_LOW_,           // Low
   PRICE_MEDIAN_,        // Median Price (HL/2)
   PRICE_TYPICAL_,       // Typical Price (HLC/3)
   PRICE_WEIGHTED_,      // Weighted Close (HLCC/4)
   PRICE_SIMPLE_,        // Simple Price (OC/2)
   PRICE_QUARTER_,       // Quarted Price (HLOC/4) 
   PRICE_TRENDFOLLOW0_,  // TrendFollow_1 Price 
   PRICE_TRENDFOLLOW1_,  // TrendFollow_2 Price 
   PRICE_DEMARK_         // Demark Price
  };
//+----------------------------------------------+
//| Indicator input parameters                   |
//+----------------------------------------------+
input Smooth_Method XMA_Method=MODE_T3; // Method of averaging
input uint XLength1=10;                 // Fast averaging depth
input uint XLength2=18;                 // Slow averaging depth
input int XPhase=70;                    // Smoothing factor
//--- XPhase: for JJMA it varies within the range -100 ... +100 and influences the quality of the transient period;
//--- XPhase: for VIDIA it is a CMO period, for AMA it is a slow average period
input Applied_price_ IPC=PRICE_CLOSE;   // Price constant
//+----------------------------------------------+
//--- declaration of dynamic arrays that
//--- will be used as indicator buffers
double IndBuffer[],ColorIndBuffer[];
double UpBuffer[],DnBuffer[];
//--- declaration of integer variables of data starting point
int min_rates_total;
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+  
void OnInit()
  {
//--- initialization of variables of the start of data calculation
   int min_rates_1=XMA1.GetStartBars(XMA_Method,XLength1,XPhase);
   int min_rates_2=XMA1.GetStartBars(XMA_Method,XLength2,XPhase);
   min_rates_total=int(MathMax(min_rates_1,min_rates_2));
//--- Set dynamic array as an indicator buffer
   SetIndexBuffer(0,UpBuffer,INDICATOR_DATA);
//--- Set dynamic array as an indicator buffer
   SetIndexBuffer(1,DnBuffer,INDICATOR_DATA);
//--- Set dynamic array as an indicator buffer
   SetIndexBuffer(2,IndBuffer,INDICATOR_DATA);
//--- Setting a dynamic array as a color index buffer   
   SetIndexBuffer(3,ColorIndBuffer,INDICATOR_COLOR_INDEX);
//--- shift the beginning of indicator drawing
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//--- setting the indicator values that won't be visible on a chart
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,0.0);
//--- shift the beginning of indicator drawing
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,min_rates_total);
//--- setting the indicator values that won't be visible on a chart
   PlotIndexSetDouble(1,PLOT_EMPTY_VALUE,0.0);
//--- creation of the name to be displayed in a separate sub-window and in a pop up help
   IndicatorSetString(INDICATOR_SHORTNAME,"T3_TRIX");
//--- determining the accuracy of the indicator values
   IndicatorSetInteger(INDICATOR_DIGITS,4);
//---
  }
//+------------------------------------------------------------------+
//| Custom indicator iteration function                              |
//+------------------------------------------------------------------+
int OnCalculate(const int rates_total,    // number of bars in history at the current tick
                const int prev_calculated,// amount of history in bars at the previous tick
                const datetime &time[],
                const double &open[],
                const double& high[],     // price array of maximums of price for the calculation of indicator
                const double& low[],      // price array of price lows for the indicator calculation
                const double &close[],
                const long &tick_volume[],
                const long &volume[],
                const int &spread[])
  {
//--- checking if the number of bars is enough for the calculation
   if(rates_total<min_rates_total) return(0);
//--- declarations of local variables 
   double price,xfast,xslow;
   int first,bar;
   static double prev_xfast,prev_xslow;
   static uint start;
//--- calculation of the starting number 'first' for the cycle of recalculation of bars
   if(prev_calculated>rates_total || prev_calculated<=0) // checking for the first start of the indicator calculation
     {
      start=1;
      first=int(start); // starting index for calculation of all bars
      prev_xfast=PriceSeries(IPC,start-1,open,low,high,close);
      prev_xslow=prev_xfast;
     }
   else first=prev_calculated-1; // Starting index for the calculation of new bars  
//--- main calculation loop of the indicator
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      price=PriceSeries(IPC,bar,open,low,high,close);
      xfast=XMA1.XMASeries(start,prev_calculated,rates_total,XMA_Method,XPhase,XLength1,price,bar,false);
      xslow=XMA2.XMASeries(start,prev_calculated,rates_total,XMA_Method,XPhase,XLength2,price,bar,false);
      //---
      if(prev_xfast) IndBuffer[bar]=(xfast-prev_xfast)/prev_xfast; else IndBuffer[bar]=0.0;
      if(prev_xslow) DnBuffer[bar]=(xslow-prev_xslow)/prev_xslow; else DnBuffer[bar]=0.0;
      UpBuffer[bar]=IndBuffer[bar];
      //--- saving values of variables
      if(bar!=rates_total-1)
        {
         prev_xfast=xfast;
         prev_xslow=xslow;
        }
     }

   if(prev_calculated>rates_total || prev_calculated<=0) first++;
//--- Main loop of the Ind indicator coloring
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      int clr=2;

      if(IndBuffer[bar]>0)
        {
         if(IndBuffer[bar]>IndBuffer[bar-1]) clr=4;
         if(IndBuffer[bar]<IndBuffer[bar-1]) clr=3;
        }

      if(IndBuffer[bar]<0)
        {
         if(IndBuffer[bar]<IndBuffer[bar-1]) clr=0;
         if(IndBuffer[bar]>IndBuffer[bar-1]) clr=1;
        }

      ColorIndBuffer[bar]=clr;
     }
//---     
   return(rates_total);
  }
//+------------------------------------------------------------------+
