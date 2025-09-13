/*
 * For the indicator to work, place the
 * SmoothAlgorithms.mqh
 * in the directory: MetaTrader\\MQL5\Include
 */
//+------------------------------------------------------------------+
//|                                                3XMA_Ishimoku.mq5 | 
//|                               Copyright © 2011, Nikolay Kositsin | 
//|                              Khabarovsk,   farria@mail.redcom.ru | 
//+------------------------------------------------------------------+ 
#property copyright "Copyright © 2011, Nikolay Kositsin"
#property link "farria@mail.redcom.ru" 
#property description "3Ishimoku XMA"
//---- indicator version number
#property version   "1.01"
//---- drawing the indicator in the main window
#property indicator_chart_window 
//---- number of indicator buffers 3
#property indicator_buffers 3 
//---- only 3 plots are used
#property indicator_plots   2
//+-----------------------------------+ 
//|  Declaration of constants         |
//+-----------------------------------+
#define RESET 0 // The constant for returning the indicator recalculation command to the terminal
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
//|  Indicator drawing parameters     |
//+-----------------------------------+
//---- drawing the indicator as a line
#property indicator_type2   DRAW_FILLING
//---- the following colors are used for the indicator line
#property indicator_color2 Lime,Red
//---- displaying the indicator label
#property indicator_label1  "Ishimoku Cloud"

//+-----------------------------------+
//|  Averaging classes description    |
//+-----------------------------------+
#include <SmoothAlgorithms.mqh> 
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
input uint Up_period1=3; //high price calculation period 1
input uint Dn_period1=3; //low price calculation period 1
//----
input uint Up_period2=6; //high price calculation period 2
input uint Dn_period2=6; //low price calculation period 2
//----
input uint Up_period3=9; //high price calculation period 3
input uint Dn_period3=9; //low price calculation period 3
//---- 
input MODE_PRICE Up_mode1=HIGH;  //highs searching timeseries 1 
input MODE_PRICE Dn_mode1=LOW;   //lows searching timeseries 1 
//---- 
input MODE_PRICE Up_mode2=HIGH;  //highs searching timeseries 2 
input MODE_PRICE Dn_mode2=LOW;   //lows searching timeseries 2 
//---- 
input MODE_PRICE Up_mode3=HIGH;  //highs searching timeseries 3 
input MODE_PRICE Dn_mode3=LOW;   //lows searching timeseries 3 
//---- 
input Smooth_Method XMA1_Method=MODE_SMA; //smoothing method 1
input Smooth_Method XMA2_Method=MODE_SMA; //smoothing method 2
input Smooth_Method XMA3_Method=MODE_SMA; //smoothing method 3
//----
input int XLength1=8; //smoothing depth 1 
input int XLength2=25; //smoothing depth 2
input int XLength3=80; //smoothing depth 3
//----                  
input int XPhase=15; //smoothing parameter,
                     // for JJMA that can change withing the range -100 ... +100. It impacts the quality of the intermediate process of smoothing;
// For VIDIA, it is a CMO period, for AMA, it is a slow moving average period
//---- 
input int Shift1=0; // horizontal shift of the indicator 1 in bars
input int Shift2=0; // horizontal shift of the indicator 2 in bars
input int Shift3=0; // horizontal shift of the indicator 3 in bars
//+-----------------------------------+

//---- declaration of a dynamic array that further 
// will be used as an indicator buffer
double XMA1[],XMA2[],XMA3[];

//---- Declaration of integer variables of data starting point
int StartBars;
//---- Declaration of integer variables for the indicator handles
int XMA1_Handle,XMA2_Handle,XMA3_Handle;
//+------------------------------------------------------------------+   
//| 3 Ishimoku XMA indicator initialization function                 | 
//+------------------------------------------------------------------+ 
void OnInit()
  {
//---- declaration of the CXMA classes variables from the SmoothAlgorithms.mqh file
   CXMA XMA;
//---- Initialization of variables of the start of data calculation
   int StartBars1=int(MathMax(Up_period1,Dn_period1));
   StartBars1+=XMA.GetStartBars(XMA1_Method,XLength1,XPhase);
   int StartBars2=int(MathMax(Up_period2,Dn_period2));
   StartBars1+=XMA.GetStartBars(XMA2_Method,XLength2,XPhase);
   int StartBars3=int(MathMax(Up_period3,Dn_period3));
   StartBars3+=XMA.GetStartBars(XMA3_Method,XLength3,XPhase);
   StartBars=MathMax(StartBars1,MathMax(StartBars2,StartBars3));
   
//---- getting handle of the XMA_Ishimoku 1 indicator
   XMA1_Handle=iCustom(Symbol(),PERIOD_CURRENT,"XMA_Ishimoku",
                  Up_period1,Dn_period1,Up_mode1,Dn_mode1,XMA1_Method,XLength1,XPhase,Shift1,0);
   if(XMA1_Handle==INVALID_HANDLE) Print(" Failed to get handle of the XMA_Ishimoku 1 indicator");
   
//---- getting handle of the XMA_Ishimoku 2 indicator
   XMA2_Handle=iCustom(Symbol(),PERIOD_CURRENT,"XMA_Ishimoku",
                  Up_period2,Dn_period2,Up_mode2,Dn_mode2,XMA2_Method,XLength2,XPhase,Shift2,0);
   if(XMA2_Handle==INVALID_HANDLE) Print(" Failed to get handle of the XMA_Ishimoku 2 indicator");
   
//---- getting handle of the XMA_Ishimoku 3 indicator
   XMA3_Handle=iCustom(Symbol(),PERIOD_CURRENT,"XMA_Ishimoku",
                   Up_period3,Dn_period3,Up_mode3,Dn_mode3,XMA3_Method,XLength3,XPhase,Shift3,0);
   if(XMA3_Handle==INVALID_HANDLE) Print(" Failed to get handle of the XMA_Ishimoku 3 indicator");

//---- set dynamic array as an indicator buffer
   SetIndexBuffer(0,XMA1,INDICATOR_DATA);
//---- moving the indicator 1 horizontally
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift1);
//---- performing the shift of beginning of indicator drawing
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,StartBars);
//---- setting the indicator values that won't be visible on a chart
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//---- indexing elements in the buffer as time series
   ArraySetAsSeries(XMA1,true);
   
//---- set dynamic array as an indicator buffer
   SetIndexBuffer(1,XMA2,INDICATOR_DATA);
//---- shifting the indicator 2 horizontally
   PlotIndexSetInteger(1,PLOT_SHIFT,Shift2);
//---- performing the shift of beginning of indicator drawing
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,StartBars);
//---- setting the indicator values that won't be visible on a chart
   PlotIndexSetDouble(1,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//---- indexing elements in the buffer as time series
   ArraySetAsSeries(XMA2,true);
   
//---- set dynamic array as an indicator buffer
   SetIndexBuffer(2,XMA3,INDICATOR_DATA);
//---- shifting the indicator 3 horizontally
   PlotIndexSetInteger(2,PLOT_SHIFT,Shift3);
//---- performing the shift of beginning of indicator drawing
   PlotIndexSetInteger(2,PLOT_DRAW_BEGIN,StartBars);
//---- setting the indicator values that won't be visible on a chart
   PlotIndexSetDouble(2,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//---- indexing elements in the buffer as time series
   ArraySetAsSeries(XMA3,true);

//---- initializations of variable for indicator short name
   string shortname="3 Ishimoku XMA";
//--- creation of the name to be displayed in a separate sub-window and in a pop up help
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);

//--- determining the accuracy of displaying the indicator values
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits);
//---- end of initialization
  }
//+------------------------------------------------------------------+ 
//| 3 Ishimoku XMA iteration function                                | 
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
//---- Checking if the number of bars is sufficient for the calculation
   if(BarsCalculated(XMA1_Handle)<rates_total
    || BarsCalculated(XMA2_Handle)<rates_total
    || BarsCalculated(XMA3_Handle)<rates_total
    || StartBars>rates_total) return(RESET);

//---- declaration of local variables 
   int to_copy;

//---- calculation of amount data to be copied
   if(prev_calculated>rates_total || prev_calculated<=0)
        to_copy=rates_total-1;
   else to_copy=rates_total-prev_calculated+1;

//---- copy newly appeared data into the arrays
   if(CopyBuffer(XMA1_Handle,0,0,to_copy,XMA1)<=0) return(RESET);
   if(CopyBuffer(XMA2_Handle,0,0,to_copy,XMA2)<=0) return(RESET);
   if(CopyBuffer(XMA3_Handle,0,0,to_copy,XMA3)<=0) return(RESET);
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
