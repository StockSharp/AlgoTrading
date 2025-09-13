//+------------------------------------------------------------------+
//|                                                     UltraWPR.mq5 |
//|                                   Copyright © 2008, dm34@mail.ru | 
//|                                    http://www.fxexpert.ru/forum/ | 
//+------------------------------------------------------------------+
//---- author of the indicator
#property copyright "Copyright © 2008, dm34@mail.ru"
//---- link to the website of the author
#property link "http://www.fxexpert.ru/forum/"
//---- indicator version number
#property version   "1.00"
//---- drawing indicator in a separate window
#property indicator_separate_window
//---- two buffers are used for the indicator calculation and drawing
#property indicator_buffers 2
//---- one plot is used
#property indicator_plots   1
//+-----------------------------------+ 
//|  Declaration of constants         |
//+-----------------------------------+ 
#define RESET 0 // The constant for returning the indicator recalculation command to the terminal
//+-----------------------------------+
//|  Filling drawing parameters       |
//+-----------------------------------+
//---- drawing indicator as a filling between two lines
#property indicator_type1   DRAW_FILLING
//---- green and magenta colors are used as the indicator filling colors
#property indicator_color1  Lime, Magenta
//---- displaying the indicator label
#property indicator_label1 "Ultra WPR"
//+-----------------------------------+
//|  CXMA class description           |
//+-----------------------------------+
#include <SmoothAlgorithms.mqh> 
//+-----------------------------------+

//---- declaration of the CXMA class variables from the SmoothAlgorithms.mqh file
CXMA XMA[];
//+-----------------------------------+
//|  Declaration of enumerations      |
//+-----------------------------------+
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
//|  enumeration declaration          |
//+-----------------------------------+  
enum WIDTH
  {
   Width_1=1, //1
   Width_2,   //2
   Width_3,   //3
   Width_4,   //4
   Width_5    //5
  };
//+-----------------------------------+
//|  enumeration declaration          |
//+-----------------------------------+
enum STYLE
  {
   SOLID_,//Solid line
   DASH_,//Dashed line
   DOT_,//Dotted line
   DASHDOT_,//Dot-dash line
   DASHDOTDOT_   //Dot-dash line with double dots
  };
//+----------------------------------------------+
//| Indicator input parameters                   |
//+----------------------------------------------+
input int WPR_Period=13; //WPR indicator period
//----
input Smooth_Method W_Method=MODE_JJMA; //smoothing method
input int StartLength=3; //initial smoothing period                    
input int WPhase=100; //smoothing parameter,
                      // for JJMA that can change withing the range -100 ... +100. It impacts the quality of the intermediate process of smoothing;
// For VIDIA, it is a CMO period, for AMA, it is a slow moving average period
//----  
input uint Step=5; //period change step
input uint StepsTotal=10; //number of period changes
//----
input Smooth_Method SmoothMethod=MODE_JJMA; //smoothing method
input int SmoothLength=3; //smoothing depth                    
input int SmoothPhase=100; //smoothing parameter,
                           // for JJMA that can change withing the range -100 ... +100. It impacts the quality of the intermediate process of smoothing;
//----                          
input uint UpLevel=80; // overbought level, %%
input uint DnLevel=20; // oversold level, %%
input color UpLevelsColor=Blue; //overbought level color
input color DnLevelsColor=Blue; //oversold level color
input STYLE Levelstyle=DASH_;   //levels style
input WIDTH  LevelsWidth=Width_1;  //levels width
//+----------------------------------------------+

//---- declaration of dynamic arrays that will further be 
// used as indicator buffers
double BullsBuffer[];
double BearsBuffer[];
//---- declaration of the variables array for storing WPR indicator signal lines periods
int period[];
//---- declaration of variables arrays for the WPR indicator values storage
double xwpr0[],xwpr1[];
//---- Declaration of integer variables for the indicator handles
int WPR_Handle;
//---- Declaration of integer variables of data starting point
uint StTot1,StTot2;
int min_rates_total,min_rates_wpr,min_rates_xma;
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+  
void OnInit()
  {
//---- memory allocation for arrays of variables
   int size=int(StepsTotal+3);
   if(ArrayResize(XMA,size)<size) Print("Failed to distribute the memory for XMA[] array");
   size-=2;
   if(ArrayResize(xwpr0,size)<size) Print("Failed to distribute the memory for xwpr0[] array");
   if(ArrayResize(xwpr1,size)<size) Print("Failed to distribute the memory for xwpr1[] array");
   if(ArrayResize(period,size)<size) Print("Failed to distribute the memory for period[] array");
   
//---- Initialization of variables
   for(int sm=int(StepsTotal); sm>=0 && !IsStopped(); sm--) period[sm]=int(StartLength+sm*Step);
         
//---- setting alerts for invalid values of external parameters
   XMA[0].XMALengthCheck("StartLength", StartLength);
   XMA[0].XMAPhaseCheck("WPhase", WPhase, W_Method);
   XMA[0].XMALengthCheck("SmoothLength", SmoothLength);
   XMA[0].XMAPhaseCheck("SmoothPhase", SmoothPhase, SmoothMethod);

//---- Initialization of variables of the start of data calculation
   min_rates_wpr=WPR_Period;
   min_rates_xma=min_rates_wpr+XMA[0].GetStartBars(W_Method,StartLength+Step*StepsTotal,WPhase)+1;
   min_rates_total=min_rates_xma+XMA[0].GetStartBars(SmoothMethod,SmoothLength,SmoothPhase);
   StTot1=StepsTotal+1;
   StTot2=StepsTotal+2;

//---- getting handle of the WPR indicator
   WPR_Handle=iWPR(NULL,0,WPR_Period);
   if(WPR_Handle==INVALID_HANDLE) Print(" Failed to get handle of the WPR indicator");

//---- transformation of the dynamic array BullsBuffer into an indicator buffer
   SetIndexBuffer(0,BullsBuffer,INDICATOR_DATA);
//---- shifting the starting point for drawing indicator 1 by min_rates_total
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- indexing elements in the buffer as time series
   ArraySetAsSeries(BullsBuffer,true);

//---- transformation of the BearsBuffer dynamic array into an indicator buffer
   SetIndexBuffer(1,BearsBuffer,INDICATOR_DATA);
//---- shifting the starting point for drawing indicator 2 by min_rates_total
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,min_rates_total);
//---- indexing elements in the buffer as time series
   ArraySetAsSeries(BearsBuffer,true);

//---- initializations of variable for indicator short name
   string shortname="Ultra WPR";
//--- creation of the name to be displayed in a separate sub-window and in a pop up help
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
//--- determining the accuracy of displaying the indicator values
   IndicatorSetInteger(INDICATOR_DIGITS,2);
   
//---- lines drawing parameters  
   IndicatorSetInteger(INDICATOR_LEVELS,2);
   
   IndicatorSetDouble(INDICATOR_LEVELVALUE,0,StepsTotal*UpLevel/100);
   IndicatorSetInteger(INDICATOR_LEVELCOLOR,0,UpLevelsColor);
   IndicatorSetInteger(INDICATOR_LEVELSTYLE,0,Levelstyle);
   IndicatorSetInteger(INDICATOR_LEVELWIDTH,0,LevelsWidth);
   
   IndicatorSetDouble(INDICATOR_LEVELVALUE,1,StepsTotal*DnLevel/100);
   IndicatorSetInteger(INDICATOR_LEVELCOLOR,1,DnLevelsColor);
   IndicatorSetInteger(INDICATOR_LEVELSTYLE,1,Levelstyle);
   IndicatorSetInteger(INDICATOR_LEVELWIDTH,1,LevelsWidth);
//----
  }
//+------------------------------------------------------------------+
//| Custom indicator iteration function                              |
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
//---- checking the number of bars to be enough for calculation
   if(BarsCalculated(WPR_Handle)<rates_total
      || rates_total<min_rates_total)
      return(RESET);

//---- declaration of local variables 
   int to_copy,limit,bar,maxbar1,maxbar2;
   double WPR[],upsch,dnsch;

//---- calculation of maxbar initial index for the XMASeries() function
   maxbar1=rates_total-1-min_rates_wpr;
   maxbar2=rates_total-1-min_rates_xma;

//---- calculation of the starting number limit for the bar recalculation loop
   if(prev_calculated>rates_total || prev_calculated<=0)// checking for the first start of the indicator calculation
      limit=maxbar1; // starting index for calculation of all bars
   else limit=rates_total-prev_calculated; // starting index for the calculation of new bars
//----   
   to_copy=limit+2;
//---- copy newly appeared data into the arrays
   if(CopyBuffer(WPR_Handle,0,0,to_copy,WPR)<=0) return(RESET);

//---- indexing elements in arrays as timeseries  
   ArraySetAsSeries(WPR,true);

//---- main indicator calculation loop
   for(bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      for(int sm=int(StepsTotal); sm>=0 && !IsStopped(); sm--)
         xwpr0[sm]=XMA[sm].XMASeries(maxbar1,prev_calculated,rates_total,W_Method,WPhase,period[sm],WPR[bar],bar,true);

      if(bar>maxbar2)
        {
         if(bar) ArrayCopy(xwpr1,xwpr0,0,0,WHOLE_ARRAY);
         continue;
        }

      upsch=0;
      dnsch=0;
      for(int sm=int(StepsTotal); sm>=0 && !IsStopped(); sm--)
         if(xwpr0[sm]>xwpr1[sm]) upsch++;
         else                    dnsch++;
         
      BullsBuffer[bar]=XMA[StTot1].XMASeries(maxbar2,prev_calculated,rates_total,SmoothMethod,SmoothPhase,SmoothLength,upsch,bar,true);
      BearsBuffer[bar]=XMA[StTot2].XMASeries(maxbar2,prev_calculated,rates_total,SmoothMethod,SmoothPhase,SmoothLength,dnsch,bar,true);

      if(bar) ArrayCopy(xwpr1,xwpr0,0,0,WHOLE_ARRAY);
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
