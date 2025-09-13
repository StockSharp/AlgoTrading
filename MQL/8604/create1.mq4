//+---------------------------------------------------------------------+
//|                              create1.mq4                            |
//|                                                                     |
//+---------------------------------------------------------------------+
//
//
//
extern double    lots=0.1;
//extern int       StopLoss=30 ;
//extern int       TrailingStop=15;
//extern int       Slippage=2;

extern int BB = 100;
extern int MM = 1;
extern int II = 0;
extern double KK = 1.0;
extern int NN = 1102;

double  maH0,maH1,maL0,maL1;
//+------------------------------------------------------------------+
//+------------------------------------------------------------------+
//| expert start function                                            |
//+------------------------------------------------------------------+
int start()
  {
maH0=iMA(NULL,0,1,0,MODE_LWMA,PRICE_HIGH,0); //мувинг по high
maH1=iMA(NULL,0,1,0,MODE_LWMA,PRICE_HIGH,1); //мувинг по high
maL0=iMA(NULL,0,1,0,MODE_LWMA,PRICE_LOW,0);  //мувинг по low
maL1=iMA(NULL,0,1,0,MODE_LWMA,PRICE_LOW,1);  //мувинг по low

   int      B= BB;         // 
   int      M= MM;         // 
   int      I= II;         // 
   double   K= KK;         // 
   int      N= NN;         // 
   
   double cgh = iCustom(NULL,0,"Center of Gravity",B,M,I,K,N,3,0);
   double cgl = iCustom(NULL,0,"Center of Gravity",B,M,I,K,N,4,0);
   


if (cgl<maL0)  //если мувинг (low) пересек самую нижнюю линию индикатора снизу вверх
   {
   OrderSend(NULL,OP_BUY,lots,Ask,2,Ask-10*Point,Ask+20*Point,"create1",123,0,Lime);
   }
/*
if (cgh>maH0)
   {
   OrderSend(NULL,OP_SELL,lots,Bid,2,Bid+10*Point,Bid-20*Point,"create1",123,0,Red);
   }  
*/   
   
    
    
 
//----
   return(0);
  }
//+------------------------------------------------------------------+





















