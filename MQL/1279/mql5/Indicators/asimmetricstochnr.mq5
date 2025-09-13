/*
 * For the indicator to work, place the
 * SmoothAlgorithms.mqh
 * in the directory: MetaTrader\\MQL5\Include
 */
//+------------------------------------------------------------------+
//|                                            AsimmetricStochNR.mq5 | 
//|                                    Copyright © 2010,   Svinozavr | 
//|                                                                  | 
//+------------------------------------------------------------------+
#property copyright "Copyright © 2010,   Svinozavr"
#property link ""
#property description "Asimmetric Stoch NR"
//---- indicator version number
#property version   "1.00"
//---- drawing indicator in a separate window
#property indicator_separate_window
//---- number of indicator buffers 2
#property indicator_buffers 2 
//---- only two plots are used
#property indicator_plots   2
//+----------------------------------------------+
//|  declaring constants                         |
//+----------------------------------------------+
#define RESET 0 // The constant for returning the indicator recalculation command to the terminal
//+-----------------------------------+
//|  Indicator drawing parameters     |
//+-----------------------------------+
//---- drawing the indicator as a line
#property indicator_type1   DRAW_LINE
//---- deep pink color is used for the indicator line
#property indicator_color1 DeepPink
//---- the indicator line is a continuous curve
#property indicator_style1  STYLE_DASHDOTDOT
//---- indicator line width is equal to 1
#property indicator_width1  1
//---- displaying the indicator label
#property indicator_label1  "Asimmetric Stochastic NR"
//+-----------------------------------+
//|  Indicator drawing parameters     |
//+-----------------------------------+
//---- drawing the indicator as a line
#property indicator_type2   DRAW_LINE
//---- Teal color is used for indicator line
#property indicator_color2 Teal
//---- the indicator line is a dot-dash one
#property indicator_style2  STYLE_SOLID
//---- indicator line width is equal to 1
#property indicator_width2  1
//---- displaying the indicator label
#property indicator_label2  "Signal"
//+----------------------------------------------+
//| Parameters of displaying horizontal levels   |
//+----------------------------------------------+
#property indicator_level1       70.0
#property indicator_level2       30.0
#property indicator_levelcolor DarkOrchid
#property indicator_levelstyle STYLE_DASHDOTDOT

//+-----------------------------------+
//|  CXMA class description           |
//+-----------------------------------+
#include <SmoothAlgorithms.mqh> 
//+-----------------------------------+
//---- declaration of the CXMA class variables from the SmoothAlgorithms.mqh file
CXMA XMA;
//+-----------------------------------+
//|  Declaration of enumerations      |
//+-----------------------------------+
/*enum Smooth_Method is declared in the SmoothAlgorithms.mqh file
  {
   MODE_SMA_,  //SMA
   MODE_EMA_,  //EMA
   MODE_SMMA_, //SMMA
   MODE_LWMA_, //LWMA
   MODE_JJMA,  //JJMA
   MODE_JurX,  //JurX
   MODE_ParMA, //ParMA
   MODE_T3,    //T3
   MODE_VIDYA, //VIDYA
   MODE_AMA,   //AMA
  }; */
//+-----------------------------------+
//|  enumeration declaration          |
//+-----------------------------------+  
enum WIDTH
  {
   Width_1=1, //1
   Width_2,   //2
   Width_3,   //3
   Width_4,   //4
   Width_5    //5
  };
//+-----------------------------------+
//|  enumeration declaration          |
//+-----------------------------------+
enum STYLE
  {
   SOLID_,//Solid line
   DASH_,//Dashed line
   DOT_,//Dotted line
   DASHDOT_,//Dot-dash line
   DASHDOTDOT_   //Dot-dash line with double dots
  };
//+-----------------------------------+
//|  INDICATOR INPUT PARAMETERS       |
//+-----------------------------------+
input uint KperiodShort=5; //%K long period
input uint KperiodLong=12; //%K short period

input Smooth_Method DMethod=MODE_SMA; //signal line smoothing method 
input uint Dperiod=7;  //%D signal period
input int DPhase=15;   //signal line smoothing parameter,
                       // for JJMA it varies within the range -100 ... +100 and influences the quality of the transient period;
// For VIDIA, it is a CMO period, for AMA, it is a slow moving average period

input uint Slowing=3; // slowing
input ENUM_STO_PRICE PriceField=STO_LOWHIGH; //prices selection parameter for calculation
input uint Sens=7; // sensitivity in points
input uint OverBought=80; // overbought level, %%
input uint OverSold=20; // oversold level, %%
input color LevelsColor=Blue; //levels color
input STYLE Levelstyle=DASH_;   //levels style
input WIDTH  LevelsWidth=Width_1;  //levels width
input int Shift=0; //horizontal shift of the indicator in bars
//+-----------------------------------+

//---- declaration of dynamic arrays that will further be 
// used as indicator buffers
double Stoch[],XStoch[];

double sens; // sensitivity in prices

//---- Declaration of integer variables of data starting point
int min_rates_total,min_rates_stoch;
//+------------------------------------------------------------------+   
//| Asimmetric Stoch NR indicator initialization function            | 
//+------------------------------------------------------------------+ 
void OnInit()
  {
//---- Initialization of variables of the start of data calculation
   min_rates_stoch=int(MathMax(KperiodShort,KperiodLong)+Slowing);
   min_rates_total=min_rates_stoch+XMA.GetStartBars(DMethod,Dperiod,DPhase);

//---- Initialization of variables   
   sens=Sens*_Point; // sensitivity in prices
   
//---- lines drawing parameters  
   IndicatorSetInteger(INDICATOR_LEVELS,2);
   
   IndicatorSetDouble(INDICATOR_LEVELVALUE,0,OverSold);
   IndicatorSetInteger(INDICATOR_LEVELCOLOR,0,LevelsColor);
   IndicatorSetInteger(INDICATOR_LEVELSTYLE,0,Levelstyle);
   IndicatorSetInteger(INDICATOR_LEVELWIDTH,0,LevelsWidth);
   
   IndicatorSetDouble(INDICATOR_LEVELVALUE,1,OverBought);
   IndicatorSetInteger(INDICATOR_LEVELCOLOR,1,LevelsColor);
   IndicatorSetInteger(INDICATOR_LEVELSTYLE,1,Levelstyle);
   IndicatorSetInteger(INDICATOR_LEVELWIDTH,1,LevelsWidth);

//---- setting alerts for invalid values of external parameters
   XMA.XMALengthCheck("Dperiod",Dperiod);
   XMA.XMALengthCheck("Dperiod",Dperiod);
   XMA.XMAPhaseCheck("DPhase",DPhase,DMethod);

//---- set dynamic array as an indicator buffer
   SetIndexBuffer(0,Stoch,INDICATOR_DATA);
//---- moving the indicator 1 horizontally
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//---- performing the shift of beginning of indicator drawing
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- setting the indicator values that won't be visible on a chart
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//---- indexing elements in the buffer as time series
   ArraySetAsSeries(Stoch,true);

//---- set dynamic array as an indicator buffer
   SetIndexBuffer(1,XStoch,INDICATOR_DATA);
//---- moving the indicator 1 horizontally
   PlotIndexSetInteger(1,PLOT_SHIFT,Shift);
//---- performing the shift of beginning of indicator drawing
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,min_rates_total);
//---- setting the indicator values that won't be visible on a chart
   PlotIndexSetDouble(1,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//---- indexing elements in the buffer as time series
   ArraySetAsSeries(XStoch,true);

//---- initializations of variable for indicator short name
   string shortname,Smooth;
   Smooth=XMA.GetString_MA_Method(DMethod);
   StringConcatenate(shortname,"Asimmetric Stochastic NR(",KperiodShort,",",KperiodLong,",",Dperiod,",",Smooth,",",Slowing,")");
//---- creating name for displaying if separate sub-window and in tooltip
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);

//--- determining the accuracy of displaying the indicator values
   IndicatorSetInteger(INDICATOR_DIGITS,2);
//---- end of initialization
  }
//+------------------------------------------------------------------+ 
//| Asimmetric Stoch NR iteration function                           | 
//+------------------------------------------------------------------+ 
int OnCalculate(
                const int rates_total,    // amount of history in bars at the current tick
                const int prev_calculated,// amount of history in bars at the previous tick
                const datetime &time[],
                const double &open[],
                const double &high[],
                const double &low[],
                const double &close[],
                const long &tick_volume[],
                const long &volume[],
                const int &spread[]
                )
  {
//---- checking the number of bars to be enough for calculation
   if(rates_total<min_rates_total) return(RESET);

//---- Declaration of integer variables
   int limit,bar,maxbar;

//---- memory static variables declaration
   static uint Kperiod0,Kperiod1;

//---- calculations of the necessary amount of data to be copied and
//the starting number limit for the bar recalculation loop
   if(prev_calculated>rates_total || prev_calculated<=0)// checking for the first start of the indicator calculation
     {
      limit=rates_total-1-min_rates_stoch; // starting index for the calculation of all bars
      Kperiod0=KperiodShort;
      Kperiod1=KperiodShort;
     }
   else limit=rates_total-prev_calculated; // starting index for the calculation of new bars 

//---- indexing elements in arrays as timeseries  
   ArraySetAsSeries(close,true);
   ArraySetAsSeries(high,true);
   ArraySetAsSeries(low,true);

   maxbar=rates_total-1-min_rates_stoch;

//---- Main calculation loop of the indicator
   for(bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      Stoch[bar]=Stoch(Kperiod0,Kperiod1,Slowing,PriceField,sens,bar,low,high,close);
      //----
      XStoch[bar]=XMA.XMASeries(maxbar,prev_calculated,rates_total,DMethod,DPhase,Dperiod,Stoch[bar],bar,true);
      
      // switching direction
      if(XStoch[bar+1]>OverBought)
        { // uptrend
         Kperiod0=KperiodShort;
         Kperiod1=KperiodLong;
        }
        
      if(XStoch[bar+1]<OverSold)
        { // downtrend
         Kperiod0=KperiodLong;
         Kperiod1=KperiodShort;
        }
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
//| calculation of the stochastic provided by noise-cancelling       |
//+------------------------------------------------------------------+    
double Stoch(int Kperiod0,int Kperiod1,int Slowing_,int PriceField_,double sens_,int Bar,
             const double &Low[],const double &High[],const double &Close[])
  {
//----
   double max,min,c,delta,diff;

   c=0.0;
   max=0.0;
   min=0.0;
   
   for(int j=Bar; j<Bar+Slowing_; j++)
     {
      if(PriceField_==STO_CLOSECLOSE)
        {
         max+=Close[ArrayMaximum(Close,j,Kperiod0)];
         min+=Close[ArrayMinimum(Close,j,Kperiod1)];
        }

      if(PriceField_==STO_LOWHIGH)
        {
         max+=High[ArrayMaximum(High,j,Kperiod0)];
         min+=Low[ArrayMinimum(Low,j,Kperiod1)];
        }

      c+=Close[j];
     }

// noise-cancelling
   sens_*=Slowing_; // setting sensitivity according to a slowing period
   delta=max-min; // range
   diff=sens-delta; // the difference between the sensitivity limit and a range\

   if(diff>0)
     { // in case the difference >0 (a range value is less than the limit), then
      delta=sens; // a range = the limit,
      min-=diff/2; // new minimum value
     }
// calculation of the oscillator
   if(delta) return(100*(c-min)/delta); // stochastic
//----
   return(-2);
  }
//+------------------------------------------------------------------+
