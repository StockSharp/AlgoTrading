//+------------------------------------------------------------------+
//|                                              ytg_Speed_MA_ea.mq4 |
//|                                                     YURIY TOKMAN |
//|                                            yuriytokman@gmail.com |
//+------------------------------------------------------------------+
#property copyright "YURIY TOKMAN"
#property link      "yuriytokman@gmail.com"

extern int  PeriodMA     = 13;
extern int  Porog        = 10 ;
extern int  shift        = 1;
extern bool Revers       = false;
extern int  TP           = 500;
extern int  SL           = 490;
//+------------------------------------------------------------------+
int start()
  {
//----
   double MA_bar0 =iMA(NULL,0,PeriodMA,0,0,0,shift);
   double MA_bar1 =iMA(NULL,0,PeriodMA,0,0,0,shift+1);
   double Delta = MA_bar0-MA_bar1;   
   bool buy = false, sell = false;
   if(Delta>Porog*Point){if(Revers)sell = true;else buy = true;}   
   if(Delta<-Porog*Point){if(Revers)buy = true;else sell = true;}
   
   if(OrdersTotal()<1){
   if(buy)OrderSend(Symbol(),OP_BUY,0.1,Ask,50,Bid-SL*Point,Ask+TP*Point);
   if(sell)OrderSend(Symbol(),OP_SELL,0.1,Bid,50,Ask+SL*Point,Bid-TP*Point);}   
//----
   return(0);
  }
//+------------------------------------------------------------------+