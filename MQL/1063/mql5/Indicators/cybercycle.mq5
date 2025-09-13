//+------------------------------------------------------------------+
//|                                                   CyberCycle.mq5 |
//|                                                                  |
//| Cyber Cycle                                                      |
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
//---- two buffers are used for the indicator calculation and drawing
#property indicator_buffers 2
//---- two plots are used
#property indicator_plots   2
//+----------------------------------------------+
//|  Cycle indicator drawing parameters          |
//+----------------------------------------------+
//---- drawing indicator 1 as a line
#property indicator_type1   DRAW_LINE
//---- red color is used as the color of the bullish line of the indicator
#property indicator_color1  Red
//---- line of the indicator 1 is a continuous curve
#property indicator_style1  STYLE_SOLID
//---- thickness of line of the indicator 1 is equal to 1
#property indicator_width1  1
//---- displaying of the bullish label of the indicator
#property indicator_label1  "Cyber Cycle"
//+----------------------------------------------+
//|  Trigger indicator drawing parameters        |
//+----------------------------------------------+
//---- drawing the indicator 2 as a line
#property indicator_type2   DRAW_LINE
//---- blue color is used for the indicator bearish line
#property indicator_color2  Blue
//---- the indicator 2 line is a continuous curve
#property indicator_style2  STYLE_SOLID
//---- indicator 2 line width is equal to 1
#property indicator_width2  1
//---- displaying of the bearish label of the indicator
#property indicator_label2  "Trigger"
//+----------------------------------------------+
//| Parameters of displaying horizontal levels   |
//+----------------------------------------------+
#property indicator_level1 0.0
#property indicator_levelcolor Gray
#property indicator_levelstyle STYLE_DASHDOTDOT
//+----------------------------------------------+
//| Indicator input parameters                   |
//+----------------------------------------------+
input double Alpha=0.07;// Indicator ratio 
input int Shift=0; // horizontal shift of the indicator in bars 
//+----------------------------------------------+
//---- declaration of dynamic arrays that will further be 
// used as indicator buffers
double CycleBuffer[];
double TriggerBuffer[];
//---- Declaration of integer variables of data starting point
int min_rates_total;
//---- declaration of dynamic arrays that will further be 
// used as ring buffers
int Count[];
double Smooth[],Price[];
//---- Declaration of global variables
double K0,K1,K2,K3;
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

   Max2=7;
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
   min_rates_total=7;
   
   //---- Initialization of variables
   K0=MathPow((1.0 - 0.5*Alpha),2);
   K1=2.0;
   K2=K1 *(1.0 - Alpha);
   K3=MathPow((1.0 - Alpha),2);

//---- memory distribution for variables' arrays  
   ArrayResize(Count,7);
   ArrayResize(Smooth,7);
   ArrayResize(Price,7);

//---- Initialization of arrays of variables
   ArrayInitialize(Smooth,0.0);
   ArrayInitialize(Price,0.0);

//---- set dynamic array as an indicator buffer
   SetIndexBuffer(0,CycleBuffer,INDICATOR_DATA);
//---- shifting indicator 1 horizontally by Shift
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//---- shifting the starting point for drawing indicator 1 by min_rates_total
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- indexing elements in the buffer as time series
   ArraySetAsSeries(CycleBuffer,true);

//---- set dynamic array as an indicator buffer
   SetIndexBuffer(1,TriggerBuffer,INDICATOR_DATA);
//---- shifting the indicator 2 horizontally by Shift
   PlotIndexSetInteger(1,PLOT_SHIFT,Shift);
//---- shifting the starting point for drawing indicator 2 by min_rates_total+1
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,min_rates_total+1);
//---- indexing elements in the buffer as time series
   ArraySetAsSeries(TriggerBuffer,true);

//---- initializations of variable for indicator short name
   string shortname;
   StringConcatenate(shortname,"Cyber Cycle(",DoubleToString(Alpha,4),", ",Shift,")");
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
   if(rates_total<min_rates_total) return(0);

//---- declaration of local variables 
   int MaxBar,limit,bar;
   int bar0,bar1,bar2,bar3;

//---- calculation of the starting number limit for the bar recalculation loop
   if(prev_calculated>rates_total || prev_calculated<=0)// checking for the first start of the indicator calculation
      limit=rates_total-2; // starting index for the calculation of all bars
   else limit=rates_total-prev_calculated; // starting index for the calculation of new bars

   MaxBar=rates_total-min_rates_total-3;

//---- indexing elements in arrays as timeseries  
   ArraySetAsSeries(high,true);
   ArraySetAsSeries(low,true);

//---- main indicator calculation loop
   for(bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      bar0=Count[0];
      bar1=Count[1];
      bar2=Count[2];
      bar3=Count[3];

      Price[bar0]=(high[bar]+low[bar])/2.0;
      Smooth[bar0]=(Price[bar0]+2.0*Price[bar1]+2.0*Price[bar2]+Price[bar3])/6.0;

      if(bar<=MaxBar) CycleBuffer[bar]=K0*(Smooth[bar0]-K1*Smooth[bar1]+Smooth[bar2])+K2*CycleBuffer[bar+1]-K3*CycleBuffer[bar+2];
      else CycleBuffer[bar]=(Price[bar0]-2.0*Price[bar1]+Price[bar2])/4.0;

      TriggerBuffer[bar]=CycleBuffer[bar+1];
      if(bar>0) Recount_ArrayZeroPos(Count);
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
