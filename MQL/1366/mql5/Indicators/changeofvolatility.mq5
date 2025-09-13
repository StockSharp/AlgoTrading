//+------------------------------------------------------------------+
//|                                           ChangeOfVolatility.mq5 |
//|                      Copyright © 2007, MetaQuotes Software Corp. |
//|                                        http://www.metaquotes.ru/ |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2007, MetaQuotes Software Corp."
#property link      "http://www.metaquotes.ru/"
//---- indicator version number
#property version   "1.00"
//---- drawing indicator in a separate window
#property indicator_separate_window 
//---- number of indicator buffers 2
#property indicator_buffers 2 
//---- only one plot is used
#property indicator_plots   1
//+-----------------------------------+
//|  Indicator drawing parameters     |
//+-----------------------------------+
//---- drawing the indicator as a histogram
#property indicator_type1 DRAW_COLOR_HISTOGRAM
//---- the following colors are used for the indicator diagram
#property indicator_color1 Gray,Purple,Red,Gold
//---- the indicator line is a continuous curve
#property indicator_style1 STYLE_SOLID
//---- Indicator line width is equal to 2
#property indicator_width1 2
//---- displaying the indicator label
#property indicator_label1 "Change of Volatility"
//+-----------------------------------+
//|  Declaration of constants         |
//+-----------------------------------+
#define RESET 0 // The constant for getting the command for the indicator recalculation back to the terminal
//+-----------------------------------+
//|  Indicator input parameters       |
//+-----------------------------------+
input uint MPeriod = 1;                              // Momentum period
input uint Short=6;
input uint Long=100;
input uint MaxTrendLevel=80;                         // Maximum trend level
input uint MiddLeTrendLevel=50;                      // Middle trend level
input uint FlatLevel=30;                             // Flat level
//+-----------------------------------+
//---- indicator buffers
double StdDevBuffer[],ColorBuffer[];

//---- declaration of the integer variables for the start of data calculation
int min_rates_total;
//+------------------------------------------------------------------+
// The iPriceSeries function description                             |
// Description of the function iPriceSeriesAlert                     |
// Описание классов CMomentum и CStdDeviation                        |
//+------------------------------------------------------------------+ 
#include <SmoothAlgorithms.mqh> 
//+------------------------------------------------------------------+    
//| StdDev indicator initialization function                         | 
//+------------------------------------------------------------------+  
void OnInit()
  {
//---- initialization of variables of the start of data calculation
   min_rates_total=int(MPeriod+MathMax(Short,Long));
   
//---- set StdDevBuffer dynamic array as an indicator buffer
   SetIndexBuffer(0,StdDevBuffer,INDICATOR_DATA);
//---- performing the shift of beginning of indicator drawing
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- setting the indicator values that won't be visible on a chart
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,EMPTY_VALUE);

//---- set dynamic array as a color index buffer   
   SetIndexBuffer(1,ColorBuffer,INDICATOR_COLOR_INDEX);
//---- performing the shift of beginning of indicator drawing
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,min_rates_total);
   
//---- initializations of variable for indicator short name
   string shortname;  
   StringConcatenate(shortname,"Change of Volatility( ",MPeriod,", ",Short,", ",Long," )");
//--- creation of the name to be displayed in a separate sub-window and in a pop up help
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
//--- determining the accuracy of displaying the indicator values
   IndicatorSetInteger(INDICATOR_DIGITS,0);
   
//---- the number of the indicator 3 horizontal levels   
   IndicatorSetInteger(INDICATOR_LEVELS,3);
//---- values of the indicator horizontal levels   
   IndicatorSetDouble(INDICATOR_LEVELVALUE,0,MaxTrendLevel);
   IndicatorSetDouble(INDICATOR_LEVELVALUE,1,MiddLeTrendLevel);
   IndicatorSetDouble(INDICATOR_LEVELVALUE,2,FlatLevel);
//---- gray and magenta colors are used for horizontal levels lines  
   IndicatorSetInteger(INDICATOR_LEVELCOLOR,0,Magenta);
   IndicatorSetInteger(INDICATOR_LEVELCOLOR,1,Blue);
   IndicatorSetInteger(INDICATOR_LEVELCOLOR,2,Gray);
//---- short dot-dash is used for the horizontal level line  
   IndicatorSetInteger(INDICATOR_LEVELSTYLE,0,STYLE_DASHDOTDOT);
   IndicatorSetInteger(INDICATOR_LEVELSTYLE,1,STYLE_DASHDOTDOT);
   IndicatorSetInteger(INDICATOR_LEVELSTYLE,2,STYLE_DASHDOTDOT);
//---- end of initialization
  }
//+------------------------------------------------------------------+  
//| StdDev iteration function                                        | 
//+------------------------------------------------------------------+  
int OnCalculate(const int rates_total,    // number of bars in history at the current tick
                const int prev_calculated,// amount of history in bars at the previous tick
                const datetime &time[],
                const double &open[],
                const double &high[],
                const double &low[],
                const double &close[],
                const long &tick_volume[],
                const long &volume[],
                const int &spread[])
  {
//---- checking for the sufficiency of bars for the calculation
   if(rates_total<min_rates_total) return(RESET);

//---- declaration of integer variables
   int first,bar;
//---- declaration of variables with a floating point  
   double stdl,stds,smal,smas,momentum;

//---- calculation of the starting number first for the bar recalculation loop
   if(prev_calculated==0) // checking for the first start of the indicator calculation
      first=0; // starting number for calculation of all bars
   else first=prev_calculated-1; // starting number for calculation of new bars

//---- declaration of variables of the classes CMomentum and CStdDeviation from the file SmoothAlgorithms.mqh
   static CMomentum Mom;
   static CStdDeviation STDL,STDS;
   static CMoving_Average SMAL,SMAS;

//---- Main calculation loop of the indicator
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      momentum=Mom.MomentumSeries(0,prev_calculated,rates_total,MPeriod,close[bar],bar,false);
      momentum/=_Point;    
       
      smal=SMAL.SMASeries(MPeriod,prev_calculated,rates_total,Long,momentum,bar,false);      
      stdl=STDL.StdDevSeries(MPeriod,prev_calculated,rates_total,Long,2,momentum,smal,bar,false);    
      smas=SMAS.SMASeries(MPeriod,prev_calculated,rates_total,Short,momentum,bar,false);      
      stds=STDS.StdDevSeries(MPeriod,prev_calculated,rates_total,Short,2,momentum,smas,bar,false);
            
      if(stdl) StdDevBuffer[bar]=100*stds/stdl;
      else StdDevBuffer[bar]=0.0;
      
      ColorBuffer[bar]=0;     
      if(StdDevBuffer[bar]>MaxTrendLevel) ColorBuffer[bar]=3;
      else if(StdDevBuffer[bar]>MiddLeTrendLevel) ColorBuffer[bar]=2;
      else if(StdDevBuffer[bar]>FlatLevel) ColorBuffer[bar]=1;
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
