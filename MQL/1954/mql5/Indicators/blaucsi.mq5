//+---------------------------------------------------------------------+
//|                                                         BlauCSI.mq5 |
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
#property description "Candle Stochastic Index"
//---- Indicator version number
#property version   "1.00"
//---- drawing indicator in a separate window
#property indicator_separate_window
//----two buffers are used for calculating and drawing the indicator
#property indicator_buffers 2
//---- one plot is used
#property indicator_plots   1
//+----------------------------------------------+
//|  Indicator 2 drawing parameters            |
//+----------------------------------------------+
//---- drawing indicator as a five-color histogram
#property indicator_type1 DRAW_COLOR_HISTOGRAM
//---- the following colors are used
#property indicator_color1 clrMagenta,clrOrange,clrGray,clrDeepSkyBlue,clrBlue
//---- indicator line width is 2
#property indicator_width1 2
//---- displaying the indicator label
#property indicator_label1  "Candle Stochastic Index"
//+----------------------------------------------+
//| Parameters of displaying horizontal levels   |
//+----------------------------------------------+
#property indicator_level1   +25
#property indicator_level2     0
#property indicator_level3   -25
#property indicator_levelcolor clrBlue          // color of the level line
#property indicator_levelstyle STYLE_DASHDOTDOT // style of the level line
#property indicator_levelwidth 1                // width of the level line
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
//| Indicator input parameters                 |
//+----------------------------------------------+
input Smooth_Method XMA_Method=MODE_EMA; // Averaging method
input uint XLength=1;                    // Period of Momentum
input uint XLength1=20;                  // Depth of the first averaging
input uint XLength2=5;                   // Depth of the second averaging
input uint XLength3=3;                   // Depth of the third averaging
input int XPhase=15;                     // Smoothing parameter
// XPhase: for JJMA it varies within the range -100 ... +100 and influences the quality of the transient period;
// XPhase: for VIDIA it is a CMO period, for AMA it is a slow average period
input Applied_price_ IPC1=PRICE_CLOSE;   // Price constant of closing
input Applied_price_ IPC2=PRICE_OPEN;    // Price constant of opening
input int Shift=0;                       // Horizontal shift of the indicator in bars 
//+----------------------------------------------+
//---- declaration of dynamic arrays that
//---- will be used as indicator buffers
double IndBuffer[],ColorIndBuffer[];
//---- declaration of the integer variables for the start of data calculation
int min_rates_total,min_rates_1,min_rates_2,min_rates_3;
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+  
void OnInit()
  {
//---- Initialization of variables of the start of data calculation
   min_rates_1=int(XLength-1);
   min_rates_2=min_rates_1+XMA1.GetStartBars(XMA_Method,XLength1,XPhase);
   min_rates_3=min_rates_2+XMA1.GetStartBars(XMA_Method,XLength2,XPhase);
   min_rates_total=min_rates_3+XMA1.GetStartBars(XMA_Method,XLength3,XPhase);   
   if(IPC1==IPC2 && XLength==1) Print("Invalid values of price constants!");
//---- Set IndBuffer dynamic array as an indicator buffer
   SetIndexBuffer(0,IndBuffer,INDICATOR_DATA);
//---- shifting the start of drawing of the indicator
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- Setting the indicator values that won't be visible on a chart
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,0.0);
//---- set dynamic array as a color index buffer
   SetIndexBuffer(1,ColorIndBuffer,INDICATOR_COLOR_INDEX);
//--- Creation of the name to be displayed in a separate sub-window and in a pop up help
   IndicatorSetString(INDICATOR_SHORTNAME,"Candle Stochastic Index");
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
   double Mom,XMom,XXMom,XXXMom,Renge,XRenge,XXRenge,XXXRenge,max,min;
   int first,bar,kkk;

//---- calculation of the 'first' starting number for the bars recalculation loop
   if(prev_calculated>rates_total || prev_calculated<=0) // checking for the first start of calculation of an indicator
     {
      first=min_rates_1; // starting number for calculation of all bars
     }
   else first=prev_calculated-1; // starting number for calculation of new bars

//---- The main loop of the indicator calculation
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      Mom=PriceSeries(IPC1,bar,open,low,high,close)-PriceSeries(IPC2,bar-XLength+1,open,low,high,close);
      
      min=+1000000.0;
      max=-1000000.0;
      for(kkk=bar-int(XLength-1); kkk<=bar; kkk++)
        {
         if(min>low[kkk])  min=low[kkk];
         if(max<high[kkk]) max=high[kkk];
        }
      Renge=max-min;
      
      XMom=XMA1.XMASeries(min_rates_1,prev_calculated,rates_total,XMA_Method,XPhase,XLength1,Mom,bar,false);
      XRenge=XMA2.XMASeries(min_rates_1,prev_calculated,rates_total,XMA_Method,XPhase,XLength1,Renge,bar,false);     
      
      XXMom=XMA3.XMASeries(min_rates_2,prev_calculated,rates_total,XMA_Method,XPhase,XLength2,XMom,bar,false);
      XXRenge=XMA4.XMASeries(min_rates_2,prev_calculated,rates_total,XMA_Method,XPhase,XLength2,XRenge,bar,false);
      
      XXXMom=XMA5.XMASeries(min_rates_3,prev_calculated,rates_total,XMA_Method,XPhase,XLength3,XXMom,bar,false);
      XXXRenge=XMA6.XMASeries(min_rates_3,prev_calculated,rates_total,XMA_Method,XPhase,XLength3,XXRenge,bar,false);
      
      if(XXXRenge) IndBuffer[bar]=100*XXXMom/XXXRenge;
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