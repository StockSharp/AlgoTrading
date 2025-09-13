#property copyright "FORTRADER.RU"
#property link      "http://FORTRADER.RU"

/*

Подробное описание параметров советника доступно в номере журнала от 26 Мая 2008, 
предложения и отзывы мы будем рады видеть в нашей электропочте: letters@fortrader.ru
http://www.fortrader.ru/arhiv.php

A detailed description of the parameters adviser available issue of the journal dated May 26 2008, 
suggestions and feedback we will be glad to see in our e-mail: letters@fortrader.ru
http://www.fortrader.ru/arhiv.php

*/




extern string x="Настройки MACD:";
extern int FastEMA = 12;
extern int SlowEMA = 24;
 int SignalEMA = 9;
extern int predel = 6;
extern string x1="Настройки MA:";
extern int SMA1 = 50;
extern int SMA2 = 100;
extern int otstup = 10; 
extern string x2="Значение баров для расчета Стоп-Лосс :";
extern int stoplossbars = 6;
extern string x3="Коэффициент для расчета профит уровня и закрытия половины позиций:";
extern int pprofitum = 2;
extern string x4="фильтр по ADX:";
extern int enable = 0;
extern int periodADX = 14;

extern double Lots=1;

datetime Bar;int buy,sell,i,a,b;double stoploss,setup2,adx,okbuy,oksell;

int start()
  {

     buy=0;sell=0;
     for(  i=0;i<OrdersTotal();i++)
         {
           OrderSelect(i, SELECT_BY_POS, MODE_TRADES);
           if(OrderType()==OP_BUY){buy=1;}
           if(OrderType()==OP_SELL){sell=1;}
         }   
   
   //загружаем индикаторы
   double macd =iMACD(NULL,0,FastEMA,SlowEMA,SignalEMA,PRICE_CLOSE,MODE_MAIN,1);
   double sma1 =iMA(NULL,0,SMA1,0,MODE_SMA,PRICE_CLOSE,1);
   double sma2 =iMA(NULL,0,SMA2,0,MODE_SMA,PRICE_CLOSE,1);
   
   if(Close[1]<sma2){okbuy=1;}
    if(Close[1]>sma2){oksell=1;}
    
   if(enable==1)
   {
   adx=iADX(NULL,0,14,PRICE_CLOSE,MODE_MAIN,0);
   }else{adx=60;}
   
   

  
  if(Close[1]+otstup*Point>sma1 && Close[1]+otstup*Point>sma2 && macd>0 && buy==0)
  {
  
      buy=0;
      for( i=predel;i>0;i--)
      {
      macd=iMACD(NULL,0,FastEMA,SlowEMA,SignalEMA,PRICE_CLOSE,MODE_MAIN,i);
      if(macd<0){buy=2;}
      }
   
      if(buy==2 && adx>50 && okbuy==1)
      {okbuy=0;
          double stoploss=Low[iLowest(NULL,0,MODE_LOW,stoplossbars,1)];
          OrderSend(Symbol(),OP_BUY,Lots,Ask,3,stoploss,0,0,16385,0,Green);
          a=0;
       }
   }
   
   if(Close[1]-otstup*Point<sma1 && Close[1]-otstup*Point<sma2 && macd<0 && sell==0)
  {
  
      sell=0;
      for( i=predel;i>0;i--)
      {
      macd=iMACD(NULL,0,FastEMA,SlowEMA,SignalEMA,PRICE_CLOSE,MODE_MAIN,i);
      if(macd>0){sell=2;}
      }
   
      if(sell==2 && adx>50 && oksell==1)
      {oksell=0;
        
           stoploss=High[iHighest(NULL,0,MODE_HIGH,stoplossbars,1)];
          OrderSend(Symbol(),OP_SELL,Lots,Bid,3,stoploss,0,0,16385,0,White);
          b=0;
       }
   }
   
   
   if(buy==2 || buy==1)
   {
    for( i=0;i<OrdersTotal();i++)
         {
           OrderSelect(i, SELECT_BY_POS, MODE_TRADES);
          
           
           if(OrderType()==OP_BUY )
           {  
           double setup2=OrderOpenPrice()+((OrderOpenPrice()-OrderStopLoss())*pprofitum);

            if(Close[1]>setup2 && a==0)
            {
             OrderModify(OrderTicket(),OrderOpenPrice(),OrderOpenPrice(),OrderTakeProfit(),0,White);
              OrderClose(OrderTicket(),OrderLots()/2,Bid,3,Violet); 
             
              a=1;
            }
            
            if(a==1 && sma1> Close[1]-otstup*Point)
            {
            OrderClose(OrderTicket(),OrderLots(),Bid,3,Violet); 
            }
            
           
           }
      }  
        
  }    
  
           if(sell==2 || sell==1)
   {
    for( i=0;i<OrdersTotal();i++)
         {
           OrderSelect(i, SELECT_BY_POS, MODE_TRADES);
           
           
           if(OrderType()==OP_SELL )
           {  
            setup2=OrderOpenPrice()-((OrderStopLoss()-OrderOpenPrice())*pprofitum);

            if(Close[1]<setup2 && b==0)
            {
             OrderModify(OrderTicket(),OrderOpenPrice(),OrderOpenPrice(),OrderTakeProfit(),0,White);
              OrderClose(OrderTicket(),OrderLots()/2,Ask,3,Violet); 
              b=1;
            }
            
            if(b==1 && Close[1]-otstup*Point> sma1)
            {
            OrderClose(OrderTicket(),OrderLots(),Ask,3,Violet); 
            
            }
            
           
           }
      } 
      }
  
    
   
   
   

   return(0);
  }

