//+------------------------------------------------------------------+
//|                                      Price_Extreme_Indicator.mq5 |
//|                        Copyright 2018, MetaQuotes Software Corp. |
//|                                                 https://mql5.com |
//+------------------------------------------------------------------+
#property copyright "Copyright 2018, MetaQuotes Software Corp."
#property link      "https://mql5.com"
#property version   "1.00"
#property indicator_chart_window
#property indicator_buffers 2
#property indicator_plots   2
//--- plot BorderHigh
#property indicator_label1  "Border High"
#property indicator_type1   DRAW_LINE
#property indicator_color1  clrYellow
#property indicator_style1  STYLE_SOLID
#property indicator_width1  1
//--- plot BorderLow
#property indicator_label2  "Border Low"
#property indicator_type2   DRAW_LINE
#property indicator_color2  clrBlue
#property indicator_style2  STYLE_SOLID
#property indicator_width2  1
//--- input parameters
input int      InpMultiplier=5;     // Length of levels
//--- indicator buffers
double         BufferBorderHigh[];
double         BufferBorderLow[];
//--- global variables
int            multiplier;
int            coeff;
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
int OnInit()
  {
//--- settings parameters
   multiplier=(InpMultiplier<1 ? 1 : InpMultiplier);
   coeff=PeriodSeconds()*multiplier;
//--- indicator buffers mapping
   SetIndexBuffer(0,BufferBorderHigh,INDICATOR_DATA);
   SetIndexBuffer(1,BufferBorderLow,INDICATOR_DATA);
   PlotIndexSetInteger(0,PLOT_SHIFT,multiplier);
   PlotIndexSetInteger(1,PLOT_SHIFT,multiplier);
//---
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
//--- Checking for minimum number of bars
   if(rates_total<multiplier+2) return 0;
//--- Set arrays as time series
   ArraySetAsSeries(BufferBorderHigh,true);
   ArraySetAsSeries(BufferBorderLow,true);
   ArraySetAsSeries(high,true);
   ArraySetAsSeries(low,true);
   ArraySetAsSeries(time,true);
//--- check for limits
   int limit=rates_total-prev_calculated;
   if(limit>1)
     {
      limit=rates_total-1-multiplier;
     }
   else limit+=multiplier+2;
//--- calculate indicator
   for(int i=0; i<limit; i++)
     {
      datetime BeginTime=(datetime)ceil(time[i]/coeff)*coeff;
      datetime EndTime=(datetime)ceil(time[i]/coeff+1)*coeff;
      int begin_bar=BarShift(PERIOD_CURRENT,BeginTime);
      int end_bar=BarShift(PERIOD_CURRENT,EndTime);
      if(begin_bar==WRONG_VALUE || end_bar==WRONG_VALUE) continue;
      int bar_h=Highest(PERIOD_CURRENT,begin_bar-end_bar+1,end_bar);
      int bar_l=Lowest(PERIOD_CURRENT,begin_bar-end_bar+1,end_bar);
      if(bar_h==WRONG_VALUE || bar_l==WRONG_VALUE) continue;
      BufferBorderHigh[i]=high[bar_h];
      BufferBorderLow[i]=low[bar_l];
     }
//--- return value of prev_calculated for next call
   return(rates_total);
  }
//+------------------------------------------------------------------+
//| Returns index of highest value                                   |
//+------------------------------------------------------------------+
int Highest(const ENUM_TIMEFRAMES timeframe,const int count,const int start)
  {
   double array[];
   ArraySetAsSeries(array,true);
   if(CopyHigh(Symbol(),timeframe,start,count,array)==count)
      return ArrayMaximum(array)+start;
   return WRONG_VALUE;
  }
//+------------------------------------------------------------------+
//| Returns index of lowest value                                    |
//+------------------------------------------------------------------+
int Lowest(const ENUM_TIMEFRAMES timeframe,const int count,const int start)
  {
   double array[];
   ArraySetAsSeries(array,true);
   if(CopyLow(Symbol(),timeframe,start,count,array)==count)
      return ArrayMinimum(array)+start;
   return WRONG_VALUE;
  }
//+------------------------------------------------------------------+
//| Returns the offset of the bar by time                            |
//+------------------------------------------------------------------+
int BarShift(const ENUM_TIMEFRAMES timeframe,const datetime time)
  {
   int res=WRONG_VALUE;
   datetime last_bar=0;
   if(::SeriesInfoInteger(Symbol(),timeframe,SERIES_LASTBAR_DATE,last_bar))
     {
      if(time>last_bar) res=0;
      else
        {
         const int shift=::Bars(Symbol(),timeframe,time,last_bar);
         if(shift>0) res=shift-1;
        }
     }
   return(res);
  }
//+------------------------------------------------------------------+
