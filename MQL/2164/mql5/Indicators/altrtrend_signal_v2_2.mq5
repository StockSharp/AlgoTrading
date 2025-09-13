//+------------------------------------------------------------------+
//|                                        AltrTrend_Signal_v2_2.mq5 |
//|                                Copyright © 2005, OlegVS, GOODMAN |
//|                                                                  |
//+------------------------------------------------------------------+
//--- Copyright
#property copyright "Copyright © 2005, OlegVS, GOODMAN"
//--- link to the website of the author
#property link      ""
//--- Indicator version
#property version   "1.01"
//--- drawing the indicator in the main window
#property indicator_chart_window 
//--- two buffers are used for calculating and drawing the indicator
#property indicator_buffers 2
//--- two plots are used
#property indicator_plots   2
//+----------------------------------------------+
//|  Parameters of drawing the bearish indicator |
//+----------------------------------------------+
//--- drawing the indicator 1 as a symbol
#property indicator_type1   DRAW_ARROW
//--- orange is used as the color of the indicator bearish line
#property indicator_color1  clrDarkOrange
//--- indicator 1 line width is equal to 4
#property indicator_width1  4
//--- display of the indicator bullish label
#property indicator_label1  "Sell"
//+----------------------------------------------+
//|  Bullish indicator drawing parameters        |
//+----------------------------------------------+
//--- drawing the indicator 2 as a symbol
#property indicator_type2   DRAW_ARROW
//---- green color is used as the color of the indicator bullish line
#property indicator_color2  clrLime
//---- indicator 2 line width is equal to 4
#property indicator_width2  4
//--- display of the bearish indicator label
#property indicator_label2 "Buy"
//+----------------------------------------------+
//|  declaring constants                         |
//+----------------------------------------------+
#define RESET 0  // A constant for returning the indicator recalculation command to the terminal
//+----------------------------------------------+
//| Indicator input parameters                   |
//+----------------------------------------------+
input uint K=30;
input double Kstop = 0.5;
input uint Kperiod = 150;
input uint PerADX=14;
//+----------------------------------------------+
//--- declaration of dynamic arrays that 
//--- will be used as indicator buffers
double SellBuffer[];
double BuyBuffer[];
//--- declaration of integer variables for the start of data calculation
int min_rates_total;
//--- declaration of integer variables for the indicators handles
int ADX_Handle;
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
int OnInit()
  {
//--- initialization of global variables 
   min_rates_total=int(PerADX)+1;
//--- getting the handle of the iADX indicator
   ADX_Handle=iADX(NULL,PERIOD_CURRENT,PerADX);
   if(ADX_Handle==INVALID_HANDLE)
     {
      Print(" Failed to get the handle of the iADX indicator");
      return(INIT_FAILED);
     }
//--- Set dynamic array as an indicator buffer
   SetIndexBuffer(0,SellBuffer,INDICATOR_DATA);
//--- shifting the start of drawing the indicator 1
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//--- indicator symbol
   PlotIndexSetInteger(0,PLOT_ARROW,234);
//--- Indexing elements in the buffer as in timeseries
   ArraySetAsSeries(SellBuffer,true);
//--- Set dynamic array as an indicator buffer
   SetIndexBuffer(1,BuyBuffer,INDICATOR_DATA);
//--- shifting the starting point of calculation of drawing of the indicator 2
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,min_rates_total);
//--- indicator symbol
   PlotIndexSetInteger(1,PLOT_ARROW,233);
//--- Indexing elements in the buffer as in timeseries
   ArraySetAsSeries(BuyBuffer,true);
//--- setting the format of accuracy of displaying the indicator
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits);
//--- name for the data window and the label for sub-windows 
   string short_name="AltrTrend_Signal_v2_2";
   IndicatorSetString(INDICATOR_SHORTNAME,short_name);
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
   if(BarsCalculated(ADX_Handle)<rates_total || rates_total<min_rates_total) return(RESET);
//--- declarations of local variables 
   int to_copy,limit,bar,end,trend;
   double ADX[],Res,Range,AvgRange,smin,smax,SsMax,SsMin,SSP;
   static int old_trend;
//--- calculations of the necessary amount of data to be copied
//--- and the 'limit' starting index for the bars recalculation loop
   if(prev_calculated>rates_total || prev_calculated<=0)// Checking for the first start of the indicator calculation
     {
      limit=rates_total-min_rates_total; // starting index for calculation of all bars
      old_trend=0;
     }
   else
     {
      limit=rates_total-prev_calculated; // starting index for calculation of new bars
     }
   to_copy=limit+2;
//--- copy newly appeared data in the array
   if(CopyBuffer(ADX_Handle,0,0,to_copy,ADX)<=0) return(RESET);
//--- apply timeseries indexing to array elements  
   ArraySetAsSeries(ADX,true);
   ArraySetAsSeries(high,true);
   ArraySetAsSeries(low,true);
//--- main indicator calculation loop
   for(bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      SSP=MathCeil(Kperiod/ADX[bar+1]);
      end=int(MathMin(bar+SSP,rates_total));

      AvgRange=0;
      for(int iii=bar; iii<end; iii++) AvgRange+=MathAbs(high[iii]-low[iii]);
      Range=AvgRange/(SSP+1);

      SsMax=high[bar];
      SsMin=low[bar];
      for(int kkk=bar; kkk<end; kkk++)
        {
         if(SsMax<high[kkk]) SsMax=high[kkk];
         if(SsMin>=low[kkk]) SsMin=low[kkk];
        }
      //---
      Res=(SsMax-SsMin)*K/100;
      smin = SsMin + Res;
      smax = SsMax - Res;

      BuyBuffer[bar]=0.0;
      SellBuffer[bar]=0.0;

      if(close[bar]<smin) trend=-1;
      if(close[bar]>smax) trend=+1;
      if(!old_trend) old_trend=trend;
      if(trend != old_trend && close[bar] > smax) BuyBuffer[bar]=low[bar]-Range*Kstop;
      if(trend != old_trend && close[bar] < smin) SellBuffer[bar]=high[bar]+Range*Kstop;
      if(bar) old_trend=trend;
     }
//---     
   return(rates_total);
  }
//+------------------------------------------------------------------+
