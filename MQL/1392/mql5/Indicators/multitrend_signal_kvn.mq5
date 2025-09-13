//+------------------------------------------------------------------+
//|                                        MultiTrend_Signal_KVN.mq5 | 
//|                               Copyright © 2007, Vladimir Korykin | 
//|                                            koryvladimir@inbox.ru | 
//+------------------------------------------------------------------+ 
#property copyright "Copyright © 2007, Vladimir Korykin"
#property link "koryvladimir@inbox.ru"
//---- indicator version number
#property version   "1.00"
//---- drawing the indicator in the main window
#property indicator_chart_window 
//---- number of indicator buffers
#property indicator_buffers 2 
//---- only two plots are used
#property indicator_plots   2
//+----------------------------------------------+
//|  declaring constants                         |
//+----------------------------------------------+
#define RESET 0 // The constant for returning the indicator recalculation command to the terminal
//+----------------------------------------------+
//|  Bullish indicator drawing parameters        |
//+----------------------------------------------+
//---- drawing the indicator 1 as a label
#property indicator_type1   DRAW_ARROW
//---- Teal color is used for indicator line
#property indicator_color1  clrTeal
//---- the indicator 1 line is a dot-dash one
#property indicator_style1  STYLE_DASHDOTDOT
//---- indicator 1 line width is equal to 2
#property indicator_width1  2
//---- displaying the indicator line label
#property indicator_label1  "Buy MultiTrend_Signal_KVN"
//+----------------------------------------------+
//|  Bearish indicator drawing parameters        |
//+----------------------------------------------+
//---- drawing the indicator 2 as a label
#property indicator_type2   DRAW_ARROW
//---- magenta color is used for the indicator line
#property indicator_color2  clrMagenta
//---- the indicator 2 line is a dot-dash one
#property indicator_style2  STYLE_DASHDOTDOT
//---- indicator 2 line width is equal to 2
#property indicator_width2  2
//---- displaying the indicator line label
#property indicator_label2  "Sell MultiTrend_Signal_KVN"

//+----------------------------------------------+
//|  INDICATOR INPUT PARAMETERS                  |
//+----------------------------------------------+
input   uint     K=48;
input   double   Kstop=0.5;
input   uint     Kperiod=150;
input   uint     PerADX=14;
input   int      Shift=0; // horizontal shift of the indicator in bars
//+----------------------------------------------+

//---- declaration of dynamic arrays that will further be 
// used as indicator buffers
double BuyBuffer[];
double SellBuffer[];

double K100;
//---- Declaration of integer variables for the indicator handles
int ADX_Handle;
//---- Declaration of integer variables of data starting point
int min_rates_total;
//+------------------------------------------------------------------+   
//| MultiTrend_Signal_KVN indicator initialization function          | 
//+------------------------------------------------------------------+ 
void OnInit()
  {
//---- Initialization of variables of the start of data calculation
   min_rates_total=int(Kperiod+PerADX+1);
   K100=K/50;

//---- getting handle of the iADX indicator
   ADX_Handle=iADX(NULL,PERIOD_CURRENT,PerADX);
   if(ADX_Handle==INVALID_HANDLE)Print(" Failed to get handle of the iADX indicator");

//---- set dynamic array as an indicator buffer
   SetIndexBuffer(0,BuyBuffer,INDICATOR_DATA);
//---- indexing buffer elements as time series   
   ArraySetAsSeries(BuyBuffer,true);
//---- moving the indicator 1 horizontally
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//---- performing the shift of beginning of indicator drawing
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- setting the indicator values that won't be visible on a chart
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,0.0);
//---- indicator symbol
   PlotIndexSetInteger(0,PLOT_ARROW,217);

//---- set dynamic array as an indicator buffer
   SetIndexBuffer(1,SellBuffer,INDICATOR_DATA);
//---- indexing buffer elements as time series   
   ArraySetAsSeries(SellBuffer,true);
//---- moving the indicator 1 horizontally
   PlotIndexSetInteger(1,PLOT_SHIFT,Shift);
//---- performing the shift of beginning of indicator drawing
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,min_rates_total);
//---- setting the indicator values that won't be visible on a chart
   PlotIndexSetDouble(1,PLOT_EMPTY_VALUE,0.0);
//---- indicator symbol
   PlotIndexSetInteger(1,PLOT_ARROW,218);

//---- creating name for displaying if separate sub-window and in tooltip
   IndicatorSetString(INDICATOR_SHORTNAME,"MultiTrend_Signal_KVN");

//---- determine the accuracy of displaying indicator values
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits);
//---- end of initialization 
  }
//+------------------------------------------------------------------+ 
//| MultiTrend_Signal_KVN iteration function                         | 
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
   if(BarsCalculated(ADX_Handle)<rates_total || rates_total<min_rates_total) return(RESET);

//---- declaration of variables with a floating point  
   double ADX[],Range,AvgRange,Swing;
   double smin,smax,SsMax,SsMin,val1,val2;
//---- Declaration of integer variables
   int to_copy,limit,bar,SSP,Trend;
   static int prev_Trend;

//---- indexing elements in arrays as timeseries  
   ArraySetAsSeries(high,true);
   ArraySetAsSeries(low,true);
   ArraySetAsSeries(close,true);
   ArraySetAsSeries(ADX,true);

//---- calculations of the necessary amount of data to be copied and
//the starting number limit for the bar recalculation loop
   if(prev_calculated>rates_total || prev_calculated<=0)// checking for the first start of the indicator calculation
     {
      limit=rates_total-1-min_rates_total; // starting index for the calculation of all bars
      prev_Trend=0;
     }
   else limit=rates_total-prev_calculated; // starting index for the calculation of new bars
//----   
   to_copy=limit+1;

//---- copy newly appeared data into the arrays
   if(CopyBuffer(ADX_Handle,MAIN_LINE,0,to_copy,ADX)<=0) return(RESET);

//---- Main calculation loop of the indicator
   for(bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      SSP=int(MathCeil(Kperiod/ADX[bar]));
      SSP=MathMax(1,SSP);
      Range=0;
      AvgRange=0;
      for(int i1=bar+SSP-1; i1>=bar; i1--) AvgRange+=MathAbs(high[i1]-low[i1]);
      Range=AvgRange/SSP;
      SsMax=high[ArrayMaximum(high,bar,SSP)];
      SsMin=low[ArrayMinimum(low,bar,SSP)];
      Swing=(SsMax-SsMin)*K/100;
      smin = SsMin+Swing;
      smax = SsMax-Swing;      
      Trend=prev_Trend;
      val1=0.0;
      val2=0.0;

      if(close[bar]<smin)
        {
         Trend=-1;
         if(prev_Trend>-1) val1=high[bar]+Range*Kstop;
        }

      if(close[bar]>smax)
        {
         Trend=+1; 
         if(prev_Trend<+1) val2=low[bar]-Range*Kstop;
        }

      BuyBuffer[bar]=val2;
      SellBuffer[bar]=val1;
      
      //---- memorize values of the variables before running at the current bar
      if(bar) prev_Trend=Trend;
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
