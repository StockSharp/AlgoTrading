//+------------------------------------------------------------------+
//|                                                     ind_Test.mq4 |
//|                                                                * |
//|                                                                * |
//+------------------------------------------------------------------+
#property copyright "Integer"
#property link      "for-good-letters@yandex.ru"

#property indicator_chart_window
#property indicator_buffers 2
#property indicator_color1 Blue
#property indicator_color2 Red
//---- input parameters
extern int FastMAPeriod=13; // период быстрой МА
extern int FastMAMethod=0; // метод быстрой МА: цена быстрой МА: 0-SMA, 1-EMA, 2-SMMA, 4-LWMA
extern int FastMAPrice=0; // цена быстрой МА: 0-Close, 1-Open, 2-High, 3-Low, 4-Median, 5-Typical, 6-Weighted
extern int SlowMAPeriod=21; // период медленной МА
extern int SlowMAMethod=0; // метод медленной МА: 0-SMA, 1-EMA, 2-SMMA, 4-LWMA
extern int SlowMAPrice=0; // цена медленной МА: 0-Close, 1-Open, 2-High, 3-Low, 4-Median, 5-Typical, 6-Weighted



//---- buffers
double ExtMapBuffer1[];
double ExtMapBuffer2[];
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
int init()
  {
//---- indicators
   SetIndexStyle(0,DRAW_ARROW);
   SetIndexArrow(0,241);
   SetIndexBuffer(0,ExtMapBuffer1);
   SetIndexEmptyValue(0,0.0);
   SetIndexStyle(1,DRAW_ARROW);
   SetIndexArrow(1,242);
   SetIndexBuffer(1,ExtMapBuffer2);
   SetIndexEmptyValue(1,0.0);
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
   int limit=Bars-IndicatorCounted();
   
      for(int i=limit-1;i>=0;i--){
         double fast_ma_1=iMA(NULL,0,FastMAPeriod,0,FastMAMethod,FastMAPrice,i);
         double slow_ma_1=iMA(NULL,0,SlowMAPeriod,0,SlowMAMethod,SlowMAPrice,i);
         double fast_ma_2=iMA(NULL,0,FastMAPeriod,0,FastMAMethod,FastMAPrice,i+1);
         double slow_ma_2=iMA(NULL,0,SlowMAPeriod,0,SlowMAMethod,SlowMAPrice,i+1);
         
            if(fast_ma_1>slow_ma_1 && !(fast_ma_2>slow_ma_2)){
               ExtMapBuffer1[i]=Low[i]-Point*5;
            }
            if(fast_ma_1<slow_ma_1 && !(fast_ma_2<slow_ma_2)){
               ExtMapBuffer2[i]=High[i]+Point*5;
            }            
                  
         
      }
   
//----
   
//----
   return(0);
  }
//+------------------------------------------------------------------+