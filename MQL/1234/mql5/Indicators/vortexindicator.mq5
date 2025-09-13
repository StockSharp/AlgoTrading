//+------------------------------------------------------------------+
//|                                              VortexIndicator.mq5 |
//|                                     Copyright © 2010, Scratchman |
//|                   http://creativecommons.org/licenses/by-sa/3.0/ |
//+------------------------------------------------------------------+
//---- author of the indicator
#property copyright "Copyright © 2010, Scratchman"
//---- author of the indicator
#property link      "http://creativecommons.org/licenses/by-sa/3.0/"
//---- indicator version number
#property version   "1.00"
//---- drawing indicator in a separate window
#property indicator_separate_window
//---- two buffers are used for the indicator calculation and drawing
#property indicator_buffers 2
//---- one plot is used
#property indicator_plots   1
//+----------------------------------------------+
//|  Indicator drawing parameters                |
//+----------------------------------------------+
//---- drawing the indicator 1 as a cloud
#property indicator_type1   DRAW_FILLING
//---- the following colors are used for the indicator bullish ine
#property indicator_color1  clrDarkOrange,clrLime
//---- displaying of the bullish label of the indicator
#property indicator_label1  "VortexIndicator"
//+----------------------------------------------+
//|  declaring constants                         |
//+----------------------------------------------+
#define RESET 0 // The constant for returning the indicator recalculation command to the terminal
//+----------------------------------------------+
//| Parameters of displaying horizontal levels   |
//+----------------------------------------------+
#property indicator_level1 0.0
#property indicator_levelcolor Gray
#property indicator_levelstyle STYLE_DASHDOTDOT
//+----------------------------------------------+
//| Indicator input parameters                   |
//+----------------------------------------------+
input uint VI_Length=14;// indicator period 
input int Shift=0; // horizontal shift of the indicator in bars 
//+----------------------------------------------+
//---- declaration of dynamic arrays that will further be 
// used as indicator buffers
double DnBuffer[];
double UpBuffer[];
//---- Declaration of integer variables for the indicator handles
int ATR_Handle;
//---- Declaration of integer variables of data starting point
int min_rates_total;
//---- declaration of dynamic arrays that will further be 
// used as ring buffers
int Count[];
double UpVM[],DnVM[];
//+------------------------------------------------------------------+
//|  recalculation of position of a newest element in the array      |
//+------------------------------------------------------------------+   
void Recount_ArrayZeroPos
(
 int &CoArr[]// Return the current value of the price series by the link
 )
// Recount_ArrayZeroPos(count, Length)
//+ - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -+
  {
//----
   int numb,Max1,Max2;
   static int count=1;

   Max2=int(VI_Length);
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
//---- Initialization of variables of the start of data calculation
   min_rates_total=int(VI_Length+1);

//---- memory distribution for variables' arrays  
   ArrayResize(Count,VI_Length);
   ArrayResize(UpVM,VI_Length);
   ArrayResize(DnVM,VI_Length);

//---- Initialization of arrays of variables
   ArrayInitialize(UpVM,0.0);
   ArrayInitialize(DnVM,0.0);

//---- getting the iATR indicator handle
   ATR_Handle=iATR(NULL,0,1);
   if(ATR_Handle==INVALID_HANDLE) Print(" Failed to get handle of iATR indicator");

//---- set dynamic array as an indicator buffer
   SetIndexBuffer(0,DnBuffer,INDICATOR_DATA);
//---- indexing elements in the buffer as time series
   ArraySetAsSeries(DnBuffer,true);

//---- set dynamic array as an indicator buffer
   SetIndexBuffer(1,UpBuffer,INDICATOR_DATA);
//---- indexing elements in the buffer as time series
   ArraySetAsSeries(UpBuffer,true);

//---- shifting indicator 1 horizontally by Shift
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//---- shifting the starting point for drawing indicator 1 by min_rates_total
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);

//---- initializations of variable for indicator short name
   string shortname;
   StringConcatenate(shortname,"VortexIndicator(",VI_Length,", ",Shift,")");
//--- creation of the name to be displayed in a separate sub-window and in a pop up help
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
//--- determining the accuracy of displaying the indicator values
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits);
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
   if(BarsCalculated(ATR_Handle)<rates_total || rates_total<min_rates_total) return(RESET);

//---- declaration of local variables 
   int to_copy,MaxBar,limit,bar,kkk;
   double SumUpVM,SumDnVM,SumTR,ATR[];

//---- calculation of the starting number limit for the bar recalculation loop
   if(prev_calculated>rates_total || prev_calculated<=0)// checking for the first start of the indicator calculation
     {
      limit=rates_total-2; // starting index for the calculation of all bars
      to_copy=limit+1;
     }
   else
     {
      limit=rates_total-prev_calculated; // starting index for the calculation of new bars
      to_copy=limit+1+int(VI_Length);
     }

   MaxBar=rates_total-min_rates_total-1;

//--- copy newly appeared data in the array
   if(CopyBuffer(ATR_Handle,0,0,to_copy,ATR)<=0) return(RESET);

//---- indexing elements in arrays as timeseries  
   ArraySetAsSeries(high,true);
   ArraySetAsSeries(low,true);
   ArraySetAsSeries(ATR,true);

//---- main indicator calculation loop
   for(bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      UpVM[Count[0]]=MathAbs(high[bar]-low[bar+1]);
      DnVM[Count[0]]=MathAbs(low[bar]-high[bar+1]);

      if(bar>MaxBar)
        {
         Recount_ArrayZeroPos(Count);
         continue;
        }

      SumUpVM=0.0;
      SumDnVM=0.0;
      SumTR=0.0;

      for(kkk=0; kkk<int(VI_Length); kkk++)
        {
         SumUpVM+=UpVM[kkk];
         SumDnVM+=DnVM[kkk];
         SumTR+=ATR[bar+kkk];
        }

      if(SumTR)
        {
         UpBuffer[bar]=SumUpVM/SumTR;
         DnBuffer[bar]=SumDnVM/SumTR;
        }
      else
        {
         UpBuffer[bar]=EMPTY_VALUE;
         DnBuffer[bar]=EMPTY_VALUE;
        }

      if(bar) Recount_ArrayZeroPos(Count);
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
