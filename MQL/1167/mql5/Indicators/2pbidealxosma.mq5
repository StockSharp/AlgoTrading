//+---------------------------------------------------------------------+ 
//|                                                   2pbIdealXOSMA.mq5 | 
//|                                  Copyright © 2012, Nikolay Kositsin | 
//|                                 Khabarovsk,   farria@mail.redcom.ru | 
//+---------------------------------------------------------------------+ 
//| For the indicator to work, place the file SmoothAlgorithms.mqh      |
//| in the directory: terminal_data_folder\MQL5\Include                 |
//+---------------------------------------------------------------------+
#property copyright "Copyright © 2012, Nikolay Kositsin"
#property link "farria@mail.redcom.ru" 
//---- indicator version number
#property version   "1.01"
//---- drawing indicator in a separate window
#property indicator_separate_window 
//---- number of indicator buffers 2
#property indicator_buffers 2 
//---- only one plot is used
#property indicator_plots   1
//+-----------------------------------+
//|  Parameters of indicator drawing  |
//+-----------------------------------+
//---- drawing the indicator as a four-color histogram
#property indicator_type1 DRAW_COLOR_HISTOGRAM
//---- the following colors are used in the four color histogram
#property indicator_color1 Gray,OliveDrab,DodgerBlue,DeepPink,Magenta
//---- Indicator line is a solid one
#property indicator_style1 STYLE_SOLID
//---- Indicator line width is equal to 2
#property indicator_width1 2
//---- displaying the indicator label
#property indicator_label1 "2pbIdealXOSMA"

//+-----------------------------------+
//|  Averagings classes description   |
//+-----------------------------------+
#include <SmoothAlgorithms.mqh> 
//+-----------------------------------+

//---- declaration of the CXMA class variables from the SmoothAlgorithms.mqh file
CXMA XMA1;
//+-----------------------------------+
//|  Declaration of enumerations      |
//+-----------------------------------+
/*enum Smooth_Method - the enumeration is declared in the SmoothAlgorithms.mqh file
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
//|  Input parameters of the indicator|
//+-----------------------------------+
input int Period1 = 10; //rough smoothing
input int Period2 = 10; //adjusting smoothing

input int PeriodX1 = 10; //first rough smoothing
input int PeriodX2 = 10; //first adjusting smoothing
input int PeriodY1 = 10; //second rough smoothing
input int PeriodY2 = 10; //second adjusting smoothing
input int PeriodZ1 = 10; //third rough smoothing
input int PeriodZ2 = 10; //third adjusting smoothing

input Smooth_Method SmoothMethod=MODE_JJMA; //smoothing method
input int Smooth_XMA=9; //smoothing period
input int Smooth_Phase=100;   //moving averages smoothing parameter,
                       // for JJMA that can change withing the range -100 ... +100. It impacts the quality of the intermediate process of smoothing;
// for VIDIA it is a CMO period, for AMA it is a slow average period
//+-----------------------------------+
//---- declaration of the integer variables for the start of data calculation
int min_rates_total;
//---- declaration of dynamic arrays that further 
//---- will be used as indicator buffers
double IndBuffer[],ColorIndBuffer[];
//---- declarations of variables for smoothing constants
double w1,w2,wX1,wX2,wY1,wY2,wZ1,wZ2;
//---- declarations of variables for storing smoothing results
double Moving0_,Moving01_,Moving11_,Moving21_;
//+------------------------------------------------------------------+
// The iPriceSeries function description                             |
// Moving_Average class description                                  | 
//+------------------------------------------------------------------+ 
#include <SmoothAlgorithms.mqh>
//+------------------------------------------------------------------+
//|  Smoothing from Neutron                                          |
//+------------------------------------------------------------------+
double GetIdealMASmooth
(
 double W1_,//first smoothing constant
 double W2_,//second smoothing constant
 double Series1,//the value of the time series from the current bar 
 double Series0,//the value of the time series from the previous bar 
 double Resalt1 //the value of the moving from the previous bar
 )
  {
//----
   double Resalt0,dSeries,dSeries2;
   dSeries=Series0-Series1;
   dSeries2=dSeries*dSeries-1.0;

   Resalt0=(W1_ *(Series0-Resalt1)+
            Resalt1+W2_*Resalt1*dSeries2)
   /(1.0+W2_*dSeries2);
//----
   return(Resalt0);
  }
//+------------------------------------------------------------------+    
//| 2pbIdealXOSMA indicator initialization function                  | 
//+------------------------------------------------------------------+  
void OnInit()
  {
//---- Initialization of variables of the start of data calculation
   min_rates_total=2+XMA1.GetStartBars(SmoothMethod,Smooth_XMA,Smooth_Phase);

//---- initializations of variables
   w1=1.0/Period1;
   w2=1.0/Period2;
   wX1=1.0/PeriodX1;
   wX2=1.0/PeriodX2;
   wY1=1.0/PeriodY1;
   wY2=1.0/PeriodY2;
   wZ1=1.0/PeriodZ1;
   wZ2=1.0/PeriodZ2;

//---- set IndBuffer dynamic array as an indicator buffer
   SetIndexBuffer(0,IndBuffer,INDICATOR_DATA);
//---- performing the shift of beginning of indicator drawing
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//--- create a label to display in DataWindow
   PlotIndexSetString(0,PLOT_LABEL,"Ind");
//---- setting the indicator values that won't be visible on a chart
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,0.0);

//---- set dynamic array as a color index buffer   
   SetIndexBuffer(1,ColorIndBuffer,INDICATOR_COLOR_INDEX);

//---- setting up alerts for unacceptable values of external parameters
   XMA1.XMALengthCheck("Smooth_XMA",Smooth_XMA);
//---- setting up alerts for unacceptable values of external parameters
   XMA1.XMAPhaseCheck("Smooth_Phase",Smooth_Phase,SmoothMethod);

//--- creation of the name to be displayed in a separate sub-window and in a pop up help
   IndicatorSetString(INDICATOR_SHORTNAME,"2pbIdealXOSMA");
//---- determination of accuracy of displaying the indicator values
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits+1);
//---- end of initialization
  }
//+------------------------------------------------------------------+  
//| 2pbIdealXOSMA iteration function                                 | 
//+------------------------------------------------------------------+  
int OnCalculate(
                const int rates_total,     // number of bars in history at the current tick
                const int prev_calculated, // number of bars in history at the previous tick
                const int begin,           // number of beginning of reliable counting of bars
                const double &price[]      // price array for calculation of the indicator
                )
  {
//---- Checking the number of bars to be enough for calculation
   if(rates_total<min_rates_total+begin) return(0);

//---- declaration of local variables 
   int first,bar;
   double Moving0,SlowMA,FastMA,macd;
   double Moving00,Moving10,Moving20;
   double Moving01,Moving11,Moving21;

//---- calculation of the starting number 'first' for the cycle of recalculation of bars
   if(prev_calculated>rates_total || prev_calculated<=0) // checking for the first start of calculation of an indicator
     {
      first=1+begin;  // starting number for calculation of all bars
      //---- increase the position of the beginning of data by 'begin' bars as a result of calculation using data of another indicator
      if(begin>0) PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total+begin);

      //---- the starting initialization  
      Moving0_=price[begin];
      Moving01_=price[begin];
      Moving11_=price[begin];
      Moving21_=price[begin];
     }
   else first=prev_calculated-1; // starting number for calculation of new bars

//---- restore values of the variables
   Moving0=Moving0_;
   Moving01=Moving01_;
   Moving11=Moving11_;
   Moving21=Moving21_;

//---- Main cycle of calculation of the indicator
   for(bar=first; bar<rates_total; bar++)
     {
      Moving0=GetIdealMASmooth(w1,w2,price[bar-1],price[bar],Moving0);
      Moving00=GetIdealMASmooth(wX1,wX2,price[bar-1],price[bar],Moving01);
      Moving10=GetIdealMASmooth(wY1,wY2,Moving01,    Moving00,  Moving11);
      Moving20=GetIdealMASmooth(wZ1,wZ2,Moving11,    Moving10,  Moving21);
      //----                       
      Moving01 = Moving00;
      Moving11 = Moving10;
      Moving21 = Moving20;

      SlowMA=Moving20;
      FastMA=Moving0;
      macd=FastMA-SlowMA;
      IndBuffer[bar]=XMA1.XMASeries(1+begin,prev_calculated,rates_total,SmoothMethod,Smooth_Phase,Smooth_XMA,macd,bar,false);

      if(bar==rates_total-2)
        {
         Moving0_=Moving0;
         Moving01_=Moving01;
         Moving11_=Moving11;
         Moving21_=Moving21;
        }
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
