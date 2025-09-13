//+---------------------------------------------------------------------+
//|                                                      Flat-Trend.mq5 | 
//|                                            Copyright © 2009, Sergey | 
//|                                                  http://wsforex.ru/ | 
//+---------------------------------------------------------------------+ 
//| For the indicator to work, place the file SmoothAlgorithms.mqh      |
//| in the directory: terminal_data_folder\MQL5\Include                 |
//+---------------------------------------------------------------------+
#property copyright "Copyright © 2009, Sergey"
#property link "http://wsforex.ru/"
//---- indicator version number
#property version   "1.00"
//---- drawing indicator in a separate window
#property indicator_separate_window 
//---- number of indicator buffers
#property indicator_buffers 2 
//---- only one plot is used
#property indicator_plots   1
//+-----------------------------------+
//|  Declaration of constants         |
//+-----------------------------------+
#define RESET 0 // The constant for returning the indicator recalculation command to the terminal
//+-----------------------------------+
//|  Indicator drawing parameters     |
//+-----------------------------------+
//---- drawing the indicator as a multy-color histogram
#property indicator_type1   DRAW_COLOR_HISTOGRAM
//---- colors of the three-color line are
#property indicator_color1  Gray,BlueViolet,Magenta
//---- the indicator line is a continuous curve
#property indicator_style1  STYLE_SOLID
//---- Indicator line width is equal to 2
#property indicator_width1  2
//---- displaying the indicator label
#property indicator_label1  "Flat-Trend"

//+-----------------------------------+
//|  CXMA class description           |
//+-----------------------------------+
#include <SmoothAlgorithms.mqh> 
//+-----------------------------------+

//---- declaration of the CXMA class variables from the SmoothAlgorithms.mqh file
CXMA XMA1,XMA2;
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
input uint StDevPeriod=20; //StDev period 
input Smooth_Method StDev_Method=MODE_LWMA; //StDev smoothing method
input uint StDevLength=5; //StDev smoothing depth                    
input int StDevPhase=15; //StDev smoothing parameter,
                         // for JJMA that can change withing the range -100 ... +100. It impacts the quality of the intermediate process of smoothing;
// For VIDIA, it is a CMO period, for AMA, it is a slow moving average period

input uint ATRPeriod=20; //StDev period   
input Smooth_Method ATR_Method=MODE_LWMA; //ATR smoothing method
input uint ATRLength=5; //ATR smoothing depth
input int ATRPhase=15;  //ATR smoothing parameter,
                        // for JJMA that can change withing the range -100 ... +100. It impacts the quality of the intermediate process of smoothing;
// For VIDIA, it is a CMO period, for AMA, it is a slow moving average period
input int Shift=0; // horizontal shift of the indicator in bars
//+-----------------------------------+

//---- declaration of dynamic arrays that will further be 
// used as indicator buffers
double IndBuffer[];
double ColorIndBuffer[];

//---- Declaration of integer variables for the indicator handles
int ATR_Handle,StDev_Handle;
//---- Declaration of integer variables of data starting point
int min_rates_total;
//+------------------------------------------------------------------+   
//| Flat-Trend indicator initialization function                     | 
//+------------------------------------------------------------------+ 
void OnInit()
  {
//---- Initialization of variables of the start of data calculation
   int min_rate_1=int(StDevPeriod)+XMA1.GetStartBars(StDev_Method,StDevLength,StDevPhase);
   int min_rate_2=int(ATRPeriod)+XMA2.GetStartBars(ATR_Method,ATRLength,ATRPhase);

   min_rates_total=MathMax(min_rate_1,min_rate_2)+1;

//---- setting alerts for invalid values of external parameters
   XMA1.XMALengthCheck("StDevLength",StDevLength);
   XMA2.XMALengthCheck("ATRLength",ATRLength);
//---- setting alerts for invalid values of external parameters
   XMA1.XMAPhaseCheck("StDevPhase",StDevPhase,StDev_Method);
   XMA2.XMAPhaseCheck("ATRPhase",ATRPhase,ATR_Method);

//---- getting the ATR indicator handle
   ATR_Handle=iATR(NULL,PERIOD_CURRENT,ATRPeriod);
   if(ATR_Handle==INVALID_HANDLE)Print(" Failed to get handle of the ATR indicator");

//---- getting handle of the StdDev indicator
   StDev_Handle=iStdDev(NULL,PERIOD_CURRENT,ATRPeriod,0,MODE_SMA,PRICE_CLOSE);
   if(StDev_Handle==INVALID_HANDLE)Print(" Failed to get handle of the StdDev indicator");

//---- set dynamic array as an indicator buffer
   SetIndexBuffer(0,IndBuffer,INDICATOR_DATA);
//---- indexing buffer elements as time series   
   ArraySetAsSeries(IndBuffer,true);

//---- setting dynamic array as a color index buffer   
   SetIndexBuffer(1,ColorIndBuffer,INDICATOR_COLOR_INDEX);
//---- indexing buffer elements as time series   
   ArraySetAsSeries(ColorIndBuffer,true);

//---- moving the indicator 1 horizontally
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//---- performing the shift of beginning of indicator drawing
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- setting the indicator values that won't be visible on a chart
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,0.0);

//---- creating name for displaying if separate sub-window and in tooltip
   IndicatorSetString(INDICATOR_SHORTNAME,"Flat-Trend");

//---- determine the accuracy of displaying indicator values
   IndicatorSetInteger(INDICATOR_DIGITS,0);
//---- end of initialization 
  }
//+------------------------------------------------------------------+ 
//| Flat-Trend iteration function                                    | 
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
   if(BarsCalculated(ATR_Handle)<rates_total
      || BarsCalculated(StDev_Handle)<rates_total
      || rates_total<min_rates_total) return(RESET);

//---- declaration of variables with a floating point  
   double StDev[],ATR[],xatr,xstdev;
//---- Declaration of integer variables
   int to_copy,limit,bar,maxbar1,maxbar2,res;
   
//---- indexing elements in arrays as timeseries  
   ArraySetAsSeries(StDev,true);
   ArraySetAsSeries(ATR,true);  

//---- Declaration of static variables
   static double prev_xatr,prev_xstdev;

   maxbar1=int(rates_total-ATRPeriod-1);
   maxbar2=int(rates_total-StDevPeriod-1);

//---- calculations of the necessary amount of data to be copied and
//the starting number limit for the bar recalculation loop
   if(prev_calculated>rates_total || prev_calculated<=0)// checking for the first start of the indicator calculation
     {
      limit=rates_total-1; // starting index for the calculation of all bars
      prev_xatr=0.0;
      prev_xstdev=0.0;
     }
   else limit=rates_total-prev_calculated; // starting index for the calculation of new bars
//----   
   to_copy=limit+1;

//---- copy newly appeared data into the arrays
   if(CopyBuffer(ATR_Handle,0,0,to_copy,ATR)<=0) return(RESET);
   if(CopyBuffer(StDev_Handle,0,0,to_copy,StDev)<=0) return(RESET);

//---- Main calculation loop of the indicator
   for(bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      //---- Two calls of the XMASeries function. 
      //---- The 'begin' parameter is increased by StartBars1 in the second call, as ê. another XMA smoothing  
      xatr=XMA1.XMASeries(maxbar1,prev_calculated,rates_total,ATR_Method,ATRPhase,ATRLength,ATR[bar],bar,true);
      xstdev=XMA2.XMASeries(maxbar2,prev_calculated,rates_total,StDev_Method,StDevPhase,StDevLength,StDev[bar],bar,true);
      //----       
      res=0;
      if(prev_xatr>xatr && prev_xstdev>xstdev) res=1;
      if(prev_xatr<xatr && prev_xstdev<xstdev) res=2;

      IndBuffer[bar]=res+1;
      ColorIndBuffer[bar]=res;

      //---- memorize values of the variables before running at the current bar
      if(bar)
        {
         prev_xatr=xatr;
         prev_xstdev=xstdev;
        }
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
