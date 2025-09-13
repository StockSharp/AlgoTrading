//+------------------------------------------------------------------+
//|                                             CorrectedAverage.mq5 |
//|                            Copyright © 2006, Alexander Piechotta |
//|                                     http://onix-trade.net/forum/ |
//+------------------------------------------------------------------+
//---- author of the indicator
#property copyright "Copyright © 2006, Alexander Piechotta"
//---- author of the indicator
#property link      "http://onix-trade.net/forum/"
//---- indicator version number
#property version   "1.00"
//---- drawing the indicator in the main window
#property indicator_chart_window 
//---- number of indicator buffers
#property indicator_buffers 1 
//---- only one plot is used
#property indicator_plots   1
//+-----------------------------------+
//|  Indicator drawing parameters     |
//+-----------------------------------+
//---- drawing the indicator as a line
#property indicator_type1   DRAW_LINE
//---- clrMediumSlateBlue color is used as the color of the bullish line of the indicator
#property indicator_color1 clrMediumSlateBlue
//---- the indicator line is a continuous curve
#property indicator_style1  STYLE_SOLID
//---- Indicator line width is equal to 2
#property indicator_width1 2
//---- displaying the indicator label
#property indicator_label1  "CorrectedAverage"
//+-----------------------------------+
//|  Declaration of constants         |
//+-----------------------------------+
#define RESET 0 // The constant for returning the indicator recalculation command to the terminal
//+-----------------------------------+
//|  INDICATOR INPUT PARAMETERS       |
//+-----------------------------------+
input ENUM_MA_METHOD MA_Method=MODE_SMA; ///method of averaging
input uint Length=12; // smoothing depth
input ENUM_APPLIED_PRICE Applied_price=PRICE_CLOSE;//price constant                
input int Shift=0; // horizontal shift of the indicator in bars
input int PriceShift=0; // vertical shift of the indicator in pointsõ
//+-----------------------------------+
//---- indicator buffer
double MABuffer[];
double dPriceShift;
//---- Declaration of global variables
int min_rates_total;
//---- declaration of integer variables for the indicators handles
int MA_Handle,STD_Handle;
//+------------------------------------------------------------------+    
//| MA indicator initialization function                             | 
//+------------------------------------------------------------------+  
int OnInit()
  {
//---- Initialization of variables of the start of data calculation
   switch(int(MA_Method))
     {
      case MODE_SMA: min_rates_total=int(Length); break;
      case MODE_EMA: min_rates_total=2; break;
      case MODE_SMMA: min_rates_total=int(Length)+1; break;
      case MODE_LWMA: min_rates_total=int(Length)+1; break;
     }
   min_rates_total++;

//---- getting handle of the iStdDev indicator
   STD_Handle=iStdDev(NULL,PERIOD_CURRENT,Length,0,MA_Method,Applied_price);
   if(STD_Handle==INVALID_HANDLE)
     {
      Print(" Failed to get handle of the iStdDev indicator");
      return(1);
     }

//---- getting the iMA indicator handle
   MA_Handle=iMA(NULL,PERIOD_CURRENT,Length,0,MA_Method,Applied_price);
   if(MA_Handle==INVALID_HANDLE)
     {
      Print(" Failed to get handle of the iMA indicator");
      return(1);
     }

//---- set dynamic array as an indicator buffer
   SetIndexBuffer(0,MABuffer,INDICATOR_DATA);
//---- indexing elements in the buffer as time series
   ArraySetAsSeries(MABuffer,true);
   
//---- shifting the indicator horizontally by Shift
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//---- performing the shift of beginning of indicator drawing
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- setting the indicator values that won't be visible on a chart
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,EMPTY_VALUE);

//---- initializations of variable for indicator short name
   string shortname;
   StringConcatenate(shortname,"CorrectedAverage( Length = ",Length,")");
//--- creation of the name to be displayed in a separate sub-window and in a pop up help
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);

//--- determining the accuracy of displaying the indicator values
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits+1);

//---- Initialization of the vertical shift
   dPriceShift=_Point*PriceShift;
//---- end of initialization
   return(0);
  }
//+------------------------------------------------------------------+  
//| MA iteration function                                            | 
//+------------------------------------------------------------------+  
int OnCalculate(
                const int rates_total,    // amount of history in bars at the current tick
                const int prev_calculated,// amount of history in bars at the previous tick
                const int begin,          // number of beginning of reliable counting of bars
                const double &price[]     // price array for calculation of the indicator
                )
  {
//---- checking the number of bars to be enough for calculation
   if(BarsCalculated(STD_Handle)<rates_total
      || BarsCalculated(MA_Handle)<rates_total
      || rates_total<min_rates_total+begin) return(RESET);

//---- declaration of local variables 
   int limit,bar,to_copy;
   double v1,v2,k,MA[],STD[];

//---- indexing elements in arrays as timeseries  
   ArraySetAsSeries(MA,true);
   ArraySetAsSeries(STD,true);

//--- calculations of the necessary amount of data to be copied and
//the limit starting index for loop of bars recalculation
   if(prev_calculated>rates_total || prev_calculated<=0)// checking for the first start of the indicator calculation
     {
      limit=rates_total-min_rates_total-1; // starting index for calculation of all bars
      to_copy=limit+2;
      //---- performing the shift of beginning of indicator drawing
      PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total+begin);
      for(bar=limit; bar>=0 && !IsStopped(); bar--) MABuffer[bar]=EMPTY_VALUE;
     }
   else
     {
      limit=rates_total-prev_calculated; // starting index for calculation of new bars
      to_copy=limit+1;
     }

//---- copy the newly appeared data into the STD[] and ATR[] arrays
   if(CopyBuffer(MA_Handle,0,0,to_copy,MA)<=0) return(RESET);
   if(CopyBuffer(STD_Handle,0,0,to_copy,STD)<=0) return(RESET);
   
   if(prev_calculated>rates_total || prev_calculated<=0) MABuffer[limit+1]=MA[limit+1];

//---- Main calculation loop of the indicator
   for(bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      v1=MathPow(STD[bar],2);
      v2=MathPow(MABuffer[bar+1]-MA[bar],2);
      //----
      if(v2<v1 || !v2) k=0.0; 
      else k=1-v1/v2;

      MABuffer[bar]=MABuffer[bar+1]+k*(MA[bar]-MABuffer[bar+1]);
      MABuffer[bar]+=dPriceShift;
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
