//+---------------------------------------------------------------------+
//|                                                          F2a_AO.mq5 | 
//|                                               Copyright © 2009, XXX | 
//|                                                                     | 
//+---------------------------------------------------------------------+ 
//| Place the SmoothAlgorithms.mqh file                                 |
//| in the directory: terminal_data_folder\MQL5\Include                 |
//+---------------------------------------------------------------------+
#property copyright "Copyright © 2009, XXX"
#property link ""
//---- indicator version number
#property version   "1.00"
#property description "The semaphore indicator on the basis of three NavelEMA"
//---- indicator version number
#property version   "1.00"
//---- drawing the indicator in the main window
#property indicator_chart_window 
//----two buffers are used for calculation and drawing of the indicator
#property indicator_buffers 2
//---- only two plots are used
#property indicator_plots   2
//+----------------------------------------------+
//|  Parameters of drawing the bearish indicator |
//+----------------------------------------------+
//---- drawing the indicator 1 as a symbol
#property indicator_type1   DRAW_ARROW
//---- Tomato color is used as the color of the bearish indicator line
#property indicator_color1  clrTomato
//---- indicator 1 line width is equal to 4
#property indicator_width1  4
//---- bullish indicator label display
#property indicator_label1  "F2a_AO Sell"
//+----------------------------------------------+
//|  Bullish indicator drawing parameters        |
//+----------------------------------------------+
//---- drawing the indicator 2 as a symbol
#property indicator_type2   DRAW_ARROW
//---- DodgerBlue color is used for the indicator bullish line
#property indicator_color2  clrDodgerBlue
//---- thickness of the indicator 2 line is equal to 4
#property indicator_width2  4
//---- bearish indicator label display
#property indicator_label2 "F2a_AO Buy"
//+----------------------------------------------+
//|  declaring constants                         |
//+----------------------------------------------+
#define RESET 0 // The constant for getting the command for the indicator recalculation back to the terminal
//+----------------------------------------------+
//| Indicator input parameters                   |
//+----------------------------------------------+
input uint  MA_Filtr=3;
input uint  MA_Fast=13;
input uint  MA_Slow=144;
//+----------------------------------------------+

//---- declaration of dynamic arrays that will further be 
// used as indicator buffers
double SellBuffer[];
double BuyBuffer[];
//---- declaration of the integer variables for the start of data calculation
int min_rates_total;
//---- declaration of global variables
int Count[];
double Value1[],Value2[];
//+------------------------------------------------------------------+
// CMoving_Average class description                                 |
//+------------------------------------------------------------------+
#include <SmoothAlgorithms.mqh> 
//+------------------------------------------------------------------+
//|  Recalculation of position of the newest element in the array    |
//+------------------------------------------------------------------+   
void Recount_ArrayZeroPos(
                          int &CoArr[],// Return the current value of the price series by the link
                          int Size
                          )
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
//---- initialization of global variables
   int nshift=3;
   min_rates_total=int(MathMax(MathMax(MA_Filtr,MA_Fast),MA_Slow)+nshift+9);

//---- memory allocation for arrays of variables  
   ArrayResize(Count,nshift);
   ArrayResize(Value1,nshift);
   ArrayResize(Value2,nshift);

   ArrayInitialize(Count,0);
   ArrayInitialize(Value1,0.0);
   ArrayInitialize(Value2,0.0);

//---- set dynamic array as an indicator buffer
   SetIndexBuffer(0,SellBuffer,INDICATOR_DATA);
//---- shifting the start of drawing of the indicator 1
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- indicator symbol
   PlotIndexSetInteger(0,PLOT_ARROW,234);
//---- setting values of the indicator that won't be visible on a chart
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,0);

//---- set dynamic array as an indicator buffer
   SetIndexBuffer(1,BuyBuffer,INDICATOR_DATA);
//---- shifting the starting point of calculation of drawing of the indicator 2
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,min_rates_total);
//---- indicator symbol
   PlotIndexSetInteger(1,PLOT_ARROW,233);
//---- setting values of the indicator that won't be visible on a chart
   PlotIndexSetDouble(1,PLOT_EMPTY_VALUE,0);

//---- Setting the format of accuracy of displaying the indicator
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits);
//---- name for the data window and the label for sub-windows 
   string short_name="F2a_AO";
   IndicatorSetString(INDICATOR_SHORTNAME,short_name);
//----   
  }
//+------------------------------------------------------------------+
//| Custom indicator iteration function                              |
//+------------------------------------------------------------------+
int OnCalculate(const int rates_total,
                const int prev_calculated,
                const datetime &time[],
                const double &open[],
                const double &high[],
                const double &low[],
                const double &close[],
                const long &tick_volume[],
                const long &volume[],
                const int &spread[])
  {
//---- checking for the sufficiency of bars for the calculation
   if(rates_total<min_rates_total) return(RESET);

//---- declaration of local variables 
   int first,bar;
   static int startbar,trend;
   double series,fema,sema,current,prev;
//---- declaration of the CMoving_Average class variables from the SmoothAlgorithms.mqh file 
   static CMoving_Average EMA1,EMA2,EMA3;

//---- calculation of the starting number first for the bar recalculation loop
   if(prev_calculated>rates_total || prev_calculated<=0) //checking for the first start of calculation of an indicator
     {
      first=10; // starting number for calculation of all bars
      startbar=first;
      trend=0;
     }
   else first=prev_calculated-1; // starting index for the calculation of new bars

//---- main loop of the indicator calculation
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      series=(close[bar]*5+open[bar]*2+high[bar]+low[bar])/9;
      fema=EMA1.EMASeries(startbar,prev_calculated,rates_total,MA_Fast,series,bar,false);
      sema=EMA2.EMASeries(startbar,prev_calculated,rates_total,MA_Slow,series,bar,false);
      Value1[Count[0]]=fema-sema;
      Value2[Count[0]]=EMA3.EMASeries(startbar,prev_calculated,rates_total,MA_Filtr,series,bar,false);
      current=Value2[Count[0]];
      prev=Value2[Count[1]];

      double AvgRange=0.0;
      for(int count=bar; count>=bar-9; count--) AvgRange+=MathAbs(high[count]-low[count]);
      double Range=AvgRange/10;

      BuyBuffer[bar]=0.0;
      SellBuffer[bar]=0.0;

      if(trend<=0)
         if(Value1[Count[0]]>Value1[Count[1]] && current>=prev && Value1[Count[1]]<=Value1[Count[2]])
           {
            BuyBuffer[bar]=low[bar]-Range*0.5;
            if(bar<rates_total-1) trend=+1;
           }

      if(trend>=0)
         if(Value1[Count[0]]<Value1[Count[1]] && current<=prev && Value1[Count[1]]>=Value1[Count[2]])
           {
            SellBuffer[bar]=high[bar]+Range*0.5;
            if(bar<rates_total-1) trend=-1;
           }

      if(bar<rates_total-1) Recount_ArrayZeroPos(Count,3);
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
