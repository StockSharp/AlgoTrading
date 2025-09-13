//+------------------------------------------------------------------+
//|                                                   VLT_TRADER.mq4 |
//|                                                     FORTRADER.RU |
//|                                          http://www.fortrader.ru |
//+------------------------------------------------------------------+
#property copyright "FORTRADER.RU"
#property link      "http://www.fortrader.ru"

extern int profit = 10;
extern int stop = 10;
double BUYLIMIT,SELLLIMIT,bar,sup,sup1;
int okbuy,oksell,onepossell,oneposbuy,i;
double value=100;

int start()
  {
  double VLT,VSE,ULTRA;

  
  
  VLT=MathAbs(iHigh(NULL,0,1)-iLow(NULL,0,1));

  
  for(int i=2;i<10;i++)
  {
  
  if (MathAbs(iHigh(NULL,0,i)-iLow(NULL,0,i))<value && MathAbs(iHigh(NULL,0,i)-iLow(NULL,0,i))>0){value=MathAbs(iHigh(NULL,0,i)-iLow(NULL,0,i));}
  }  
  
  
  okbuy=0;oksell=0;
  for(int cnt=0;cnt<OrdersTotal();cnt++)
     {
      OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES);
       
         if(OrderType()==OP_BUY || OrderType()==OP_BUYSTOP)
         {
         okbuy=1;
         }
        
         if(OrderType()==OP_SELL || OrderType()==OP_SELLSTOP)
         {
         oksell=1;
         }
      }
    
  

  if(VLT<value && okbuy==0)
  {
  double OpenPrice=High[1]+10*Point;
  double Profit=OpenPrice+profit*Point;
  double Stop=OpenPrice-stop*Point;
  OrderSend(Symbol(),OP_BUYSTOP,0.1,OpenPrice,3,Stop,Profit,"My order #",16384,0,Green);
  okbuy=1;
  }
  
      if(VLT<value && oksell==0)
  {
   OpenPrice=Low[1]-10*Point;
   Profit=OpenPrice-profit*Point;
   Stop=OpenPrice+stop*Point;
  OrderSend(Symbol(),OP_SELLSTOP,0.1,OpenPrice,3,Stop,Profit,"My order #",16384,0,Green);
  oksell=1;
  }


 

  
value=100;
   return(0);
  }

