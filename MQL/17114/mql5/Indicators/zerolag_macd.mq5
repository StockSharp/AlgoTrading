//+------------------------------------------------------------------+
//|                                                 ZeroLag_MACD.mq5 |
//|                                                   Copyright 2009 |
//|                                                                  |
//+------------------------------------------------------------------+
#property copyright   "Copyright 2010"
#property link        ""
#property description "ZeroLag Moving Average Convergence/Divergence"
#include <MovingAverages.mqh>
//--- indicator settings
#property indicator_separate_window
#property indicator_buffers 7
#property indicator_plots   3
#property indicator_type1   DRAW_LINE
#property indicator_type2   DRAW_HISTOGRAM
#property indicator_type3   DRAW_HISTOGRAM
#property indicator_color1  DarkGray
#property indicator_color2  Green
#property indicator_color3  Red
#property indicator_width1  1
#property indicator_width2  1
#property indicator_width3  1
#property indicator_label1  "MACD"
#property indicator_label2  "Histogram up"
#property indicator_label3  "Histogram down"
//--- input parameters
input int                InpFastEMA=12;               // Fast EMA period
input int                InpSlowEMA=26;               // Slow EMA period
input ENUM_APPLIED_PRICE InpAppliedPrice=PRICE_CLOSE; // Applied price
//--- indicator buffers
double                   ExtMacdBuffer[];
double                   ExtMacdUpBuffer[];
double                   ExtMacdDownBuffer[];
double                   ExtFastMaBuffer[];
double                   ExtSlowMaBuffer[];
double                   ExtFastResMaBuffer[];
double                   ExtSlowResMaBuffer[];
//--- MA handles
int                      ExtFastMaHandle;
int                      ExtSlowMaHandle;
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
void OnInit()
  {
//--- indicator buffers mapping
   SetIndexBuffer(0,ExtMacdBuffer,INDICATOR_DATA);
   SetIndexBuffer(1,ExtMacdUpBuffer,INDICATOR_DATA);
   SetIndexBuffer(2,ExtMacdDownBuffer,INDICATOR_DATA);
   SetIndexBuffer(3,ExtFastMaBuffer,INDICATOR_CALCULATIONS);
   SetIndexBuffer(4,ExtSlowMaBuffer,INDICATOR_CALCULATIONS);
   SetIndexBuffer(5,ExtFastResMaBuffer,INDICATOR_CALCULATIONS);
   SetIndexBuffer(6,ExtSlowResMaBuffer,INDICATOR_CALCULATIONS);
//--- sets first bar from what index will be drawn
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,InpFastEMA-1);
//--- name for Dindicator subwindow label
   IndicatorSetString(INDICATOR_SHORTNAME,"ZeroLag MACD ("+string(InpFastEMA)+","+string(InpSlowEMA)+")");
//--- get MA handles
   ExtFastMaHandle=iMA(NULL,0,InpFastEMA,0,MODE_EMA,InpAppliedPrice);
   ExtSlowMaHandle=iMA(NULL,0,InpSlowEMA,0,MODE_EMA,InpAppliedPrice);
//--- initialization done
  }
//+------------------------------------------------------------------+
//| Moving Averages Convergence/Divergence                           |
//+------------------------------------------------------------------+
int OnCalculate(const int rates_total,const int prev_calculated,
                const datetime &Time[],
                const double &Open[],
                const double &High[],
                const double &Low[],
                const double &Close[],
                const long &TickVolume[],
                const long &Volume[],
                const int &Spread[])
  {
//--- check for data
   if(rates_total<InpFastEMA)
      return(0);
//--- not all data may be calculated
   int calculated=BarsCalculated(ExtFastMaHandle);
   if(calculated<rates_total)
     {
      Print("Not all data of ExtFastMaHandle is calculated (",calculated,"bars ). Error",GetLastError());
      return(0);
     }
   calculated=BarsCalculated(ExtSlowMaHandle);
   if(calculated<rates_total)
     {
      Print("Not all data of ExtSlowMaHandle is calculated (",calculated,"bars ). Error",GetLastError());
      return(0);
     }
//--- we can copy not all data
   int to_copy;
   if(prev_calculated>rates_total || prev_calculated<0) to_copy=rates_total;
   else
     {
      to_copy=rates_total-prev_calculated;
      if(prev_calculated>0) to_copy++;
     }
//--- get Fast EMA buffer
   if(CopyBuffer(ExtFastMaHandle,0,0,to_copy,ExtFastMaBuffer)<=0)
     {
      Print("Getting fast EMA is failed! Error",GetLastError());
      return(0);
     }
//--- get SlowSMA buffer
   if(CopyBuffer(ExtSlowMaHandle,0,0,to_copy,ExtSlowMaBuffer)<=0)
     {
      Print("Getting slow SMA is failed! Error",GetLastError());
      return(0);
     }
//---
   int limit;
   if(prev_calculated==0)
      limit=0;
   else limit=prev_calculated-1;
//--- calculate MACD
   ExtMacdUpBuffer[0]=0;
   ExtMacdDownBuffer[0]=0;
   ExponentialMAOnBuffer(rates_total,prev_calculated,0,InpFastEMA,ExtFastMaBuffer,ExtFastResMaBuffer);
   ExponentialMAOnBuffer(rates_total,prev_calculated,0,InpSlowEMA,ExtSlowMaBuffer,ExtSlowResMaBuffer);
   for(int i=limit;i<rates_total;i++)
     {
      ExtMacdBuffer[i]=10*((2*ExtFastMaBuffer[i]-ExtFastResMaBuffer[i]) -(2*ExtSlowMaBuffer[i]-ExtSlowResMaBuffer[i]));
      if(i>0)
        {
         if(ExtMacdBuffer[i]>ExtMacdBuffer[i-1])
           {
            ExtMacdUpBuffer[i]=ExtMacdBuffer[i];
            ExtMacdDownBuffer[i]=0;
           }
         else
           {
            ExtMacdDownBuffer[i]=ExtMacdBuffer[i];
            ExtMacdUpBuffer[i]=0;
           }
        }
     }
//-0.-- OnCalculate done. Return new prev_calculated.
   return(rates_total);
  }
//+------------------------------------------------------------------+
