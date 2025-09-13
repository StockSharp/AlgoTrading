//+------------------------------------------------------------------+
//|                                                         t_ma.mq4 |
//|                                                                  |
//|                                                                  |
//+------------------------------------------------------------------+
#property copyright ""
#property link      ""

#property indicator_chart_window
#property indicator_buffers 2
#property indicator_color1 Turquoise
#property indicator_color2 Red
//---- buffers
double ExtMapBuffer1[];
double ExtMapBuffer2[];
extern int MA_Period=34;
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
int init()
  {
//---- indicators
   SetIndexStyle(0,DRAW_LINE);
   SetIndexBuffer(0,ExtMapBuffer1);
   
   SetIndexStyle(1,DRAW_LINE);
   SetIndexBuffer(1,ExtMapBuffer2);   
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
   int  i,  counted_bars=IndicatorCounted();
//----
 for(i=10;i>=0;i--)
 {
 ExtMapBuffer1[i]=iMA(NULL,0,MA_Period,0,MODE_LWMA,PRICE_OPEN,i);
 ExtMapBuffer2[i]=
 (iMA(NULL,0,MA_Period,0,MODE_LWMA,PRICE_OPEN,i)
  +iMA(NULL,0,MA_Period,0,MODE_LWMA,PRICE_OPEN,i+1)
    +iMA(NULL,0,MA_Period,0,MODE_LWMA,PRICE_OPEN,i+2)
      +iMA(NULL,0,MA_Period,0,MODE_LWMA,PRICE_OPEN,i+3)
        +iMA(NULL,0,MA_Period,0,MODE_LWMA,PRICE_OPEN,i+4)
          +iMA(NULL,0,MA_Period,0,MODE_LWMA,PRICE_OPEN,i+5)  
            )/6;
 }
//----
   return(0);
  }
//+------------------------------------------------------------------+