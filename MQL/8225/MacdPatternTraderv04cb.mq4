//+------------------------------------------------------------------+
//|                                            MacdPatternTrader.mq4 |
//|                                                     FORTRADER.RU |
//|                                              http://FORTRADER.RU |
//+------------------------------------------------------------------+
 
/*

Обрабатываем двойная вершина двойное дно. Расширенная версия советника с адаптивными уровнями стоп лосса, тейкпрофита, 
управлением позиций доступна для скачивания в нашем номере 18 номере журнала.

Manufacturing double apex double bottom. The expanded version adviser with adaptive levels StopLoss, TakeProfit, 
management positions available for download in our room 18 issue of the journal.

Приглашаем авторов статей по трейдингу!
 
Original description strategies:
http://www.unfx.ru/strategies_to_trade/strategies_134.php
 
Подробное описание параметров советника доступно в 19 номере журнала, 
предложения и отзывы мы будем рады видеть в нашей электропочте: letters@fortrader.ru
http://www.fortrader.ru/arhiv.php
 
A detailed description of the parameters adviser available 18 issue of the journal., 
suggestions and feedback we will be glad to see in our e-mail: letters@fortrader.ru
http://www.fortrader.ru/arhiv.php
 
Looking for an interpreter for the English version of the magazine on partnership.
 
 
*/
 
#property copyright "FORTRADER.RU"
#property link      "http://FORTRADER.RU"
 

 
int buy,sell;int nummodb,nummods;int flaglot,bars_bup;
int start()
  {   
      Comment("FORTRADER.RU");
      AOPattern(5,13);
     
 
   return(0);
  }
int aop_maxur,aop_minur,aop_oksell,aop_okbuy,S,S1,H,H2,bS,bS1,bH,bH2,stops,stops1,sstops,sstops1;
double max1,max2,max3,min1,min2,min3;
int AOPattern(double FastEMA,double SlowEMA)
{
   //загружаем индикаторы
   double macdcurr =iMACD(NULL,0,FastEMA,SlowEMA,1,PRICE_CLOSE,MODE_MAIN,1);
   double macdlast =iMACD(NULL,0,FastEMA,SlowEMA,1,PRICE_CLOSE,MODE_MAIN,2);
   double macdlast3 =iMACD(NULL,0,FastEMA,SlowEMA,1,PRICE_CLOSE,MODE_MAIN,3);
   
   
   if(macdcurr>0.0045 && macdcurr<macdlast && macdlast>macdlast3 &&  stops==0){max1=macdlast;Print("m1"+max1);stops=1;}
   
 
   if(macdcurr<0.0045 ){stops=0;max1=0;S=0;}
   
    if(stops==1 && macdcurr>0.0045&& macdcurr<macdlast && macdlast>macdlast3 && macdlast<max1 ){aop_oksell=1;} 
 
   if(macdcurr<0.0045 ){aop_oksell=0;}

   
   if(aop_oksell==1 /*&& max3<max2 && max3<max1*/)
   {max1=0;max2=0;max3=0;
      OrderSend(Symbol(),OP_SELL,0.1,Bid,3,Bid+100*Point,Bid-300*Point,"FORTRADER.RU",16385,0,Red);
      aop_oksell=0;
      aop_maxur=0;
      nummods=0;
      flaglot=0;
      bars_bup=0;
   }
   
    if(macdcurr>-0.0045 ){sstops=0;min1=0;bS=0;}
   
    if(macdcurr<-0.0045 && sstops==1 && macdcurr<0.0045&& macdcurr>macdlast && macdlast<macdlast3 && macdlast>min1 ){aop_okbuy=1;} 
    
   if(macdcurr>0.0045 ){aop_okbuy=0;}

   
   
   if(aop_okbuy==1 )
   {
       OrderSend(Symbol(),OP_BUY,0.1,Ask,3,Ask-100*Point,Ask+300*Point,"FORTRADER.RU",16385,0,Red);
      aop_okbuy=0;
      aop_minur=0;
      nummodb=0;
      flaglot=0;
      sstops1=0;
      min1=0;
      min2=0;
      min3=0;
   }
 
}

 

