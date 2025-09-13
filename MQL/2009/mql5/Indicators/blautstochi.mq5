//+---------------------------------------------------------------------+
//|                                                     BlauTStochI.mq5 |
//|                                  Copyright © 2013, Nikolay Kositsin | 
//|                                 Khabarovsk,   farria@mail.redcom.ru | 
//+---------------------------------------------------------------------+ 
//| Place the SmoothAlgorithms.mqh file                                 |
//| in the directory: terminal_data_folder\MQL5\Include                 |
//+---------------------------------------------------------------------+
//---- Copyright
#property copyright "Copyright © 2013, Nikolay Kositsin"
//---- link to the website of the author
#property link "farria@mail.redcom.ru" 
#property description "q-period Stochastic Index"
//---- Indicator version number
#property version   "1.00"
//---- drawing indicator in a separate window
#property indicator_separate_window
//----two buffers are used for calculating and drawing the indicator
#property indicator_buffers 2
//---- one plot is used
#property indicator_plots   1
//+----------------------------------------------+
//|  Indicator 1 drawing parameters              |
//+----------------------------------------------+
//---- drawing indicator as a four-color histogram
#property indicator_type1 DRAW_COLOR_HISTOGRAM
//---- colors of the five-color histogram are as follows
#property indicator_color1 clrMagenta,clrOrange,clrGray,clrTurquoise,clrBlue
//---- Indicator line is a solid one
#property indicator_style1 STYLE_SOLID
//---- indicator line width is 2
#property indicator_width1 2
//---- displaying the indicator label
#property indicator_label1  "Blau TStochI"
//+----------------------------------------------+
//| Parameters of displaying horizontal levels   |
//+----------------------------------------------+
#property indicator_level1 +10
#property indicator_level2  0
#property indicator_level3 -10
#property indicator_levelcolor clrBlue
#property indicator_levelstyle STYLE_DASHDOTDOT
//+----------------------------------------------+
//|  CXMA class description                      |
//+----------------------------------------------+
#include <SmoothAlgorithms.mqh> 
//+----------------------------------------------+
//---- declaration of the CXMA class variables from the SmoothAlgorithms.mqh file
CXMA XMA1,XMA2,XMA3,XMA4,XMA5,XMA6;
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
   PRICE_SIMPL_,         // Simple Price (OC/2)
   PRICE_QUARTER_,       // Quarted Price (HLOC/4) 
   PRICE_TRENDFOLLOW0_,  // TrendFollow_1 Price 
   PRICE_TRENDFOLLOW1_,  // TrendFollow_2 Price 
   PRICE_DEMARK_         // Demark Price
  };
//+----------------------------------------------+
//| Indicator input parameters                   |
//+----------------------------------------------+
input Smooth_Method XMA_Method=MODE_EMA;// Method of averaging
input uint XLength=20;                  // Depth of price averaging 
input uint XLength1=5;                  // Depth of the first averaging
input uint XLength2=3;                  // Depth of the second averaging
input uint XLength3=8;                  // Depth of averaging of the signal line
input int XPhase=15;                    // Smoothing parameter
//--- XPhase: for JJMA it varies within the range -100 ... +100 and influences the quality of the transient period;
//--- XPhase: for VIDIA it is a CMO period, for AMA it is a slow average period
input Applied_price_ IPC=PRICE_CLOSE;   // Price constant
//+----------------------------------------------+
//---- declaration of dynamic arrays which will be used as indicator buffers
double IndBuffer[],ColorIndBuffer[];
double UpBuffer[],DnBuffer[];
//---- declaration of the integer variables for the start of data calculation
int min_rates_total,min_rates_1,min_rates_2,min_rates_3;
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+  
void OnInit()
  {
//---- Initialization of variables of the start of data calculation
   min_rates_1=int(XLength);
   min_rates_2=min_rates_1+XMA1.GetStartBars(XMA_Method,XLength1,XPhase);
   min_rates_3=min_rates_2+XMA1.GetStartBars(XMA_Method,XLength2,XPhase);
   min_rates_total=min_rates_3+XMA1.GetStartBars(XMA_Method,XLength3,XPhase);
//---- Set dynamic array as an indicator buffer
   SetIndexBuffer(0,IndBuffer,INDICATOR_DATA);
//---- set dynamic array as a color index buffer   
   SetIndexBuffer(1,ColorIndBuffer,INDICATOR_COLOR_INDEX);
//---- shifting the start of drawing of the indicator
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- Setting the indicator values that won't be visible on a chart
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,0.0);
//--- Creation of the name to be displayed in a separate sub-window and in a pop up help
   IndicatorSetString(INDICATOR_SHORTNAME,"Blau TStochI");
//--- Determining the accuracy of displaying the indicator values
   IndicatorSetInteger(INDICATOR_DIGITS,0);
//----
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
//---- checking the number of bars to be enough for the calculation
   if(rates_total<min_rates_total) return(0);
//---- declaration of local variables 
   double price,HH,LL,Stoch,xStoch,xxStoch,xxxStoch,Range,xRange,xxRange,xxxRange;
   int first,bar,sbar;
//---- indexing elements in arrays as in timeseries  
   ArraySetAsSeries(high,true);
   ArraySetAsSeries(low,true);
//---- calculation of the 'first' starting number for the bars recalculation loop
   if(prev_calculated>rates_total || prev_calculated<=0) // checking for the first start of calculation of an indicator
     {
      first=0; // starting index for calculation of all bars
     }
   else first=prev_calculated-1; // starting number for calculation of new bars
//---- The main loop of the indicator calculation
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      price=PriceSeries(IPC,bar,open,low,high,close);
      sbar=rates_total-1-bar;
      HH=high[ArrayMaximum(high,sbar,XLength)];
      LL=low[ArrayMinimum(low,sbar,XLength)];
      Stoch=price-LL;
      Range=HH-LL;

      xStoch=XMA1.XMASeries(min_rates_1,prev_calculated,rates_total,XMA_Method,XPhase,XLength1,Stoch,bar,false);
      xRange=XMA2.XMASeries(min_rates_1,prev_calculated,rates_total,XMA_Method,XPhase,XLength1,Range,bar,false);

      xxStoch=XMA3.XMASeries(min_rates_2,prev_calculated,rates_total,XMA_Method,XPhase,XLength2,xStoch,bar,false);
      xxRange=XMA4.XMASeries(min_rates_2,prev_calculated,rates_total,XMA_Method,XPhase,XLength2,xRange,bar,false);

      xxxStoch=XMA5.XMASeries(min_rates_3,prev_calculated,rates_total,XMA_Method,XPhase,XLength3,xxStoch,bar,false);
      xxxRange=XMA6.XMASeries(min_rates_3,prev_calculated,rates_total,XMA_Method,XPhase,XLength3,xxRange,bar,false);

      if(xxxRange) IndBuffer[bar]=100*xxxStoch/xxxRange-50;
      else IndBuffer[bar]=0.0;
     }

   if(prev_calculated>rates_total || prev_calculated<=0) first++;
//---- main cycle of the indicator coloring
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
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
