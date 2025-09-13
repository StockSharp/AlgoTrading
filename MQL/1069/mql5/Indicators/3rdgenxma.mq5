/*
 * The operation of the indicator requires
 * SmoothAlgorithms.mqh
 * to be placed in the directory: MetaTrader\\MQL5\Include
 */
//+------------------------------------------------------------------+
//|                                                    3rdGenXMA.mq5 | 
//|                                      Copyright © 2011, EarnForex |
//|                                        http://www.earnforex.com/ |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2011, EarnForex"
#property link      "http://www.earnforex.com"
//---- indicator version number
#property version   "1.01"
//---- indicator description
#property description "3rd Generation MA based on research paper by Dr. Manfred"
#property description "Durschner: http://www.vtad.de/node/1441 (in German)."
#property description "Offers least possible lag but still provides price smoothing."
//---- drawing the indicator in the main window
#property indicator_chart_window 
//---- number of indicator buffers
#property indicator_buffers 1 
//---- only one plot is used
#property indicator_plots   1
//+-----------------------------------+
//|  Indicator drawing parameters   |
//+-----------------------------------+
//---- drawing the indicator as a line
#property indicator_type1   DRAW_LINE
//---- DeepPink color is used as the indicator line color
#property indicator_color1 clrDeepPink
//---- the indicator line is a continuous curve
#property indicator_style1  STYLE_SOLID
//---- the indicator line width is 3
#property indicator_width1  3
//---- displaying the indicator label
#property indicator_label1  "3rdGenXMA"

//+-----------------------------------+
//|  CXMA class description            |
//+-----------------------------------+
#include <SmoothAlgorithms.mqh> 
//+-----------------------------------+

//---- declaration of the CXMA class variables from SmoothAlgorithms.mqh
CXMA XMA1,XMA2;
//+-----------------------------------+
//|  declaration of enumerations          |
//+-----------------------------------+
enum Applied_price_ //Type of constant
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
//|  INDICATOR INPUT PARAMETERS     |
//+-----------------------------------+
input Smooth_Method XMA_Method=MODE_EMA; //smoothing method
input int XLength=50; //smoothing depth                    
input int XPhase=15; //smoothing parameter,
                     //for JJMA, it varies within the range -100 ... +100 and influences the quality of the transient process;
// For VIDIA, it is a CMO period, for AMA, it is a slow moving average period
input Applied_price_ IPC=PRICE_TYPICAL;//price constant
/* , used for the indicator calculation ( 1-CLOSE, 2-OPEN, 3-HIGH, 4-LOW, 
  5-MEDIAN, 6-TYPICAL, 7-WEIGHTED, 8-SIMPL, 9-QUARTER, 10-TRENDFOLLOW, 11-0.5 * TRENDFOLLOW.) */
input int Shift=0; // horizontal shift of the indicator in bars
input int PriceShift=0; // vertical shift of the indicator in points
//+-----------------------------------+

//---- declaration of a dynamic array that will further 
// be used as an indicator buffer
double LineBuffer[];
//----
double Lambda,Alpha;
int MA_Sampling_Period;
//---- Declaration of the variable value of the vertical shift of the moving average
double dPriceShift;
//---- Declaration of integer variables of data starting point
int min_rates_,min_rates_total;
//+------------------------------------------------------------------+   
//| X2MA indicator initialization function                           | 
//+------------------------------------------------------------------+ 
void OnInit()
  {
//---- setting alerts for invalid values of external parameters
   XMA1.XMALengthCheck("XLength",XLength);
//---- setting alerts for invalid values of external parameters
   XMA1.XMAPhaseCheck("XPhase",XPhase,XMA_Method);

//---- Initialization of the vertical shift
   dPriceShift=_Point*PriceShift;

//---- Initialization of variables
   MA_Sampling_Period=2*XLength;
   Lambda= 1.0 * MA_Sampling_Period/(1.0 * XLength);
   Alpha = Lambda *(MA_Sampling_Period-1)/(MA_Sampling_Period-Lambda);

//---- Initialization of variables of data starting point
   min_rates_=XMA1.GetStartBars(XMA_Method,MA_Sampling_Period,XPhase);
   min_rates_total=min_rates_+XMA1.GetStartBars(XMA_Method,XLength,XPhase);

//---- setting dynamic array as indicator buffer
   SetIndexBuffer(0,LineBuffer,INDICATOR_DATA);
//---- shifting the indicator 1 horizontally
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//---- shifting the starting point of the indicator drawing
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- setting the indicator values that will be invisible on the chart
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,0);

//---- initialization of a variable for a short name of the indicator
   string shortname;
   string Smooth=XMA1.GetString_MA_Method(XMA_Method);
   StringConcatenate(shortname,"3rdGenXMA(",Smooth,", ",XLength,", ",XPhase,", ",Shift,", ",PriceShift,")");
//--- creating a name to be displayed in a separate subwindow and in a tooltip
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);

//--- determining the accuracy of displaying the indicator values
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits+1);
//---- end of initialization
  }
//+------------------------------------------------------------------+ 
//| X2MA iteration function                                          | 
//+------------------------------------------------------------------+ 
int OnCalculate(
                const int rates_total,    // history in bars at the current tick
                const int prev_calculated,// history in bars at the previous tick
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
   if(rates_total<min_rates_total) return(0);

//---- Declaration of variables with a floating point  
   double price_,x1xma,x2xma;
//---- Declaration of integer variables and getting the bars already calculated
   int first,bar;

//---- calculation of the starting number first for the bar recalculation loop
   if(prev_calculated>rates_total || prev_calculated<=0) // checking for the first start of the indicator calculation
      first=0; // starting number for calculation of all bars
   else first=prev_calculated-1; // starting number for calculation of new bars

//---- Main indicator calculation loop
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      //---- Calling the PriceSeries function to get the input price price_
      price_=PriceSeries(IPC,bar,open,low,high,close);

      //---- Two calls of the XMASeries function. 
      //The 'begin' parameter in the second call is increased by min_rates_ as this is a repeated XMA smoothing  
      x1xma = XMA1.XMASeries(0, prev_calculated, rates_total,XMA_Method,XPhase,MA_Sampling_Period,price_,bar,false);
      x2xma = XMA2.XMASeries(min_rates_, prev_calculated,rates_total,XMA_Method,XPhase,XLength,x1xma,bar,false);
      LineBuffer[bar]=(Alpha+1)*x1xma-Alpha*x2xma+dPriceShift;
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
