//+---------------------------------------------------------------------+
//|                                                      Slow-Stoch.mq5 |
//|                                        Copyright © 2005, Nick Bilak |
//|                                                 beluck[at]gmail.com |
//+---------------------------------------------------------------------+ 
//| Place the SmoothAlgorithms.mqh file                                 |
//| in the directory: terminal_data_folder\MQL5\Include                 |
//+---------------------------------------------------------------------+
//--- copyright
#property copyright "Copyright © 2005, Nick Bilak"
//--- link
#property link      "beluck[at]gmail.comm"
//--- indicator version
#property version   "1.00"
//--- drawing the indicator in a separate window
#property indicator_separate_window 
//--- two buffers are used for calculating and drawing the indicator
#property indicator_buffers 2
//--- one plot is used
#property indicator_plots   1
//+----------------------------------------------+
//| Stochastic indicator drawing parameters      |
//+----------------------------------------------+
//--- drawing the indicator as a colored cloud
#property indicator_type1   DRAW_FILLING
//---- the following colors are used as the indicator colors
#property indicator_color1  clrTeal,clrMagenta
//--- displaying the indicator label
#property indicator_label1  "Stochastic; Signal"
//+----------------------------------------------+
//| Parameters of displaying horizontal levels   |
//+----------------------------------------------+
#property indicator_level1  +75
#property indicator_level2  +50
#property indicator_level3  +25
#property indicator_levelcolor clrGray
#property indicator_levelstyle STYLE_DASHDOTDOT
//+----------------------------------------------+
//| Indicator window size limitation  |
//+----------------------------------------------+
#property indicator_minimum 0
#property indicator_maximum 100
//+----------------------------------------------+
//| declaring constants                          |
//+----------------------------------------------+
#define RESET 0 // A constant for returning the indicator recalculation command to the terminal
//+----------------------------------------------+
//| CXMA class description                       |
//+----------------------------------------------+
#include <SmoothAlgorithms.mqh>
//+----------------------------------------------+
//| Indicator input parameters                   |
//+----------------------------------------------+
input uint KPeriod=5;
input uint DPeriod=3;
input uint Slowing=3;
input ENUM_MA_METHOD STO_Method=MODE_SMA;
input ENUM_STO_PRICE Price_field=STO_LOWHIGH;
input Smooth_Method XMA_Method=MODE_JJMA;    // Method of averaging
input uint XLength=5;                        // Type of smoothing
input int XPhase=15;                         // Smoothing parameter
//--- XPhase: for JJMA it varies within the range -100 ... +100 and influences the quality of the transient period;
//--- XPhase: for VIDIA it is a CMO period, for AMA it is a slow average period
input int Shift=0;                           // Horizontal shift of the indicator in bars
//+----------------------------------------------+
//--- declaration of dynamic arrays that will be used as indicator buffers
double StoBuffer[];
double SignBuffer[];
//--- declaration of integer variables for the indicators handles
int STO_Handle;
//--- declaration of integer variables for the start of data calculation
int min_rates_total,min_rates_Sto;
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+  
int OnInit()
  {
//--- initialization of variables of data calculation start
   min_rates_Sto=int(KPeriod+DPeriod+Slowing);
   min_rates_total=GetStartBars(XMA_Method,XLength,XPhase);
//--- получение хендла индикатора iStochastic 
   STO_Handle=iStochastic(NULL,0,KPeriod,DPeriod,Slowing,STO_Method,Price_field);
   if(STO_Handle==INVALID_HANDLE)
     {
      Print(" Failed to get the handle of the iStochastic indicator");
      return(INIT_FAILED);
     }
//--- set dynamic array as an indicator buffer
   SetIndexBuffer(0,StoBuffer,INDICATOR_DATA);
//---- Indexing buffer elements as timeseries   
   ArraySetAsSeries(StoBuffer,true);
//--- set dynamic array as an indicator buffer
   SetIndexBuffer(1,SignBuffer,INDICATOR_DATA);
//---- Indexing buffer elements as timeseries   
   ArraySetAsSeries(SignBuffer,true);
//---- shifting the indicator 1 horizontally by Shift
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//---- shifting the starting point of the indicator 1 drawing by min_rates_total
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//--- creation of the name to be displayed in a separate sub-window and in a pop up help
   IndicatorSetString(INDICATOR_SHORTNAME,"Slow-Stoch");
//--- determining the accuracy of the indicator values
   IndicatorSetInteger(INDICATOR_DIGITS,0);
//--- initialization end
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+
//| Custom indicator iteration function                              |
//+------------------------------------------------------------------+
int OnCalculate(const int rates_total,    // number of bars in history at the current tick
                const int prev_calculated,// amount of history in bars at the previous tick
                const datetime &time[],
                const double &open[],
                const double& high[],     // price array of price maximums for the indicator calculation
                const double& low[],      // price array of minimums of price for the indicator calculation
                const double &close[],
                const long &tick_volume[],
                const long &volume[],
                const int &spread[])
  {
//--- checking if the number of bars is enough for the calculation
   if(BarsCalculated(STO_Handle)<rates_total || rates_total<min_rates_total) return(RESET);
//--- declarations of local variables 
   int limit,to_copy,maxbar,bar;
   double STO[],SIG[];
//--- declaration of the CXMA class variables from the SmoothAlgorithms.mqh file
   static CXMA XMA1,XMA2;
//--- apply timeseries indexing to array elements  
   ArraySetAsSeries(STO,true);
   ArraySetAsSeries(SIG,true);
   maxbar=rates_total-1-min_rates_Sto;
//--- calculation of the 'first' starting index for the bars recalculation loop
   if(prev_calculated>rates_total || prev_calculated<=0) // checking for the first start of calculation of an indicator
      limit=maxbar; // starting index for calculation of all bars
   else limit=rates_total-prev_calculated; // Starting index for the calculation of new bars
//---
   to_copy=limit+1;
//--- copy newly appeared data in the array
   if(CopyBuffer(STO_Handle,MAIN_LINE,0,to_copy,STO)<=0) return(RESET);
   if(CopyBuffer(STO_Handle,SIGNAL_LINE,0,to_copy,SIG)<=0) return(RESET);
//--- main indicator calculation loop
   for(bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      StoBuffer[bar]=XMA1.XMASeries(maxbar,prev_calculated,rates_total,XMA_Method,XPhase,XLength,STO[bar],bar,true);
      SignBuffer[bar]=XMA2.XMASeries(maxbar,prev_calculated,rates_total,XMA_Method,XPhase,XLength,SIG[bar],bar,true);
     }
//---     
   return(rates_total);
  }
//+------------------------------------------------------------------+
