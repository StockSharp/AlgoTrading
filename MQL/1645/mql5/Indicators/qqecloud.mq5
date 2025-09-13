//+---------------------------------------------------------------------+
//|	     	                        				           QQECloud.mq5 |
//|                                         Copyright © 2010, EarnForex |
//|                                            http://www.earnforex.com |
//|                                Based on version by Tim Hyder (2008) |
//|                            Based on version by Roman Ignatov (2006) |
//+---------------------------------------------------------------------+ 
//| Place the SmoothAlgorithms.mqh file                                 |
//| in the directory: terminal_data_folder\MQL5\Include                 |
//+---------------------------------------------------------------------+
#property copyright "www.EarnForex.com, 2010"
#property link      "http://www.earnforex.com"
#property version   "1.1"
#property description "QQE - Qualitative Quantitative Estimation."
#property description "Calculated as two indicators:"
#property description "1) MA on RSI"
#property description "2) Difference of MA on RSI and MA of MA of ATR of MA of RSI"
#property description "The signal for buy is when blue line crosses level 50 from below"
#property description "after crossing the yellow line from below."
#property description "The signal for sell is when blue line crosses level 50 from above"
#property description "after crossing the yellow line from above." 
//---- indicator version number
#property version   "1.00"
//---- drawing indicator in a separate window
#property indicator_separate_window
//---- two buffers are used for the indicator calculation and drawing
#property indicator_buffers 2
//---- one plot is used
#property indicator_plots   1
//+----------------------------------------------+
//|  Bullish indicator drawing parameters        |
//+----------------------------------------------+
//---- drawing the indicator 1 as a cloud
#property indicator_type1   DRAW_FILLING
//---- the following colors are used for the indicator
#property indicator_color1  clrBlueViolet,clrRed
//---- displaying of the bullish label of the indicator
#property indicator_label1  "QQE Up Trend; QQE Down Trend"

//+----------------------------------------------+
//| Parameters of displaying horizontal levels   |
//+----------------------------------------------+
#property indicator_level1 60.0
#property indicator_level2 50.0
#property indicator_level3 40.0
#property indicator_levelcolor clrTeal
#property indicator_levelstyle STYLE_DASHDOTDOT

//+----------------------------------------------+
//|  declaring constants                         |
//+----------------------------------------------+
#define RESET 0 // The constant for returning the indicator recalculation command to the terminal
//+----------------------------------------------+
//|  CXMA class description                      |
//+----------------------------------------------+
#include <SmoothAlgorithms.mqh> 
//+----------------------------------------------+

//---- declaration of the CXMA class variables from the SmoothAlgorithms.mqh file
CXMA XMA1,XMA2,XMA3;
//+----------------------------------------------+
//|  declaration of enumerations                 |
//+----------------------------------------------+
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
//+----------------------------------------------+
//|  INDICATOR INPUT PARAMETERS                  |
//+----------------------------------------------+
input uint RSI_Period=14;
input uint SF=5;
input double DARFACTOR=4.236;
input Smooth_Method XMA_Method=MODE_SMA; // Averaging method        
input int XPhase=15; //smoothing parameter,
                     //for JJMA, it varies within the range -100 ... +100 and influences on the quality of the transient period;
// For VIDIA, it is a CMO period, for AMA, it is a slow moving average period
input int Shift=0; // horizontal shift of the indicator in bars
//+----------------------------------------------+

//---- declaration of dynamic arrays that will further be 
// used as indicator buffers
double IndBuffer1[];
double IndBuffer2[];
//---- Declaration of integer variables for the indicator handles
int RSI_Handle;
//---- Declaration of integer variables of data starting point
int min_rates_total,min_rates_1,min_rates_2,min_rates_3,Wilders_Period;
//+------------------------------------------------------------------+   
//| QQEA indicator initialization function                           | 
//+------------------------------------------------------------------+ 
void OnInit()
  {
//---- Initialization of variables of data calculation starting point
   Wilders_Period=int(RSI_Period*2-1);
   min_rates_1=int(RSI_Period+1);
   min_rates_2=min_rates_1+XMA1.GetStartBars(XMA_Method,SF,XPhase);
   min_rates_3=min_rates_2+XMA1.GetStartBars(XMA_Method,Wilders_Period,XPhase);
   min_rates_total=min_rates_3+XMA1.GetStartBars(XMA_Method,Wilders_Period,XPhase);

//---- getting handle of the MA indicator
   RSI_Handle=iRSI(NULL,0,RSI_Period,PRICE_CLOSE);
   if(RSI_Handle==INVALID_HANDLE) Print(" Failed to get handle of the RSI indicator");

//---- set dynamic array as an indicator buffer
   SetIndexBuffer(0,IndBuffer1,INDICATOR_DATA);
//---- set dynamic array as an indicator buffer
   SetIndexBuffer(1,IndBuffer2,INDICATOR_DATA);
   
//---- shifting the indicator 1 horizontally
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//---- shifting the starting point of the indicator drawing
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- setting values of the indicator that won't be visible on a chart
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,EMPTY_VALUE);

//---- initializations of variable for indicator short name
   string shortname="Qualitative Quantitative Estimation";
//--- creation of the name to be displayed in a separate sub-window and in a pop up help
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);

//--- determination of accuracy of displaying the indicator values
   IndicatorSetInteger(INDICATOR_DIGITS,0);
//---- end of initialization
  }
//+------------------------------------------------------------------+ 
//| QQEA iteration function                                          | 
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
//---- checking for the sufficiency of the number of bars for the calculation
   if(BarsCalculated(RSI_Handle)<rates_total || rates_total<min_rates_total) return(RESET);

//---- Declaration of variables with a floating point  
   double RSI[1],xrsi,momxrsi,xmomxrsi,xxmomxrsi,dar,tr,dv;
//---- Declaration of integer variables and getting the bars already calculated
   int first,bar;
   static double prev_xrsi;

//---- calculation of the starting number first for the bar recalculation loop
   if(prev_calculated>rates_total || prev_calculated<=0) //checking for the first start of calculation of an indicator
     {
      first=1; // starting number for calculation of all bars
      prev_xrsi=50;
     }
   else first=prev_calculated-1; // starting index for the calculation of new bars

//---- Main calculation loop of the indicator
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      if(CopyBuffer(RSI_Handle,0,rates_total-1-bar,1,RSI)<=0) return(RESET);
      xrsi=XMA1.XMASeries(min_rates_1,prev_calculated,rates_total,XMA_Method,XPhase,SF,RSI[0],bar,false);
      momxrsi=MathAbs(prev_xrsi-xrsi);
      xmomxrsi=XMA2.XMASeries(min_rates_2,prev_calculated,rates_total,XMA_Method,XPhase,Wilders_Period,momxrsi,bar,false);
      xxmomxrsi=XMA3.XMASeries(min_rates_3,prev_calculated,rates_total,XMA_Method,XPhase,Wilders_Period,xmomxrsi,bar,false);
      dar=xxmomxrsi*DARFACTOR;
      //----       
      tr=IndBuffer2[bar-1];
      dv=tr;

      if(xrsi<tr)
        {
         tr=xrsi+dar;
         if(prev_xrsi<dv && tr>dv) tr=dv;
        }
      else if(xrsi>tr)
        {
         tr=xrsi-dar;
         if(prev_xrsi>dv && tr<dv) tr=dv;
        }

      IndBuffer2[bar]=tr;
      IndBuffer1[bar]=xrsi;
      
      if(bar<rates_total-1) prev_xrsi=xrsi;
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
