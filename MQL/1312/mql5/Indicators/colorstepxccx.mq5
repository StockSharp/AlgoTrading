//+------------------------------------------------------------------+
//|                                                ColorStepXCCX.mq5 | 
//|                             Copyright © 2011,   Nikolay Kositsin | 
//|                              Khabarovsk,   farria@mail.redcom.ru | 
//+------------------------------------------------------------------+
//| Place the SmoothAlgorithms.mqh file                              |
//| to the directory: terminal_data_folder\MQL5\Include              |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2011, Nikolay Kositsin"
#property link "farria@mail.redcom.ru"
//---- indicator version number
#property version   "1.00"
//---- drawing indicator in a separate window
#property indicator_separate_window
//---- number of indicator buffers 2
#property indicator_buffers 2 
//---- only one plot is used
#property indicator_plots   1
//+----------------------------------------------+
//|  StepXCCX indicator drawing parameters       |
//+----------------------------------------------+
//---- drawing the indicator 1 as a cloud
#property indicator_type1   DRAW_FILLING
//---- teal and magenta colors are used for the indicator
#property indicator_color1  Teal,Magenta
//---- line of the indicator 1 is a continuous curve
#property indicator_style1  STYLE_SOLID
//---- thickness of line of the indicator 1 is equal to 1
#property indicator_width1  1
//---- displaying the indicator line label
#property indicator_label1  "Step XCCX"
//+----------------------------------------------+
//| Parameters of displaying horizontal levels   |
//+----------------------------------------------+
#property indicator_level1       -50.0
#property indicator_level2        50.0
#property indicator_levelcolor Blue
#property indicator_levelstyle STYLE_DASHDOTDOT
//+-----------------------------------+
//|  CXMA class description           |
//+-----------------------------------+
#include <SmoothAlgorithms.mqh> 
//+-----------------------------------+
//---- declaration of the CXMA class variables from the SmoothAlgorithms.mqh file
CXMA XMAD,XMAH,XMAL;
//+-----------------------------------+
//|  Declaration of enumerations      |
//+-----------------------------------+
enum Applied_price_      // Type of constant
  {
   PRICE_CLOSE_ = 1,     // Close
   PRICE_OPEN_,          // Open
   PRICE_HIGH_,          // High
   PRICE_LOW_,           // Low
   PRICE_MEDIAN_,        // Median Price (HL/2)
   PRICE_TYPICAL_,       // Typical Price (HLC/3)
   PRICE_WEIGHTED_,      // Weighted Close (HLCC/4)
   PRICE_SIMPLE,         // Simple Price (OC/2)
   PRICE_QUARTER_,       // Quarted Price (HLOC/4) 
   PRICE_TRENDFOLLOW0_,  // TrendFollow_1 Price 
   PRICE_TRENDFOLLOW1_   // TrendFollow_2 Price 
  };
/*enum Smooth_Method is declared in the SmoothAlgorithms.mqh file
  {
   MODE_SMA_,  // SMA
   MODE_EMA_,  // EMA
   MODE_SMMA_, // SMMA
   MODE_LWMA_, // LWMA
   MODE_JJMA,  // JJMA
   MODE_JurX,  // JurX
   MODE_ParMA, // ParMA
   MODE_T3,    // T3
   MODE_VIDYA, // VIDYA
   MODE_AMA,   // AMA
  }; */
//+-----------------------------------+
//|  Indicator input parameters       |
//+-----------------------------------+
input Smooth_Method DSmoothMethod=MODE_JJMA; // Price smoothing method
input int DPeriod=30;                        // Moving average period
input int DPhase=100;                        // Smoothing parameter
input Smooth_Method MSmoothMethod=MODE_T3;   // Deviation smoothing method
input int MPeriod=7;                         // Average deviation period
input int MPhase=15;                         // Deviation smoothing parameter
input Applied_price_ IPC=PRICE_TYPICAL;      // Applied price
input int StepSizeFast=5;                    // Fast step
input int StepSizeSlow=30;                   // Slow step
input int Shift=0;                           // Horizontal shift of the indicator in bars
//+-----------------------------------+
//---- declaration of dynamic arrays that 
//---- will be used as indicator buffers
double Line2Buffer[];
double Line3Buffer[];
//---- declaration of integer variables for the indicators handles
int RSI_Handle;
//---- declaration of the integer variables for the start of data calculation
int min_rates_total,min_rates_total_D,min_rates_total_M;
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+  
void OnInit()
  {
//---- initialization of variables of the start of data calculation
   min_rates_total_D=XMAD.GetStartBars(DSmoothMethod,DPeriod,DPhase);
   min_rates_total_M=XMAD.GetStartBars(MSmoothMethod,MPeriod,MPhase);
   min_rates_total=min_rates_total_D+min_rates_total_M;

//---- setting alerts for invalid values of external parameters
   XMAD.XMALengthCheck("DPeriod", DPeriod);
   XMAD.XMALengthCheck("MPeriod", MPeriod);
//---- setting alerts for invalid values of external parameters
   XMAD.XMAPhaseCheck("DPhase",DPhase,DSmoothMethod);

//---- set Line2Buffer[] dynamic array as an indicator buffer
   SetIndexBuffer(0,Line2Buffer,INDICATOR_DATA);
//---- shifting the indicator 2 horizontally by Shift
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//---- shifting the starting point of the indicator 1 drawing by min_rates_total
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);

//---- set dynamic array as an indicator buffer
   SetIndexBuffer(1,Line3Buffer,INDICATOR_DATA);
//---- shifting the indicator 3 horizontally by Shift
   PlotIndexSetInteger(1,PLOT_SHIFT,Shift);
//---- shifting the starting point of the indicator 2 drawing by min_rates_total
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,min_rates_total);

//---- initializations of variable for indicator short name
   string shortname,SmoothD,SmoothM;
   SmoothD=XMAD.GetString_MA_Method(DSmoothMethod);
   SmoothM=XMAD.GetString_MA_Method(MSmoothMethod);
   StringConcatenate(shortname,"StepXCCX(",
                     string(DPeriod),",",string(MPeriod),",",SmoothD,",",SmoothM,
                     StepSizeFast,", ",StepSizeSlow,", ",Shift,")");
//---- creating name for displaying if separate sub-window and in tooltip
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);

//---- determine the accuracy of displaying indicator values
   IndicatorSetInteger(INDICATOR_DIGITS,0);
//----
  }
//+------------------------------------------------------------------+
//| Custom indicator iteration function                              |
//+------------------------------------------------------------------+
int OnCalculate(const int rates_total,     // number of bars in history at the current tick
                const int prev_calculated, // amount of history in bars at the previous tick
                const datetime &time[],
                const double &open[],
                const double& high[],      // price array of maximums of price for the indicator calculation
                const double& low[],       // price array of minimums of price for the indicator calculation
                const double &close[],
                const long &tick_volume[],
                const long &volume[],
                const int &spread[])
  {
//---- checking for the sufficiency of bars for the calculation
   if(rates_total<min_rates_total) return(0);

//---- declaration of local variables 
   int first,bar,ftrend,strend,recbar;
   double fmin0,fmax0,smin0,smax0;
   double price_,xma,upccx,dnccx,xupccx,xdnccx,xccx;
   static double fmax1,fmin1,smin1,smax1;
   static int ftrend_,strend_;

//---- restore values of the variables
   ftrend = ftrend_;
   strend = strend_;

//---- calculate the first starting index for the loop of bars recalculation and initialization of variables
   if(prev_calculated>rates_total || prev_calculated<=0) // checking for the first start of the indicator calculation
     {
      first=0; // starting index for calculation of all bars

      fmin1=+999999;
      fmax1=-999999;
      smin1=+999999;
      smax1=-999999;
      ftrend_=0;
      strend_=0;
     }
   else first=prev_calculated-1; // starting index for calculation of new bars

   recbar=rates_total-1;

//---- main loop of the indicator calculation
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      //---- memorize values of the variables before running at the current bar
      if(rates_total!=prev_calculated && bar==recbar)
        {
         ftrend_=ftrend;
         strend_=strend;
        }

      //---- call of the PriceSeries function to get the input price 'price_'
      price_=PriceSeries(IPC,bar,open,low,high,close);

      //---- one call of the XMASeries function 
      xma=XMAD.XMASeries(0,prev_calculated,rates_total,DSmoothMethod,DPhase,DPeriod,price_,bar,false);

      //---- avoid performing further calculations on the part of history where there is not enough data
      if(bar<min_rates_total_D) continue;

      upccx=price_-xma;
      dnccx=MathAbs(upccx);

      //---- two calls of the XMASeries function  
      xupccx=XMAH.XMASeries(min_rates_total_D,prev_calculated,rates_total,MSmoothMethod,MPhase,MPeriod,upccx,bar,false);
      xdnccx=XMAL.XMASeries(min_rates_total_D,prev_calculated,rates_total,MSmoothMethod,MPhase,MPeriod,dnccx,bar,false);

      //---- indicator buffer initialization
      if(xupccx!=0.0) // prohibition for zero divide!
         xccx=100*xupccx/xdnccx;
      else xccx=0.0;

      fmax0=xccx+2*StepSizeFast;
      fmin0=xccx-2*StepSizeFast;

      if(xccx>fmax1)  ftrend=+1;
      if(xccx<fmin1)  ftrend=-1;

      if(ftrend>0 && fmin0<fmin1) fmin0=fmin1;
      if(ftrend<0 && fmax0>fmax1) fmax0=fmax1;

      smax0=xccx+2*StepSizeSlow;
      smin0=xccx-2*StepSizeSlow;

      if(xccx>smax1)  strend=+1;
      if(xccx<smin1)  strend=-1;

      if(strend>0 && smin0<smin1) smin0=smin1;
      if(strend<0 && smax0>smax1) smax0=smax1;

      if(ftrend>0) Line2Buffer[bar]=fmin0+StepSizeFast;
      if(ftrend<0) Line2Buffer[bar]=fmax0-StepSizeFast;
      if(strend>0) Line3Buffer[bar]=smin0+StepSizeSlow;
      if(strend<0) Line3Buffer[bar]=smax0-StepSizeSlow;

      if(bar<recbar)
        {
         fmin1=fmin0;
         fmax1=fmax0;
         smin1=smin0;
         smax1=smax0;
        }
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
