//+------------------------------------------------------------------+
//|                                                   ColorMETRO.mq5 | 
//|                           Copyright © 2005, TrendLaboratory Ltd. |
//|                                       E-mail: igorad2004@list.ru |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2005, TrendLaboratory Ltd."
#property link      "E-mail: igorad2004@list.ru"
#property description "METRO"
//---- indicator version number
#property version   "1.00"
//---- drawing indicator in a separate window
#property indicator_separate_window
//---- number of indicator buffers 3
#property indicator_buffers 3 
//---- only two plots are used
#property indicator_plots   2
//+----------------------------------------------+
//|  StepRSI indicator drawing parameters        |
//+----------------------------------------------+
//---- drawing the indicator 1 as a cloud
#property indicator_type1   DRAW_FILLING
//---- blue color is used for the indicator bearish line
#property indicator_color1  Blue,Magenta
//---- line of the indicator 1 is a continuous curve
#property indicator_style1  STYLE_SOLID
//---- thickness of line of the indicator 1 is equal to 1
#property indicator_width1  1
//---- displaying of the bearish label of the indicator
#property indicator_label1  "Step RSI"
//+----------------------------------------------+
//|  RSI indicator drawing parameters            |
//+----------------------------------------------+
//---- drawing the indicator 2 as a line
#property indicator_type2   DRAW_LINE
//---- orange color is used as the color of the bullish line of the indicator
#property indicator_color2  Orange
//---- the indicator 2 line is a continuous curve
#property indicator_style2  STYLE_SOLID
//---- indicator 2 line width is equal to 1
#property indicator_width2  1
//---- displaying of the bullish label of the indicator
#property indicator_label2  "RSI"

//+----------------------------------------------+
//| Parameters of displaying horizontal levels   |
//+----------------------------------------------+
#property indicator_level1  70
#property indicator_level2  50
#property indicator_level3  30
#property indicator_levelcolor Gray
#property indicator_levelstyle STYLE_DASHDOTDOT
//+----------------------------------------------+
//| Indicator window size limitation             |
//+----------------------------------------------+
#property indicator_minimum   0
#property indicator_maximum 100
//+----------------------------------------------+
//| Indicator input parameters                   |
//+----------------------------------------------+
input int PeriodRSI=7;//indicator period
input int StepSizeFast=5;//fast step
input int StepSizeSlow=15;//slow step
input int Shift=0; //horizontal shift of the indicator in bars 
//+----------------------------------------------+
//---- declaration of dynamic arrays that will further be 
// used as indicator buffers
double Line1Buffer[];
double Line2Buffer[];
double Line3Buffer[];
//---- Declaration of integer variables for the indicator handles
int RSI_Handle;
//---- Declaration of integer variables of data starting point
int min_rates_total;
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+  
void OnInit()
  {
//---- Initialization of variables of the start of data calculation
   min_rates_total=PeriodRSI;

//---- getting handle of the RSI indicator
   RSI_Handle=iRSI(NULL,0,PeriodRSI,PRICE_CLOSE);
   if(RSI_Handle==INVALID_HANDLE) Print(" Failed to get handle of the RSI indicator");

//---- set dynamic array as an indicator buffer
   SetIndexBuffer(0,Line2Buffer,INDICATOR_DATA);
//---- shifting the indicator 2 horizontally by Shift
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//---- shifting the starting point for drawing indicator 1 by min_rates_total
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- indexing buffer elements as time series   
   ArraySetAsSeries(Line2Buffer,true);

//---- set dynamic array as an indicator buffer
   SetIndexBuffer(1,Line3Buffer,INDICATOR_DATA);
//---- shifting the indicator 3 horizontally by Shift
   PlotIndexSetInteger(1,PLOT_SHIFT,Shift);
//---- shifting the starting point for drawing indicator 2 by min_rates_total
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,min_rates_total);
//---- indexing buffer elements as time series   
   ArraySetAsSeries(Line3Buffer,true);
   
//---- set dynamic array as an indicator buffer
   SetIndexBuffer(2,Line1Buffer,INDICATOR_DATA);
//---- shifting indicator 1 horizontally by Shift
   PlotIndexSetInteger(2,PLOT_SHIFT,Shift);
//---- shifting the starting point for drawing indicator 2 by min_rates_total
   PlotIndexSetInteger(2,PLOT_DRAW_BEGIN,min_rates_total);
//---- indexing buffer elements as time series   
   ArraySetAsSeries(Line1Buffer,true);


//---- initializations of variable for indicator short name
   string shortname;
   StringConcatenate(shortname,"METRO(",PeriodRSI,", ",StepSizeFast,", ",StepSizeSlow,", ",Shift,")");
//--- creation of the name to be displayed in a separate sub-window and in a pop up help
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
//--- determining the accuracy of displaying the indicator values
   IndicatorSetInteger(INDICATOR_DIGITS,2);
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
   if(BarsCalculated(RSI_Handle)<rates_total || rates_total<min_rates_total) return(0);

//---- declaration of local variables 
   int limit,to_copy,bar,ftrend,strend;
   double fmin0,fmax0,smin0,smax0,RSI0,RSI[];
   static double fmax1,fmin1,smin1,smax1;
   static int ftrend_,strend_;

//---- indexing elements in arrays as timeseries  
   ArraySetAsSeries(RSI,true);

//---- calculation of the starting number first for the bar recalculation loop
   if(prev_calculated>rates_total || prev_calculated<=0) // checking for the first start of calculation of an indicator
     {
      limit=rates_total-1; // starting index for the calculation of all bars

      fmin1=+999999;
      fmax1=-999999;
      smin1=+999999;
      smax1=-999999;
      ftrend_=0;
      strend_=0;
     }
   else limit=rates_total-prev_calculated; // starting index for the calculation of new bars

   to_copy=limit+1;

//--- copy newly appeared data in the array
   if(CopyBuffer(RSI_Handle,0,0,to_copy,RSI)<=0) return(0);

//---- restore values of the variables
   ftrend = ftrend_;
   strend = strend_;

//---- main indicator calculation loop
   for(bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      //---- memorize values of the variables before running at the current bar
      if(rates_total!=prev_calculated && bar==0)
        {
         ftrend_=ftrend;
         strend_=strend;
        }

      RSI0=RSI[bar];

      fmax0=RSI0+2*StepSizeFast;
      fmin0=RSI0-2*StepSizeFast;

      if(RSI0>fmax1)  ftrend=+1;
      if(RSI0<fmin1)  ftrend=-1;

      if(ftrend>0 && fmin0<fmin1) fmin0=fmin1;
      if(ftrend<0 && fmax0>fmax1) fmax0=fmax1;

      smax0=RSI0+2*StepSizeSlow;
      smin0=RSI0-2*StepSizeSlow;

      if(RSI0>smax1)  strend=+1;
      if(RSI0<smin1)  strend=-1;

      if(strend>0 && smin0<smin1) smin0=smin1;
      if(strend<0 && smax0>smax1) smax0=smax1;

      Line1Buffer[bar]=RSI0;

      if(ftrend>0) Line2Buffer[bar]=fmin0+StepSizeFast;
      if(ftrend<0) Line2Buffer[bar]=fmax0-StepSizeFast;
      if(strend>0) Line3Buffer[bar]=smin0+StepSizeSlow;
      if(strend<0) Line3Buffer[bar]=smax0-StepSizeSlow;

      if(bar>0)
        {
         fmin1=fmin0;
         fmax1=fmax0;
         smin1=smin0;
         smax1=smax0;
        }
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
