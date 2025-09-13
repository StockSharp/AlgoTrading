#property copyright "FORTRADER.RU"
#property link      "http://FORTRADER.RU"

/*

Подробное описание параметров советника доступно в номере журнала от 2 Июня, 
предложения и отзывы мы будем рады видеть в нашей электропочте: letters@fortrader.ru
http://www.fortrader.ru/arhiv.php

A detailed description of the parameters adviser available issue of the journal dated Iune 2, 
suggestions and feedback we will be glad to see in our e-mail: letters@fortrader.ru
http://www.fortrader.ru/arhiv.php

Looking for an interpreter for the English version of the magazine on partnership.

*/




extern string x="Настройки BB:";
extern int BB1 = 20;
extern int BB2 = 20;
extern int BB3 = 20;
extern string x1="Настройки RSI:";
extern int RSI = 14;
extern int predel = 6;
extern int otstup = 10; 

extern double Lots=1;

datetime Bar,sBar;int buy,sell,i,a,b,nbb3ok,vbb3ok,nbar;double Sstoploss,stoploss,setup2,rsi,okbuy,oksell;

int start()
  {
  if (Volume[0]>1){return(0); }

     buy=0;sell=0;
     for(  i=0;i<OrdersTotal();i++)
         {
           OrderSelect(i, SELECT_BY_POS, MODE_TRADES);
           if(OrderType()==OP_BUY){buy=1;}
           if(OrderType()==OP_SELL){sell=1;}
         }   
   
   double vbb1 =iBands(NULL,0,BB1,1,0,PRICE_CLOSE,MODE_UPPER,1);
   double vbb2 =iBands(NULL,0,BB2,2,0,PRICE_CLOSE,MODE_UPPER,1);
   double vbb3 =iBands(NULL,0,BB3,3,0,PRICE_CLOSE,MODE_UPPER,1);
   double nbb1 =iBands(NULL,0,BB1,1,0,PRICE_CLOSE,MODE_LOWER,1);
   double nbb2 =iBands(NULL,0,BB2,2,0,PRICE_CLOSE,MODE_LOWER,1);
   double nbb3 =iBands(NULL,0,BB3,3,0,PRICE_CLOSE,MODE_LOWER,1);
  
  
  if(Low[1]<=nbb3 )
  { okbuy=0;  Bar=Time[1];
  
      for( i=predel;i>0;i--)
      {
      double rsi=iRSI(NULL,0,RSI,PRICE_CLOSE,i);
      if(rsi<30){nbb3ok=1;}
      }
  }
  
 
  if(okbuy==0 && nbb3ok==1 && Close[1]>nbb2 && buy==0)
  {okbuy=1;nbb3ok=0;a=0;
   stoploss=Low[iLowest(NULL,0,MODE_LOW,iBarShift(NULL,0,Bar,FALSE),1)]-otstup*Point;
   OrderSend(Symbol(),OP_BUY,Lots,Ask,3,stoploss,0,0,16385,0,Green);
  
  }
  
  for(i=0;i<OrdersTotal();i++)
  {
   OrderSelect(i, SELECT_BY_POS, MODE_TRADES);
   if(OrderType()==OP_BUY )
   {  
    double setup2=OrderOpenPrice()+((OrderOpenPrice()-OrderStopLoss()));
    
          if(High[1]>vbb2)
       {
        OrderClose(OrderTicket(),OrderLots(),Bid,3,Violet);
       } 
       
      if(High[1]>setup2 && a==0)
      { 
         OrderModify(OrderTicket(),OrderOpenPrice(),OrderOpenPrice(),OrderTakeProfit(),0,White);
         OrderClose(OrderTicket(),OrderLots()/2,Bid,3,Violet); 
         a=1;
       }
 
       
    }      
  }
  
  /**********************************************sell**********************************************************/
  
    if(High[1]>=vbb3 )
  { oksell=0;  sBar=Time[1];
  
      for( i=predel;i>0;i--)
      {
      rsi=iRSI(NULL,0,RSI,PRICE_CLOSE,i);
      if(rsi>70){vbb3ok=1;}
      }
  }
 
  if(oksell==0 && vbb3ok==1 && Close[1]<vbb2 && sell==0)
  {oksell=1;vbb3ok=0;b=0;
   Sstoploss=High[iHighest(NULL,0,MODE_HIGH,iBarShift(NULL,0,sBar,FALSE),1)]+otstup*Point;
   OrderSend(Symbol(),OP_SELL,Lots,Bid,3,Sstoploss,0,0,16385,0,White);
  }
  
  for(i=0;i<OrdersTotal();i++)
  {
   OrderSelect(i, SELECT_BY_POS, MODE_TRADES);
   if(OrderType()==OP_SELL )
   {  
      setup2=OrderOpenPrice()-((OrderStopLoss()-OrderOpenPrice()));
      
       if(Low[1]<nbb2)
    {
     OrderClose(OrderTicket(),OrderLots(),Ask,3,Violet);
    } 
     
      if(Low[1]<setup2 && b==0)
      {
       OrderModify(OrderTicket(),OrderOpenPrice(),OrderOpenPrice(),OrderTakeProfit(),0,White);
       OrderClose(OrderTicket(),OrderLots()/2,Ask,3,Violet); 
       b=1;
      }      
      
   
       
    }      
  }
  
  
   

   return(0);
  }

