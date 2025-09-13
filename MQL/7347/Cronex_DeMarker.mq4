//+------------------------------------------------------------------+
//|                                              Cronex DeMarker.mq4 |
//|                                        Copyright © 2007, Cronex. |
//|                                       http://www.metaquotes.net/ |
//+------------------------------------------------------------------+
#property  copyright "Copyright © 2007, Cronex"
#property  link      "http://www.metaquotes.net/"
//---- indicator settings
#property  indicator_separate_window
#property  indicator_buffers 3
#property  indicator_color1  Silver
#property  indicator_color2  Blue
#property  indicator_color3  Red
//#property  indicator_width1  2
//---- indicator parameters
extern int DeMarker=25;
extern int FastMA=14;
extern int SlowMA=25;
//---- indicator buffers
double     DeMarkerBuffer[];
double     FastMABuffer[];
double     SlowMABuffer[];
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
int init()
  {
//---- drawing settings
   SetIndexStyle(0,DRAW_NONE);
   SetIndexStyle(1,DRAW_LINE);
   SetIndexStyle(2,DRAW_LINE);   
   SetIndexDrawBegin(1,SlowMA);
//   IndicatorDigits(Digits+1);
//---- indicator buffers mapping
   SetIndexBuffer(0,DeMarkerBuffer);
   SetIndexBuffer(1,FastMABuffer);
   SetIndexBuffer(2,SlowMABuffer);
//---- name for DataWindow and indicator subwindow label
   IndicatorShortName("Cronex DeMarker");
   SetIndexLabel(0,"DeMarker");
   SetIndexLabel(1,"Fast MA");
   SetIndexLabel(2,"Slow MA");
//---- initialization done
   return(0);
  }
//+------------------------------------------------------------------+
//| Cronex DeMarker                                                  |
//+------------------------------------------------------------------+
int start()
  {
   int limit;
   int counted_bars=IndicatorCounted();
//---- last counted bar will be recounted
   if(counted_bars>0) counted_bars--;
   limit=Bars-counted_bars;
//---- DeMarker counted in the 1-st buffer

   for(int i=0; i<limit; i++)
      DeMarkerBuffer[i]=iDeMarker(NULL,0,DeMarker,i);
//---- signal line counted in the 2-nd buffer
   for(i=0; i<limit; i++)
    {  
      FastMABuffer[i]=iMAOnArray(DeMarkerBuffer,Bars,FastMA,0,MODE_LWMA,i);
      SlowMABuffer[i]=iMAOnArray(DeMarkerBuffer,Bars,SlowMA,0,MODE_LWMA,i);
    } 
//---- done
   return(0);
  }
//+------------------------------------------------------------------+