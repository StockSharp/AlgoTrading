//+------------------------------------------------------------------+
//|                                                          CHO.mq4 |
//|                      Copyright © 2007, MetaQuotes Software Corp. |
//|                                        http://www.metaquotes.net |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2007, MetaQuotes Software Corp."
#property link      "http://www.metaquotes.net"

#property  indicator_separate_window
#property indicator_buffers 1
#property indicator_color1 DarkBlue
//---- input parameters
extern int       SlowPeriod=10;
extern int       FastPeriod=3;
extern int       TypeSmooth=0;// 0 - SMA, 1 - EMA
//---- buffers
double CHO[];
double AD[];
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
int init()
  {
//---- indicators
   string name,smoothString;
   if (TypeSmooth<0 || TypeSmooth>1) TypeSmooth=0;
   if (TypeSmooth==0) smoothString="SMA"; else smoothString="EMA";
   name="Chaikin Oscillator("+SlowPeriod+","+FastPeriod+","+smoothString+")";

   IndicatorBuffers(2);
   SetIndexStyle(0,DRAW_LINE);
   SetIndexBuffer(0,CHO);
   SetIndexLabel(0,name);
   SetIndexEmptyValue(0,0.0);
   SetIndexBuffer(1,AD);
   SetIndexEmptyValue(1,0.0);
   IndicatorShortName(name);
   
//----
   return(0);
  }
//+------------------------------------------------------------------+
//| Custom indicator deinitialization function                       |
//+------------------------------------------------------------------+
int deinit()
  {
//----
   
//----
   return(0);
  }
//+------------------------------------------------------------------+
//| Custom indicator iteration function                              |
//+------------------------------------------------------------------+
int start()
  {
   int    counted_bars=IndicatorCounted();
   int limit,i;
//----
   if (counted_bars<0) return(-1);

   if (counted_bars==0) 
      {
      limit=Bars-1;
      for (i=limit;i>=0;i--)
        {
         AD[i]=iAD(NULL,0,i);
        }
      for(i=limit-SlowPeriod;i>=0;i--)
        {
         CHO[i]=iMAOnArray(AD,0,FastPeriod,0,TypeSmooth,i)-iMAOnArray(AD,0,SlowPeriod,0,TypeSmooth,i);
        }
      }

   if (counted_bars>0) 
     {
      limit=Bars-counted_bars;
      for (i=limit;i>=0;i--)
        {
         AD[i]=iAD(NULL,0,i);
        }
      for(i=limit;i>=0;i--)
        {
         CHO[i]=iMAOnArray(AD,0,FastPeriod,0,TypeSmooth,i)-iMAOnArray(AD,0,SlowPeriod,0,TypeSmooth,i);
        }
      }            
//----
   return(0);
  }
//+------------------------------------------------------------------+