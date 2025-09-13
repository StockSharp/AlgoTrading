//+---------------------------------------------------------------------+ 
//|                                                           OsHMA.mq4 |
//|                                            Copyright © 2009, sealdo |
//|                                                    sealdo@yandex.ru |
//+---------------------------------------------------------------------+ 
//| For the indicator to work, place the file SmoothAlgorithms.mqh      |
//| in the directory: terminal_data_folder\MQL5\Include                 |
//+---------------------------------------------------------------------+
#property  copyright "Copyright © 2009, sealdo"
#property  link      "sealdo@yandex.ru" 
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
//---- colors of the four-color histogram are as follows
#property indicator_color1 clrGray,clrBlue,clrDodgerBlue,clrDarkOrange,clrMagenta
//---- indicator line is a solid one
#property indicator_style1 STYLE_SOLID
//---- Indicator line width is equal to 2
#property indicator_width1 2
//---- displaying the indicator label
#property indicator_label1 "OsHMA"
//+-----------------------------------+
//|  INDICATOR INPUT PARAMETERS       |
//+-----------------------------------+
input int FastHMA=13; // Period of fast HMA
input int SlowHMA=26; // period of slow MA
//+-----------------------------------+
//---- Declaration of integer variables of data starting point
int min_rates_total;
//---- declaration of dynamic arrays that will further be 
// used as indicator buffers
double IndBuffer[],ColorIndBuffer[];
//---- Declaration of integer variables
int fHma2_Period,fSqrt_Period,sHma2_Period,sSqrt_Period;
//+------------------------------------------------------------------+    
//| OsHMA indicator initialization function                          | 
//+------------------------------------------------------------------+  
void OnInit()
  {
//---- Initialization of variables
   fHma2_Period=int(MathFloor(FastHMA/2));
   sHma2_Period=int(MathFloor(SlowHMA/2));
   fSqrt_Period=int(MathFloor(MathSqrt(FastHMA)));
   sSqrt_Period=int(MathFloor(MathSqrt(SlowHMA)));

//---- Initialization of variables of the start of data calculation
   min_rates_total=MathMax(fHma2_Period+fSqrt_Period,sHma2_Period+sSqrt_Period);

//---- set IndBuffer dynamic array as an indicator buffer
   SetIndexBuffer(0,IndBuffer,INDICATOR_DATA);
//---- performing the shift of beginning of indicator drawing
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//--- create a label to display in DataWindow
   PlotIndexSetString(0,PLOT_LABEL,"Ind");
//---- setting the indicator values that won't be visible on a chart
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,0.0);

//---- setting dynamic array as a color index buffer   
   SetIndexBuffer(1,ColorIndBuffer,INDICATOR_COLOR_INDEX);

//---- name for the data window and the label for sub-windows 
   string short_name="OsHMA";
   IndicatorSetString(INDICATOR_SHORTNAME,short_name+"("+string(FastHMA)+","+string(SlowHMA)+")");
//--- determining the accuracy of displaying the indicator values
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits+1);
//---- end of initialization
  }
//+------------------------------------------------------------------+
// CMoving_Average class description                                 | 
//+------------------------------------------------------------------+  
#include <SmoothAlgorithms.mqh>
//+------------------------------------------------------------------+  
//| OsHMA iteration function                                         | 
//+------------------------------------------------------------------+  
int OnCalculate(
                const int rates_total,     // amount of history in bars at the current tick
                const int prev_calculated, // amount of history in bars at the previous tick
                const int begin,           // number of beginning of reliable counting of bars
                const double &price[]      // price array for calculation of the indicatorà
                )
  {
//---- checking the number of bars to be enough for calculation
   if(rates_total<min_rates_total+begin) return(0);

//---- declaration of local variables 
   int first,bar;
   double lwma1,lwma2,fhma,shma,series;
   static uint fbegin,sbegin;

//---- calculation of the starting number first for the bar recalculation loop
   if(prev_calculated==0) // checking for the first start of the indicator calculation
     {
      first=begin; // starting number for calculation of all bars
      int minbar=min_rates_total+begin;  
      PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,minbar);
      for(bar=0; bar<=minbar; bar++) IndBuffer[bar]=0;
      fbegin=FastHMA+1+begin;
      sbegin=SlowHMA+1+begin;
     }
   else first=prev_calculated-1; // starting number for calculation of new bars

//---- declaration of variable of the CMoving_Average class from the HMASeries_Cls.mqh file
   static CMoving_Average MA1,MA2,MA3,MA4,MA5,MA6;

//---- main indicator calculation loop
   for(bar=first; bar<rates_total; bar++)
     {
      series=price[bar];

      lwma1=MA1.LWMASeries(begin,prev_calculated,rates_total,fHma2_Period,series,bar,false);
      lwma2=MA2.LWMASeries(begin,prev_calculated,rates_total,FastHMA,series,bar,false);
      fhma=MA3.LWMASeries(fbegin,prev_calculated,rates_total,fSqrt_Period,2*lwma1-lwma2,bar,false);
      //----
      lwma1=MA4.LWMASeries(begin,prev_calculated,rates_total,sHma2_Period,series,bar,false);
      lwma2=MA5.LWMASeries(begin,prev_calculated,rates_total,SlowHMA,series,bar,false);
      shma=MA6.LWMASeries(sbegin,prev_calculated,rates_total,sSqrt_Period,2*lwma1-lwma2,bar,false);
      //----
      IndBuffer[bar]=fhma-shma;
     }

   if(prev_calculated>rates_total || prev_calculated<=0) first++;
//---- main loop of the Ind indicator coloring
   for(bar=first; bar<rates_total; bar++)
     {
      ColorIndBuffer[bar]=0;

      if(IndBuffer[bar]>0)
        {
         if(IndBuffer[bar]>IndBuffer[bar-1]) ColorIndBuffer[bar]=1;
         if(IndBuffer[bar]<IndBuffer[bar-1]) ColorIndBuffer[bar]=2;
        }

      if(IndBuffer[bar]<0)
        {
         if(IndBuffer[bar]<IndBuffer[bar-1]) ColorIndBuffer[bar]=3;
         if(IndBuffer[bar]>IndBuffer[bar-1]) ColorIndBuffer[bar]=4;
        }
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
