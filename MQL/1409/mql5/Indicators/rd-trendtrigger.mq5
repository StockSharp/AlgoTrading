//+---------------------------------------------------------------------+
//|                                                 RD-TrendTrigger.mq5 | 
//|                                   Copyright © 2007, Paul Y. Shimada | 
//|                                             www.strategybuilder.com | 
//|	    Notes := Modified version of Trend Trigger Factor by mikesbon | 
//|	    (thanks to perkyz)                                            |
//+---------------------------------------------------------------------+ 
//| For the indicator to work, place the file SmoothAlgorithms.mqh      |
//| in the directory: terminal_data_folder\MQL5\Include                 |
//+---------------------------------------------------------------------+
#property copyright "Copyright © 2007, Paul Y. Shimada"
#property link "www.strategybuilder.com"
//---- indicator version number
#property version   "1.10"
//---- drawing indicator in a separate window
#property indicator_separate_window
//---- number of indicator buffers
#property indicator_buffers 1 
//---- only one plot is used
#property indicator_plots   1
//+----------------------------------------------+
//|  Indicator drawing parameters                |
//+----------------------------------------------+
//---- drawing the indicator as a line
#property indicator_type1   DRAW_LINE
//---- MediumSlateBlue color is used as the color of the indicator line
#property indicator_color1 clrMediumSlateBlue
//---- the indicator line is a continuous curve
#property indicator_style1  STYLE_SOLID
//---- indicator line width is equal to 1
#property indicator_width1  1
//---- displaying the indicator label
#property indicator_label1  "RD-TrendTrigger"
//+----------------------------------------------+
//|  XMA class description                       |
//+----------------------------------------------+
#include <SmoothAlgorithms.mqh> 
//+----------------------------------------------+
//---- declaration of the CMoving_Average and CT3 classes variables from the SmoothAlgorithms.mqh file
CT3 T3;
//+----------------------------------------------+
//|  INDICATOR INPUT PARAMETERS                  |
//+----------------------------------------------+
input uint Regress=15;
input uint T3Length=5;//smoothing depth                    
input int T3Phase=70; //smoothing parameter
input int Shift=0; // horizontal shift of the indicator in bars
//+----------------------------------------------+
//| Parameters of displaying horizontal levels   |
//+----------------------------------------------+
input int HighLevel=  +50;
input int MiddleLevel=  0;
input int LowLevel=   -50;
//+----------------------------------------------+

//---- declaration of a dynamic array that further 
// will be used as an indicator buffer
double IndBuffer[];

//---- Declaration of integer variables of data starting point
int min_rates_,min_rates_total;
//+------------------------------------------------------------------+   
//| RD-TrendTrigger indicator initialization function                | 
//+------------------------------------------------------------------+ 
void OnInit()
  {
//---- Initialization of variables of the start of data calculation
   min_rates_=int(2*Regress+1);
   min_rates_total=min_rates_+1;

//---- set dynamic array as an indicator buffer
   SetIndexBuffer(0,IndBuffer,INDICATOR_DATA);
//---- moving the indicator 1 horizontally
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//---- performing the shift of beginning of indicator drawing
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- setting the indicator values that won't be visible on a chart
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,0);
//---- indexing elements in the buffer as time series
   ArraySetAsSeries(IndBuffer,true);

//--- creation of the name to be displayed in a separate sub-window and in a pop up help
   IndicatorSetString(INDICATOR_SHORTNAME,"RD-TrendTrigger");

//--- determining the accuracy of displaying the indicator values
   IndicatorSetInteger(INDICATOR_DIGITS,0);
   
//---- the number of the indicator 3 horizontal levels   
   IndicatorSetInteger(INDICATOR_LEVELS,3);
//---- values of the indicator horizontal levels   
   IndicatorSetDouble(INDICATOR_LEVELVALUE,0,HighLevel);
   IndicatorSetDouble(INDICATOR_LEVELVALUE,1,MiddleLevel);
   IndicatorSetDouble(INDICATOR_LEVELVALUE,2,LowLevel);
//---- gray and magenta colors are used for horizontal levels lines  
   IndicatorSetInteger(INDICATOR_LEVELCOLOR,0,clrTeal);
   IndicatorSetInteger(INDICATOR_LEVELCOLOR,1,clrGray);
   IndicatorSetInteger(INDICATOR_LEVELCOLOR,2,clrMagenta);
//---- short dot-dash is used for the horizontal level line  
   IndicatorSetInteger(INDICATOR_LEVELSTYLE,0,STYLE_DASHDOTDOT);
   IndicatorSetInteger(INDICATOR_LEVELSTYLE,1,STYLE_DASHDOTDOT);
   IndicatorSetInteger(INDICATOR_LEVELSTYLE,2,STYLE_DASHDOTDOT);
//---- end of initialization
  }
//+------------------------------------------------------------------+ 
//| RD-TrendTrigger iteration function                               | 
//+------------------------------------------------------------------+ 
int OnCalculate(
                const int rates_total,    // amount of history in bars at the current tick
                const int prev_calculated,// amount of history in bars at the previous tick
                const datetime &Time[],
                const double &Open[],
                const double &High[],
                const double &Low[],
                const double &Close[],
                const long &Tick_Volume[],
                const long &Volume[],
                const int &Spread[]
                )
  {
//---- checking the number of bars to be enough for calculation
   if(rates_total<min_rates_total) return(0);

//---- declaration of variables with a floating point  
   double HighestHighRecent,HighestHighOlder,LowestLowRecent,LowestLowOlder,BuyPower,SellPower,TTF,Res;
//---- Declaration of integer variables
   int limit,maxbar;
   
   maxbar=rates_total-min_rates_;

//---- calculation of the starting number limit for the bar recalculation loop
   if(prev_calculated>rates_total || prev_calculated<=0)// checking for the first start of the indicator calculation
      limit=rates_total-min_rates_total-1; // starting index for the calculation of all bars
   else limit=rates_total-prev_calculated;  // starting index for calculation of new bars only

//---- indexing elements in arrays as timeseries  
   ArraySetAsSeries(High,true);
   ArraySetAsSeries(Low,true);

//---- main indicator calculation loop
   for(int bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      HighestHighRecent=High[ArrayMaximum(High,bar,Regress)];
      HighestHighOlder=High[ArrayMaximum(High,bar+Regress,Regress)];
      LowestLowRecent=Low[ArrayMinimum(Low,bar,Regress)];
      LowestLowOlder=Low[ArrayMinimum(Low,bar+Regress,Regress)];
      BuyPower=HighestHighRecent-LowestLowOlder;
      SellPower=HighestHighOlder-LowestLowRecent;
      Res=BuyPower+SellPower;
      if(Res) TTF=(BuyPower-SellPower)/(0.5 *Res)*100;
      else TTF=0.0;

      IndBuffer[bar]=T3.T3Series(maxbar,prev_calculated,rates_total,MODE_T3,T3Phase,T3Length,TTF,bar,true);
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
