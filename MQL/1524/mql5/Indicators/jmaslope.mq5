//+------------------------------------------------------------------+
//|                                                     JMASlope.mq5 |
//|                    Copyright © 2005, Weld & TrendLaboratory Ltd. |
//|            http://finance.groups.yahoo.com/group/TrendLaboratory |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2005, Weld & TrendLaboratory Ltd."
#property link      "http://finance.groups.yahoo.com/group/TrendLaboratory"
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
//---- drawing the indicator as a four-color histogram
#property indicator_type1 DRAW_COLOR_HISTOGRAM
//---- colors of the five-color histogram are as follows
#property indicator_color1 clrMagenta,clrPurple,clrGray,clrBlue,clrDodgerBlue
//---- indicator line is a solid one
#property indicator_style1 STYLE_SOLID
//---- Indicator line width is equal to 2
#property indicator_width1 2
//---- displaying the indicator label
#property indicator_label1 "JMASlope"
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
   PRICE_TRENDFOLLOW1_,  //TrendFollow_2 Price 
   PRICE_DEMARK_         //Demark Price
  };
//+-----------------------------------+
//|  INDICATOR INPUT PARAMETERS       |
//+-----------------------------------+
input int jLength=14; // depth of smoothing                   
input int jPhase=0; // smoothing parameter,
                      //that changes within the range -100 ... +100,
//depends of the quality of the transitional prices;
input Applied_price_ IPC=PRICE_CLOSE_;//price constant
/* , used for calculation of the indicator ( 1-CLOSE, 2-OPEN, 3-HIGH, 4-LOW, 
  5-MEDIAN, 6-TYPICAL, 7-WEIGHTED, 8-SIMPL, 9-QUARTER, 10-TRENDFOLLOW, 11-0.5 * TRENDFOLLOW.) */
input int Shift=0; // horizontal shift of the indicator in bars
//+-----------------------------------+
//---- Declaration of integer variables of data starting point
int min_rates_total;
//---- declaration of dynamic arrays that will further be 
// used as indicator buffers
double IndBuffer[],ColorIndBuffer[];
//+------------------------------------------------------------------+
// The iPriceSeries() function description                           |
// iPriceSeriesAlert() function description                          |
// CJJMA class description                                           |
//+------------------------------------------------------------------+ 
#include <SmoothAlgorithms.mqh> 
//+------------------------------------------------------------------+    
//| JMASlope indicator initialization function                       | 
//+------------------------------------------------------------------+  
void OnInit()
  {
//---- Initialization of variables of the start of data calculation
   min_rates_total=30+2;

//---- set IndBuffer dynamic array as an indicator buffer
   SetIndexBuffer(0,IndBuffer,INDICATOR_DATA);
//---- performing the shift of beginning of indicator drawing
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- setting the indicator values that won't be visible on a chart
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,0.0);
//---- shifting the indicator horizontally by Shift
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);

//---- setting dynamic array as a color index buffer   
   SetIndexBuffer(1,ColorIndBuffer,INDICATOR_COLOR_INDEX);

//---- declaration of variable of the class CJJMA from the file JJMASeries_Cls.mqh
   CJJMA JMA;
//---- setting alerts for invalid values of external parameters
   JMA.JJMALengthCheck("jLength",jLength);
//---- setting alerts for invalid values of external parameters
   JMA.JJMAPhaseCheck("jPhase",jPhase);

//--- creation of the name to be displayed in a separate sub-window and in a pop up help
   IndicatorSetString(INDICATOR_SHORTNAME,"JMASlope");
//--- determining the accuracy of displaying the indicator values
   IndicatorSetInteger(INDICATOR_DIGITS,0);
//---- end of initialization
  }
//+------------------------------------------------------------------+  
//| JMASlope iteration function                                      | 
//+------------------------------------------------------------------+  
int OnCalculate(
                const int rates_total,    // amount of history in bars at the current tick
                const int prev_calculated,// amount of history in bars at the previous tick
                const datetime &time[],
                const double &open[],
                const double& high[],     // price array of maximums of price for the calculation of indicator
                const double& low[],      // price array of price lows for the indicator calculation
                const double &close[],
                const long &tick_volume[],
                const long &volume[],
                const int &spread[]
                )
  {
//---- Checking if the number of bars is sufficient for the calculation
   if(rates_total<min_rates_total) return(0);

//---- Declaration of integer variables
   int first,bar;
//---- declaration of variables with a floating point  
   double series,j1jma,djjma;
   static double prev_j1jma;

   if(prev_calculated>rates_total || prev_calculated<=0) // checking for the first start of calculation of an indicator
     {
      first=1; // starting number for calculation of all bars
      prev_j1jma=0;
     }
   else first=prev_calculated-1; // starting number for calculation of new bars
   
//---- declaration of variable of the class JJMA from the file JJMASeries_Cls.mqh
   static CJJMA JMA;

//---- Main calculation loop of the indicator
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {      
      //---- calling the PriceSeries function to get the 'Series' input price
      series=PriceSeries(IPC,bar,open,low,high,close);

      //---- One call of the JJMASeries function. 
      //The parameters Phase_ and Length_ don't change at every bar (Din = 0) 
      j1jma=JMA.JJMASeries(1,prev_calculated,rates_total,0,jPhase,jLength,series,bar,false);
      
      djjma=(j1jma-prev_j1jma)/_Point;
      IndBuffer[bar]=djjma;
      
      if(bar!=rates_total-1) prev_j1jma=j1jma; 
      
      ColorIndBuffer[bar]=2;

      if(djjma>0)
        {
         if(djjma>IndBuffer[bar-1]) ColorIndBuffer[bar]=4;
         if(djjma<IndBuffer[bar-1]) ColorIndBuffer[bar]=3;
        }

      if(djjma<0)
        {
         if(djjma<IndBuffer[bar-1]) ColorIndBuffer[bar]=0;
         if(djjma>IndBuffer[bar-1]) ColorIndBuffer[bar]=1;
        }
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
