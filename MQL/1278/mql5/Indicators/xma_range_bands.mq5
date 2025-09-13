/*
 * For the indicator to work, place the
 * SmoothAlgorithms.mqh
 * in the directory: MetaTrader\\MQL5\Include
 */
//+------------------------------------------------------------------+
//|                                              XMA_Range_Bands.mq4 |
//|                               Copyright © 2009, Daniel V. Bugaev |
//|                                         e-mail: buga1922@mail.ru |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2009, Daniel V. Bugaev"
#property link "mailto:buga1922@mail.ru"
#property description "XMA Range Bands"
//---- indicator version number
#property version   "1.00"
//---- drawing the indicator in the main window
#property indicator_chart_window 
//---- number of indicator buffers 3
#property indicator_buffers 3 
//---- only 5 graphical plots are used
#property indicator_plots   3
//+-----------------------------------+
//|  Indicator drawing parameters     |
//+-----------------------------------+
//---- drawing the indicator as a line
#property indicator_type1   DRAW_LINE
//---- SlateBlue color is used for indicator line
#property indicator_color1 clrSlateBlue
//---- the indicator line is a continuous curve
#property indicator_style1  STYLE_SOLID
//---- indicator line width is equal to 1
#property indicator_width1  1
//---- displaying the indicator label
#property indicator_label1  "XMA Range Bands"

//+--------------------------------------------+
//|  Levels indicator drawing parameters       |
//+--------------------------------------------+
//---- drawing the levels as lines
#property indicator_type2   DRAW_LINE
#property indicator_type3   DRAW_LINE
//---- selection of levels colors
#property indicator_color2  clrLimeGreen
#property indicator_color3  clrDeepPink
//---- levels are continuous curves
#property indicator_style2 STYLE_SOLID
#property indicator_style3 STYLE_SOLID
//---- levels width is equal to 1
#property indicator_width2  1
#property indicator_width3  1
//---- display levels labels
#property indicator_label2  "Upper XMA Range Bands"
#property indicator_label3  "Lower XMA Range Bands"

//+-----------------------------------+
//|  Averaging classes description    |
//+-----------------------------------+
#include <SmoothAlgorithms.mqh> 
//+-----------------------------------+

//---- declaration of the CXMA classes variables from the SmoothAlgorithms.mqh file
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
input Smooth_Method MA_Method1=MODE_SMA; //smoothing method of moving average
input int Length1=100; //smoothing depth of moving average                  
input int Phase1=15; //moving average smoothing parameter,
                     // for JJMA that can change withing the range -100 ... +100. It impacts the quality of the intermediate process of smoothing;
// For VIDIA, it is a CMO period, for AMA, it is a slow moving average period

input Smooth_Method MA_Method2=MODE_JJMA; //candles size smoothing method
input int Length2=20; //smoothing depth of candles size 
input int Phase2=100;  //candles size smoothing parameter,
                       // for JJMA that can change withing the range -100 ... +100. It impacts the quality of the intermediate process of smoothing;
// For VIDIA, it is a CMO period, for AMA, it is a slow moving average period

input double Deviation = 2.0; //channel expansion ratio

input Applied_price_ IPC=PRICE_CLOSE;//price constant
/* , used for calculation of the indicator ( 1-CLOSE, 2-OPEN, 3-HIGH, 4-LOW, 
  5-MEDIAN, 6-TYPICAL, 7-WEIGHTED, 8-SIMPL, 9-QUARTER, 10-TRENDFOLLOW, 11-0.5 * TRENDFOLLOW.) */
input int Shift=0; // horizontal shift of the indicator in bars
input int PriceShift=0; // vertical shift of the indicator in pointsõ
//+-----------------------------------+

//---- declaration of dynamic arrays that will further be 
//---- will be used as Bollinger Bands indicator buffers
double ExtLineBuffer0[],ExtLineBuffer1[],ExtLineBuffer2[];

//---- Declaration of the average vertical shift value variable
double dPriceShift;
//---- Declaration of integer variables of data starting point
int min_rates_total,min_rates_;
//+------------------------------------------------------------------+   
//| XMA_Range_Bands initialization function                          | 
//+------------------------------------------------------------------+ 
void OnInit()
  {
//---- Initialization of variables of the start of data calculation
   int min_rates_1=+XMA1.GetStartBars(MA_Method1, Length1, Phase1);
   int min_rates_2=XMA2.GetStartBars(MA_Method2, Length2, Phase2);
   min_rates_total=MathMax(min_rates_1,min_rates_2);

//---- setting alerts for invalid values of external parameters
   XMA1.XMALengthCheck("Length1", Length1);
   XMA2.XMALengthCheck("Length2", Length2);
//---- setting alerts for invalid values of external parameters
   XMA1.XMAPhaseCheck("Phase1", Phase1, MA_Method1);
   XMA2.XMAPhaseCheck("Phase2", Phase2, MA_Method2);

//---- Initialization of the vertical shift
   dPriceShift=_Point*PriceShift;

//---- set dynamic array as an indicator buffer
   SetIndexBuffer(0,ExtLineBuffer0,INDICATOR_DATA);
//---- moving the indicator 1 horizontally
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//---- performing the shift of beginning of indicator drawing
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_);
//---- setting the indicator values that won't be visible on a chart
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,EMPTY_VALUE);

//---- setting dynamic arrays as indicator buffers
   SetIndexBuffer(1,ExtLineBuffer1,INDICATOR_DATA);
   SetIndexBuffer(2,ExtLineBuffer2,INDICATOR_DATA);
//---- set the position, from which the Bollinger Bands drawing starts
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,min_rates_total);
   PlotIndexSetInteger(2,PLOT_DRAW_BEGIN,min_rates_total);

//---- restriction to draw empty values for the indicator
   PlotIndexSetDouble(1,PLOT_EMPTY_VALUE,EMPTY_VALUE);
   PlotIndexSetDouble(2,PLOT_EMPTY_VALUE,EMPTY_VALUE);

//--- creation of the name to be displayed in a separate sub-window and in a pop up help
   IndicatorSetString(INDICATOR_SHORTNAME,"XMA Range Bands");

//--- determining the accuracy of displaying the indicator values
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits+1);
//---- end of initialization
  }
//+------------------------------------------------------------------+ 
//| XMA_Range_Bands iteration function                               | 
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
   double price_,xma,xxma,range,xrange;
//---- Declaration of integer variables and getting the bars already calculated
   int first,bar;

//---- calculation of the starting number first for the bar recalculation loop
   if(prev_calculated>rates_total || prev_calculated<=0) // checking for the first start of calculation of an indicator
      first=0; // starting number for calculation of all bars
   else first=prev_calculated-1; // starting number for calculation of new bars

//---- Main calculation loop of the indicator
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      price_=PriceSeries(IPC,bar,open,low,high,close);
 
      xma = XMA1.XMASeries(0,prev_calculated,rates_total,MA_Method1,Phase1,Length1,price_,bar,false);
      
      range=high[bar]-low[bar];
      xrange=XMA2.XMASeries(0,prev_calculated,rates_total,MA_Method2,Phase2,Length2,range,bar,false);
      //---- 
      xxma=xma+dPriceShift;      
      ExtLineBuffer0[bar]=xxma;
      ExtLineBuffer1[bar]=xxma+xrange;
      ExtLineBuffer2[bar]=xxma-xrange;
     }
//----  
   return(rates_total);
  }
//+------------------------------------------------------------------+
