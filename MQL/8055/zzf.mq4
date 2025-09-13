//+------------------------------------------------------------------+
//|                                                   FX-CHANNEL.mq4 |
//|                      Copyright © 2007, MetaQuotes Software Corp. |
//|                                        http://www.metaquotes.net |
//+------------------------------------------------------------------+
#property copyright "Chris Battles"
#property link      "mailto:cbattles@neo.rr.com"

#property indicator_chart_window
#property indicator_buffers 1
#property indicator_color1 Gold

//---- buffers
double v1[];

//----
double val1;

int k;
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+  
int init()
  {
//---- drawing settings
   
//----   
   SetIndexStyle(0, DRAW_SECTION, STYLE_SOLID, 1);
   SetIndexDrawBegin(0, k);
   SetIndexBuffer(0, v1);
   SetIndexLabel(0, "UPPER");  
//----

//----
   return(0);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int start()
  {

  
   k = Bars;
   
   while(k >= 0)
     {   
       val1 =iCustom(NULL, 0, "ZIGZAG-FRACTALS",0,k);
       if(val1 > 0)
           v1[k] = val1;
       else
           v1[k] = v1[k+1]; 
      
       k--;
     }   
   return(0);
}
 
//+------------------------------------------------------------------+