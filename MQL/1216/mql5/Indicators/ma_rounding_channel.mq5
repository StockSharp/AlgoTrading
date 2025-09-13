//+------------------------------------------------------------------+
//|                                          MA_Rounding_Channel.mq5 | 
//|                   Copyright © 2011, BACKSPACE + Nikolay Kositsin | 
//|                              Khabarovsk,   farria@mail.redcom.ru | 
//+------------------------------------------------------------------+
#property copyright "Copyright © 2011, BACKSPACE + Nikolay Kositsin"
#property link "farria@mail.redcom.ru"
//---- indicator version number
#property version   "1.00"
//---- drawing the indicator in the main window
#property indicator_chart_window 
//---- number of indicator buffers 3
#property indicator_buffers 3 
//---- only three plots are used
#property indicator_plots   3
//+----------------------------------------------+
//|  Indicator drawing parameters                |
//+----------------------------------------------+
//---- drawing the indicator as a line
#property indicator_type1   DRAW_LINE
//---- DarkViolet color is used for indicator line
#property indicator_color1 clrDarkViolet
//---- the indicator line is a continuous curve
#property indicator_style1  STYLE_SOLID
//---- indicator line width is equal to 1
#property indicator_width1  1
//---- displaying the indicator label
#property indicator_label1  "MA Rounding"
//+----------------------------------------------+
//| Upper line drawing parameters                |
//+----------------------------------------------+
//---- drawing the indicator 2 as a line
#property indicator_type2   DRAW_ARROW
//---- light blue color is used as the color of the upper line of the indicator
#property indicator_color2  clrDodgerBlue
//---- indicator 2 line width is equal to 1
#property indicator_width2  1
//---- displaying of the bullish label of the indicator
#property indicator_label2  "Up MA Rounding"
//+----------------------------------------------+
//| Lower line drawing parameters                |
//+----------------------------------------------+
//---- drawing indicator 3 as line
#property indicator_type3   DRAW_ARROW
//---- red color is used as the color of the lower line of the indicator
#property indicator_color3  clrRed
//---- the indicator 3 line is a continuous curve
#property indicator_style3  STYLE_SOLID
//---- thickness of the indicator 3 line is equal to 1
#property indicator_width3 1 
//---- displaying of the bearish label of the indicator
#property indicator_label3  "Down MA Rounding"

//+----------------------------------------------+
//|  declaring constants                         |
//+----------------------------------------------+
#define RESET 0 // The constant for returning the indicator recalculation command to the terminal

//+----------------------------------------------+
//|  CXMA class description                      |
//+----------------------------------------------+
#include <SmoothAlgorithms.mqh> 
//+----------------------------------------------+

//---- declaration of the CXMA class variables from the SmoothAlgorithms.mqh file
CXMA XMA1;
//+----------------------------------------------+
//|  declaration of enumerations                 |
//+----------------------------------------------+
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
//+----------------------------------------------+
//|  declaration of enumerations                 |
//+----------------------------------------------+
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
//+----------------------------------------------+
//|  INDICATOR INPUT PARAMETERS                  |
//+----------------------------------------------+
input Smooth_Method XMA_Method=MODE_SMA; //smoothing method
input int XLength=12; //smoothing depth                    
input int XPhase=15; //smoothing parameter,
                     // for JJMA that can change withing the range -100 ... +100. It impacts the quality of the intermediate process of smoothing;
// For VIDIA, it is a CMO period, for AMA, it is a slow moving average period
input Applied_price_ IPC=PRICE_CLOSE;//price constant
/* , used for calculation of the indicator ( 1-CLOSE, 2-OPEN, 3-HIGH, 4-LOW, 
  5-MEDIAN, 6-TYPICAL, 7-WEIGHTED, 8-SIMPL, 9-QUARTER, 10-TRENDFOLLOW, 11-0.5 * TRENDFOLLOW.) */
input uint MaRound=50; //rounding ratio 
input int    ATRPeriod=12; //ATR period for the channel width
input double ATR_Factor=1.0; //channel deviation ratio
input bool  ChanContinuity=false; //channel continuity
input int Shift=0; // horizontal shift of the indicator in bars
//+----------------------------------------------+

//---- declaration of dynamic arrays that will further be 
// used as indicator buffers
double UpBuffer[];
double DnBuffer[];
double IndBuffer[];
//---- Declaration of the vertical shift value variable
double MaRo;
//---- Declaration of a variable for storing handle of the indicator
int ATR_Handle;
//---- Declaration of integer variables of data starting point
int min_rates_total;
//+------------------------------------------------------------------+   
//| XMA indicator initialization function                            | 
//+------------------------------------------------------------------+ 
void OnInit()
  {
//---- Initialization of variables of the start of data calculation
   min_rates_total=XMA1.GetStartBars(XMA_Method,XLength,XPhase)+2;
//---- setting alerts for invalid values of external parameters
   XMA1.XMALengthCheck("XLength", XLength);
   XMA1.XMAPhaseCheck("XPhase", XPhase, XMA_Method);
//---- Initialization of the vertical shift
   MaRo=_Point*MaRound;

//---- getting the ATR indicator handle
   ATR_Handle=iATR(NULL,PERIOD_CURRENT,ATRPeriod);
   if(ATR_Handle==INVALID_HANDLE)Print(" Failed to get handle of the ATR indicator");

//---- set dynamic array as an indicator buffer
   SetIndexBuffer(0,IndBuffer,INDICATOR_DATA);
//---- moving the indicator 1 horizontally
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//---- performing the shift of beginning of indicator drawing
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- setting the indicator values that won't be visible on a chart
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,0);

//---- set dynamic array as an indicator buffer
   SetIndexBuffer(1,UpBuffer,INDICATOR_DATA);
//---- shifting indicator 1 horizontally by Shift
   PlotIndexSetInteger(1,PLOT_SHIFT,Shift);
//---- shifting the starting point for drawing indicator 1 by min_rates_total
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,min_rates_total);
//---- setting the indicator values that won't be visible on a chart
   PlotIndexSetDouble(1,PLOT_EMPTY_VALUE,0);
//---- indicator symbol
   PlotIndexSetInteger(1,PLOT_ARROW,167);

//---- set dynamic array as an indicator buffer
   SetIndexBuffer(2,DnBuffer,INDICATOR_DATA);
//---- shifting the indicator 2 horizontally by Shift
   PlotIndexSetInteger(2,PLOT_SHIFT,Shift);
//---- shifting the starting point for drawing indicator 2 by min_rates_total
   PlotIndexSetInteger(2,PLOT_DRAW_BEGIN,min_rates_total);
//---- setting the indicator values that won't be visible on a chart
   PlotIndexSetDouble(2,PLOT_EMPTY_VALUE,0);
//---- indicator symbol
   PlotIndexSetInteger(2,PLOT_ARROW,167);

//---- initializations of variable for indicator short name
   string shortname;
   string Smooth1=XMA1.GetString_MA_Method(XMA_Method);
   StringConcatenate(shortname,"MA Rounding(",XLength,", ",MaRound,", ",Smooth1,")");
//--- creation of the name to be displayed in a separate sub-window and in a pop up help
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);

//--- determining the accuracy of displaying the indicator values
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits+1);
//---- end of initialization
  }
//+------------------------------------------------------------------+ 
//| XMA iteration function                                           | 
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
   if(BarsCalculated(ATR_Handle)<rates_total || rates_total<min_rates_total) return(RESET);

//---- declaration of variables with a floating point  
   double price_,MovAve0,MovAle0,res0,res1,Range[],range0;
//---- Declaration of integer variables
   int to_copy,first,bar;

//---- Declaration of static variables  
   static double MovAle1,MovAve1,range1;

//---- calculation of the starting number first for the bar recalculation loop
   if(prev_calculated>rates_total || prev_calculated<=0) // checking for the first start of calculation of an indicator
     {
      first=2; // starting number for calculation of all bars
      MovAve1=PriceSeries(IPC,first,open,low,high,close);
      MovAle1=0;
      range1=0;
     }
   else first=prev_calculated-1; // starting number for calculation of new bars

//---- restore values of the variables
   range0=range1;
   
   to_copy=rates_total-first;

//---- copy newly appeared data in the Range[] array
   if(CopyBuffer(ATR_Handle,0,0,to_copy,Range)<=0) return(RESET);
   
//---- indexing elements in arrays as timeseries  
   ArraySetAsSeries(Range,true);

//---- Main calculation loop of the indicator
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      //---- Calling the PriceSeries function to get the input price price_
      price_=PriceSeries(IPC,bar,open,low,high,close);

      MovAve0=XMA1.XMASeries(2,prev_calculated,rates_total,XMA_Method,XPhase,XLength,price_,bar,false);

      res1=IndBuffer[bar-1];

      if(MovAve0>MovAve1+MaRo
         || MovAve0<MovAve1-MaRo
         || MovAve0>res1+MaRo
         || MovAve0<res1-MaRo
         || (MovAve0>res1 && MovAle1==+1)
         || (MovAve0<res1 && MovAle1==-1))
         IndBuffer[bar]=MovAve0;
      else IndBuffer[bar]=res1;

      MovAle0=0;
      res0=IndBuffer[bar];
      if(res0<res1) MovAle0 =-1;
      if(res0>res1) MovAle0 =+1;
      if(res0==res1) MovAle0=MovAle1;

      //---- zero out the contents of the indicator buffers
      UpBuffer[bar]=0.0;
      DnBuffer[bar]=0.0;

      //---- build a channel
      if(res0==res1)
        {
         if(res1!=IndBuffer[bar-2]) range0=Range[rates_total-bar-1]*ATR_Factor;
         else range0=range1;

         UpBuffer[bar]=res0+range0;
         DnBuffer[bar]=res0-range0;
        }
      else if(ChanContinuity)
        {
         UpBuffer[bar]=UpBuffer[bar-1];
         DnBuffer[bar]=DnBuffer[bar-1];
        }

      //--- recalculation of positions in ring buffers  
      if(bar<rates_total-1)
        {
         MovAle1=MovAle0;
         MovAve1=MovAve0;
         range1=range0;
        }
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
