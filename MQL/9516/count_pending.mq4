//+------------------------------------------------------------------+
//|count pending.mq4                                                 |
//|Desmond Wright aka " Buju"                                        |
//|notes:                                                            |
// this EA will count all the pending orders                         |
// for the chart it has been placed on                               |
//                                                                   |
//+------------------------------------------------------------------+
#property copyright "Desmond Wright aka  Buju"
#property link      ""

//+------------------------------------------------------------------+
//| expert start function                                            |
//+------------------------------------------------------------------+
int OpenBuyOrders;
int OpenSellOrders;
int total;

int start()
  {


OpenBuyOrders=0;
OpenSellOrders=0;
total=0;
 
//Count Pending Stop Orders
   for(int i=0;i<OrdersTotal(); i++ )
   {
      if(OrderSelect(i, SELECT_BY_POS)==true)
         {
         if (OrderType()==OP_BUYSTOP)
            OpenBuyOrders++;
         if (OrderType()==OP_SELLSTOP)
            OpenSellOrders++;   
         }
total=OpenBuyOrders + OpenSellOrders;    
   }
   Comment ("buy_stop=",OpenBuyOrders,"   sell_stop=",OpenSellOrders,"   total=",total);
  }
//+------------------------------------------------------------------+