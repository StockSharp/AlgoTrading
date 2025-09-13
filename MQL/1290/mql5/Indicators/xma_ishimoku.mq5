//+------------------------------------------------------------------+
//|                                                      ProjectName |
//|                                      Copyright 2011, CompanyName |
//|                                       http://www.companyname.net |
//+------------------------------------------------------------------+
/*
 * For the indicator to work, place the
 * SmoothAlgorithms.mqh
 * in the directory: MetaTrader\\MQL5\Include
 */
//+------------------------------------------------------------------+
//|                                                 XMA_Ishimoku.mq5 | 
//|                                        Copyright © 2010, ellizii | 
//|                                                                  | 
//+------------------------------------------------------------------+
#property copyright "Copyright © 2010, ellizii"
#property link ""
#property description "Ishimoku XMA"
//---- indicator version number
#property version   "1.00"
//---- drawing the indicator in the main window
#property indicator_chart_window 
//---- //---- number of indicator buffers 1
#property indicator_buffers 1 
//---- only 1 graphical plot is used
#property indicator_plots   1  
//+-----------------------------------+
//|  Indicator drawing parameters     |
//+-----------------------------------+
//---- drawing the indicator as a line
#property indicator_type1   DRAW_LINE
//---- blue color is used for the indicator line
#property indicator_color1 Blue
//---- the indicator line is a dash-dotted curve
#property indicator_style1  STYLE_SOLID
//---- Indicator line width is equal to 2
#property indicator_width1  2
//---- displaying the indicator label
#property indicator_label1  "Ishimoku XMA"

//+-----------------------------------+
//|  Averaging classes description    |
//+-----------------------------------+
#include <SmoothAlgorithms.mqh> 
//+-----------------------------------+

//---- declaration of the CXMA classes variables from the SmoothAlgorithms.mqh file
CXMA XMA1;
//+-----------------------------------+
//|  Declaration of enumerations      |
//+-----------------------------------+
enum MODE_PRICE //Type of constant
  {
   OPEN = 0,     //By open prices
   LOW,          //By lows
   HIGH,         //By highs
   CLOSE         //By close prices
  };
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
input uint Up_period=3; //high price calculation period
input uint Dn_period=3; //low price calculation period
//---- 
input MODE_PRICE Up_mode=HIGH;  //Highs searching timeseries 
input MODE_PRICE Dn_mode=LOW;   //Lows searching timeseries 
//---- 
input Smooth_Method XMA_Method=MODE_SMA; //smoothing method
input int XLength=8; //smoothing depth                    
input int XPhase=15; //smoothing parameter,
                     // for JJMA that can change withing the range -100 ... +100. It impacts the quality of the intermediate process of smoothing;
// For VIDIA, it is a CMO period, for AMA, it is a slow moving average period
//---- 
input int Shift=0; // horizontal shift of the indicator in bars
input int PriceShift=0; // vertical shift of the indicator in pointsõ
//+-----------------------------------+

//---- declaration of a dynamic array that further 
// will be used as an indicator buffer
double XMA[];

//---- Declaration of the average vertical shift value variable
double dPriceShift;
//---- Declaration of integer variables of data starting point
int StartBars,StartBars1;
//+------------------------------------------------------------------+   
//| Ishimoku XMA indicator initialization function                   | 
//+------------------------------------------------------------------+ 
void OnInit()
  {
//---- Initialization of variables of the start of data calculation

   StartBars1=int(MathMax(Up_period,Dn_period));
   StartBars=StartBars1+XMA1.GetStartBars(XMA_Method,XLength,XPhase);

//---- setting alerts for invalid values of external parameters
   XMA1.XMALengthCheck("XLength", XLength);
   XMA1.XMAPhaseCheck("XPhase", XPhase, XMA_Method);

//---- Initialization of the vertical shift
   dPriceShift=_Point*PriceShift;

//---- set dynamic array as an indicator buffer
   SetIndexBuffer(0,XMA,INDICATOR_DATA);
//---- moving the indicator 1 horizontally
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//---- performing the shift of beginning of indicator drawing
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,StartBars);
//---- setting the indicator values that won't be visible on a chart
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//---- indexing elements in the buffer as time series
   ArraySetAsSeries(XMA,true);

//---- initializations of variable for indicator short name
   string shortname;
   string Smooth=XMA1.GetString_MA_Method(XMA_Method);
   StringConcatenate(shortname,"Ishimoku XMA(",XLength,", ",XPhase,", ",Smooth,")");
//--- creation of the name to be displayed in a separate sub-window and in a pop up help
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);

//--- determining the accuracy of displaying the indicator values
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits);
//---- end of initialization
  }
//+------------------------------------------------------------------+
//| Searching for highs                                              |
//+------------------------------------------------------------------+
int FindMaximum
(
 const double &Open[],
 const double &High[],
 const double &Low[],
 const double &Close[],
 MODE_PRICE Mode,
 uint index,
 uint period
 )
// FindMaximum(open,high,low,close,Up_mode,bar,Up_period)
  {
//----
   int max;
   int Mode_=int(Mode);

   switch(Mode_)
     {
      case OPEN: max=ArrayMaximum(Open,index,period); break;
      case LOW: max=ArrayMaximum(Low,index,period); break;
      case HIGH: max=ArrayMaximum(High,index,period); break;
      case CLOSE: max=ArrayMaximum(Close,index,period); break;
     }

//----
   return(max);
  }
//+------------------------------------------------------------------+
//| Searching for lows                                               |
//+------------------------------------------------------------------+
int FindMinimum
(
 const double &Open[],
 const double &High[],
 const double &Low[],
 const double &Close[],
 MODE_PRICE Mode,
 uint index,
 uint period
 )
// FindMinimum(open,high,low,close,Dn_mode,bar,Dn_period)
  {
//----
   int min;
   int Mode_=int(Mode);

   switch(Mode_)
     {
      case OPEN: min=ArrayMinimum(Open,index,period); break;
      case LOW: min=ArrayMinimum(Low,index,period); break;
      case HIGH: min=ArrayMinimum(High,index,period); break;
      case CLOSE: min=ArrayMinimum(Close,index,period); break;
     }

//----
   return(min);
  }
//+------------------------------------------------------------------+ 
//| Ishimoku XMA iteration function                                  | 
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
   double ish_Up,ish_Dn;
//---- Declaration of integer variables
   int limit,maxbar;

   maxbar=rates_total-1-StartBars1;
//---- calculation of the starting number limit for the bar recalculation loop
   if(prev_calculated>rates_total || prev_calculated<=0)// checking for the first start of the indicator calculation
      limit=maxbar; // starting index for calculation of all bars
   else limit=rates_total-prev_calculated;  // starting index for calculation of new bars only

//---- indexing elements in arrays as timeseries  
   ArraySetAsSeries(open,true);
   ArraySetAsSeries(low,true);
   ArraySetAsSeries(high,true);
   ArraySetAsSeries(close,true);

//---- main indicator calculation loop
   for(int bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      ish_Up=high[FindMaximum(open,high,low,close,Up_mode,bar,Up_period)];
      ish_Dn=low[FindMinimum(open,high,low,close,Dn_mode,bar,Dn_period)];
      XMA[bar]=XMA1.XMASeries(maxbar,prev_calculated,rates_total,XMA_Method,XPhase,XLength,(ish_Up+ish_Dn)/2,bar,true)+PriceShift;
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
