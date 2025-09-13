//+------------------------------------------------------------------+
//| Huge Income                                                      | 
//| Modified by Christopher      http://expert-profit.webs.com       |
//+------------------------------------------------------------------+

extern double lot = 0.1;
extern double marketclosehour = 23; // on friday

int start()
  {
     double h1 = iHigh(NULL,1440,0);
     double l1 = iLow(NULL,1440,0); 
     double o1 = iOpen(NULL,1440,0);
     {
     if ((Bid>o1 && (o1-l1)>15*Point) && OrdersTotal()<1 && Hour()<22)
     OrderSend(Symbol(),OP_BUY,lot,Ask,0,0,0,"Easiest ever",0,0);

     if ((Ask<o1 && (h1-o1)>15*Point) && OrdersTotal()<1 && Hour()<16)
     OrderSend(Symbol(),OP_SELL,lot,Bid,0,0,0,"Easiest ever",0,0);
       {
          for (int i=0; i<OrdersTotal(); i++)
         {                                               
            if (OrderSelect(i,SELECT_BY_POS,MODE_TIME)==true)
             if (OrderType()==OP_BUY && Hour()>marketclosehour-1)
             OrderClose(OrderTicket(),OrderLots(),Bid,0,CLR_NONE);
        
            if (OrderType()==OP_SELL && Hour()>marketclosehour-1)
            OrderClose(OrderTicket(),OrderLots(),Ask,0,CLR_NONE);
         }   
       }
     }
   return(0);
  }
//+------------------------------------------------------------------+