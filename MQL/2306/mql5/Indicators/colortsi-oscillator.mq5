//+------------------------------------------------------------------+
//|                                          ColorTSI-Oscillator.mq5 |
//|                      Copyright © 2005, MetaQuotes Software Corp. |
//|                                        http://www.metaquotes.net |
//|                                                                  |
//|                                   Modified from TSI-Osc by Toshi |
//|                                  http://toshi52583.blogspot.com/ |
//+------------------------------------------------------------------+
//| Place the SmoothAlgorithms.mqh file                              |
//| to the directory: terminal_data_folder\\MQL5\Include             |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2005, MetaQuotes Software Corp."
#property link      "http://www.metaquotes.net"
//--- indicator version
#property version   "1.01"
//--- drawing the indicator in a separate window
#property indicator_separate_window
//--- number of indicator buffers is 2
#property indicator_buffers 2 
//--- one plot is used
#property indicator_plots   1
//+-----------------------------------+
//| Indicator 1 drawing parameters    |
//+-----------------------------------+
//--- indicator drawing style
#property indicator_type1   DRAW_FILLING
//--- dark orchid and deep pink colors are used for the indicator
#property indicator_color1 clrDarkOrchid,clrDeepPink
//--- displaying the indicator label
#property indicator_label1  "TSI-Oscillator"
//+----------------------------------------------+
//| Parameters of displaying horizontal levels   |
//+----------------------------------------------+
#property indicator_level1 +50.0
#property indicator_level2   0.0
#property indicator_level3 -50.0
#property indicator_levelcolor clrGray
#property indicator_levelstyle STYLE_DASHDOTDOT
//+-----------------------------------+
//| CMTM class description            |
//+-----------------------------------+
#include <SmoothAlgorithms.mqh> 
//+-----------------------------------+
//--- declaration of the CMTM class variables from the SmoothAlgorithms.mqh file
CXMA MTM1,MTM2,ABSMTM1,ABSMTM2;
//+-----------------------------------+
//| Declaration of enumerations       |
//+-----------------------------------+
enum Applied_price_      // type of constant
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
   PRICE_TRENDFOLLOW1_,  // TrendFollow_2 Price 
   PRICE_DEMARK_         // Demark Price
  };
//+-----------------------------------+
//| Declaration of enumerations       |
//+-----------------------------------+
/*enum Smooth_Method - enumeration is declared in SmoothAlgorithms.mqh
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
input Smooth_Method First_Method=MODE_SMA;  // Smoothing method 1
input int First_Length=12;                  // Smoothing depth 1
input int First_Phase=15;                   // Smoothing parameter 1
input Smooth_Method Second_Method=MODE_SMA; // Smoothing method 2
input int Second_Length=12;                 // Smoothing depth 2
input int Second_Phase=15;                  // Smoothing parameter 2
input Applied_price_ IPC=PRICE_CLOSE;       // Price constant
input int Shift=0;                          // Horizontal shift of the indicator in bars
input uint TriggerShift=1;                  // Bar shift for the trigger 
//+-----------------------------------+
//--- declaration of integer variables for the start of data calculation
int min_rates_total,min_rates_total1,min_rates_total2;
//--- declaration of dynamic arrays that
//--- will be used as indicator buffers
double TSIBuffer[],TriggerBuffer[];
//+------------------------------------------------------------------+   
//| Color TSI indicator initialization function                      |
//+------------------------------------------------------------------+ 
void OnInit()
  {
//--- initialization of variables of data calculation start
   min_rates_total1=MTM1.GetStartBars(First_Method,First_Length,First_Phase)+1;
   min_rates_total2=min_rates_total1+MTM1.GetStartBars(First_Method,First_Length,First_Phase);
   min_rates_total=int(min_rates_total1+MTM1.GetStartBars(Second_Method,Second_Length,Second_Phase)+TriggerShift);
//--- setting up alerts for unacceptable values of external variables
   MTM1.XMALengthCheck("First_Length",First_Length);
   MTM1.XMAPhaseCheck("First_Phase",First_Phase, First_Method);
   MTM1.XMALengthCheck("Second_Length",Second_Length);
   MTM1.XMAPhaseCheck("Second_Phase",Second_Phase,Second_Method);
//--- set the TSIBuffer[] dynamic array as an indicator buffer
   SetIndexBuffer(0,TSIBuffer,INDICATOR_DATA);
//--- shifting the indicator 1 horizontally
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//--- shifting the start of drawing of the indicator
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//--- setting the indicator values that won't be visible on a chart
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//--- set the TriggerBuffer[] dynamic array as an indicator buffer
   SetIndexBuffer(1,TriggerBuffer,INDICATOR_DATA);
//--- shifting the indicator 1 horizontally
   PlotIndexSetInteger(1,PLOT_SHIFT,Shift);
//--- shifting the start of drawing of the indicator
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,min_rates_total);
//--- setting the indicator values that won't be visible on a chart
   PlotIndexSetDouble(1,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//--- initializations of a variable for the indicator short name
   string shortname;
   string Smooth1=MTM1.GetString_MA_Method(First_Method);
   string Smooth2=MTM1.GetString_MA_Method(Second_Method);
   StringConcatenate(shortname,"TSI-Oscillator(",Smooth1,", ",First_Length,", ",Smooth2,", ",Second_Length,")");
//--- creation of the name to be displayed in a separate sub-window and in a pop up help
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
//--- determining the accuracy of the indicator values
   IndicatorSetInteger(INDICATOR_DIGITS,0);
//--- initialization end
  }
//+------------------------------------------------------------------+ 
//| TSI iteration function                                           | 
//+------------------------------------------------------------------+ 
int OnCalculate(const int rates_total,    // number of bars in history at the current tick
                const int prev_calculated,// amount of history in bars at the previous tick
                const int begin,          // number of beginning of reliable counting of bars
                const double &price[])    // price array for the indicator calculation
  {
//--- checking if the number of bars is enough for the calculation
   if(rates_total<min_rates_total+begin) return(0);
//--- declaration of variables with a floating point  
   double dprice,absdprice,mtm1,absmtm1,mtm2,absmtm2;
//--- declaration of integer variables and getting already calculated bars
   int first,bar;
//--- calculation of the 'first' starting index for the bars recalculation loop
   if(prev_calculated>rates_total || prev_calculated<=0) // checking for the first start of calculation of an indicator
      first=1;                   // starting index for calculation of all bars
   else first=prev_calculated-1; // starting number for calculation of new bars
//--- main indicator calculation loop
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      dprice=price[bar]-price[bar-1];
      absdprice=MathAbs(dprice);
      //---
      mtm1=MTM1.XMASeries(1,prev_calculated,rates_total,First_Method,First_Phase,First_Length,dprice,bar,false);
      absmtm1=ABSMTM1.XMASeries(1,prev_calculated,rates_total,First_Method,First_Phase,First_Length,absdprice,bar,false);
      //---
      mtm2=MTM2.XMASeries(min_rates_total1,prev_calculated,rates_total,Second_Method,Second_Phase,Second_Length,mtm1,bar,false);
      absmtm2=ABSMTM2.XMASeries(min_rates_total1,prev_calculated,rates_total,Second_Method,Second_Phase,Second_Length,absmtm1,bar,false);
      //---
      if(bar>min_rates_total2) TSIBuffer[bar]=100.0*mtm2/absmtm2;
      else TSIBuffer[bar]=EMPTY_VALUE;
      //---
      if(bar>min_rates_total) TriggerBuffer[bar]=TSIBuffer[bar-TriggerShift];
      else                    TriggerBuffer[bar]=EMPTY_VALUE;
     }
//---     
   return(rates_total);
  }
//+------------------------------------------------------------------+
