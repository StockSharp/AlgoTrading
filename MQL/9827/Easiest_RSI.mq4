//+------------------------------------------------------------------+
//| Easiest RSI          Author: LordoftheMoney                      |
//| Important!                                                       |
//| At a five digit broker one more zero must be added to the        |
//|    numbers below (ts, sl, dif)!                                  |
//|                                                                  |
//+------------------------------------------------------------------+
 
extern double lot = 1;
extern double ts = 50; //for EURUSD M5; For EURUSD H1 ts = 150
extern double sl = 50; //for EURUSD M5; For EURUSD H1 sl = 150
extern double dif = 20;

 
int start()
  { 
     double r1 = iRSI(NULL,0,14,PRICE_CLOSE,1);
     double r2 = iRSI(NULL,0,14,PRICE_CLOSE,2);
     
     {
     if ((r2<30 && r1>30 && OrdersTotal()<1) || (OrderType()==OP_BUY && OrdersTotal()>=1 &&
        OrdersTotal()<3 && Bid>OrderOpenPrice()+dif*Point))
     OrderSend(Symbol(),OP_BUY,lot,Ask,0,Ask-sl*Point,0,"Easiest ever",0,0);
 
     if ((r2>70 && r1<70 && OrdersTotal()<1) || (OrderType()==OP_SELL && OrdersTotal()>=1 &&
       OrdersTotal()<3 && Ask<OrderOpenPrice()-dif*Point))
     OrderSend(Symbol(),OP_SELL,lot,Bid,0,Bid+sl*Point,0,"Easiest ever",0,0);
       {
          for (int i=0; i<OrdersTotal(); i++)
         {                                               
            if (OrderSelect(i,SELECT_BY_POS,MODE_TIME)==true)
             if (OrderType()==OP_BUY && Bid-ts*Point>OrderStopLoss()+5*Point)
             OrderModify(OrderTicket(),OrderOpenPrice(),Bid-ts*Point,0,0,CLR_NONE);
        
            if (OrderType()==OP_SELL && Ask+ts*Point<OrderStopLoss()-5*Point)
             OrderModify(OrderTicket(),OrderOpenPrice(),Ask+ts*Point,0,0,CLR_NONE);
         }   
       }
     }
   return(0);
  }
//+------------------------------------------------------------------+