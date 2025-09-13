//+------------------------------------------------------------------+
//|                                                         OzFx.mq4 |
//|                                                     FORTRADER.RU |
//|                                          http://www.FORTRADER.RU |
//+------------------------------------------------------------------+
#property copyright "FORTRADER.RU"
#property link      "http://www.FORTRADER.RU"


double AC,Stoh,ACPrev;
extern int stoploss=100;
extern int point=50;
extern int sto=5;

int stopb,bar,stops,modok;
int comment;
 string com;
int start()
  {
  
  AC=iAC(NULL, 0, 1);
  ACPrev=iAC(NULL, 0, 2);
  Stoh=iStochastic(NULL,0,sto,3,3,MODE_SMA,0,MODE_MAIN,1);
    if(OrdersTotal()<1){stopb=0;  stops=0;}
  
  
  if(Stoh>50 && AC>ACPrev && AC>0 && ACPrev<0 && stopb==0 && bar!=Bars)
  {

  comment=comment+1;
   com="aa"+comment;

  OrderSend(Symbol(),OP_BUY,0.1,Ask,3,Ask-stoploss*Point,Ask+point*Point,com,0,0,White);
  OrderSend(Symbol(),OP_BUY,0.1,Ask,3,Ask-stoploss*Point,Ask+(point*2)*Point,"",0,0,White);
  OrderSend(Symbol(),OP_BUY,0.1,Ask,3,Ask-stoploss*Point,Ask+(point*3)*Point,"",0,0,White);
  OrderSend(Symbol(),OP_BUY,0.1,Ask,3,Ask-stoploss*Point,Ask+(point*4)*Point,"",0,0,White);
  OrderSend(Symbol(),OP_BUY,0.1,Ask,3,Ask-stoploss*Point,0,"s",0,0,Green);
  stopb=1;
  bar=Bars;
  }
  
  
  if( Stoh<50 && AC<ACPrev && AC<0 && ACPrev>0 && stops==0 && bar!=Bars )
  {  comment=comment+1;
   com="aa"+comment;
  OrderSend(Symbol(),OP_SELL,0.1,Bid,3,Bid+stoploss*Point,Bid-point*Point,com,0,0,Green);
  OrderSend(Symbol(),OP_SELL,0.1,Bid,3,Bid+stoploss*Point,Bid-(point*2)*Point,"",0,45645,Green);
  OrderSend(Symbol(),OP_SELL,0.1,Bid,3,Bid+stoploss*Point,Bid-(point*3)*Point,"",0,45645,Green);
  OrderSend(Symbol(),OP_SELL,0.1,Bid,3,Bid+stoploss*Point,Bid-(point*4)*Point,"",0,456456,Green);
  OrderSend(Symbol(),OP_SELL,0.1,Bid,3,Bid+stoploss*Point,0,"s",0,56456,Green);
  stops=1;
  bar=Bars;
  }
  
 modok=0;
       int i,accTotal=OrdersHistoryTotal();
       for(i=0;i<accTotal;i++)
       { 
         OrderSelect(i, SELECT_BY_POS, MODE_HISTORY);
         string ca=com+"[tp]";
         if(OrderProfit()>10 && OrderComment()==ca){modok=1;}
         
       }
          
  
   for(int cnt=0;cnt<OrdersTotal();cnt++)
     {
      OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES);
        {
         if(OrderType()==OP_BUY && modok==1)  
          {
          OrderModify(OrderTicket(),OrderOpenPrice(),OrderOpenPrice(),OrderTakeProfit(),0,Blue);
         // OrderClose(OrderTicket(),OrderLots(),Bid,10,Violet); 
          // return(0); 
          }
          if(OrderType()==OP_SELL && modok==1)  
          {
          OrderModify(OrderTicket(),OrderOpenPrice(),OrderOpenPrice(),OrderTakeProfit(),0,Blue);
         // OrderClose(OrderTicket(),OrderLots(),Bid,10,Violet); 
          // return(0); 
          }
          if(OrderType()==OP_BUY && Stoh<50 && AC<ACPrev && AC<0 && ACPrev>0 )  
          {
          OrderClose(OrderTicket(),OrderLots(),Bid,10,Violet); 
          }
           if(OrderType()==OP_SELL  && Stoh>50 && AC>ACPrev && AC>0 && ACPrev<0 )   
          {
           OrderClose(OrderTicket(),OrderLots(),Ask,10,Violet); 
           stopb=0;
          }
         }
       }
      
   return(0);
  }

