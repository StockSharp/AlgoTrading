/*
 * For the indicator to work, place the
 * SmoothAlgorithms.mqh
 * in the directory: MetaTrader\\MQL5\Include
 */
//+------------------------------------------------------------------+ 
//|                                           ColorNonLagDotMACD.mq5 | 
//|                               Copyright © 2011, Nikolay Kositsin | 
//|                              Khabarovsk,   farria@mail.redcom.ru | 
//+------------------------------------------------------------------+ 
#property copyright "Copyright © 2011, Nikolay Kositsin"
#property link "farria@mail.redcom.ru" 
//---- indicator version number
#property version   "1.00"
//---- drawing indicator in a separate window
#property indicator_separate_window 
//---- number of indicator buffers is 4
#property indicator_buffers 4 
//---- only two plots are used
#property indicator_plots   2
//+-----------------------------------+
//|  Declaration of constants         |
//+-----------------------------------+
#define RESET  0 // the constant for getting the command for the indicator recalculation back to the terminal
//+-----------------------------------+
//|  Indicator drawing parameters     |
//+-----------------------------------+
//---- drawing the indicator as a four-color histogram
#property indicator_type1 DRAW_COLOR_HISTOGRAM
//---- colors of the four-color histogram are as follows
#property indicator_color1 Gray,Lime,Blue,Red,Magenta
//---- indicator line is a solid one
#property indicator_style1 STYLE_SOLID
//---- Indicator line width is equal to 2
#property indicator_width1 2
//---- displaying the indicator label
#property indicator_label1 "NonLagDotMACD"

//---- drawing indicator as a three-colored line
#property indicator_type2 DRAW_COLOR_LINE
//---- colors of the three-color line are
#property indicator_color2 Gray,DodgerBlue,DarkOrange
//---- the indicator line is a dash-dotted curve
#property indicator_style2 STYLE_DASHDOTDOT
//---- the width of indicator line is 3
#property indicator_width2 3
//---- displaying label of the signal line
#property indicator_label2  "Signal Line"
//+-----------------------------------+
//|  INDICATOR INPUT PARAMETERS       |
//+-----------------------------------+
input ENUM_APPLIED_PRICE Price=PRICE_CLOSE;   // Applied price
input int                Filter= 0;
input double             Deviation=0;         // Deviation
//----
input ENUM_MA_METHOD     Fast_Type=MODE_SMA;  // Smoothing method
input int                Fast_Length_=12;     // period of the fast moving average
//----
input ENUM_MA_METHOD     Slow_Type=MODE_SMA;  // Smoothing method
input int                Slow_Length_=26;     // slow moving average period
//----
input int Signal_MA=9; //signal line period 
input ENUM_MA_METHOD Signal_Method_=MODE_EMA; //indicator smoothing method
//+-----------------------------------+

//---- Declaration of integer variables for the indicator handles
int Fast_Handle,Slow_Handle;
//---- Declaration of integer variables of data starting point
int start,macd_start=0;
//---- declaration of dynamic arrays that will further be 
// used as indicator buffers
double MACDBuffer[],SignBuffer[],ColorMACDBuffer[],ColorSignBuffer[];
//+------------------------------------------------------------------+
// The iPriceSeries function description                             |
// Moving_Average class description                                  | 
//+------------------------------------------------------------------+ 
#include <SmoothAlgorithms.mqh> 
//+------------------------------------------------------------------+    
//| MACD indicator initialization function                           | 
//+------------------------------------------------------------------+  
void OnInit()
  {
//---- Initialization of variables of the start of data calculation
   macd_start=6*MathMax(Fast_Length_,Slow_Length_);
   start=macd_start+Signal_MA+1;
   
//---- getting Fast NonLagDot indicator handle
   Fast_Handle=iCustom(NULL,0,"NonLagDot",Price,Fast_Type,Fast_Length_,Filter,Deviation,0);
   if(Fast_Handle==INVALID_HANDLE) Print(" Failed to get handle of Fast NonLagDot indicator");
   
//---- getting Slow NonLagDot indicator handle
   Slow_Handle=iCustom(NULL,0,"NonLagDot",Price,Slow_Type,Slow_Length_,Filter,Deviation,0);
   if(Slow_Handle==INVALID_HANDLE) Print(" Failed to get handle of Slow NonLagDot indicator");

//---- transformation of the dynamic array MACDBuffer into an indicator buffer
   SetIndexBuffer(0,MACDBuffer,INDICATOR_DATA);
//---- performing the shift of beginning of indicator drawing
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,macd_start);
//--- create a label to display in DataWindow
   PlotIndexSetString(0,PLOT_LABEL,"NonLagDotMACD");
//---- setting the indicator values that won't be visible on a chart
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,EMPTY_VALUE);

//---- setting dynamic array as a color index buffer   
   SetIndexBuffer(1,ColorMACDBuffer,INDICATOR_COLOR_INDEX);
//---- performing the shift of beginning of indicator drawing
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,macd_start+1);

//---- set SignBuffer dynamic array as an indicator buffer
   SetIndexBuffer(2,SignBuffer,INDICATOR_DATA);
//---- performing the shift of beginning of indicator drawing
   PlotIndexSetInteger(2,PLOT_DRAW_BEGIN,start);
//--- create a label to display in DataWindow
   PlotIndexSetString(2,PLOT_LABEL,"Signal SMA");
//---- setting the indicator values that won't be visible on a chart
   PlotIndexSetDouble(2,PLOT_EMPTY_VALUE,EMPTY_VALUE);

//---- setting dynamic array as a color index buffer   
   SetIndexBuffer(3,ColorSignBuffer,INDICATOR_COLOR_INDEX);
//---- performing the shift of beginning of indicator drawing
   PlotIndexSetInteger(3,PLOT_DRAW_BEGIN,start+1);

//---- initializations of variable for indicator short name
   string shortname;
   StringConcatenate(shortname,"NonLagDotMACD( ",Fast_Length_,", ",Slow_Length_,", ",Signal_MA," )");
//--- creation of the name to be displayed in a separate sub-window and in a pop up help
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
//--- determining the accuracy of displaying the indicator values
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits+1);
//---- end of initialization
  }
//+------------------------------------------------------------------+  
//| MACD iteration function                                          | 
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
   if(BarsCalculated(Fast_Handle)<rates_total
    || BarsCalculated(Slow_Handle)<rates_total
    || rates_total<start) return(RESET);

//---- Declaration of integer variables
   int first1,first2,first3,limit,bar;
//---- declaration of variables with a floating point  
   double Fast[],Slow[];
   
//---- indexing elements in arrays as timeseries  
   ArraySetAsSeries(Fast,true);
   ArraySetAsSeries(Slow,true);

//---- Initialization of the indicator in the OnCalculate() block
   if(prev_calculated>rates_total || prev_calculated<=0)// checking for the first start of the indicator calculation
     {
      first1=0; // starting number for the calculation of all bars in the first loop
      first2=macd_start+1; // starting number for the calculation of all bars in the second loop
      first3=start+1; // starting index for calculation of all third loop bars
      limit=rates_total-macd_start-1; // starting index for the calculation of all bars
     }
   else // starting number for the calculation of new bars
     {
      first1=prev_calculated-1;
      first2=first1;
      first3=first1;
      limit=rates_total-prev_calculated; // starting index for the calculation of new bars
     }
     
//---- copy newly appeared data into the arrays
   if(CopyBuffer(Fast_Handle,0,0,limit+1,Fast)<=0) return(RESET);
   if(CopyBuffer(Slow_Handle,0,0,limit+1,Slow)<=0) return(RESET);

//---- indexing elements in the buffer as time series
   ArraySetAsSeries(MACDBuffer,true);

//---- Main calculation loop of the indicator
   for(bar=limit; bar>=0 && !IsStopped(); bar--) MACDBuffer[bar]=Fast[bar]-Slow[bar];     

//---- indexing elements in the buffer not as time series
   ArraySetAsSeries(MACDBuffer,false);    
   
//---- declaration of the CMoving_Average class variables from the MASeries_Cls.mqh file
   static CMoving_Average MA; 
     
   for(bar=first1; bar<rates_total && !IsStopped(); bar++)
      SignBuffer[bar]=MA.MASeries(macd_start,prev_calculated,rates_total,Signal_MA,Signal_Method_,MACDBuffer[bar],bar,false);

//---- Main loop of the MACD indicator coloring
   for(bar=first2; bar<rates_total && !IsStopped(); bar++)
     {
      ColorMACDBuffer[bar]=0;

      if(MACDBuffer[bar]>0)
        {
         if(MACDBuffer[bar]>MACDBuffer[bar-1]) ColorMACDBuffer[bar]=1;
         if(MACDBuffer[bar]<MACDBuffer[bar-1]) ColorMACDBuffer[bar]=2;
        }

      if(MACDBuffer[bar]<0)
        {
         if(MACDBuffer[bar]<MACDBuffer[bar-1]) ColorMACDBuffer[bar]=3;
         if(MACDBuffer[bar]>MACDBuffer[bar-1]) ColorMACDBuffer[bar]=4;
        }
     }

//---- Main loop of the signal line coloring
   for(bar=first3; bar<rates_total && !IsStopped(); bar++)
     {
      ColorSignBuffer[bar]=0;
      if(MACDBuffer[bar]>SignBuffer[bar-1]) ColorSignBuffer[bar]=1;
      if(MACDBuffer[bar]<SignBuffer[bar-1]) ColorSignBuffer[bar]=2;
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
