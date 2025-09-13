//+------------------------------------------------------------------+
//|                                       Adaptive CG Oscillator.mq5 |
//|                                                                  |
//| Adaptive CG Oscillator                                           |
//|                                                                  |
//| Algorithm taken from book                                        |
//|     "Cybernetics Analysis for Stock and Futures"                 |
//| by John F. Ehlers                                                |
//|                                                                  |
//|                                              contact@mqlsoft.com |
//|                                          http://www.mqlsoft.com/ |
//+------------------------------------------------------------------+
//---- author of the indicator
#property copyright "Coded by Witold Wozniak"
//---- author of the indicator
#property link      "www.mqlsoft.com"
//---- indicator version number
#property version   "1.00"
//---- drawing indicator in a separate window
#property indicator_separate_window
//---- two buffers are used for calculation of drawing of the indicator
#property indicator_buffers 2
//---- two plots are used
#property indicator_plots   2
//+----------------------------------------------+
//|  CG indicator drawing parameters             |
//+----------------------------------------------+
//---- drawing indicator 1 as a line
#property indicator_type1   DRAW_LINE
//---- red color is used as the color of the indicator basic line
#property indicator_color1  Red
//---- line of the indicator 1 is a continuous curve
#property indicator_style1  STYLE_SOLID
//---- thickness of line of the indicator 1 is equal to 1
#property indicator_width1  1
//---- displaying the indicator line label
#property indicator_label1  "Adaptive CG Oscillator"
//+----------------------------------------------+
//|  Trigger indicator drawing parameters        |
//+----------------------------------------------+
//---- drawing the indicator 2 as a line
#property indicator_type2   DRAW_LINE
//---- blue color is used for the indicator signal line
#property indicator_color2  Blue
//---- the indicator 2 line is a continuous curve
#property indicator_style2  STYLE_SOLID
//---- indicator 2 line width is equal to 1
#property indicator_width2  1
//---- displaying the indicator line label
#property indicator_label2  "Trigger"
//+----------------------------------------------+
//| Parameters of displaying horizontal levels   |
//+----------------------------------------------+
#property indicator_level1 0.0
#property indicator_levelcolor Gray
#property indicator_levelstyle STYLE_DASHDOTDOT
//+-----------------------------------+
//|  Declaration of constants         |
//+-----------------------------------+
#define RESET 0 // the constant for getting the command for the indicator recalculation back to the terminal
//+----------------------------------------------+
//| Indicator input parameters                   |
//+----------------------------------------------+
input double Alpha=0.07;  // Indicator smoothing ratio 
input int Shift=0;        // Horizontal shift of the indicator in bars 
//+----------------------------------------------+
//---- declaration of dynamic arrays that 
//---- will be used as indicator buffers
double ACGBuffer[];
double TriggerBuffer[];
//---- declaration of variables for the indicators handles
int CP_Handle;
//---- declaration of the integer variables for the start of data calculation
int min_rates_total;
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+  
int OnInit()
  {
//---- initialization of variables of the start of data calculation
   min_rates_total=1;

//---- getting handle of the CyclePeriod indicator
   CP_Handle=iCustom(NULL,0,"CyclePeriod",Alpha);
   if(CP_Handle==INVALID_HANDLE)
     {
      Print(" Failed to get handle of the CyclePeriod indicator");
      return(1);
     }
//---- set ACGBuffer[] dynamic array as an indicator buffer
   SetIndexBuffer(0,ACGBuffer,INDICATOR_DATA);
//---- shifting indicator 1 horizontally by Shift
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//---- shifting the starting point of the indicator 1 drawing by min_rates_total
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- setting the indicator values that won't be visible on a chart
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,EMPTY_VALUE);

//---- set TriggerBuffer[] dynamic array as an indicator buffer
   SetIndexBuffer(1,TriggerBuffer,INDICATOR_DATA);
//---- shifting the indicator 2 horizontally by Shift
   PlotIndexSetInteger(1,PLOT_SHIFT,Shift);
//---- performing shift of the beginning of counting of drawing the indicator 2 by min_rates_total+1
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,min_rates_total+1);
//---- setting the indicator values that won't be visible on a chart
   PlotIndexSetDouble(1,PLOT_EMPTY_VALUE,EMPTY_VALUE);

//---- initializations of variable for indicator short name
   string shortname;
   StringConcatenate(shortname,"Adaptive CG Oscillator(",DoubleToString(Alpha,4),", ",Shift,")");
//---- creating name for displaying if separate sub-window and in tooltip
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
//---- determine the accuracy of displaying indicator values
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits);
//----
  return(0);
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
//---- checking for the sufficiency of bars for the calculation
   if(BarsCalculated(CP_Handle)<rates_total || rates_total<min_rates_total) return(RESET);

//---- declaration of local variables 
   int first,bar,intperiod;
   double price,Num,Denom,period[1];

//---- calculation of the 'first' starting number for the bars recalculation loop
   if(prev_calculated>rates_total || prev_calculated<=0) // checking for the first start of the indicator calculation
      first=1;                   // starting index for calculation of all bars
   else first=prev_calculated-1; // starting index for calculation of new bars

//---- main loop of the indicator calculation
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      //--- copy newly appeared data in the array
      if(CopyBuffer(CP_Handle,0,rates_total-1-bar,1,period)<=0) return(RESET);
      intperiod=int(MathFloor(period[0]/2.0));

      Num=0.0;
      Denom=0.0;
      if(bar<intperiod) intperiod=bar; // cutting the smoothing down to the real number of bars

      for(int count=0; count<intperiod; count++)
        {
         price=(high[bar-count]+low[bar-count])/2.0;
         Num += (1.0 + count) * price;
         Denom+=price;
        }

      if(Denom!=0.0) ACGBuffer[bar]=-Num/Denom+(intperiod+1.0)/2.0;
      else           ACGBuffer[bar]=EMPTY_VALUE;

      TriggerBuffer[bar]=ACGBuffer[bar-1];
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
