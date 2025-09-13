//+------------------------------------------------------------------+
//|                                              TrendEnvelopes.mq5  |
//|                           Copyright © 2006, TrendLaboratory Ltd. |
//|            http://finance.groups.yahoo.com/group/TrendLaboratory |
//|                                   E-mail: igorad2003@yahoo.co.uk |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2006, TrendLaboratory Ltd."
#property link      "http://finance.groups.yahoo.com/group/TrendLaboratory"
//---- indicator version number
#property version   "1.10"
//---- Drawing the indicator in the main window
#property indicator_chart_window
//---- 4 buffers are used for calculation and drawing the indicator
#property indicator_buffers 4
//---- 4 plots are used
#property indicator_plots   4
//+----------------------------------------------+
//|  Bullish indicator drawing parameters        |
//+----------------------------------------------+
//---- drawing indicator 1 as a line
#property indicator_type1   DRAW_LINE
//---- blue color is used for the indicator line
#property indicator_color1  clrBlue
//---- indicator 1 line is a solid one
#property indicator_style1  STYLE_SOLID
//---- indicator 1 line width is equal to 2
#property indicator_width1  2
//---- displaying the indicator line label
#property indicator_label1  "Upper TrendEnvelopes"
//+----------------------------------------------+
//|  Parameters of drawing the bearish indicator |
//+----------------------------------------------+
//---- drawing the indicator 2 as a line
#property indicator_type2   DRAW_LINE
//---- red color is used for the indicator line
#property indicator_color2  clrRed
//---- indicator 2 line is a solid one
#property indicator_style2  STYLE_SOLID
//---- indicator 2 line width is equal to 2
#property indicator_width2  2
//---- displaying the indicator line label
#property indicator_label2  "Lower TrendEnvelopes"
//+----------------------------------------------+
//|  Bullish indicator drawing parameters        |
//+----------------------------------------------+
//---- drawing the indicator 3 as a label
#property indicator_type3   DRAW_ARROW
//---- deep sky blue color is used for the indicator
#property indicator_color3  clrDeepSkyBlue
//---- indicator 3 width is equal to 4
#property indicator_width3  4
//---- displaying the indicator label
#property indicator_label3  "Buy TrendEnvelopes"
//+----------------------------------------------+
//|  Parameters of drawing the bearish indicator |
//+----------------------------------------------+
//---- drawing the indicator 4 as a label
#property indicator_type4   DRAW_ARROW
//---- red color is used for the indicator
#property indicator_color4  clrRed
//---- indicator 4 width is equal to 4
#property indicator_width4  4
//---- displaying the indicator label
#property indicator_label4  "Sell TrendEnvelopes"
//+----------------------------------------------+
//|  Declaration of constants                    |
//+----------------------------------------------+
#define RESET  0 // the constant for getting the command for the indicator recalculation back to the terminal
//+----------------------------------------------+
//|  Indicator input parameters                  |
//+----------------------------------------------+
input uint     MA_Period=14;
input ENUM_MA_METHOD MA_Method=MODE_EMA;
input ENUM_APPLIED_PRICE Applied_Price=PRICE_CLOSE;
input double   Deviation_=0.2;
input uint     ATRPeriod=15;        // ATR indicator period
input double   ATRSensitivity=0.5;  // ATR shift sensitivity
input int      Shift=0;             // Horizontal shift of the indicator in bars
//+----------------------------------------------+
//---- declaration of dynamic arrays that
//---- will be used as indicator buffers
double ExtMapBufferUp[];
double ExtMapBufferDown[];
double ExtMapBufferUp1[];
double ExtMapBufferDown1[];

double Deviation;
//---- declaration of integer variables for the indicators handles
int ATR_Handle,MA_Handle;
//---- declaration of the integer variables for the start of data calculation
int min_rates_total;
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+  
int OnInit()
  {
//---- getting handle of the ATR indicator
   ATR_Handle=iATR(NULL,0,ATRPeriod);
   if(ATR_Handle==INVALID_HANDLE)
     {
      Print(" Failed to get handle of the ATR indicator");
      return(1);
     }

//---- getting handle of the iMA indicator
   MA_Handle=iMA(NULL,0,MA_Period,0,MA_Method,Applied_Price);
   if(MA_Handle==INVALID_HANDLE)
     {
      Print(" Failed to get handle of the iMA indicator");
      return(1);
     }

//---- initialization of variables of the start of data calculation
   min_rates_total=int(MathMax(ATRPeriod,MA_Period)+1);
   Deviation=MathMax(Deviation_,0.1);
   Deviation=MathMin(Deviation_,100.0);

//---- set ExtMapBufferUp[] dynamic array as an indicator buffer
   SetIndexBuffer(0,ExtMapBufferUp,INDICATOR_DATA);
//---- shifting the indicator 1 horizontally by Shift
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//---- shifting the start of drawing of the indicator 1
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- indexing the elements in buffers as timeseries   
   ArraySetAsSeries(ExtMapBufferUp,true);
//---- setting the indicator values that won't be visible on a chart
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,EMPTY_VALUE);

//---- set ExtMapBufferDown[] dynamic array as an indicator buffer
   SetIndexBuffer(1,ExtMapBufferDown,INDICATOR_DATA);
//---- shifting the indicator 2 horizontally by Shift
   PlotIndexSetInteger(1,PLOT_SHIFT,Shift);
//---- shifting the start of drawing of the indicator 2
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,min_rates_total);
//---- indexing the elements in buffers as timeseries   
   ArraySetAsSeries(ExtMapBufferDown,true);
//---- setting the indicator values that won't be visible on a chart
   PlotIndexSetDouble(1,PLOT_EMPTY_VALUE,EMPTY_VALUE);

//---- set ExtMapBufferUp1[] dynamic array as an indicator buffer
   SetIndexBuffer(2,ExtMapBufferUp1,INDICATOR_DATA);
//---- shifting the indicator 1 horizontally by Shift
   PlotIndexSetInteger(2,PLOT_SHIFT,Shift);
//---- shifting the start of drawing of the indicator 3
   PlotIndexSetInteger(2,PLOT_DRAW_BEGIN,min_rates_total);
//---- indexing the elements in buffers as timeseries   
   ArraySetAsSeries(ExtMapBufferUp1,true);
//---- setting the indicator values that won't be visible on a chart
   PlotIndexSetDouble(2,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//---- indicator symbol
   PlotIndexSetInteger(2,PLOT_ARROW,159);

//---- set ExtMapBufferDown1[] dynamic array as an indicator buffer
   SetIndexBuffer(3,ExtMapBufferDown1,INDICATOR_DATA);
//---- shifting the indicator 2 horizontally by Shift
   PlotIndexSetInteger(3,PLOT_SHIFT,Shift);
//---- shifting the start of drawing of the indicator 4
   PlotIndexSetInteger(3,PLOT_DRAW_BEGIN,min_rates_total);
//---- indexing the elements in buffers as timeseries   
   ArraySetAsSeries(ExtMapBufferDown1,true);
//---- setting the indicator values that won't be visible on a chart
   PlotIndexSetDouble(3,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//---- indicator symbol
   PlotIndexSetInteger(3,PLOT_ARROW,159);

//--- creation of the name to be displayed in a separate sub-window and in a pop up help
   IndicatorSetString(INDICATOR_SHORTNAME,"TrendEnvelopes");
//--- determining the accuracy of displaying the indicator values
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
//---- checking the number of bars to be enough for the calculation
   if(BarsCalculated(ATR_Handle)<rates_total
      || BarsCalculated(MA_Handle)<rates_total
      || rates_total<min_rates_total)
      return(RESET);

//---- declaration of local variables 
   int limit,to_copy,bar,trend;
   double ATR[],MA[],smax,smin;
   static double prev_smax,prev_smin;
   static int prev_trend;

//---- indexing elements in arrays as time series  
   ArraySetAsSeries(low,true);
   ArraySetAsSeries(high,true);
   ArraySetAsSeries(close,true);
   ArraySetAsSeries(ATR,true);
   ArraySetAsSeries(MA,true);

//---- calculation of the 'limit' starting index for the bars recalculation loop
   if(prev_calculated>rates_total || prev_calculated<=0) // checking for the first start of the indicator calculation
     {
      limit=rates_total-min_rates_total-1;               // starting index for calculation of all bars
      prev_smax=0.0;
      prev_smin=999999999.9;
      prev_trend=0;
     }
   else
     {
      limit=rates_total-prev_calculated;                 // starting index for calculation of new bars
     }

   to_copy=limit+1;
//---- copy newly appeared data into the arrays
   if(CopyBuffer(ATR_Handle,0,0,to_copy,ATR)<=0) return(RESET);
   if(CopyBuffer(MA_Handle,0,0,to_copy,MA)<=0) return(RESET);

//---- main loop of the indicator calculation
   for(bar=limit; bar>=0; bar--)
     {
      ExtMapBufferUp[bar]=EMPTY_VALUE;
      ExtMapBufferDown[bar]=EMPTY_VALUE;
      ExtMapBufferUp1[bar]=EMPTY_VALUE;
      ExtMapBufferDown1[bar]=EMPTY_VALUE;

      smax=(1+Deviation/100)*MA[bar];
      smin=(1-Deviation/100)*MA[bar];
      trend=prev_trend;

      if(close[bar]>prev_smax) trend=+1;
      if(close[bar]<prev_smin) trend=-1;

      if(trend>0)
        {
         if(smin<prev_smin) smin=prev_smin;
         ExtMapBufferUp[bar]=smin;
         if(prev_trend<0) ExtMapBufferUp1[bar]=smin-ATRSensitivity*ATR[bar];
        }
      else
        {
         if(smax>prev_smax) smax=prev_smax;
         ExtMapBufferDown[bar]=smax;
         if(prev_trend>0) ExtMapBufferDown1[bar]=smax+ATRSensitivity*ATR[bar];
        }

      if(bar)
        {
         prev_smax=smax;
         prev_smin=smin;
         prev_trend=trend;
        }
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
