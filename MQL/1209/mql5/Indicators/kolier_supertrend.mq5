//+------------------------------------------------------------------+
//|                                            Kolier_SuperTrend.mq5 |
//|                                       Copyright 2010, KoliEr Li. |
//|                                                 http://kolier.li |
//+------------------------------------------------------------------+
//---- author of the indicator
#property copyright "Copyright 2010, KoliEr Li."
//---- link to the website of the author
#property link "http://kolier.li"
//---- indicator version number
#property version   "1.00"
//---- drawing the indicator in the main window
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
//---- LightSeaGreen color is used for indicator line
#property indicator_color1  clrLightSeaGreen
//---- the indicator 1 line is a dotted line
#property indicator_style1  STYLE_DASH
//---- thickness of line of the indicator 1 is equal to 1
#property indicator_width1  1
//---- displaying the indicator line label
#property indicator_label1  "Upper SuperTrend"
//+----------------------------------------------+
//|  Bearish indicator drawing parameters        |
//+----------------------------------------------+
//---- drawing the indicator 2 as a line
#property indicator_type2   DRAW_LINE
//---- DeepPink color is used for the indicator line
#property indicator_color2  clrDeepPink
//---- the indicator 2 line is a dotted line
#property indicator_style2  STYLE_DASH
//---- indicator 2 line width is equal to 1
#property indicator_width2  1
//---- displaying the indicator line label
#property indicator_label2  "Lower SuperTrend"
//+----------------------------------------------+
//|  Bullish indicator drawing parameters        |
//+----------------------------------------------+
//---- drawing the indicator 3 as a label
#property indicator_type3   DRAW_ARROW
//---- lime color is used for the indicator
#property indicator_color3  clrLime
//---- indicator 3 width is equal to 4
#property indicator_width3  4
//---- displaying the indicator label
#property indicator_label3  "Buy SuperTrend"
//+----------------------------------------------+
//|  Bearish indicator drawing parameters        |
//+----------------------------------------------+
//---- drawing the indicator 4 as a label
#property indicator_type4   DRAW_ARROW
//---- red color is used for the indicator
#property indicator_color4  Red
//---- indicator 4 width is equal to 4
#property indicator_width4  4
//---- displaying the indicator label
#property indicator_label4  "Sell SuperTrend"
//+----------------------------------------------+
//|  declaring constants                         |
//+----------------------------------------------+
#define RESET 0 // The constant for returning the indicator recalculation command to the terminal
#define PHASE_NONE 0
#define PHASE_BUY 1
#define PHASE_SELL -1
//+-----------------------------------------------+
//|  Declaration of enumerations                  |
//+-----------------------------------------------+
enum Mode
  {
   SuperTrend=0,//Display as SuperTrend
   NewWay,//Display as NeWay
   Visual,      //Display for the visual trading
   ExpertSignal //Display for the automated trading
  };
//+----------------------------------------------+
//| Indicator input parameters                   |
//+----------------------------------------------+
input Mode TrendMode=NewWay; //Display option
input uint ATR_Period=10;
input double ATR_Multiplier=3.0;
input int Shift=0; // Horizontal shift of the indicator in bars
//+----------------------------------------------+
//---- declaration of dynamic arrays that
//---- will be used as indicator buffers
double UpBuffer[];
double DnBuffer[];
double BuyBuffer[];
double SellBuffer[];
//---- declaration of integer variables for the indicators handles
int ATR_Handle;
//---- declaration of the integer variables for the start of data calculation
int min_rates_total;
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+  
int OnInit()
  {
//---- getting the ATR indicator handle
   ATR_Handle=iATR(NULL,0,ATR_Period);
   if(ATR_Handle==INVALID_HANDLE)
     {
      Print(" Failed to get handle of the ATR indicator");
      return(1);
     }

//---- initialization of variables of the start of data calculation
   min_rates_total=int(ATR_Period+3);

//---- set UpBuffer[] dynamic array as an indicator buffer
   SetIndexBuffer(0,UpBuffer,INDICATOR_DATA);
//---- shifting indicator 1 horizontally by Shift
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//---- shifting the start of drawing of the indicator 1
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- indexing the elements in buffers as timeseries   
   ArraySetAsSeries(UpBuffer,true);
//---- setting the indicator values that won't be visible on a chart
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,0);

//---- set DnBuffer[] dynamic array as an indicator buffer
   SetIndexBuffer(1,DnBuffer,INDICATOR_DATA);
//---- shifting the indicator 2 horizontally by Shift
   PlotIndexSetInteger(1,PLOT_SHIFT,Shift);
//---- shifting the start of drawing of the indicator 2
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,min_rates_total);
//---- indexing the elements in buffers as timeseries   
   ArraySetAsSeries(DnBuffer,true);
//---- setting the indicator values that won't be visible on a chart
   PlotIndexSetDouble(1,PLOT_EMPTY_VALUE,0);

//---- set BuyBuffer[] dynamic array as an indicator buffer
   SetIndexBuffer(2,BuyBuffer,INDICATOR_DATA);
//---- shifting indicator 1 horizontally by Shift
   PlotIndexSetInteger(2,PLOT_SHIFT,Shift);
//---- shifting the start of drawing of the indicator 3
   PlotIndexSetInteger(2,PLOT_DRAW_BEGIN,min_rates_total);
//---- indexing the elements in buffers as timeseries   
   ArraySetAsSeries(BuyBuffer,true);
//---- setting the indicator values that won't be visible on a chart
   PlotIndexSetDouble(2,PLOT_EMPTY_VALUE,0);
//---- indicator symbol
   PlotIndexSetInteger(2,PLOT_ARROW,167);

//---- set SellBuffer[] dynamic array as an indicator buffer
   SetIndexBuffer(3,SellBuffer,INDICATOR_DATA);
//---- shifting the indicator 2 horizontally by Shift
   PlotIndexSetInteger(3,PLOT_SHIFT,Shift);
//---- shifting the start of drawing of the indicator 4
   PlotIndexSetInteger(3,PLOT_DRAW_BEGIN,min_rates_total);
//---- indexing the elements in buffers as timeseries   
   ArraySetAsSeries(SellBuffer,true);
//---- setting the indicator values that won't be visible on a chart
   PlotIndexSetDouble(3,PLOT_EMPTY_VALUE,0);
//---- indicator symbol
   PlotIndexSetInteger(3,PLOT_ARROW,167);

//---- initializations of variable for indicator short name
   string shortname;
   StringConcatenate(shortname,"SuperTrend(",ATR_Period,", ",ATR_Multiplier,", ",Shift,")");
//--- creation of the name to be displayed in a separate sub-window and in a pop up help
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
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
//---- checking for the sufficiency of bars for the calculation
   if(BarsCalculated(ATR_Handle)<rates_total || rates_total<min_rates_total) return(RESET);

//---- declaration of local variables 
   double ATR[],atr,band_upper,band_lower;
   int limit,to_copy,bar,phase;
   static int phase_;

//---- indexing elements in arrays as time series  
   ArraySetAsSeries(close,true);
   ArraySetAsSeries(high,true);
   ArraySetAsSeries(low,true);
   ArraySetAsSeries(ATR,true);

//---- calculation of the 'limit' starting index for the bars recalculation loop
   if(prev_calculated>rates_total || prev_calculated<=0) // checking for the first start of the indicator calculation
     {
      limit=rates_total-min_rates_total-1;               // starting index for calculation of all bars
      phase_=PHASE_NONE;
     }
   else
     {
      limit=rates_total-prev_calculated;                 // starting index for calculation of new bars
     }

   to_copy=limit+1;
//---- copy newly appeared data into the arrays
   if(CopyBuffer(ATR_Handle,0,0,to_copy,ATR)<=0) return(RESET);

//---- restore values of the variables
   phase=phase_;

   for(bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      double mediane=(high[bar]+low[bar])/2;
      atr=ATR[bar];
      atr*=ATR_Multiplier;
      band_upper = mediane + atr;
      band_lower = mediane - atr;

      UpBuffer[bar]=0.0;
      DnBuffer[bar]=0.0;
      BuyBuffer[bar]=0.0;
      SellBuffer[bar]=0.0;

      if(phase==PHASE_NONE)
        {
         UpBuffer[bar]=mediane;
         DnBuffer[bar]=mediane;
        }

      if(phase!=PHASE_BUY && close[bar]>DnBuffer[bar+1] && DnBuffer[bar+1])
        {
         phase=PHASE_BUY;
         UpBuffer[bar]=band_lower;
         if(TrendMode<Visual) UpBuffer[bar+1]=DnBuffer[bar+1];
         else if(TrendMode==Visual) DnBuffer[bar]=DnBuffer[bar+1];
        }

      if(phase!=PHASE_SELL && close[bar]<UpBuffer[bar+1] && UpBuffer[bar+1])
        {
         phase=PHASE_SELL;
         DnBuffer[bar]=band_upper;
         if(TrendMode<Visual) DnBuffer[bar+1]=UpBuffer[bar+1];
         else if(TrendMode==Visual) UpBuffer[bar]=UpBuffer[bar+1];
        }

      if(phase==PHASE_BUY && ((TrendMode==SuperTrend && UpBuffer[bar+2]) || TrendMode>SuperTrend))
        {
         if(band_lower>UpBuffer[bar+1] || (UpBuffer[bar] && TrendMode>NewWay)) UpBuffer[bar]=band_lower;
         else UpBuffer[bar]=UpBuffer[bar+1];
        }

      if(phase==PHASE_SELL && ((TrendMode==SuperTrend && DnBuffer[bar+2]) || TrendMode>SuperTrend))
        {
         if(band_upper<DnBuffer[bar+1] || (DnBuffer[bar] && TrendMode>NewWay)) DnBuffer[bar]=band_upper;
         else DnBuffer[bar]=DnBuffer[bar+1];
        }

      if(TrendMode!=Visual)
        {
         if(DnBuffer[bar+1] && UpBuffer[bar]) BuyBuffer[bar]=UpBuffer[bar];
         if(UpBuffer[bar+1] && DnBuffer[bar]) SellBuffer[bar]=DnBuffer[bar];
        }
      else
        {
         if(!UpBuffer[bar+1] && UpBuffer[bar]) BuyBuffer[bar]=UpBuffer[bar];
         if(!DnBuffer[bar+1] && DnBuffer[bar]) SellBuffer[bar]=DnBuffer[bar];
        }

      //---- store values of the variables
      if(bar==1) phase_=phase;

     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
