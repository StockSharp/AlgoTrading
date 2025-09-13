//+------------------------------------------------------------------+
//|                                             Donchian_Channel.mq5 |
//+------------------------------------------------------------------+
#property copyright "Copyright© 2013, Swan"
#property link      "https://login.mql5.com/ru/users/Swan"
#property version   "1.00"
#property indicator_chart_window
#property indicator_buffers 2
#property indicator_plots   2
//--- plot Label1
#property indicator_label1  "High"
#property indicator_type1   DRAW_LINE
#property indicator_color1  C'0,96,255'
#property indicator_style1  STYLE_SOLID
#property indicator_width1  1
//--- plot Label2
#property indicator_label2  "Low"
#property indicator_type2   DRAW_LINE
#property indicator_color2  C'255,96,96'
#property indicator_style2  STYLE_SOLID
#property indicator_width2  1
//--- input parameters
input int InpChannelPeriod=20; // Period
//--- indicator buffers
double ExtHighBuffer[];
double ExtLowBuffer[];
//---
int i,limit,start;
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
int OnInit()
  {
//--- indicator buffers mapping
   SetIndexBuffer(0,ExtHighBuffer,INDICATOR_DATA);
   SetIndexBuffer(1,ExtLowBuffer,INDICATOR_DATA);
//--- set accuracy
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits);
//--- set first bar from what index will be drawn
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,InpChannelPeriod);
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,InpChannelPeriod);
//---
   return(0);
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
//--- check for rates
   if(rates_total<InpChannelPeriod) return(0);
//--- preliminary calculations
   if(prev_calculated==0) limit=InpChannelPeriod;
   else limit=prev_calculated;
//--- the main loop of calculations
   for(i=limit;i<rates_total && !IsStopped();i++)
     {
      start=i-InpChannelPeriod;
      ExtHighBuffer[i]=high[ArrayMaximum(high,start,InpChannelPeriod)];
      ExtLowBuffer[i]=low[ArrayMinimum(low,start,InpChannelPeriod)];
     }
//--- return value of prev_calculated for next call
   return(rates_total);
  }
//+------------------------------------------------------------------+