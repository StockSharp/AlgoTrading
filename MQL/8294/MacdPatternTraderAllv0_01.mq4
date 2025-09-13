//+------------------------------------------------------------------+
//|                                            MacdPatternTrader.mq4 |
//|                                                     FORTRADER.RU |
//|                                              http://FORTRADER.RU |
//+------------------------------------------------------------------+
 
/*
23 номер
http://www.fortrader.ru/ftgate.php?id=0&num=67

В каждом номере торговая стратегия и советник. Присоединяйтесь!
Приглашаем авторов статей по трейдингу.

История изменений:
0.01 добавлено возможность выбора диапазона работы по в ремени с startdate по stopdate
0.01 добавлена возможность работы с растянутым мартингейлом

Скачать номер журнала и исследованием данного эксперта вы можете по данной ссылке:


 
Original description strategies:
http://www.unfx.ru/strategies_to_trade/strategies_134.php
 
Подробное описание параметров советника доступно в номере журнала от 26 Июля, 
предложения и отзывы мы будем рады видеть в нашей электропочте: letters@fortrader.ru
http://www.fortrader.ru/arhiv.php
 
A detailed description of the parameters adviser available issue of the journal dated Iule 26, 
suggestions and feedback we will be glad to see in our e-mail: letters@fortrader.ru
http://www.fortrader.ru/arhiv.php
 
Looking for an interpreter for the English version of the magazine on partnership.



 
 
*/
#property copyright "FORTRADER.RU"
#property link      "http://FORTRADER.RU"

extern string p1="Настройки паттерна №1(A)";
extern bool p1enable=true;
extern int stoplossbars1 = 22;
extern int takeprofitbars1 = 32;
extern int otstup1 = 40; 
extern int lowema1=13;
extern int fastema1=24;
extern double maxur1=0.0095;
extern double minur1=-0.0045;

extern string p2="Настройки паттерна №2(B)";
extern bool p2enable=true;
extern int stoplossbars2 = 2;
extern int takeprofitbars2 = 2;
extern int otstup2 = 50; 
extern int lowema2=7;
extern int fastema2=17;
extern double maxur2=0.0045;
extern double minur2=-0.0035;

extern string p3="Настройки паттерна №3(C)";
extern bool p3enable=true;
extern int stoplossbars3 = 8;
extern int takeprofitbars3 = 12;
extern int otstup3 = 2; 
extern int lowema3=2;
extern int fastema3=32;

extern double maxur3=0.0015;
extern double maxur13=0.004;
extern double minur3=-0.005;
extern double minur13=-0.0005;


extern string p4="Настройки паттерна №4(D)";
extern bool p4enable=true;
extern int stoplossbars4 = 10;
extern int takeprofitbars4 = 32;
extern int otstup4 = 45; 
extern int lowema4=9;
extern int fastema4=4;

extern int sum_bars_bup4=10;
extern double maxur4=0.0165;
extern double maxur14=0.0001;
extern double minur4=-0.0005;
extern double minur14=-0.0006;


extern string p5="Настройки паттерна №5(I)";
extern bool p5enable=true;
extern int stoplossbars5 = 8;
extern int takeprofitbars5 = 47;
extern int otstup5 = 45; 
extern int lowema5=2;
extern int fastema5=6;

extern double maxu5=0.0005;
extern double maxur5=0.0015;
double maxur15=0.0000;

extern double minu5=-0.0005;
extern double minur5=-0.0030;
double minur15=0.0000;
 
extern string p6="Настройки паттерна №6(F)";
extern bool p6enable=true;
extern int stoplossbars6 = 26;
extern int takeprofitbars6 = 42;
extern int otstup6 = 20; 
extern int lowema6=8;
extern int fastema6=4;

extern double maxur6=0.0005;
extern double minur6=-0.0010;
double maxbars6=5;
double minbars6=5;
double countbars6=4;
 
extern string x="Настройки MA:";
extern  int perema1=7;
extern  int perema2=21;
extern  int persma3=98;
extern  int perema4=365;
 
extern double Lots=0.1;
datetime Bar;
 int maxdrow=24250;
 
extern bool timecontrol=true;
extern string starttime = "07:00:00";
extern string stoptime = "17:00:00";

extern bool slowmartin=true;


 
int buy,sell;int nummodb,nummods;int flaglot,bars_bup;



int start()
  {Comment("FORTRADER.RU");
  
  Lots=slowmartin(slowmartin);
  
  if(timecontrol==true)
  {
  if(timecontrol(starttime,stoptime)==1){return(0);}
  }
  
  
       if(Bar!=iTime(NULL,0,0))
       {
         Bar=iTime(NULL,0,0);
  
       if(p6enable==true){  AOPattern6(countbars6,maxbars6,minbars6,lowema6,fastema6,maxur6,minur6,stoplossbars6,otstup6,takeprofitbars6);}
       if(p5enable==true){  AOPattern5(lowema5,fastema5,maxur5,minur5,stoplossbars5,otstup5,takeprofitbars6);}
       if(p4enable==true){  AOPattern4(lowema4,fastema4,maxur4,minur4,stoplossbars4,otstup4,takeprofitbars4);}
       if(p3enable==true){  AOPattern3(lowema3,fastema3,maxur3,minur3,stoplossbars3,otstup3,takeprofitbars3);}
       if(p2enable==true){  AOPattern2(lowema2,fastema2,maxur2,minur2,stoplossbars2,otstup2,takeprofitbars2);}
       if(p1enable==true){  AOPattern1(lowema1,fastema1,maxur1,minur1,stoplossbars1,otstup1,takeprofitbars1);}
       ActivePosManager(perema1,perema2,persma3,perema4);
      }else{return(0);}
       
 
   return(0);
  }
  
  int timecontrol(string starttime, string stoptime)
{
  if (Time[0]<=StrToTime(starttime) ||  Time[0]>=StrToTime(stoptime)) 
      { 
      return(1);
      }  
      return(0);
}
  
double  slowmartin(bool slowmartin)
  {//для работы фунции при открытии позиции необходимо сбрасывать flaglot=0;

      if(slowmartin==true)
   {
    if(OrdersTotal()<2 && flaglot==0)
     {
        if(true==OrderSelect(OrdersHistoryTotal()-1, SELECT_BY_POS, MODE_HISTORY))
        {
        if(OrderProfit()>1){Lots=0.1;return(Lots);}else{Lots=Lots+0.1;flaglot=1;return(Lots);}
        }
      }
   }
   return(Lots);
 }
  
int stop,sstop,barnumm,barnumms,aopmaxur,aop_oksell,aop_okbuy;


int AOPattern6(int countbars,int maxbars,int minbars,double FastEMA,double SlowEMA,double maxur,double minur,int stoplossbars, int otstup,int takeprofitbars)
{  double sl;
   string comment=maxdrow;
   //загружаем индикаторы
   double macdcurr =iMACD(NULL,0,FastEMA,SlowEMA,1,PRICE_CLOSE,MODE_MAIN,1);
   double macdlast =iMACD(NULL,0,FastEMA,SlowEMA,1,PRICE_CLOSE,MODE_MAIN,2);
   double macdlast3 =iMACD(NULL,0,FastEMA,SlowEMA,1,PRICE_CLOSE,MODE_MAIN,3);

   if(macdcurr<maxur){sstop=0;}
   
   if(macdcurr>maxur && barnumm<=maxbars && sstop==0)
   {
   barnumm=barnumm+1;
   }
   if(barnumm>maxbars)
   {
   barnumm=0;sstop=1;
   }
   if(barnumm<minbars && macdcurr<maxur )
   {
   barnumm=0;
   }
   if(macdcurr<maxur && barnumm>countbars)
   {
    aop_oksell=1;
   }
   if(aop_oksell==1)
   {sl=StopLoss(0,stoplossbars,otstup);if(sl<Bid){sl=sl+10*Point;}
     OrderSend(Symbol(),OP_SELL,Lots,Bid,3,sl,TakeProfit(0,takeprofitbars),"FORTRADER.RU",maxdrow,0,Red);
      aop_oksell=0;barnumm=0;
      nummods=0;
      sstop=0;
      flaglot=0;
   }
   

   
   if(macdcurr>minur){stop=0;}

   if(macdcurr<minur && barnumms<=maxbars && stop==0)
   {
   barnumms=barnumms+1; 
   }
   if(barnumms>maxbars)
   {
   stop=1;barnumms=0;
   }
   if(barnumms<minbars && macdcurr>minur )
   {
   barnumms=0;
   }
   if(macdcurr>minur && barnumms>countbars)
   {
    aop_okbuy=1;
   }
   if(aop_okbuy==1 )
   {sl=StopLoss(1,stoplossbars,otstup);if(sl>Ask){sl=sl-10*Point;}
      OrderSend(Symbol(),OP_BUY,Lots,Ask,3,sl,TakeProfit(1,takeprofitbars),"FORTRADER.RU",maxdrow,0,Red);
      barnumms=0;
      aop_okbuy=0;
      nummodb=0;
      stop=0;
      flaglot=0;
   }
 
}

int stops5, Sb5, aop_oksell5, stopb5, Ss5, aop_okbuy5;
int AOPattern5(double FastEMA,double SlowEMA,double maxur,double minur,int stoplossbars, int otstup,int takeprofitbars)
{  double sl;
   //загружаем индикаторы
   double macdcurr =iMACD(NULL,0,FastEMA,SlowEMA,1,PRICE_CLOSE,MODE_MAIN,1);
   double macdlast =iMACD(NULL,0,FastEMA,SlowEMA,1,PRICE_CLOSE,MODE_MAIN,2);
   double macdlast3 =iMACD(NULL,0,FastEMA,SlowEMA,1,PRICE_CLOSE,MODE_MAIN,3);
 
   if(macdcurr<minu5 &&  stops5==0){stops5=1;}
   if(macdcurr>minur && stops5==1){stops5=0;Sb5=1;}
   if(Sb5==1 && macdcurr<macdlast && macdlast>macdlast3 && macdcurr<minur && macdlast>minur){aop_oksell5=1;Sb5=0;} 
   if(macdcurr>minur15 ){stops5=0;aop_oksell5=0;Sb5=0;}

   if(aop_oksell5==1)
   {sl=StopLoss(0,stoplossbars,otstup);if(sl<Bid){sl=sl+10*Point;}
     OrderSend(Symbol(),OP_SELL,Lots,Bid,3,sl,TakeProfit(0,takeprofitbars),"FORTRADER.RU",maxdrow,0,Red);
      aop_oksell5=0;
      nummods=0;
      stops5=0;
      Sb5=0;
      flaglot=0;
   }

   if(macdcurr>maxu5 &&  stopb5==0){stopb5=1;}
   if(macdcurr<maxur15 ){stopb5=0;aop_okbuy5=0;Ss5=0;}
   if(macdcurr<maxur && stopb5==1){stopb5=0;Ss5=1;}
   if(Ss5==1 && macdcurr>macdlast && macdlast<macdlast3 && macdcurr>maxur && macdlast<maxur){aop_okbuy5=1;Ss5=0;} 

   if(aop_okbuy5==1 )
   {sl=StopLoss(1,stoplossbars,otstup);if(sl>Ask){sl=sl-10*Point;}
      OrderSend(Symbol(),OP_BUY,Lots,Ask,3,sl,TakeProfit(1,takeprofitbars),"FORTRADER.RU",maxdrow,0,Red);
      aop_okbuy5=0;  
      nummodb=0;
      Ss5=0;
      flaglot=0;
   }
}

int aop_oksell4,aop_okbuy4,stops4,sstop4;
double max14,min14;
int AOPattern4(double FastEMA,double SlowEMA,double maxur4,double minur4,int stoplossbars, int otstup,int takeprofitbars)
{  double sl;
   //загружаем индикаторы
   double macdcurr =iMACD(NULL,0,FastEMA,SlowEMA,1,PRICE_CLOSE,MODE_MAIN,1);
   double macdlast =iMACD(NULL,0,FastEMA,SlowEMA,1,PRICE_CLOSE,MODE_MAIN,2);
   double macdlast3 =iMACD(NULL,0,FastEMA,SlowEMA,1,PRICE_CLOSE,MODE_MAIN,3);
   
   
   if(macdcurr>maxur4 && macdcurr<macdlast && macdlast>macdlast3 &&  stops4==0){max14=macdlast;stops4=1;}
   if(macdcurr<maxur4 ){stops4=0;max14=0;}
   if(stops4==1 && macdcurr>maxur4&& macdcurr<macdlast && macdlast>macdlast3 && macdlast<max14 ){aop_oksell4=1;} 
   if(macdcurr<maxur4 ){aop_oksell4=0;}
 
   if(aop_oksell4==1)
   {sl=StopLoss(0,stoplossbars,otstup);if(sl<Bid){sl=sl+10*Point;}
      max14=0;
     OrderSend(Symbol(),OP_SELL,Lots,Bid,3,sl,TakeProfit(0,takeprofitbars),"FORTRADER.RU",maxdrow,0,Red);
      aop_oksell4=0;
      nummods=0;
      flaglot=0;
   }
   
   if(macdcurr<minur4 && macdcurr>macdlast && macdlast<macdlast3 &&  sstop4==0){min14=macdlast;sstop4=1;}
   if(macdcurr>minur4 ){sstop4=0;min14=0;}
   if(sstop4==1 && macdcurr<minur4&& macdcurr>macdlast && macdlast<macdlast3 && macdlast>min14 ){aop_okbuy4=1;} 
   if(macdcurr>maxur4 ){aop_okbuy4=0;}
   
   if(aop_okbuy4==1 )
   {sl=StopLoss(1,stoplossbars,otstup);if(sl>Ask){sl=sl-10*Point;}
      OrderSend(Symbol(),OP_BUY,Lots,Ask,3,sl,TakeProfit(1,takeprofitbars),"FORTRADER.RU",maxdrow,0,Red);
      aop_okbuy4=0;
      nummodb=0;
      sstop4=0;
      min14=0;
      flaglot=0;
   }
 
}

int aop_oksell3,aop_okbuy3,S3,bS3,stops3,stops13,sstops3,sstops13;
double max13,max23,max33,min13,min23,min33;
int AOPattern3(double FastEMA,double SlowEMA,double maxur3,double minur3,int stoplossbars, int otstup,int takeprofitbars)
{double sl;
   //загружаем индикаторы
   double macdcurr =iMACD(NULL,0,FastEMA,SlowEMA,1,PRICE_CLOSE,MODE_MAIN,1);
   double macdlast =iMACD(NULL,0,FastEMA,SlowEMA,1,PRICE_CLOSE,MODE_MAIN,2);
   double macdlast3 =iMACD(NULL,0,FastEMA,SlowEMA,1,PRICE_CLOSE,MODE_MAIN,3);
   
   if(macdcurr>maxur13){S3=1;bars_bup=bars_bup+1;}
    
   if(S3==1 && macdcurr<macdlast && macdlast>macdlast3 && macdlast>max13 && stops3==0){max13=macdlast;}
   
   if(max13>0 &&macdcurr<maxur3){stops3=1;}
   
   if(macdcurr<maxur13 ){stops3=0;max13=0;S3=0;}
   

    if(stops3==1 && macdcurr>maxur3&& macdcurr<macdlast && macdlast>macdlast3 && macdlast>max13 && macdlast>max23 && stops13==0){max23=macdlast;} 
    
    if(max23>0 &&macdcurr<maxur3){stops13=1;}
   
   if(macdcurr<maxur13 ){stops13=0;max23=0;}
   
   if(stops13==1 && macdcurr<maxur3&& macdlast<maxur3&& macdlast3<maxur3&&  macdcurr<macdlast && macdlast>macdlast3 && macdlast<max23 && aop_oksell3==0){max33=macdlast;aop_oksell3=1;} 

   if(macdcurr<maxur13 ){aop_oksell3=0;}

   
   if(aop_oksell3==1 )
   {max13=0;max23=0;max33=0;
   sl=StopLoss(0,stoplossbars,otstup);if(sl<Bid){sl=sl+10*Point;}
     OrderSend(Symbol(),OP_SELL,Lots,Bid,3,sl,TakeProfit(0,takeprofitbars),"FORTRADER.RU",maxdrow,0,Red);
      aop_oksell3=0;
      nummods=0;
      bars_bup=0;
      flaglot=0;
   }
   
   
   if(macdcurr<minur3){bS3=1;}
    
   if(bS3==1 && macdcurr>macdlast && macdlast<macdlast3 && macdlast<min13 && sstops3==0){min13=macdlast;}
   
   if(min13<0 &&macdcurr>minur3){sstops3=1;bS3=0;}
   
   if(macdcurr>minur13 ){sstops3=0;min13=0;bS3=0;}
   
    if(sstops3==1 && macdcurr<maxur3&& macdcurr>macdlast && macdlast<macdlast3 && macdlast<min13 && macdlast<min23 && sstops13==0){min23=macdlast;} 
    
    if(min23<0 &&macdcurr>minur3){sstops13=1;sstops3=0;}
   
   if(macdcurr>minur13 ){sstops13=0;min23=0;}
   
   if(sstops13==1 && macdcurr>minur3&& macdlast>minur3&& macdlast3>minur3&&  macdcurr>macdlast && macdlast<macdlast3 && macdlast>min23 && aop_okbuy3==0){min33=macdlast;aop_okbuy3=1;sstops13=0;} 

   if(macdcurr>maxur13 ){aop_okbuy3=0;}   
   
   if(aop_okbuy3==1 )
   {
      sl=StopLoss(1,stoplossbars,otstup);if(sl>Ask){sl=sl-10*Point;}
      OrderSend(Symbol(),OP_BUY,Lots,Ask,3,sl,TakeProfit(1,takeprofitbars),"FORTRADER.RU",maxdrow,0,Red);
      aop_okbuy3=0;
      nummodb=0;
      sstops13=0;
      min13=0;
      min23=0;
      min33=0;
      flaglot=0;
   }
 
}
  int aop_maxur2,aop_minur2,aop_oksell2,aop_okbuy2;int value_min2,value_max2,value_curr2;
int AOPattern2(double FastEMA,double SlowEMA,double maxur2,double minur2,int stoplossbars, int otstup,int takeprofitbars)
{double sl;
   //загружаем индикаторы
   double macdcurr =iMACD(NULL,0,FastEMA,SlowEMA,1,PRICE_CLOSE,MODE_MAIN,1);
   double macdlast =iMACD(NULL,0,FastEMA,SlowEMA,1,PRICE_CLOSE,MODE_MAIN,2);
   double macdlast3 =iMACD(NULL,0,FastEMA,SlowEMA,1,PRICE_CLOSE,MODE_MAIN,3);

 
   if(macdcurr>0){aop_maxur2=1;aop_oksell2=0;}

   if(macdcurr>macdlast && macdlast<macdlast3 && aop_maxur2==1  && macdcurr>minur2 && macdcurr<0 && aop_oksell2==0)
   { 
   aop_oksell2=1; value_min2= MathAbs(macdlast*10000);
   }
value_curr2=MathAbs(macdcurr*10000);

 if(aop_oksell2==1  && macdcurr<macdlast && macdlast>macdlast3 && macdcurr<0 && value_min2<=value_curr2)
   {
   aop_maxur2=0;
   }

   if(aop_oksell2==1  && macdcurr<macdlast && macdlast>macdlast3 && macdcurr<0 )
   {
  
        sl=StopLoss(0,stoplossbars,otstup);if(sl<Bid){sl=sl+10*Point;}
     OrderSend(Symbol(),OP_SELL,Lots,Bid,3,sl,TakeProfit(0,takeprofitbars),"FORTRADER.RU",maxdrow,0,Red);
 
      aop_oksell2=0;
      aop_maxur2=0;
      nummods=0;
      flaglot=0;
   }
   
   if(macdcurr<0){aop_minur2=1;aop_okbuy2=0;}
   if(macdcurr<maxur2  && macdcurr<macdlast && macdlast>macdlast3 && aop_minur2==1  && macdcurr>0)
   { 
   aop_okbuy2=1; value_max2= MathAbs(macdlast*10000);
  
   }
   value_curr2=MathAbs(macdcurr*10000);
   
   if(aop_okbuy2==1  && macdcurr>macdlast && macdlast<macdlast3 && macdcurr>0 && value_max2<=value_curr2)
   {
   aop_minur2=0;
   }
   
   if(aop_okbuy2==1  && macdcurr>macdlast && macdlast<macdlast3 && macdcurr>0 )
   {
      sl=StopLoss(1,stoplossbars,otstup);if(sl>Ask){sl=sl-10*Point;}
      OrderSend(Symbol(),OP_BUY,Lots,Ask,3,sl,TakeProfit(1,takeprofitbars),"FORTRADER.RU",maxdrow,0,Red);
      aop_okbuy2=0;
      aop_minur2=0;
      nummodb=0;
      flaglot=0;
   }

}

int aop_maxur1,aop_minur1,aop_oksell1,aop_okbuy1;
int AOPattern1(double FastEMA,double SlowEMA,double maxur1,double minur1,int stoplossbars, int otstup,int takeprofitbars)
{double sl;
   //загружаем индикаторы
   double macdcurr =iMACD(NULL,0,FastEMA,SlowEMA,1,PRICE_CLOSE,MODE_MAIN,1);
   double macdlast =iMACD(NULL,0,FastEMA,SlowEMA,1,PRICE_CLOSE,MODE_MAIN,2);
   double macdlast3 =iMACD(NULL,0,FastEMA,SlowEMA,1,PRICE_CLOSE,MODE_MAIN,3);
   
   if(macdcurr>maxur1){aop_maxur1=1;}
   if(macdcurr<0){aop_maxur1=0;}
   if(macdcurr<maxur1 && macdcurr<macdlast && macdlast>macdlast3 && aop_maxur1==1 && macdcurr>0 && macdlast3<maxur1)
   { 
   aop_oksell1=1;
   }
   if(aop_oksell1==1 )
   {
   sl=StopLoss(0,stoplossbars,otstup);if(sl<Bid){sl=sl+10*Point;}
     OrderSend(Symbol(),OP_SELL,Lots,Bid,3,sl,TakeProfit(0,takeprofitbars),"FORTRADER.RU",maxdrow,0,Red);
      aop_oksell1=0;
      aop_maxur1=0;
      nummods=0;
      flaglot=0;
   }
   
   if(macdcurr<minur1){aop_minur1=1;}
   if(macdcurr>0){aop_minur1=0;}
   if(macdcurr>minur1 && macdcurr<0 && macdcurr>macdlast && macdlast<macdlast3 && aop_minur1==1 && macdlast3>minur1 )
   { 
   aop_okbuy1=1;
   }
   if(aop_okbuy1==1 )
   {
      sl=StopLoss(1,stoplossbars,otstup);if(sl>Ask){sl=sl-10*Point;}
      OrderSend(Symbol(),OP_BUY,Lots,Ask,3,sl,TakeProfit(1,takeprofitbars),"FORTRADER.RU",maxdrow,0,Red);
      aop_okbuy1=0;
      aop_minur1=0;
      nummodb=0;
      flaglot=0;
   }
 
}

double StopLoss(int type,int stoplossbars,int otstup)
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
 
double TakeProfit(int type,int takeprofitbars)
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
{string comment=maxdrow;int i;double lt;
   double ema1 =iMA(NULL,0,perema1,0,MODE_EMA,PRICE_CLOSE,1);
    double ema2 =iMA(NULL,0,perema2,0,MODE_EMA,PRICE_CLOSE,1);
     double sma1 =iMA(NULL,0,persma3,0,MODE_SMA,PRICE_CLOSE,1);
      double ema3 =iMA(NULL,0,perema4,0,MODE_EMA,PRICE_CLOSE,1);
 
   for( i=1; i<=OrdersTotal(); i++)          
     {
      if (OrderSelect(i-1,SELECT_BY_POS)==true) 
        {                                       
              if(OrderType()==OP_BUY && OrderProfit()>5 && Close[1]>ema2 && nummodb==0&& OrderSymbol()==Symbol()&&OrderMagicNumber()==maxdrow)
              {  lt=NormalizeDouble(OrderLots()/3,2);if(lt<=0.01){lt=0.01;}
                 OrderClose(OrderTicket(),lt,Bid,3,Violet); 
                 nummodb++;
               
              }
         }
             
         if (OrderSelect(i-1,SELECT_BY_POS)==true) 
            {  
                 if(OrderType()==OP_BUY && OrderProfit()>5 && High[1]>(sma1+ema3)/2 && nummodb==1 &&OrderSymbol()==Symbol()&&OrderMagicNumber()==maxdrow)
              { lt=NormalizeDouble(OrderLots()/2,2);if(lt<=0.01){lt=0.01;}
                 OrderClose(OrderTicket(),lt,Bid,3,Violet); 
                 nummodb++;
                   
              }
             }
             
            if (OrderSelect(i-1,SELECT_BY_POS)==true) 
            { 
              
             if(OrderType()==OP_SELL && OrderProfit()>5 && Close[1]<ema2 && nummods==0 && OrderSymbol()==Symbol()&&OrderMagicNumber()==maxdrow)
              {lt=NormalizeDouble(OrderLots()/3,2);if(lt<=0.01){lt=0.01;}
                 OrderClose(OrderTicket(),lt,Ask,3,Violet); 
                 nummods++;
                
                 
              }
            }
             
                   if (OrderSelect(i-1,SELECT_BY_POS)==true) 
        { 
                   if(OrderType()==OP_SELL && OrderProfit()>5 && Low[1]<(sma1+ema3)/2 && nummods==1&&   OrderSymbol()==Symbol()&&OrderMagicNumber()==maxdrow)
              {  lt=NormalizeDouble(OrderLots()/2,2);if(lt<=0.01){lt=0.01;}
                 OrderClose(OrderTicket(),lt,Ask,3,Violet); 
                 nummods++;
                  
              }
      
      }
 }
 

 
 
}

