//+------------------------------------------------------------------+
//|                                                   CandleStop.mq5 |
//|                                         Copyright © 2009, CrushD |
//|                                                                  |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2009, CrushD"
#property link "CrushD"
#property description "The indicator to pull Trailing Stops"
//---- indicator version number
#property version   "1.00"
//---- drawing the indicator in the main window
#property indicator_chart_window 
//---- number of indicator buffers 3
#property indicator_buffers 3 
//---- only 5 graphical plots are used
#property indicator_plots   3
//+-----------------------------------+
//|  Indicator drawing parameters     |
//+-----------------------------------+
//---- drawing the indicator as a line
#property indicator_type1   DRAW_LINE
//---- SlateBlue color is used for indicator line
#property indicator_color1 clrSlateBlue
//---- the indicator line is a continuous curve
#property indicator_style1  STYLE_DASHDOTDOT
//---- indicator line width is equal to 1
#property indicator_width1  1
//---- displaying the indicator label
#property indicator_label1  "Middle Candle Stop"

//+--------------------------------------------+
//|  Levels indicator drawing parameters       |
//+--------------------------------------------+
//---- drawing the levels as lines
#property indicator_type2   DRAW_LINE
#property indicator_type3   DRAW_LINE
//---- selection of levels colors
#property indicator_color2  clrLimeGreen
#property indicator_color3  clrDeepPink
//---- levels are continuous curves
#property indicator_style2 STYLE_SOLID
#property indicator_style3 STYLE_SOLID
//---- levels width is equal to 1
#property indicator_width2  1
#property indicator_width3  1
//---- display levels labels
#property indicator_label2  "Upper Trail Stop"
#property indicator_label3  "Lower Trail Stop"

//+-----------------------------------+
//|  INDICATOR INPUT PARAMETERS       |
//+-----------------------------------+
input uint UpTrailPeriods=5; //Searching period for the high               
input uint DnTrailPeriods=5; //Searching period for the low
input int Shift=0; // horizontal shift of the indicator in bars
//+-----------------------------------+

//---- declaration of dynamic arrays that will further be 
//---- will be used as Bollinger Bands indicator buffers
double ExtLineBuffer0[],ExtLineBuffer1[],ExtLineBuffer2[];

//---- Declaration of the average vertical shift value variable
double dPriceShift;
//---- Declaration of integer variables of data starting point
int min_rates_total;
//+------------------------------------------------------------------+   
//| CandleStop initialization function                               | 
//+------------------------------------------------------------------+ 
void OnInit()
  {
//---- Initialization of variables of the start of data calculation
   min_rates_total=int(MathMax(UpTrailPeriods,DnTrailPeriods));

//---- set dynamic array as an indicator buffer
   SetIndexBuffer(0,ExtLineBuffer0,INDICATOR_DATA);
//---- moving the indicator 1 horizontally
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//---- performing the shift of beginning of indicator drawing
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- setting the indicator values that won't be visible on a chart
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//---- indexing the elements in buffers as timeseries   
   ArraySetAsSeries(ExtLineBuffer0,true);

//---- setting dynamic arrays as indicator buffers
   SetIndexBuffer(1,ExtLineBuffer1,INDICATOR_DATA);
   SetIndexBuffer(2,ExtLineBuffer2,INDICATOR_DATA);
//---- set the position, from which the Bollinger Bands drawing starts
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,min_rates_total);
   PlotIndexSetInteger(2,PLOT_DRAW_BEGIN,min_rates_total);

//---- restriction to draw empty values for the indicator
   PlotIndexSetDouble(1,PLOT_EMPTY_VALUE,EMPTY_VALUE);
   PlotIndexSetDouble(2,PLOT_EMPTY_VALUE,EMPTY_VALUE);
   
//---- indexing the elements in buffers as timeseries   
   ArraySetAsSeries(ExtLineBuffer1,true);  
   ArraySetAsSeries(ExtLineBuffer2,true);

//--- creation of the name to be displayed in a separate sub-window and in a pop up help
   IndicatorSetString(INDICATOR_SHORTNAME,"Candle Stop");

//--- determining the accuracy of displaying the indicator values
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits+1);
//---- end of initialization
  }
//+------------------------------------------------------------------+ 
//| CandleStop iteration function                                    | 
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
   double HH,LL,HL;
//---- Declaration of integer variables and getting the bars already calculated
   int limit,bar;

//---- indexing elements in arrays as time series  
   ArraySetAsSeries(low,true);
   ArraySetAsSeries(high,true);
   
//---- calculation of the 'limit' starting index for the bars recalculation loop
   if(prev_calculated>rates_total || prev_calculated<=0) // checking for the first start of the indicator calculation
     {
      limit=rates_total-min_rates_total-1;               // starting index for calculation of all bars
     }
   else
     {
      limit=rates_total-prev_calculated;                 // starting index for calculation of new bars
     }

//---- Main calculation loop of the indicator
   for(bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      HH=high[ArrayMaximum(high,bar,UpTrailPeriods)];
      LL=low [ArrayMinimum(low, bar,DnTrailPeriods)];
      HL=(HH+LL)/2.0;
           
      ExtLineBuffer0[bar]=HL;
      ExtLineBuffer1[bar]=HH;
      ExtLineBuffer2[bar]=LL;
     }
//----  
   return(rates_total);
  }
//+------------------------------------------------------------------+
