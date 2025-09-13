//+------------------------------------------------------------------+
//|                                              HighsLowsSignal.mq5 |
//|                                    Copyright © 2006, Robert Hill |
//|                                                                  |
//+------------------------------------------------------------------+
//| Allows you to enter a number and it will then show you at        |
//| which point that many higher highs/higher lows or                |
//| lower highs/lower lows candles occur.                            |
//| It also give an alert if the current bar has the                 |
//| required number of candles before it.                            |
//+------------------------------------------------------------------+
//--- Copyright
#property copyright "Copyright © 2006, Robert Hill"
//--- a link to the website of the author
#property link      ""
//--- indicator version
#property version   "1.00"
//--- drawing the indicator in the main window
#property indicator_chart_window 
//--- two buffers are used for the indicator calculation and drawing
#property indicator_buffers 2
//--- two plots are used
#property indicator_plots   2
//+----------------------------------------------+
//| Parameters of drawing a bearish indicator    |
//+----------------------------------------------+
//--- drawing the indicator 1 as a symbol
#property indicator_type1   DRAW_ARROW
//--- pink is used for the color of the bearish indicator line
#property indicator_color1  clrMagenta
//--- indicator 1 line width is equal to 4
#property indicator_width1  4
//--- display of the indicator bullish label
#property indicator_label1  "HighsLowsSignal Sell"
//+----------------------------------------------+
//| Parameters of drawing a bullish indicator    |
//+----------------------------------------------+
//--- drawing the indicator 2 as a symbol
#property indicator_type2   DRAW_ARROW
//---- light blue is used as the color of the bullish line of the indicator
#property indicator_color2  clrDodgerBlue
//---- indicator 2 line width is equal to 4
#property indicator_width2  4
//--- display of the bearish indicator label
#property indicator_label2 "HighsLowsSignal Buy"
//+----------------------------------------------+
//| declaration of constants                     |
//+----------------------------------------------+
#define RESET 0    // A constant for returning the indicator recalculation command to the terminal
//+----------------------------------------------+
//| Indicator input parameters                   |
//+----------------------------------------------+
input uint HowManyCandles=3;  // The number of candlesticks of a directed price change
//+----------------------------------------------+
//--- declaration of dynamic arrays that
//--- will be used as indicator buffers
double SellBuffer[];
double BuyBuffer[];
//--- declaration of integer variables for the indicators handles
int ATR_Handle;
//--- declaration of integer variables for the start of data calculation
int min_rates_total;
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
int OnInit()
  {
//--- initialization of global variables 
   int ATR_Period=15;
   min_rates_total=int(MathMax(ATR_Period,4));
//--- Getting the handle of the ATR indicator
   ATR_Handle=iATR(NULL,0,ATR_Period);
   if(ATR_Handle==INVALID_HANDLE)
     {
      Print(" Failed to get handle of the ATR indicator");
      return(INIT_FAILED);
     }
//--- set dynamic array as an indicator buffer
   SetIndexBuffer(0,SellBuffer,INDICATOR_DATA);
//--- shifting the start of drawing of the indicator 1
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//--- indicator symbol
   PlotIndexSetInteger(0,PLOT_ARROW,171);
//--- restriction to draw empty values for the indicator
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,0);
//--- indexing elements in the buffer as in timeseries
   ArraySetAsSeries(SellBuffer,true);
//--- set dynamic array as an indicator buffer
   SetIndexBuffer(1,BuyBuffer,INDICATOR_DATA);
//--- shifting the starting point of the indicator 2 drawing
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,min_rates_total);
//--- indicator symbol
   PlotIndexSetInteger(1,PLOT_ARROW,171);
//--- restriction to draw empty values for the indicator
   PlotIndexSetDouble(1,PLOT_EMPTY_VALUE,0);
//--- indexing elements in the buffer as in timeseries
   ArraySetAsSeries(BuyBuffer,true);
//--- setting the format of accuracy of displaying the indicator
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits);
   IndicatorSetString(INDICATOR_SHORTNAME,"HighsLowsSignal");
//--- initialization end
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+
//| Custom indicator iteration function                              |
//+------------------------------------------------------------------+
int OnCalculate(const int rates_total,
                const int prev_calculated,
                const datetime &time[],
                const double &open[],
                const double &high[],
                const double &low[],
                const double &close[],
                const long &tick_volume[],
                const long &volume[],
                const int &spread[])
  {
//--- checking if the number of bars is enough for the calculation
   if(BarsCalculated(ATR_Handle)<rates_total || rates_total<min_rates_total) return(RESET);
//--- declarations of local variables 
   int to_copy,limit,bar,HigherHighs,HigherLows,LowerHighs,LowerLows;
   double ATR[];
//--- calculations of the necessary amount of data to be copied
//--- and the 'limit' starting index for the bars recalculation loop
   if(prev_calculated>rates_total || prev_calculated<=0)// checking for the first start of calculation of an indicator
     {
      limit=rates_total-min_rates_total; // starting index for the calculation of all bars
     }
   else
     {
      limit=rates_total-prev_calculated; // starting index for the calculation of new bars
     }
   to_copy=limit+1;
//---- copy newly appeared data in the ATR[] array
   if(CopyBuffer(ATR_Handle,0,0,to_copy,ATR)<=0) return(RESET);
//--- apply timeseries indexing to array elements  
   ArraySetAsSeries(ATR,true);
   ArraySetAsSeries(high,true);
   ArraySetAsSeries(low,true);
//--- main calculation loop of the indicator
   for(bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      HigherHighs=0;
      HigherLows=0;
      LowerHighs=0;
      LowerLows=0;
      int end=bar+int(HowManyCandles);
      for(int index=bar; index<end; index++)
        {
         int index1=index+1;
         if(high[index]>high[index1]) HigherHighs++;
         if(low[index]>low[index1]) HigherLows++;
         if(high[index]<high[index1]) LowerHighs++;
         if(low[index]<low[index1]) LowerLows++;
        }
      BuyBuffer[bar]=0.0;
      SellBuffer[bar]=0.0;     
      if(HigherHighs==HowManyCandles && HigherLows==HowManyCandles) BuyBuffer[bar]=low[bar]-ATR[bar]*3/8;
      else if(LowerHighs==HowManyCandles && LowerLows==HowManyCandles) SellBuffer[bar]=high[bar]+ATR[bar]*3/8;
     }
//---     
   return(rates_total);
  }
//+------------------------------------------------------------------+
