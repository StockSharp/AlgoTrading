#property link "http://www.forex-instruments.info"
//+------------------------------------------------------------------+
//|                                                         RAVI.mq4 |
//|                      Copyright © 2005, MetaQuotes Software Corp. |
//|                                        http://www.metaquotes.net |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2005, MetaQuotes Software Corp."
#property link      "http://www.metaquotes.net"

#property indicator_separate_window 
#property indicator_buffers 1 
#property indicator_color1 Red 
//---- indicator parameters 
extern int Period1=7; 
extern int Period2=65; 
//---- buffers 
double ExtBuffer[]; 
//+------------------------------------------------------------------+ 
//| Custom indicator initialization function                         | 
//+------------------------------------------------------------------+ 
int init() 
  { 
//---- indicators 
   SetIndexStyle(0,DRAW_LINE); 
   SetIndexBuffer(0,ExtBuffer); 
   IndicatorShortName("RAVI (" + Period1+ ","+Period2+")"); 
    
//---- 
   return(0); 
  } 
//+------------------------------------------------------------------+ 
//| Custor indicator deinitialization function                       | 
//+------------------------------------------------------------------+ 
int deinit() 
  { 
//---- TODO: add your code here 
    
//---- 
   return(0); 
  } 
//+------------------------------------------------------------------+ 
//| Custom indicator iteration function                              | 
//+------------------------------------------------------------------+ 
int start() 
  { 
   int    counted_bars=IndicatorCounted(); 
   int i=0; 
   double SMA1,SMA2,result; 
    
    
//---- TODO: add your code here 
  
  for(i=0;i<Bars;i++) 
   { 
   SMA1=iMA(NULL,0,Period1,0,MODE_SMA,PRICE_CLOSE,i); 
   SMA2=iMA(NULL,0,Period2,0,MODE_SMA,PRICE_CLOSE,i); 
   result=((SMA1-SMA2)/SMA2)*100; 
   ExtBuffer[i]=result; 
   } 
  
//---- 
   return(0); 
  } 
//+------------------------------------------------------------------+ 

