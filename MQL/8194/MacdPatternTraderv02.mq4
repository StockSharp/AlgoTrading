//+------------------------------------------------------------------+
//|                                            MacdPatternTrader.mq4 |
//|                                                     FORTRADER.RU |
//|                                              http://FORTRADER.RU |
//+------------------------------------------------------------------+

/*
 
Original description strategies:
http://www.unfx.ru/strategies_to_trade/strategies_134.php
 
Подробное описание параметров советника доступно в номере журнала от 10 Июня, 
предложения и отзывы мы будем рады видеть в нашей электропочте: letters@fortrader.ru
http://www.fortrader.ru/arhiv.php
 
A detailed description of the parameters adviser available issue of the journal dated Iune 10, 
suggestions and feedback we will be glad to see in our e-mail: letters@fortrader.ru
http://www.fortrader.ru/arhiv.php
 
Looking for an interpreter for the English version of the magazine on partnership.
 

*/

#property copyright "FORTRADER.RU"
#property link      "http://FORTRADER.RU"

extern int stoplossbars = 6;
extern int takeprofitbars = 20;
extern int otstup = 10; 
extern int lowema=5;
extern int fastema=13;
extern double maxur=0.0045;
extern double minur=-0.0045;

extern string x="Настройки MA:";
extern  int perema1=7;
extern  int perema2=21;
extern  int persma3=98;
extern  int perema4=365;

extern double Lots=0.1;

int buy,sell;int nummodb,nummods;int flaglot;
int start()
  {   

      AOPattern(lowema,fastema,maxur,minur);
      ActivePosManager(perema1,perema2,persma3,perema4);
 
   return(0);
  }
int aop_maxur,aop_minur,aop_oksell,aop_okbuy;int value_min,value_max,value_curr;
int AOPattern(double FastEMA,double SlowEMA,double maxur,double minur)
{
   //загружаем индикаторы
   double macdcurr =iMACD(NULL,0,FastEMA,SlowEMA,1,PRICE_CLOSE,MODE_MAIN,1);
   double macdlast =iMACD(NULL,0,FastEMA,SlowEMA,1,PRICE_CLOSE,MODE_MAIN,2);
   double macdlast3 =iMACD(NULL,0,FastEMA,SlowEMA,1,PRICE_CLOSE,MODE_MAIN,3);

  
    buy=0;sell=0;
     for(int  i=0;i<OrdersTotal();i++)
         {
           OrderSelect(i, SELECT_BY_POS, MODE_TRADES);
           if(OrderType()==OP_BUY){buy=1;}
           if(OrderType()==OP_SELL){sell=1;}
         }   
         
   
   if(macdcurr>0){aop_maxur=1;aop_oksell=0;}

   if(macdcurr>macdlast && macdlast<macdlast3 && aop_maxur==1  && macdcurr>minur && macdcurr<0 && aop_oksell==0)
   { 
   aop_oksell=1; value_min= MathAbs(macdlast*10000);
   }
value_curr=MathAbs(macdcurr*10000);

 if(aop_oksell==1  && macdcurr<macdlast && macdlast>macdlast3 && macdcurr<0 && value_min<=value_curr)
   {
   aop_maxur=0;
   }

   if(aop_oksell==1  && macdcurr<macdlast && macdlast>macdlast3 && macdcurr<0 )
   {
  
      OrderSend(Symbol(),OP_SELL,Lots,Bid,3,StopLoss(0),TakeProfit(0),"FORTRADER.RU",16385,0,Red);
      aop_oksell=0;
      aop_maxur=0;
      nummods=0;
      flaglot=0;
   }
   
   if(macdcurr<0){aop_minur=1;aop_okbuy=0;}
   if(macdcurr<maxur  && macdcurr<macdlast && macdlast>macdlast3 && aop_minur==1  && macdcurr>0)
   { 
   aop_okbuy=1; value_max= MathAbs(macdlast*10000);
  
   }
   value_curr=MathAbs(macdcurr*10000);
   
   if(aop_okbuy==1  && macdcurr>macdlast && macdlast<macdlast3 && macdcurr>0 && value_max<=value_curr)
   {
   aop_minur=0;
   }
   
   if(aop_okbuy==1  && macdcurr>macdlast && macdlast<macdlast3 && macdcurr>0 )
   {
       OrderSend(Symbol(),OP_BUY,Lots,Ask,3,StopLoss(1),TakeProfit(1),"FORTRADER.RU",16385,0,Red);
      aop_okbuy=0;
      aop_minur=0;
      nummodb=0;
      flaglot=0;
   }

}

double StopLoss(int type)
{double stoploss;
if(type==0)
{
  stoploss=High[iHighest(NULL,0,MODE_HIGH,stoplossbars,1)]+otstup*Point;
 return(stoploss);
}
if(type==1)
{
  stoploss=Low[iLowest(NULL,0,MODE_LOW,stoplossbars,1)]-otstup*Point;
 return(stoploss);
}

}

double TakeProfit(int type)
{ int x=0,stop=0;double takeprofit;
  
  if(type==0)
   {
   while(stop==0)
         {
           takeprofit =Low[iLowest(NULL,0,MODE_LOW,takeprofitbars,x)];
          if(takeprofit>Low[iLowest(NULL,0,MODE_LOW,takeprofitbars,x+takeprofitbars)])
            {
            takeprofit =Low[iLowest(NULL,0,MODE_LOW,takeprofitbars,x+takeprofitbars)];
            x=x+takeprofitbars;
            }
          else
            {
             stop=1;return(takeprofit);
            }
         }
   }
   
   if(type==1)
   {
   while(stop==0)
         {
           takeprofit =High[iHighest(NULL,0,MODE_HIGH,takeprofitbars,x)];
          if(takeprofit<High[iHighest(NULL,0,MODE_HIGH,takeprofitbars,x+takeprofitbars)])
            {
            takeprofit =High[iHighest(NULL,0,MODE_HIGH,takeprofitbars,x+takeprofitbars)];
            x=x+takeprofitbars;
            }
          else
            {
             stop=1;return(takeprofit);
            }
         }
   }
                
}

int  ActivePosManager(int perema1, int perema2, int persma3, int perema4)
{
   double ema1 =iMA(NULL,0,perema1,0,MODE_EMA,PRICE_CLOSE,1);
    double ema2 =iMA(NULL,0,perema2,0,MODE_EMA,PRICE_CLOSE,1);
     double sma1 =iMA(NULL,0,persma3,0,MODE_SMA,PRICE_CLOSE,1);
      double ema3 =iMA(NULL,0,perema4,0,MODE_EMA,PRICE_CLOSE,1);


   for( int i=0;i<OrdersTotal();i++)
      {
              OrderSelect(i, SELECT_BY_POS, MODE_TRADES);
              if(OrderType()==OP_BUY && OrderProfit()>5 && Close[1]>ema2  && nummodb==0)
              {  
                 OrderClose(OrderTicket(),NormalizeDouble(OrderLots()/3,2),Bid,3,Violet); 
                 nummodb++;
              }
              
                 if(OrderType()==OP_BUY && OrderProfit()>5 && High[1]>(sma1+ema3)/2 && nummodb==1)
              {  
                 OrderClose(OrderTicket(),NormalizeDouble(OrderLots()/2,2),Bid,3,Violet); 
                 nummodb++;
              }
              
                if(OrderType()==OP_SELL && OrderProfit()>5 && Close[1]<ema2    && nummods==0)
              {  
                 OrderClose(OrderTicket(),NormalizeDouble(OrderLots()/3,2),Ask,3,Violet); 
                 nummods++;
              }
              
                   if(OrderType()==OP_SELL && OrderProfit()>5 && Low[1]<(sma1+ema3)/2 && nummods==1)
              {  
                 OrderClose(OrderTicket(),NormalizeDouble(OrderLots()/2,2),Ask,3,Violet); 
                 nummods++;
              }
      }

}


