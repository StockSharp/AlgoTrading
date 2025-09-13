//+------------------------------------------------------------------+
//|                                              Keltner Channel.mq4 |
//|                                                  Coded by Gilani |
//|                      Copyright © 2005, MetaQuotes Software Corp. |
//|                                        http://www.metaquotes.net |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2005, MetaQuotes Software Corp."
#property link      "http://www.metaquotes.net"

#property indicator_chart_window
#property indicator_buffers 3
#property indicator_color1 Red
#property indicator_color2 Blue
#property indicator_color3 Red
//---- input parameters
extern int period = 10;
//----
double upper[], middle[], lower[];

//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
int init()
  {
   SetIndexBuffer(0, upper);
   SetIndexStyle(0, DRAW_LINE);
   SetIndexShift(0, 0);
   SetIndexDrawBegin(0, 0);
//----
   SetIndexBuffer(1, middle);
   SetIndexStyle(1, DRAW_LINE, STYLE_DASHDOT);
   SetIndexShift(1, 0);
   SetIndexDrawBegin(1, 0);
//----
   SetIndexBuffer(2, lower);
   SetIndexStyle(2, DRAW_LINE);
   SetIndexShift(2, 0);
   SetIndexDrawBegin(2, 0);
//---- name for DataWindow label
   SetIndexLabel(0, "KChanUp(" + period + ")");    
   SetIndexLabel(1, "KChanMid(" + period + ")"); 
   SetIndexLabel(2, "KChanLow(" + period + ")"); 
//----
   return(0);
  }
//+------------------------------------------------------------------+
//| Custor indicator deinitialization function                       |
//+------------------------------------------------------------------+
int deinit()
  {
//----
   return(0);
  }
//+------------------------------------------------------------------+
//| Custom indicator iteration function                              |
//+------------------------------------------------------------------+
int start() 
  {
   int limit;
   double avg;   
   int counted_bars = IndicatorCounted();
   if(counted_bars < 0) return(-1);
   if(counted_bars > 0) counted_bars--;
   limit = Bars - counted_bars;
   if(counted_bars==0) limit-=1+period;
//----
   for(int x = 0; x < limit; x++) 
     {
      middle[x] = iMA(NULL, 0, period, 0, MODE_SMA, PRICE_TYPICAL, x);
      avg = findAvg(period, x);
      upper[x] = middle[x] + avg;
      lower[x] = middle[x] - avg;
     }
   return(0);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+  
double findAvg(int period, int shift) 
  {
   double sum = 0;
   for(int x = shift; x < (shift + period); x++) 
     {     
       sum += High[x] - Low[x];
     }
   sum = sum / period;
   return(sum);
  }  
//+------------------------------------------------------------------+

