//+------------------------------------------------------------------+
//|                                                     Beginner.mq5 |
//|                                      Copyright © 2009, EarnForex |
//|                                        http://www.earnforex.com/ |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2009, EarnForex"
#property link      "http://www.earnforex.com"
#property version   "2.00"
#property description "Beginner - basic indicator for marking chart's highs and lows."
#property description "Repaints."
#property description "The corrected version of the indicator!"
//---- indicator version number
#property version   "1.00"
//---- drawing the indicator in the main window
#property indicator_chart_window 
//---- two buffers are used for calculation of drawing of the indicator
#property indicator_buffers 2
//---- only two plots are used
#property indicator_plots   2
//+----------------------------------------------+
//|  Bearish indicator drawing parameters        |
//+----------------------------------------------+
//---- drawing the indicator 1 as a symbol
#property indicator_type1   DRAW_ARROW
//---- magenta color is used as the color of the bearish indicator line
#property indicator_color1  clrMagenta
//---- indicator 1 line width is equal to 2
#property indicator_width1  2
//---- bearish indicator label display
#property indicator_label1  "Beginner Sell"
//+----------------------------------------------+
//|  Bullish indicator drawing parameters        |
//+----------------------------------------------+
//---- drawing the indicator 2 as a line
#property indicator_type2   DRAW_ARROW
//---- DodgerBlue color is used as the color of the bullish line of the indicator
#property indicator_color2  clrDodgerBlue
//---- indicator 2 line width is equal to 2
#property indicator_width2  2
//---- bullish indicator label display
#property indicator_label2 "Beginner Buy"
//+----------------------------------------------+
//|  declaring constants                         |
//+----------------------------------------------+
#define  RESET     0 // The constant for returning the indicator recalculation command to the terminal
#define  UP       +1
#define  DOWN     -1
//+----------------------------------------------+
//| Indicator input parameters                   |
//+----------------------------------------------+
input uint Otstup = 30; //Shift
input uint Per=9;       //Period
//+----------------------------------------------+

//---- declaration of dynamic arrays that will further be 
// used as indicator buffers
double SellBuffer[];
double BuyBuffer[];
//---- Declaration of integer variables of data starting point
int min_rates_total,ATRPeriod;
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
void OnInit()
  {
//---- initialization of global variables
   ATRPeriod=10;
   min_rates_total=int(MathMax(Per,ATRPeriod));

//---- set dynamic array as an indicator buffer
   SetIndexBuffer(0,SellBuffer,INDICATOR_DATA);
//---- shifting the start of drawing of the indicator 1
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//--- create a label to display in DataWindow
   PlotIndexSetString(0,PLOT_LABEL,"Beginner Sell");
//---- indicator symbol
   PlotIndexSetInteger(0,PLOT_ARROW,108);
//---- indexing elements in the buffer as time series
   ArraySetAsSeries(SellBuffer,true);
//---- setting the indicator values that won't be visible on a chart
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,0);

//---- set dynamic array as an indicator buffer
   SetIndexBuffer(1,BuyBuffer,INDICATOR_DATA);
//---- shifting the start of drawing of the indicator 2
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,min_rates_total);
//--- Create label to display in DataWindow
   PlotIndexSetString(1,PLOT_LABEL,"Beginner Buy");
//---- indicator symbol
   PlotIndexSetInteger(1,PLOT_ARROW,108);
//---- indexing elements in the buffer as time series
   ArraySetAsSeries(BuyBuffer,true);
//---- setting the indicator values that won't be visible on a chart
   PlotIndexSetDouble(1,PLOT_EMPTY_VALUE,0);

//---- Setting the format of accuracy of displaying the indicator
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits);
//---- name for the data window and the label for sub-windows 
   string short_name="Beginner";
   IndicatorSetString(INDICATOR_SHORTNAME,short_name);
//----   
  }
//+------------------------------------------------------------------+
//| GetRange()                                                       |
//+------------------------------------------------------------------+
double GetRange(uint Len,const double &H[],const double &L[],int index)
  {
//----
   double AvgRange,Range;

   AvgRange=0.0;
   for(int count=index; count<int(index+Len); count++) AvgRange+=MathAbs(H[count]-L[count]);
   Range=AvgRange/Len;
//----
   return(Range);
  }
//+------------------------------------------------------------------+
//| Custom indicator iteration function                              |
//+------------------------------------------------------------------+
int OnCalculate(const int rates_total,
                const int prev_calculated,
                const datetime &time[],
                const double &open[],
                const double &High[],
                const double &Low[],
                const double &Close[],
                const long &tick_volume[],
                const long &volume[],
                const int &spread[])
  {
//---- checking for the sufficiency of bars for the calculation
   if(rates_total<min_rates_total) return(RESET);

//---- declaration of local variables 
   int limit,bar;
   double Range,SHMax,SHMin,diff;
   static int trend;

//---- calculation of the 'limit' starting index for the bars recalculation loop
   if(prev_calculated>rates_total || prev_calculated<=0)// checking for the first start of the indicator calculation
     {
      limit=rates_total-min_rates_total-1; // starting index for calculation of all bars
      trend=0;
     }
   else limit=rates_total-prev_calculated; // starting index for calculation of new bars

//---- indexing elements in arrays as time series  
   ArraySetAsSeries(High,true);
   ArraySetAsSeries(Low,true);
   ArraySetAsSeries(Close,true);

//---- main loop of the indicator calculation
   for(bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      Range=GetRange(ATRPeriod,High,Low,bar)/3;
      SellBuffer[bar]=0;
      BuyBuffer[bar]=0;

      SHMax=High[ArrayMaximum(High,bar,Per)];
      SHMin=Low[ArrayMinimum(Low,bar,Per)];
      diff=(SHMax-SHMin)*Otstup/100.0;

      if(Close[bar]<SHMin+diff && trend!=DOWN)
        {
         SellBuffer[bar]=High[bar]+Range;
         if(bar) trend=DOWN;
        }
      else if(Close[bar]>SHMax-diff && trend!=UP)
        {
         BuyBuffer[bar]=Low[bar]-Range;
         if(bar) trend=UP;
        }
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
