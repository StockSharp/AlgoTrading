//+------------------------------------------------------------------+
//|                                                 Adaptive RVI.mq5 |
//|                                                                  |
//| Adaptive RVI                                                     |
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
//| Adaptive RVI indicator drawing parameters    |
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
#property indicator_label1  "Adaptive RVI"
//+----------------------------------------------+
//| Trigger indicator drawing parameters         |
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
//+----------------------------------------------+
//|  Declaration of constants                    |
//+----------------------------------------------+
#define RESET 0       // the constant for getting the command for the indicator recalculation back to the terminal
#define MAXPERIOD 100 // the constant for the maximum period limitation
//+----------------------------------------------+
//| Indicator input parameters                   |
//+----------------------------------------------+
input double Alpha=0.07;  // Indicator smoothing ratio 
input int Shift=0;        // Horizontal shift of the indicator in bars 
//+----------------------------------------------+
//---- declaration of dynamic arrays that
//---- will be used as indicator buffers
double ARVIBuffer[];
double TriggerBuffer[];
//---- declaration of integer variables for the indicators handles
int CP_Handle;
//---- declaration of the integer variables for the start of data calculation
int min_rates_total;
//---- declaration of global variables
int Count[];
double Value1[],Value2[];
//+------------------------------------------------------------------+
//|  Recalculation of position of the newest element in the array    |
//+------------------------------------------------------------------+   
void Recount_ArrayZeroPos(int &CoArr[],// Return the current value of the price series by the link
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
//|  Getting the difference of the price time series values          |
//+------------------------------------------------------------------+   
double Get_dPrice(const double  &Price1[],const double  &Price2[],int bar)
  {
//----
   return(Price1[bar]-Price2[bar]);
  }
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+  
int OnInit()
  {
//---- initialization of variables of the start of data calculation
   min_rates_total=4;

//---- getting handle of the CyclePeriod indicator
   CP_Handle=iCustom(NULL,0,"CyclePeriod",Alpha);
   if(CP_Handle==INVALID_HANDLE)
     {
      Print(" Failed to get handle of the CyclePeriod indicator");
      return(1);
     }

//---- memory allocation for arrays of variables  
   ArrayResize(Count,MAXPERIOD);
   ArrayResize(Value1,MAXPERIOD);
   ArrayResize(Value2,MAXPERIOD);
   
   ArrayInitialize(Count,0);
   ArrayInitialize(Value1,0.0);
   ArrayInitialize(Value2,0.0);

//---- set ARVIBuffer[] dynamic array as an indicator buffer
   SetIndexBuffer(0,ARVIBuffer,INDICATOR_DATA);
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
   StringConcatenate(shortname,"Adaptive RVI(",DoubleToString(Alpha,4),", ",Shift,")");
//---- creating name for displaying if separate sub-window and in tooltip
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
//---- determine the accuracy of displaying indicator values
   IndicatorSetInteger(INDICATOR_DIGITS,2);
   
   return(0);
//----
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
   int first,bar,Length;
   double Num,Denom,rvi,period[4];

//---- calculation of the 'first' starting number for the bars recalculation loop
   if(prev_calculated>rates_total || prev_calculated<=0) // checking for the first start of the indicator calculation
      first=3;                   // starting index for calculation of all bars
   else first=prev_calculated-1; // starting index for calculation of new bars

//---- main loop of the indicator calculation
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      //--- copy newly appeared data in the array
      if(CopyBuffer(CP_Handle,0,rates_total-1-bar,4,period)<=0) return(RESET);

      Length=int(MathFloor((4.0*period[0]+3.0*period[1]+2.0*period[2]+period[3])/20.0));
      if(bar<Length) Length=bar; // cutting the smoothing down to the real number of bars

      Value1[Count[0]]=(Get_dPrice(close,open,bar)
                        +2.0*Get_dPrice(close,open,bar-1)
                        +2.0*Get_dPrice(close,open,bar-2)
                        +Get_dPrice(close,open,bar-3))/6.0;

      Value2[Count[0]]=(Get_dPrice(high,low,bar)
                        +2.0*Get_dPrice(high,low,bar-1)
                        +2.0*Get_dPrice(high,low,bar-2)
                        +Get_dPrice(high,low,bar-3))/6.0;

      Num=0.0;
      Denom=0.0;

      for(int iii=0; iii<Length; iii++)
        {
         Num+=Value1[Count[iii]];
         Denom+=Value2[Count[iii]];
        }

      if(Denom!=0.0) rvi=Num/Denom;
      else rvi=EMPTY_VALUE;

      ARVIBuffer[bar]=rvi;
      TriggerBuffer[bar]=ARVIBuffer[bar-1];

      if(bar<rates_total-1) Recount_ArrayZeroPos(Count,MAXPERIOD);
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
