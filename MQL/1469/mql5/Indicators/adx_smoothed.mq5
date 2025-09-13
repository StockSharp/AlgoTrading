//+------------------------------------------------------------------+
//|                                                 ADX Smoothed.mq5 |
//|                      Copyright © 2007, MetaQuotes Software Corp. |
//|                                        http://www.metaquotes.net |
//+------------------------------------------------------------------+
//---- author of the indicator
#property copyright "Copyright © 2007, MetaQuotes Software Corp."
//---- link to the website of the author
#property link      "http://www.metaquotes.net"
//---- indicator version number
#property version   "1.00"
//---- drawing indicator in a separate window
#property indicator_separate_window
//---- three buffers are used for calculation and drawing the indicator
#property indicator_buffers 3
//---- three plots are used
#property indicator_plots   3
//+----------------------------------------------+
//|  Parameters of drawing the bullish indicator |
//+----------------------------------------------+
//---- drawing indicator 1 as a line
#property indicator_type1   DRAW_LINE
//---- green color is used as the color of the bullish line of the indicator
#property indicator_color1  Lime
//---- line of the indicator 1 is a continuous curve
#property indicator_style1  STYLE_SOLID
//---- thickness of line of the indicator 1 is equal to 1
#property indicator_width1  1
//---- displaying of the bullish label of the indicator
#property indicator_label1  "Di Plus"
//+----------------------------------------------+
//|  Bearish indicator drawing parameters        |
//+----------------------------------------------+
//---- drawing the indicator 2 as a line
#property indicator_type2   DRAW_LINE
//---- red color is used as the color of the bearish indicator line
#property indicator_color2  Red
//---- the indicator 2 line is a continuous curve
#property indicator_style2  STYLE_SOLID
//---- indicator 2 line width is equal to 1
#property indicator_width2  1
//---- displaying of the bearish label of the indicator
#property indicator_label2  "Di Minus"
//+----------------------------------------------+
//|  ADX indicator drawing parameters            |
//+----------------------------------------------+
//---- drawing indicator 3 as line
#property indicator_type3   DRAW_LINE
//---- blue color is used for the indicator ADX line
#property indicator_color3  Blue
//---- the indicator 3 line is a continuous curve
#property indicator_style3  STYLE_SOLID
//---- thickness of the indicator 3 line is equal to 1
#property indicator_width3  1
//---- displaying of the bearish label of the indicator
#property indicator_label3  "ADX"
//+----------------------------------------------+
//| Parameters of displaying horizontal levels   |
//+----------------------------------------------+
#property indicator_level1 88.0
#property indicator_level2 50.0
#property indicator_level3 12.0
#property indicator_levelcolor Gray
#property indicator_levelstyle STYLE_DASHDOTDOT
//+----------------------------------------------+
//| Indicator input parameters                   |
//+----------------------------------------------+
input int    period = 14;
input double alpha1 = 0.25;
input double alpha2 = 0.33;
//+----------------------------------------------+
//---- declaration of dynamic arrays that will further be 
// used as indicator buffers
double DiPlusBuffer[];
double DiMinusBuffer[];
double ADXBuffer[];
//---- Declaration of integer variables for the indicator handles
int ADX_Handle;
//---- Declaration of integer variables of data starting point
int min_rates_total;
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+  
void OnInit()
  {
//---- getting handle of the ADX indicator
   ADX_Handle=iADX(NULL,0,period);
   if(ADX_Handle==INVALID_HANDLE)Print(" Failed to get handle of the ADX indicator");

//---- Initialization of variables of the start of data calculation
   min_rates_total=period+1;

//---- set DiPlusBuffer dynamic array as an indicator buffer
   SetIndexBuffer(0,DiPlusBuffer,INDICATOR_DATA);
//---- shifting the starting point for drawing indicator 1 by min_rates_total
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- setting the indicator values that won't be visible on a chart
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//---- indexing buffer elements as time series   
   ArraySetAsSeries(DiPlusBuffer,true);

//---- set DiMinusBuffer dynamic array as an indicator buffer
   SetIndexBuffer(1,DiMinusBuffer,INDICATOR_DATA);
//---- shifting the starting point for drawing indicator 2 by min_rates_total
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,min_rates_total);
//---- setting the indicator values that won't be visible on a chart
   PlotIndexSetDouble(1,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//---- indexing buffer elements as time series   
   ArraySetAsSeries(DiMinusBuffer,true);

//---- set DiMinusBuffer dynamic array as an indicator buffer
   SetIndexBuffer(2,ADXBuffer,INDICATOR_DATA);
//---- shifting the starting point for drawing indicator 3 by min_rates_total
   PlotIndexSetInteger(2,PLOT_DRAW_BEGIN,min_rates_total);
//---- setting the indicator values that won't be visible on a chart
   PlotIndexSetDouble(2,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//---- indexing buffer elements as time series   
   ArraySetAsSeries(ADXBuffer,true);

//---- initializations of variable for indicator short name
   string shortname;
   StringConcatenate(shortname,"ADX(",period,")smothed");
//--- creation of the name to be displayed in a separate sub-window and in a pop up help
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
//--- determining the accuracy of displaying the indicator values
   IndicatorSetInteger(INDICATOR_DIGITS,0);
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
   if(BarsCalculated(ADX_Handle)<rates_total || rates_total<min_rates_total) return(0);

//---- declaration of local variables 
   int limit,to_copy,bar;
   double ADX[],DIP[],DIM[],DiPlus,DiMinus,Adx;
   static double DiPlus_,DiMinus_,Adx_;

//---- indexing elements in arrays as timeseries  
   ArraySetAsSeries(ADX,true);
   ArraySetAsSeries(DIP,true);
   ArraySetAsSeries(DIM,true);

//---- calculation of the starting number first for the bar recalculation loop
   if(prev_calculated>rates_total || prev_calculated<=0) // checking for the first start of calculation of an indicator
     {
      limit=rates_total-2; // starting index for the calculation of all bars
      DiPlus_=0.0;
      DiMinus_=0.0;
      Adx_=0.0;
      DiPlusBuffer[rates_total-1]=0.0;
      DiMinusBuffer[rates_total-1]=0.0;
      ADXBuffer[rates_total-1]=0.0;
     }
   else limit=rates_total-prev_calculated; // starting index for the calculation of new bars

   to_copy=limit+2;

//---- copy newly appeared data into the arrays
   if(CopyBuffer(ADX_Handle,0,0,to_copy,ADX)<=0) return(0);
   if(CopyBuffer(ADX_Handle,1,0,to_copy,DIP)<=0) return(0);
   if(CopyBuffer(ADX_Handle,2,0,to_copy,DIM)<=0) return(0);

//---- restore values of the variables
   DiPlus=DiPlus_;
   DiMinus=DiMinus_;
   Adx=Adx_;

//---- main indicator calculation loop
   for(bar=limit; bar>=0; bar--)
     {
      //---- memorize values of the variables before running at the current bar
      if(rates_total!=prev_calculated && bar==0)
        {
         DiPlus_=DiPlus;
         DiMinus_=DiMinus;
         Adx_=Adx;
        }

      DiPlus=2*DIP[bar]+(alpha1-2)*DIP[bar+1]+(1-alpha1)*DiPlus;
      DiMinus=2*DIM[bar]+(alpha1-2)*DIM[bar+1]+(1-alpha1)*DiMinus;
      Adx=2*ADX[bar]+(alpha1-2)*ADX[bar+1]+(1-alpha1)*Adx;

      DiPlusBuffer[bar]=alpha2*DiPlus+(1-alpha2)*DiPlusBuffer[bar+1];
      DiMinusBuffer[bar]=alpha2*DiMinus+(1-alpha2)*DiMinusBuffer[bar+1];
      ADXBuffer[bar]=alpha2*Adx+(1-alpha2)*ADXBuffer[bar+1];
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
