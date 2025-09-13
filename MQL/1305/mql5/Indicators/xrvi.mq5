//+------------------------------------------------------------------+
//|                                                         XRVI.mq5 | 
//|                             Copyright © 2011,   Nikolay Kositsin | 
//|                              Khabarovsk,   farria@mail.redcom.ru | 
//+------------------------------------------------------------------+
/*
 * For the indicator to work, place the
 * SmoothAlgorithms.mqh
 * in the directory: MetaTrader\\MQL5\Include
 */

#property copyright "Copyright © 2011, Nikolay Kositsin"
#property link "farria@mail.redcom.ru"
//---- indicator version number
#property version   "1.01"
//---- drawing indicator in a separate window
#property indicator_separate_window 
//---- number of indicator buffers
#property indicator_buffers 2 
//---- only two plots are used
#property indicator_plots   2
//+---------------------------------------+
//|  Indicator XRVI drawing parameters    |
//+---------------------------------------+
//---- drawing the indicator as a line
#property indicator_type1   DRAW_LINE
//---- red color is used as the color of the indicator line
#property indicator_color1 Red
//---- the indicator line is a dot-dash one
#property indicator_style1  STYLE_DASHDOTDOT
//---- indicator line width is equal to 1
#property indicator_width1  1
//---- displaying the indicator label
#property indicator_label1  "XRVI"

//+---------------------------------------+
//|  Signal line drawing parameter        |
//+---------------------------------------+
//---- drawing the indicator as a line
#property indicator_type2   DRAW_LINE
//---- blue-violet color is used as the color of the indicator line
#property indicator_color2 BlueViolet
//---- the indicator line is a continuous curve
#property indicator_style2  STYLE_SOLID
//---- Indicator line width is equal to 2
#property indicator_width2  2
//---- displaying the indicator label
#property indicator_label2  "Signal line"

//+-----------------------------------+
//|  CXMA class description           |
//+-----------------------------------+
#include <SmoothAlgorithms.mqh> 
//+-----------------------------------+

//---- declaration of the CXMA class variables from the SmoothAlgorithms.mqh file
CXMA XMA1,XMA2;
//+-----------------------------------+
//|  Declaration of enumerations      |
//+-----------------------------------+
enum Applied_price_ //Type od constant
  {
   PRICE_CLOSE_ = 1,     //Close
   PRICE_OPEN_,          //Open
   PRICE_HIGH_,          //High
   PRICE_LOW_,           //Low
   PRICE_MEDIAN_,        //Median Price (HL/2)
   PRICE_TYPICAL_,       //Typical Price (HLC/3)
   PRICE_WEIGHTED_,      //Weighted Close (HLCC/4)
   PRICE_SIMPL_,         //Simpl Price (OC/2)
   PRICE_QUARTER_,       //Quarted Price (HLOC/4) 
   PRICE_TRENDFOLLOW0_,  //TrendFollow_1 Price 
   PRICE_TRENDFOLLOW1_   //TrendFollow_2 Price 
  };
/*enum Smooth_Method - enumeration is declared in SmoothAlgorithms.mqh
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
//|  INDICATOR INPUT PARAMETERS       |
//+-----------------------------------+
input Smooth_Method RviMethod=MODE_JurX; //first smoothing averaging method 
input int RviPeriod=10; //first smoothing depth                    
input int RviPhase=15; //first smoothing parameter,
  // for JJMA that can change withing the range -100 ... +100. It impacts the quality of the intermediate process of smoothing;
  // For VIDIA, it is a CMO period, for AMA, it is a slow moving average period
input Smooth_Method SignMethod=MODE_JurX; //second smoothing averaging method 
input int SignPeriod = 5; //second smoothing depth 
input int SignPhase=15;  //second smoothing parameter,
  // for JJMA that can change withing the range -100 ... +100. It impacts the quality of the intermediate process of smoothing;
  // For VIDIA, it is a CMO period, for AMA, it is a slow moving average period
input Applied_price_ IPC=PRICE_CLOSE;//price constant
/* , used for calculation of the indicator ( 1-CLOSE, 2-OPEN, 3-HIGH, 4-LOW, 
  5-MEDIAN, 6-TYPICAL, 7-WEIGHTED, 8-SIMPL, 9-QUARTER, 10-TRENDFOLLOW, 11-0.5 * TRENDFOLLOW.) */
input int Shift=0; // horizontal shift of the indicator in bars
//+-----------------------------------+

//---- declaration of dynamic arrays that will further be 
// used as indicator buffers
double XRVI[],SIGN[];

//---- Declaration of integer variables of data starting point
int StartBars,StartBars1,StartBars2;
//+------------------------------------------------------------------+   
//| XRVI indicator initialization function                           | 
//+------------------------------------------------------------------+ 
void OnInit()
  {
//---- Initialization of variables of the start of data calculation
   StartBars1=XMA1.GetStartBars(RviMethod, RviPeriod, RviPhase);
   StartBars2=XMA2.GetStartBars(SignMethod, SignPeriod, SignPhase);
   StartBars=StartBars1+StartBars2;
//---- setting alerts for invalid values of external parameters
   XMA1.XMALengthCheck("RviPeriod", RviPeriod);
   XMA2.XMALengthCheck("SignPeriod", SignPeriod);
//---- setting alerts for invalid values of external parameters
   XMA1.XMAPhaseCheck("RviPhase", RviPhase, RviMethod);
   XMA2.XMAPhaseCheck("SignPhase", SignPhase, SignMethod);
   
//---- set dynamic array as an indicator buffer
   SetIndexBuffer(0,XRVI,INDICATOR_DATA);
//---- moving the indicator 1 horizontally
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//---- performing the shift of beginning of indicator drawing
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,StartBars);
//---- setting the indicator values that won't be visible on a chart
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,0);
   
//---- set dynamic array as an indicator buffer
   SetIndexBuffer(1,SIGN,INDICATOR_DATA);
//---- moving the indicator 1 horizontally
   PlotIndexSetInteger(1,PLOT_SHIFT,Shift);
//---- performing the shift of beginning of indicator drawing
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,StartBars);
//---- setting the indicator values that won't be visible on a chart
   PlotIndexSetDouble(1,PLOT_EMPTY_VALUE,0);
   
//---- initializations of variable for indicator short name
   string shortname;
   string Smooth1=XMA1.GetString_MA_Method(RviMethod);
   string Smooth2=XMA1.GetString_MA_Method(SignMethod);
   StringConcatenate(shortname,"XRVI(",RviPeriod,", ",SignPeriod,", ",Smooth1,", ",Smooth2,")");
//--- creation of the name to be displayed in a separate sub-window and in a pop up help
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
   
//--- determining the accuracy of displaying the indicator values
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits+1);
//---- end of initialization
  }
//+------------------------------------------------------------------+ 
//| XRVI iteration function                                          | 
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
   if(rates_total<StartBars) return(0);

//---- declaration of variables with a floating point  
   double rvi,xrvi,signal;
//---- Declaration of integer variables and getting the bars already calculated
   int first,bar;

//---- calculation of the starting number first for the bar recalculation loop
   if(prev_calculated>rates_total || prev_calculated<=0) // checking for the first start of calculation of an indicator
      first=0; // starting number for calculation of all bars
   else first=prev_calculated-1; // starting number for calculation of new bars

//---- Main calculation loop of the indicator
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      //---- ðàñ÷¸ò RVI
      if(high[bar]-low[bar]!=0) rvi=(close[bar]-open[bar])/(high[bar]-low[bar]);
      else rvi = 0.0;

      //---- Two calls of the XMASeries function. 
      //---- The 'begin' parameter is increased by StartBars1 in the second call, as ê. another XMA smoothing  
      xrvi = XMA1.XMASeries( 0, prev_calculated, rates_total, RviMethod, RviPhase, RviPeriod, rvi, bar, false);
      signal = XMA2.XMASeries(StartBars1, prev_calculated, rates_total, SignMethod, SignPhase, SignPeriod,  xrvi, bar, false);
      //----       
      XRVI[bar]=xrvi;
      SIGN[bar]=signal;
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
