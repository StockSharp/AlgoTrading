//+------------------------------------------------------------------+
//|                                            MUV_NorDIFF_Cloud.mq5 | 
//|                                          Copyright © 2008, svm-d |
//|                                                                  |
//+------------------------------------------------------------------+
//---- author of the indicator
#property copyright "Copyright © 2008, svm-d"
//---- author of the indicator
#property link      ""
//---- indicator version number
#property version   "1.00"
//---- drawing indicator in a separate window
#property indicator_separate_window
//---- six buffers are used for the calculation and drawing of the indicator
#property indicator_buffers 6
//---- five graphical plots are used
#property indicator_plots   5
//+----------------------------------------------+
//|  MUV_DIFF indicator drawing parameters       |
//+----------------------------------------------+
//---- drawing the indicator 1 as a cloud
#property indicator_type1   DRAW_FILLING
//---- the following colors are used for the indicator bullish line
#property indicator_color1  clrSandyBrown,clrDeepSkyBlue
//---- displaying of the bullish label of the indicator
#property indicator_label1  "MUV_DIFF % Down; MUV_DIFF % Up"

//+----------------------------------------------+
//|  Bullish indicator drawing parameters        |
//+----------------------------------------------+
//---- drawing the indicator 2 as a line
#property indicator_type2   DRAW_LINE
//---- lettuce green color is used for the indicator bullish line
#property indicator_color2  clrBlue
//---- indicator 2 line width is equal to 1
#property indicator_width2  1
//---- bearish indicator label display
#property indicator_label2 "MUV_DIFF % Sma"

//+----------------------------------------------+
//|  Parameters of drawing the bearish indicator |
//+----------------------------------------------+
//---- drawing indicator 3 as line
#property indicator_type3   DRAW_LINE
//---- magenta color is used as the color of the bearish indicator line
#property indicator_color3  clrRed
//---- thickness of the indicator 3 line is equal to 1
#property indicator_width3  1
//---- bullish indicator label display
#property indicator_label3  "MUV_DIFF % Ema"

//+----------------------------------------------+
//|  Bullish indicator drawing parameters        |
//+----------------------------------------------+
//---- drawing the indicator 4 as a symbol
#property indicator_type4   DRAW_ARROW
//---- lettuce green color is used for the indicator bullish line
#property indicator_color4  clrLime
//---- thickness of the indicator 2 line is equal to 4
#property indicator_width4  4
//---- bearish indicator label display
#property indicator_label4 "MUV_DIFF % Buy"

//+----------------------------------------------+
//|  Parameters of drawing the bearish indicator |
//+----------------------------------------------+
//---- drawing the indicator 5 as a symbol
#property indicator_type5   DRAW_ARROW
//---- magenta color is used as the color of the bearish indicator line
#property indicator_color5  clrMagenta
//---- the indicator 3 line width is equal to 4
#property indicator_width5  4
//---- bullish indicator label display
#property indicator_label5  "MUV_DIFF % Sell"

//+----------------------------------------------+
//| Parameters of displaying horizontal levels   |
//+----------------------------------------------+
#property indicator_level1 +100.0
#property indicator_level2 -100.0
#property indicator_levelcolor clrGray
#property indicator_levelstyle STYLE_SOLID
//+----------------------------------------------+
//|  declaring constants                         |
//+----------------------------------------------+
#define RESET 0 // The constant for returning the indicator recalculation command to the terminal
//+----------------------------------------------+
//| Indicator input parameters                   |
//+----------------------------------------------+
input uint MAPeriod=14;// XMUV indicator period
input uint Momentum=1;// Momentum period
input uint KPeriod=14; // extremums searching period
input int Shift=0; // horizontal shift of the indicator in bars 
//+----------------------------------------------+
//---- declaration of dynamic arrays that will further be 
// used as indicator buffers
double SmaDiffBuffer[];
double EmaDiffBuffer[];
double SmaDiffBuffer_[];
double EmaDiffBuffer_[];
double BuyBuffer[];
double SellBuffer[];

//---- Declaration of integer variables for the indicator handles
int E_Handle,S_Handle;
//---- Declaration of integer variables of data starting point
int min_rates_total;
//---- declaration of global variables
int Count[];
double SmaValue[],EmaValue[];
//+------------------------------------------------------------------+
//|  Recalculation of position of the newest element in the array    |
//+------------------------------------------------------------------+   
void Recount_ArrayZeroPos(int &CoArr[],// Returns the number of the current value of the price series by reference
                          int Size)
  {
//----
   int numb,Max1,Max2;
   static int count=1;

   Max2=Size;
   Max1=Max2-1;

   count--;
   if(count<0) count=Max1;

   for(int iii=0; iii<Max2; iii++)
     {
      numb=iii+count;
      if(numb>Max1) numb-=Max2;
      CoArr[iii]=numb;
     }
//----
  }
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+  
void OnInit()
  {
//---- Initialization of variables of data calculation starting point
   min_rates_total=int(MAPeriod+Momentum+KPeriod);

//---- getting handle of the XMUV SMA indicator
   S_Handle=iCustom(NULL,0,"XMUV",0,MAPeriod,0,0,0);
   if(S_Handle==INVALID_HANDLE) Print(" Failed to get handle of XMUV SMA indicator");

//---- getting handle of the EMUV SMA indicator
   E_Handle=iCustom(NULL,0,"XMUV",1,MAPeriod,0,0,0);
   if(E_Handle==INVALID_HANDLE) Print(" Failed to get handle of XMUV EMA indicator");

//---- memory allocation for arrays of variables  
   ArrayResize(Count,KPeriod);
   ArrayResize(SmaValue,KPeriod);
   ArrayResize(EmaValue,KPeriod);

   ArrayInitialize(Count,0);
   ArrayInitialize(SmaValue,0.0);
   ArrayInitialize(EmaValue,0.0);

//---- set dynamic array as an indicator buffer
   SetIndexBuffer(0,SmaDiffBuffer,INDICATOR_DATA);
//---- indexing elements in the buffer as in timeseries
   ArraySetAsSeries(SmaDiffBuffer,true);

//---- set dynamic array as an indicator buffer
   SetIndexBuffer(1,EmaDiffBuffer,INDICATOR_DATA);
//---- indexing elements in the buffer as in timeseries
   ArraySetAsSeries(EmaDiffBuffer,true);
   
//---- set dynamic array as an indicator buffer
   SetIndexBuffer(2,SmaDiffBuffer_,INDICATOR_DATA);
//---- indexing elements in the buffer as in timeseries
   ArraySetAsSeries(SmaDiffBuffer_,true);

//---- set dynamic array as an indicator buffer
   SetIndexBuffer(3,EmaDiffBuffer_,INDICATOR_DATA);
//---- indexing elements in the buffer as in timeseries
   ArraySetAsSeries(EmaDiffBuffer_,true);
   
//---- set dynamic array as an indicator buffer
   SetIndexBuffer(4,BuyBuffer,INDICATOR_DATA);
//---- indexing elements in the buffer as in timeseries
   ArraySetAsSeries(BuyBuffer,true);

//---- set dynamic array as an indicator buffer
   SetIndexBuffer(5,SellBuffer,INDICATOR_DATA);
//---- indexing elements in the buffer as in timeseries
   ArraySetAsSeries(SellBuffer,true);

//---- shifting indicator 1 horizontally by Shift
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//---- shifting the starting point for drawing indicator 1 by min_rates_total
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- setting values of the indicator that won't be visible on a chart
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,EMPTY_VALUE);
   
//---- shifting indicator 1 horizontally by Shift
   PlotIndexSetInteger(1,PLOT_SHIFT,Shift);
//---- shifting the starting point for drawing indicator 1 by min_rates_total
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,min_rates_total);
//---- setting values of the indicator that won't be visible on a chart
   PlotIndexSetDouble(1,PLOT_EMPTY_VALUE,EMPTY_VALUE);
   
//---- shifting indicator 1 horizontally by Shift
   PlotIndexSetInteger(2,PLOT_SHIFT,Shift);
//---- shifting the starting point for drawing indicator 1 by min_rates_total
   PlotIndexSetInteger(2,PLOT_DRAW_BEGIN,min_rates_total);
//---- setting values of the indicator that won't be visible on a chart
   PlotIndexSetDouble(2,PLOT_EMPTY_VALUE,EMPTY_VALUE);
   
//---- shifting indicator 1 horizontally by Shift
   PlotIndexSetInteger(3,PLOT_SHIFT,Shift);
//---- shifting the starting point for drawing indicator 1 by min_rates_total
   PlotIndexSetInteger(3,PLOT_DRAW_BEGIN,min_rates_total);
//---- setting values of the indicator that won't be visible on a chart
   PlotIndexSetDouble(3,PLOT_EMPTY_VALUE,EMPTY_VALUE);
   
//---- shifting indicator 1 horizontally by Shift
   PlotIndexSetInteger(4,PLOT_SHIFT,Shift);
//---- shifting the starting point for drawing indicator 1 by min_rates_total
   PlotIndexSetInteger(4,PLOT_DRAW_BEGIN,min_rates_total);
//---- setting values of the indicator that won't be visible on a chart
   PlotIndexSetDouble(4,PLOT_EMPTY_VALUE,EMPTY_VALUE);

//---- initializations of variable for indicator short name
   string shortname;
   StringConcatenate(shortname,"MUV_DIFF %(",MAPeriod,", ",Momentum,", ",Shift,")");
//--- creation of the name to be displayed in a separate sub-window and in a pop up help
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
//--- determination of accuracy of displaying the indicator values
   IndicatorSetInteger(INDICATOR_DIGITS,0);
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
                const double& low[],      // price array of minimums of price for the calculation of indicator
                const double &close[],
                const long &tick_volume[],
                const long &volume[],
                const int &spread[]
                )
  {
//---- checking for the sufficiency of the number of bars for the calculation
   if(BarsCalculated(S_Handle)<rates_total
      || BarsCalculated(E_Handle)<rates_total
      || rates_total<min_rates_total)
      return(RESET);

//---- declaration of local variables 
   int to_copy,limit,bar;
   double SDiff[],EDiff[];
   double SmaMax,SmaMin,EmaMax,EmaMin,SmaRange,EmaRange,SmaRes,EmaRes;

//---- calculation of the starting number limit for the bar recalculation loop
   if(prev_calculated>rates_total || prev_calculated<=0)// checking for the first start of the indicator calculation
      limit=rates_total-min_rates_total-1; // starting number for calculation of all bars
   else limit=rates_total-prev_calculated; // starting number for the calculation of new bars

   to_copy=limit+1+int(Momentum);
//---- copy the new data into the array
   if(CopyBuffer(S_Handle,0,0,to_copy,SDiff)<=0) return(RESET);
   if(CopyBuffer(E_Handle,0,0,to_copy,EDiff)<=0) return(RESET);

//---- indexing elements in arrays as in timeseries 
   ArraySetAsSeries(SDiff,true);
   ArraySetAsSeries(EDiff,true);

//---- main cycle of calculation of the indicator
   for(bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      SmaValue[Count[0]]=(SDiff[bar]-SDiff[bar+Momentum]);
      EmaValue[Count[0]]=(EDiff[bar]-EDiff[bar+Momentum]);

      SmaMax=SmaValue[ArrayMaximum(SmaValue,0,KPeriod)];
      SmaMin=SmaValue[ArrayMinimum(SmaValue,0,KPeriod)];
      SmaRange=SmaMax-SmaMin;

      EmaMax=EmaValue[ArrayMaximum(EmaValue,0,KPeriod)];
      EmaMin=EmaValue[ArrayMinimum(EmaValue,0,KPeriod)];
      EmaRange=EmaMax-EmaMin;

      if(SmaRange>0) SmaRes=100-200*(SmaMax-SmaValue[Count[0]])/SmaRange; else SmaRes=100;
      if(EmaRange>0) EmaRes=100-200*(EmaMax-EmaValue[Count[0]])/EmaRange; else EmaRes=100;

      SmaDiffBuffer[bar]=SmaRes;
      SmaDiffBuffer_[bar]=SmaRes;
      EmaDiffBuffer[bar]=EmaRes;
      EmaDiffBuffer_[bar]=EmaRes;
      
      if(SmaRes==+100 || EmaRes==+100) BuyBuffer[bar]=+100; else BuyBuffer[bar]=EMPTY_VALUE;
      if(SmaRes==-100 || EmaRes==-100) SellBuffer[bar]=-100; else SellBuffer[bar]=EMPTY_VALUE;

      if(bar) Recount_ArrayZeroPos(Count,KPeriod);
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
