//+------------------------------------------------------------------+
//|                                               XD-RangeSwitch.mq5 |
//|                                        Copyright © 2009, Vic2008 |
//|                                                                  |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2009, Vic2008"
#property link      ""
//--- indicator version
#property version   "1.10"
//--- Drawing the indicator in the main window
#property indicator_chart_window
//--- four buffers are used for the indicator calculation and drawing
#property indicator_buffers 4
//--- 4 plots are used
#property indicator_plots   4
//+----------------------------------------------+
//| Parameters of drawing a bullish indicator    |
//+----------------------------------------------+
//--- drawing indicator 1 as a line
#property indicator_type1   DRAW_LINE
//--- the Brown color is used as the color of the indicator bearish line
#property indicator_color1  clrBrown
//--- the line of the indicator 1 is a continuous curve
#property indicator_style1  STYLE_SOLID
//--- indicator 1 line width is equal to 2
#property indicator_width1  2
//--- display of the bearish indicator label
#property indicator_label1  "Upper XD-RangeSwitch"
//+----------------------------------------------+
//| Parameters of drawing a bearish indicator    |
//+----------------------------------------------+
//--- drawing indicator 2 as a line
#property indicator_type2   DRAW_LINE
//--- Teal color is used for the bullish line of the indicator
#property indicator_color2  clrTeal
//--- the line of the indicator 2 is a continuous curve
#property indicator_style2  STYLE_SOLID
//--- indicator 2 line width is equal to 2
#property indicator_width2  2
//--- display of the indicator bullish label
#property indicator_label2  "Lower XD-RangeSwitch"
//+----------------------------------------------+
//| Parameters of drawing a bullish indicator    |
//+----------------------------------------------+
//--- drawing the indicator 3 as a label
#property indicator_type3   DRAW_ARROW
//--- the Chocolate color is used for the indicator Sell
#property indicator_color3  clrChocolate
//--- the line of the indicator 3 is a continuous curve
#property indicator_style3  STYLE_SOLID
//--- indicator 3 line width is equal to 1
#property indicator_width3  1
//--- show the Sell label of the indicator
#property indicator_label3  "Sell XD-RangeSwitch"
//+----------------------------------------------+
//| Parameters of drawing a bearish indicator    |
//+----------------------------------------------+
//--- drawing the indicator 4 as a label
#property indicator_type4   DRAW_ARROW
//--- the CadetBlue color is used for the indicator Buy
#property indicator_color4  clrCadetBlue
//--- the line of the indicator 2 is a continuous curve
#property indicator_style4  STYLE_SOLID
//--- indicator 4 line width is equal to 1
#property indicator_width4  1
//--- show the Buy label of the indicator
#property indicator_label4  "Buy XD-RangeSwitch"
//+----------------------------------------------+
//| Indicator input parameters                   |
//+----------------------------------------------+
input int N=4; //number of peaks
input int Shift=0; // horizontal shift of the indicator in bars 
//+----------------------------------------------+
//--- declaration of dynamic arrays that
//--- will be used as indicator buffers
double ExtMapBufferUp[];
double ExtMapBufferDown[];
double ExtMapBufferUp1[];
double ExtMapBufferDown1[];
//--- declaration of integer variables of data starting point
int min_rates_total;
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+  
void OnInit()
  {
//--- initialization of variables of the start of data calculation
   min_rates_total=N;
//--- set ExtMapBufferUp dynamic array as an indicator buffer
   SetIndexBuffer(0,ExtMapBufferUp,INDICATOR_DATA);
//---- shifting the indicator 1 horizontally by Shift
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//--- shifting the start of drawing of the indicator 1
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- Indexing buffer elements as timeseries   
   ArraySetAsSeries(ExtMapBufferUp,true);
//--- setting the indicator values that won't be visible on a chart
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//--- set ExtMapBufferDown dynamic array as an indicator buffer
   SetIndexBuffer(1,ExtMapBufferDown,INDICATOR_DATA);
//---- shifting the indicator 2 horizontally by Shift
   PlotIndexSetInteger(1,PLOT_SHIFT,Shift);
//--- shifting the starting point of the indicator 2 drawing
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,min_rates_total);
//---- Indexing buffer elements as timeseries   
   ArraySetAsSeries(ExtMapBufferDown,true);
//--- setting the indicator values that won't be visible on a chart
   PlotIndexSetDouble(1,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//--- Set the ExtMapBufferUp1 dynamic array as an indicator buffer
   SetIndexBuffer(2,ExtMapBufferUp1,INDICATOR_DATA);
//---- shifting the indicator 1 horizontally by Shift
   PlotIndexSetInteger(2,PLOT_SHIFT,Shift);
//--- shifting the starting point of the indicator 3 drawing
   PlotIndexSetInteger(2,PLOT_DRAW_BEGIN,min_rates_total);
//---- Indexing buffer elements as timeseries   
   ArraySetAsSeries(ExtMapBufferUp1,true);
//--- setting the indicator values that won't be visible on a chart
   PlotIndexSetDouble(2,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//--- indicator symbol
   PlotIndexSetInteger(2,PLOT_ARROW,110);
//--- Set the ExtMapBufferDown1 dynamic array as an indicator buffer
   SetIndexBuffer(3,ExtMapBufferDown1,INDICATOR_DATA);
//---- shifting the indicator 2 horizontally by Shift
   PlotIndexSetInteger(3,PLOT_SHIFT,Shift);
//--- shifting the starting point of the indicator 4 drawing
   PlotIndexSetInteger(3,PLOT_DRAW_BEGIN,min_rates_total);
//---- Indexing buffer elements as timeseries   
   ArraySetAsSeries(ExtMapBufferDown1,true);
//--- setting the indicator values that won't be visible on a chart
   PlotIndexSetDouble(3,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//--- indicator symbol
   PlotIndexSetInteger(3,PLOT_ARROW,110);
//--- initializations of a variable for the indicator short name
   string shortname;
   StringConcatenate(shortname,"XD-RangeSwitch(",N,")");
//--- creation of the name to be displayed in a separate sub-window and in a pop up help
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
//--- determining the accuracy of the indicator values
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits);
//---
  }
//+------------------------------------------------------------------+
//| Custom indicator iteration function                              |
//+------------------------------------------------------------------+
int OnCalculate(const int rates_total,    // number of bars in history at the current tick
                const int prev_calculated,// amount of history in bars at the previous tick
                const datetime &time[],
                const double &open[],
                const double& high[],     // price array of maximums of price for the calculation of indicator
                const double& low[],      // price array of price lows for the indicator calculation
                const double &close[],
                const long &tick_volume[],
                const long &volume[],
                const int &spread[])
  {
//--- checking if the number of bars is enough for the calculation
   if(rates_total<min_rates_total) return(0);
//--- declarations of local variables 
   int limit,bar;
//--- apply timeseries indexing to array elements  
   ArraySetAsSeries(high,true);
   ArraySetAsSeries(low,true);
   ArraySetAsSeries(close,true);
//--- calculation of the starting number 'first' for the cycle of recalculation of bars
   if(prev_calculated>rates_total || prev_calculated<=0) // checking for the first start of the indicator calculation
      limit=rates_total-min_rates_total-1; // starting index for the calculation of all bars
   else limit=rates_total-prev_calculated; // starting index for the calculation of new bars
//--- main calculation loop of the indicator
   for(bar=limit; bar>=0; bar--)
     {
      ExtMapBufferUp[bar]=EMPTY_VALUE;
      ExtMapBufferDown[bar]=EMPTY_VALUE;
      ExtMapBufferUp1[bar]=EMPTY_VALUE;
      ExtMapBufferDown1[bar]=EMPTY_VALUE;
      //---
      if(close[bar]>high[ArrayMaximum(high,bar+1,N)]) ExtMapBufferDown[bar]=low[ArrayMinimum(low,bar,N)];
      else
        {
         if(close[bar]<low[ArrayMinimum(low,bar+1,N)]) ExtMapBufferUp[bar]=high[ArrayMaximum(high,bar,N)];
         else
           {
            ExtMapBufferUp[bar]=ExtMapBufferUp[bar+1];
            ExtMapBufferDown[bar]=ExtMapBufferDown[bar+1];
           }
        }
      //---
      if(ExtMapBufferUp[bar+1]==EMPTY_VALUE && ExtMapBufferUp[bar]!=EMPTY_VALUE)
         ExtMapBufferUp1[bar]=ExtMapBufferUp[bar];
      //---
      if(ExtMapBufferDown[bar+1]==EMPTY_VALUE && ExtMapBufferDown[bar]!=EMPTY_VALUE)
         ExtMapBufferDown1[bar]=ExtMapBufferDown[bar];
     }
//---
   return(rates_total);
  }
//+------------------------------------------------------------------+
