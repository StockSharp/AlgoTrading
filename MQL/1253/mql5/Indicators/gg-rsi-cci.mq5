//+---------------------------------------------------------------------+
//|                                                      GG-RSI-CCI.mq5 |
//|                                            Copyright © 2009, GGekko |
//|                                            http://www.fx-ggekko.com |
//+---------------------------------------------------------------------+ 
//| For the indicator to work, place the file SmoothAlgorithms.mqh      |
//| in the directory: terminal_data_folder\MQL5\Include                 |
//+---------------------------------------------------------------------+
#property copyright "Copyright © 2009, GGekko"
#property link "http://www.fx-ggekko.com" 
//---- indicator version number
#property version   "1.10"
//---- drawing indicator in a separate window
#property indicator_separate_window 
//---- number of indicator buffers 2
#property indicator_buffers 2 
//---- only one plot is used
#property indicator_plots   1
//---- fixed height of the indicator subwindow in pixels 
#property indicator_height 10
//---- lower and upper scale limit of a separate indicator window
#property indicator_maximum +0.9
#property indicator_minimum +0.4
//+-----------------------------------+
//|  Declaration of constants         |
//+-----------------------------------+
#define RESET 0 // The constant for getting the command for the indicator recalculation back to the terminal
//+-----------------------------------+
//|  Indicator drawing parameters     |
//+-----------------------------------+
//---- drawing the indicator as a four-color histogram
#property indicator_type1 DRAW_COLOR_HISTOGRAM
//---- colors of the four-color histogram are as follows
#property indicator_color1 clrDeepPink,clrGray,clrDodgerBlue
//---- indicator line is a solid one
#property indicator_style1 STYLE_SOLID
//---- Indicator line width is equal to 2
#property indicator_width1 2
//---- displaying the indicator label
#property indicator_label1 "GG-RSI-CCI"

//+-----------------------------------+
//|  Averaging classes description    |
//+-----------------------------------+
#include <SmoothAlgorithms.mqh> 
//+-----------------------------------+

//---- declaration of the CXMA class variables from the SmoothAlgorithms.mqh file
CXMA XMA1,XMA2,XMA3,XMA4;
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
//|  INDICATOR INPUT PARAMETERS       |
//+-----------------------------------+
input int IndPeriod=8; //RSI and CCI indicators period
input ENUM_APPLIED_PRICE Applied_Price=PRICE_CLOSE; //type of price
input Smooth_Method SmoothMethod=MODE_SMMA; //smoothing method
input int Period1=14; //rough smoothing of indicators
input int Period2=20; //specifying smoothing of indicators
input int Smooth_Phase=100;   //smoothing parameter,
                              // for JJMA that can change withing the range -100 ... +100. It impacts the quality of the intermediate process of smoothing;
// For VIDIA, it is a CMO period, for AMA, it is a slow moving average period
//+-----------------------------------+
//---- declaration of integer variables for the indicators handles
int RSI_Handle,CCI_Handle;
//---- Declaration of integer variables of data starting point
int min_rates,min_rates_total;
//---- declaration of dynamic arrays that will further be 
// used as indicator buffers
double IndBuffer[],ColorIndBuffer[];
//+------------------------------------------------------------------+    
//| GG-RSI-CCI indicator initialization function                     | 
//+------------------------------------------------------------------+  
void OnInit()
  {
//---- Initialization of variables of the start of data calculation
   int min_rates_1=XMA1.GetStartBars(SmoothMethod,Period1,Smooth_Phase);
   int min_rates_2=XMA1.GetStartBars(SmoothMethod,Period2,Smooth_Phase);
   min_rates=IndPeriod;
   min_rates_total=min_rates+MathMax(min_rates_1,min_rates_2);

//---- getting handle of the iRSI indicator  
   RSI_Handle=iRSI(NULL,PERIOD_CURRENT,IndPeriod,Applied_Price);
   if(RSI_Handle==INVALID_HANDLE) Print(" Failed to get handle of the iRSI indicator");

//---- getting handle of the iCCI indicator 
   CCI_Handle=iCCI(NULL,PERIOD_CURRENT,IndPeriod,Applied_Price);
   if(CCI_Handle==INVALID_HANDLE) Print(" Failed to get handle of the iCCI indicator");

//---- set IndBuffer dynamic array as an indicator buffer
   SetIndexBuffer(0,IndBuffer,INDICATOR_DATA);
//---- performing the shift of beginning of indicator drawing
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- setting the indicator values that won't be visible on a chart
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,0.0);
//---- indexing elements in the buffer as time series
   ArraySetAsSeries(IndBuffer,true);

//---- setting dynamic array as a color index buffer   
   SetIndexBuffer(1,ColorIndBuffer,INDICATOR_COLOR_INDEX);
//---- indexing elements in the buffer as time series
   ArraySetAsSeries(ColorIndBuffer,true);

//---- setting alerts for invalid values of external parameters
   XMA1.XMALengthCheck("Period1",Period1);
   XMA1.XMALengthCheck("Period2",Period2);
//---- setting alerts for invalid values of external parameters
   XMA1.XMAPhaseCheck("Smooth_Phase",Smooth_Phase,SmoothMethod);

//--- creation of the name to be displayed in a separate sub-window and in a pop up help
   IndicatorSetString(INDICATOR_SHORTNAME,"GG-RSI-CCI");
//--- determining the accuracy of displaying the indicator values
   IndicatorSetInteger(INDICATOR_DIGITS,0);
//---- end of initialization
  }
//+------------------------------------------------------------------+  
//| GG-RSI-CCI iteration function                                    | 
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
   if(BarsCalculated(RSI_Handle)<rates_total
      || BarsCalculated(CCI_Handle)<rates_total
      || rates_total<min_rates_total) return(RESET);

///---- declaration of local variables 
   int limit,bar,to_copy,MaxBar;
   double x1cci,x2cci,x1rsi,x2rsi;
   double CCI[],RSI[];

//---- calculation of the starting number limit for the bar recalculation loop
   if(prev_calculated>rates_total || prev_calculated<=0)// checking for the first start of the indicator calculation
      limit=rates_total-min_rates-1; // starting index for the calculation of all bars
   else limit=rates_total-prev_calculated; // starting index for the calculation of new bars

   to_copy=limit+1;
   MaxBar=rates_total-min_rates-1;

//---- indexing elements in arrays as timeseries  
   ArraySetAsSeries(CCI,true);
   ArraySetAsSeries(RSI,true);

//---- copy newly appeared data into the arrays  
   if(CopyBuffer(RSI_Handle,0,0,to_copy,RSI)<=0) return(RESET);
   if(CopyBuffer(CCI_Handle,0,0,to_copy,CCI)<=0) return(RESET);

//---- main indicator calculation loop
   for(bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      x1rsi=XMA1.XMASeries(MaxBar,prev_calculated,rates_total,SmoothMethod,Smooth_Phase,Period1,RSI[bar],bar,true);
      x2rsi=XMA2.XMASeries(MaxBar,prev_calculated,rates_total,SmoothMethod,Smooth_Phase,Period2,RSI[bar],bar,true);

      x1cci=XMA3.XMASeries(MaxBar,prev_calculated,rates_total,SmoothMethod,Smooth_Phase,Period1,CCI[bar],bar,true);
      x2cci=XMA4.XMASeries(MaxBar,prev_calculated,rates_total,SmoothMethod,Smooth_Phase,Period2,CCI[bar],bar,true);

      IndBuffer[bar]=1;

      if(x1rsi>x2rsi && x1cci>x2cci) ColorIndBuffer[bar]=2;
      else if(x1rsi<x2rsi && x1cci<x2cci) ColorIndBuffer[bar]=0;
      else ColorIndBuffer[bar]=1;
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
