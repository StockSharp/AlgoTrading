//+------------------------------------------------------------------+
//|                                                  DSSBressert.mq5 |
//|                      Copyright © 2008, MetaQuotes Software Corp. |
//|                                       http://www.metaquotes.net/ |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2008, MetaQuotes Software Corp."
#property link      "http://www.metaquotes.net/"
//---- indicator version number
#property version   "1.00"
//---- drawing indicator in a separate window
#property indicator_separate_window
//---- number of indicator buffers 2
#property indicator_buffers 2 
//---- only one plot is used
#property indicator_plots   1
//+-----------------------------------+
//|  Indicator drawing parameters     |
//+-----------------------------------+
//---- drawing the indicator as a line
#property indicator_type1   DRAW_FILLING
//---- the following colors are used for the indicator
#property indicator_color1 Blue,DeepPink
//---- displaying the indicator label
#property indicator_label1  "DSS Bressert"
//+----------------------------------------------+
//| Parameters of displaying horizontal levels   |
//+----------------------------------------------+
#property indicator_level1 80.0
#property indicator_level2 20.0
#property indicator_levelcolor Gray
#property indicator_levelstyle STYLE_DASHDOTDOT

//+-----------------------------------+
//|  INDICATOR INPUT PARAMETERS       |
//+-----------------------------------+
input uint  EMA_period=8;  //EMA period
input uint  Sto_period=13; //stochastic period
input int   Shift=0;       //horizontal shift of the indicator in bars
//+-----------------------------------+

//---- Declaration of integer variables of data starting point
int min_rates_total;
//---- declaration of dynamic arrays that will further be 
// used as indicator buffers
double DssBuffer[],MitBuffer[];
//---- Declaration of global variables
double smooth_coefficient;
//+------------------------------------------------------------------+   
//| XMA indicator initialization function                            | 
//+------------------------------------------------------------------+ 
void OnInit()
  {
//---- Initialization of variables of the start of data calculation
   min_rates_total=int(Sto_period+1);

//---- Initialization of variables   
   smooth_coefficient=2.0/(1.0+EMA_period);

//---- set dynamic array as an indicator buffer
   SetIndexBuffer(0,DssBuffer,INDICATOR_DATA);
//---- moving the indicator 1 horizontally
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//---- performing the shift of beginning of indicator drawing
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- setting the indicator values that won't be visible on a chart
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//---- indexing elements in the buffer as time series
   ArraySetAsSeries(DssBuffer,true);

//---- set dynamic array as an indicator buffer
   SetIndexBuffer(1,MitBuffer,INDICATOR_DATA);
//---- moving the indicator 1 horizontally
   PlotIndexSetInteger(1,PLOT_SHIFT,Shift);
//---- performing the shift of beginning of indicator drawing
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,min_rates_total);
//---- setting the indicator values that won't be visible on a chart
   PlotIndexSetDouble(1,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//---- indexing elements in the buffer as time series
   ArraySetAsSeries(MitBuffer,true);

//---- initializations of variable for indicator short name
   string shortname;
   StringConcatenate(shortname,"DSS Bressert(",EMA_period,", ",Sto_period,")");
//--- creation of the name to be displayed in a separate sub-window and in a pop up help
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);

//--- determining the accuracy of displaying the indicator values
   IndicatorSetInteger(INDICATOR_DIGITS,0);
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
   if(rates_total<min_rates_total) return(0);

//---- declaration of variables with a floating point  
   double HighRange,LowRange,delta,MIT,DSS;
//---- Declaration of integer variables and getting the bars already calculated
   int limit,bar;

//---- indexing elements in arrays as timeseries  
   ArraySetAsSeries(low,true);
   ArraySetAsSeries(high,true);
   ArraySetAsSeries(close,true);

//---- calculation of the 'limit' starting index for the bars recalculation loop
   if(prev_calculated>rates_total || prev_calculated<=0)// checking for the first start of the indicator calculation
     {
      limit=rates_total-min_rates_total-1; // starting index for calculation of all bars
      MitBuffer[limit+1]=50;
      DssBuffer[limit+1]=50;
     }
   else limit=rates_total-prev_calculated; // starting index for calculation of new bars

//---- main cycle of calculation of Mit indicator
   for(bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      HighRange=high[ArrayMaximum(high,bar,Sto_period)];
      LowRange=low[ArrayMinimum(low,bar,Sto_period)];
      delta=close[bar]-LowRange;
      MIT=delta/(HighRange-LowRange)*100.0;
      MitBuffer[bar]=smooth_coefficient*(MIT-MitBuffer[bar+1])+MitBuffer[bar+1];
     }

//---- main cycle of calculation of DSS indicator
   for(bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      HighRange=MitBuffer[ArrayMaximum(MitBuffer,bar,Sto_period)];
      LowRange=MitBuffer[ArrayMinimum(MitBuffer,bar,Sto_period)];
      delta=MitBuffer[bar]-LowRange;
      DSS=delta/(HighRange-LowRange)*100.0;
      DssBuffer[bar]=smooth_coefficient*(DSS-DssBuffer[bar+1])+DssBuffer[bar+1];
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
