//+------------------------------------------------------------------+
//| Easiest ever - daytrade robot                                    |
//+------------------------------------------------------------------+

extern double lot = 1;
extern double marketclosehour = 20; // on friday

int start()
  {
     double c1 = iClose(NULL,1440,1);
     double o1 = iOpen(NULL,1440,1);
     {
     if (c1>o1 && OrdersTotal()<1 && Hour()<1)
     OrderSend(Symbol(),OP_BUY,lot,Ask,0,0,0,"Easiest ever",0,0);

     if (c1<o1 && OrdersTotal()<1 && Hour()<1)
     OrderSend(Symbol(),OP_SELL,lot,Bid,0,0,0,"Easiest ever",0,0);
       {
          for (int i=0; i<OrdersTotal(); i++)
         {                                               
            if (OrderSelect(i,SELECT_BY_POS,MODE_TIME)==true)
             if (OrderType()==OP_BUY && Hour()>(marketclosehour-1))
             OrderClose(OrderTicket(),OrderLots(),Bid,0,CLR_NONE);
        
            if (OrderType()==OP_SELL && Hour()>(marketclosehour-1))
            OrderClose(OrderTicket(),OrderLots(),Ask,0,CLR_NONE);
         }   
       }
     }
   return(0);
  }
//+------------------------------------------------------------------+