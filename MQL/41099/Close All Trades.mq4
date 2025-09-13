//+------------------------------------------------------------------+
//|                                             Close All Trades.mq4 |
//|                                                      Pranav Soan |
//|                                              pranasoan@gmail.com |
//+------------------------------------------------------------------+
#property copyright "Pranav Soan"
#property link      "pranasoan@gmail.com"
#property version   "1.00"
#property strict
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
int total = OrdersTotal();
  for (int y=OrdersTotal()-1; y>=0; y--)
   {
     if (OrderSelect(y,SELECT_BY_POS,MODE_TRADES))
       {
        int ticket=OrderClose(OrderTicket(),OrderLots(),OrderClosePrice(),5,Aqua);
       }
   }
   return(0);
  }
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {

  }
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
  {

   
  }
//+------------------------------------------------------------------+
