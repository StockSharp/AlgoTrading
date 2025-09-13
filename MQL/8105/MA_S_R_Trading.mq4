//+------------------------------------------------------------------+
//|                                               MA.S.R_Trading.mq4 |
//|                                                     FORTRADER.RU |
//|                                          http://www.fortrader.ru |
//+------------------------------------------------------------------+
#property copyright "FORTRADER.RU"
#property link      "http://www.fortrader.ru"

double maximum[100000];
double minimum[100000];
int l,m,flopen,b,s,total,cnt,flopens;
extern int perma=5;


int start()
  {
    total=OrdersTotal();
       b=0;s=0;
      for(cnt=0;cnt<total;cnt++)
         {
           OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES);
            if(OrderType()==OP_BUY)
            { b=1;}
          if(OrderType()==OP_SELL)
            { s=1;}
          }  
  /*--------------------------------------------------*/        
          
  
      double ma1=iMA(NULL,0,perma,0,MODE_SMA,PRICE_CLOSE,1);
      double ma2=iMA(NULL,0,perma,0,MODE_SMA,PRICE_CLOSE,2);
      double ma3=iMA(NULL,0,perma,0,MODE_SMA,PRICE_CLOSE,3);
      
      if(ma1<ma2 && ma2>ma3)
      {
      maximum[m]=High[iHighest(NULL,0,MODE_HIGH,10,1)];
      m++;  
      }
      
      if(ma1>ma2 && ma2<ma3)
      {
      minimum[l]=Low[iLowest(NULL,0,MODE_LOW,10,1)];
      l++;  
      }
      
      if(ma1<ma2 && ma2>ma3 && s==0)
      {
      OrderSend(Symbol(),OP_SELL,0.1,Bid,3,0,0,"",Green);
      }
      
       if(ma1>ma2 && ma2<ma3 && b==0)
      {
      OrderSend(Symbol(),OP_BUY,0.1,Bid,3,0,0,"",Red);
      }

      /*------------------------------------------*/
      
         for(int cnt=0;cnt<OrdersTotal();cnt++)
        {
          OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES);
           if(OrderType()==OP_SELL)
           {
              if(maximum[m-1]>Close[1])
              {
              OrderModify(OrderTicket(),OrderOpenPrice(),maximum[m-1],0,0,Yellow);
              }
              else{m--;}
           }
              
           if(OrderType()==OP_BUY)
           {
              if(minimum[l-1]<Close[1])
              {
              OrderModify(OrderTicket(),OrderOpenPrice(),minimum[l-1],0,0,Yellow);
              }else{l--;} 
            }     
       }
        
   return(0);
}