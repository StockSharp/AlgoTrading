/*
 * For the indicator to work, place the
 * SmoothAlgorithms.mqh
 * in the directory: MetaTrader\\MQL5\Include
 */
//+------------------------------------------------------------------+
//|                                                     UltraXMA.mq5 |
//|                             Copyright © 2011,   Nikolay Kositsin | 
//|                              Khabarovsk,   farria@mail.redcom.ru | 
//+------------------------------------------------------------------+
//---- author of the indicator
#property copyright "Copyright © 2011, Nikolay Kositsin"
//---- link to the website of the author
#property link "farria@mail.redcom.ru"
//---- indicator version number
#property version   "1.10"
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
//---- BlueViolet and DarkOrange colors are used as the indicator filling colors
#property indicator_color1  BlueViolet,DarkOrange
//---- displaying the indicator label
#property indicator_label1 "Ultra XMA"
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
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
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
input Applied_price_ IPC=PRICE_CLOSE;//price constant
//----                          
input uint UpLevel=80; // overbought level, %%
input uint DnLevel=20; // oversold level, %%
input color UpLevelsColor=Red; //overbought level color
input color DnLevelsColor=Red; //oversold level color
input STYLE Levelstyle=DASH_;   //levels style
input WIDTH  LevelsWidth=Width_1;  //levels width                       
//+----------------------------------------------+

//---- declaration of dynamic arrays that will further be 
// used as indicator buffers
double BullsBuffer[];
double BearsBuffer[];
//---- declaration of the variables array for storing WPR indicator signal lines periods
int period[];
//---- Declaration of variables arrays for the XMA indicator values storage
double xwpr0[],xwpr1[];
//---- Declaration of integer variables of data starting point
uint StTot1,StTot2;
int min_rates_total,min_rates_xma;
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
   min_rates_xma=XMA[0].GetStartBars(W_Method,StartLength+Step*StepsTotal,WPhase)+1;
   min_rates_total=min_rates_xma+XMA[0].GetStartBars(SmoothMethod,SmoothLength,SmoothPhase);
   StTot1=StepsTotal+1;
   StTot2=StepsTotal+2;

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
   string shortname="Ultra XMA";
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
   if(rates_total<min_rates_total) return(RESET);

//---- declaration of local variables 
   int limit,bar,maxbar1,maxbar2;
   double upsch,dnsch,price_;

//---- calculation of maxbar initial index for the XMASeries() function
   maxbar1=rates_total-1;
   maxbar2=rates_total-1-min_rates_xma;

//---- calculation of the starting number limit for the bar recalculation loop
   if(prev_calculated>rates_total || prev_calculated<=0)// checking for the first start of the indicator calculation
      limit=maxbar1; // starting index for calculation of all bars
   else limit=rates_total-prev_calculated; // starting index for the calculation of new bars

//---- indexing elements in arrays as timeseries  
   ArraySetAsSeries(open,true);
   ArraySetAsSeries(low,true);
   ArraySetAsSeries(high,true);
   ArraySetAsSeries(close,true);

//---- main indicator calculation loop
   for(bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      //---- Calling the PriceSeries function to get the input price price_
      price_=PriceSeries(IPC,bar,open,low,high,close);

      for(int sm=int(StepsTotal); sm>=0 && !IsStopped(); sm--)
         xwpr0[sm]=XMA[sm].XMASeries(maxbar1,prev_calculated,rates_total,W_Method,WPhase,period[sm],price_,bar,true);

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
