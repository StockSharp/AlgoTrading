//+------------------------------------------------------------------+
//|                                         XMA_Ichimoku_Channel.mq5 |
//|                                        Copyright © 2010, ellizii |
//+------------------------------------------------------------------+
//| Place the SmoothAlgorithms.mqh file                              |
//| to the terminal_data_folder\MQL5\Include                         |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2010, ellizii"
#property link ""
#property description "XMA Ichimoku Channel"
//---- indicator version number
#property version   "1.01"
//---- drawing the indicator in the main window
#property indicator_chart_window 
//---- number of indicator buffers 3
#property indicator_buffers 3 
//---- 3 plots are used
#property indicator_plots   3
//+-----------------------------------+
//|  Indicator drawing parameters     |
//+-----------------------------------+
//---- drawing the indicator as a line
#property indicator_type1   DRAW_LINE
//---- blue color is used for the indicator line
#property indicator_color1 Blue
//---- the indicator line is a dash-dotted curve
#property indicator_style1  STYLE_DASHDOTDOT
//---- indicator line width is equal to 1
#property indicator_width1  1
//---- displaying the indicator label
#property indicator_label1  "Ichimoku XMA"
//+--------------------------------------------+
//|  Levels drawing parameters                 |
//+--------------------------------------------+
//---- drawing the levels as lines
#property indicator_type2   DRAW_LINE
#property indicator_type3   DRAW_LINE
//---- selection of levels colors
#property indicator_color2  MediumSeaGreen
#property indicator_color3  Red
//---- levels are dott-dash curves
#property indicator_style2 STYLE_SOLID
#property indicator_style3 STYLE_SOLID
//---- levels width is equal to 1
#property indicator_width2  1
#property indicator_width3  1
//---- display levels labels
#property indicator_label2  "Up Ichimoku Convert"
#property indicator_label3  "Down Ichimoku Convert"
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
input int XLength=100; //smoothing depth                    
input int XPhase=15; //smoothing parameter,
                     // for JJMA that can change withing the range -100 ... +100. It impacts the quality of the intermediate process of smoothing;
// For VIDIA, it is a CMO period, for AMA, it is a slow moving average period

input double Up_percent = 1.0; //percentage of the deviation from the average for building the channel upper border
input double Dn_percent = 1.0; //percentage of the deviation from the average for building the channel lower border
//---- 
input int Shift=0; // horizontal shift of the indicator in bars
input int PriceShift=0; // vertical shift of the indicator in pointsõ
//+-----------------------------------+

//---- declaration of a dynamic array that further 
// will be used as an indicator buffer
double XMA[];

//---- declaration of dynamic arrays that will further be 
//---- will be used as Bollinger Bands indicator buffers
double UpLineBuffer[],DnLineBuffer[];

//---- Declaration of the average vertical shift value variable
double dPriceShift;
//---- Declaration of integer variables of data starting point
int min_rates_total,StartBars1;
//+------------------------------------------------------------------+   
//| XMA_Ishimoku_Channel indicator initialization function           | 
//+------------------------------------------------------------------+ 
void OnInit()
  {
//---- Initialization of variables of the start of data calculation

   StartBars1=int(MathMax(Up_period,Dn_period));
   min_rates_total=StartBars1+XMA1.GetStartBars(XMA_Method,XLength,XPhase);

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
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- setting the indicator values that won't be visible on a chart
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//---- indexing elements in the buffer as time series
   ArraySetAsSeries(XMA,true);

//---- setting dynamic arrays as indicator buffers
   SetIndexBuffer(1,UpLineBuffer,INDICATOR_DATA);
   SetIndexBuffer(2,DnLineBuffer,INDICATOR_DATA);
//---- set the position, from which the Bollinger Bands drawing starts
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,min_rates_total);
   PlotIndexSetInteger(2,PLOT_DRAW_BEGIN,min_rates_total);
//---- restriction to draw empty values for the indicator
   PlotIndexSetDouble(1,PLOT_EMPTY_VALUE,EMPTY_VALUE);
   PlotIndexSetDouble(2,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//---- horizontal shift of the indicator
   PlotIndexSetInteger(1,PLOT_SHIFT,Shift);
   PlotIndexSetInteger(2,PLOT_SHIFT,Shift);
//---- indexing elements in the buffer as time series
   ArraySetAsSeries(UpLineBuffer,true);
   ArraySetAsSeries(DnLineBuffer,true);

//---- initializations of variable for indicator short name
   string shortname;
   string Smooth=XMA1.GetString_MA_Method(XMA_Method);
   StringConcatenate(shortname,"XMA Ishimoku Convert Channel(",XLength,", ",XPhase,", ",Smooth,")");
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
   int max=0;
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
   int min=0;
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
//| XMA_Ishimoku_Channel iteration function                          | 
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
   if(rates_total<min_rates_total) return(0);

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
      UpLineBuffer[bar]=(1+(Up_percent/100))*XMA[bar];
      DnLineBuffer[bar]=(1-(Dn_percent/100))*XMA[bar];
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
