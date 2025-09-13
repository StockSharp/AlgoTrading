//+------------------------------------------------------------------+
//|                                         simple_trailing_stop.mq4 |
//|                                              Borys Yermokhin, UA |
//|                                         borys_ermokhin@yahoo.com |
//+------------------------------------------------------------------+
#property copyright "Borys Yermokhin, UA"
#property link      "borys_ermokhin@yahoo.com"
#property version   "1.00"
#property strict
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
//---
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
//---
  }
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
  {
//--- Set up parameters to trailing stop
   trailing(NULL,"My expert",777,25);
  }
//+------------------------------------------------------------------+
//| Trailing stop script                                             |
//| (Symbol for trailing, Comment in order,                          |
//| Magic number of order, points for trail)                         |
//+------------------------------------------------------------------+
void trailing(string symbol,string comment,int magic,int trail_p)
  {
   if(symbol==NULL) symbol=Symbol();
   for(int i=0;i<OrdersTotal();i++)
     {
      if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES))
        {
         if(OrderSymbol()==symbol && OrderComment()==comment && OrderMagicNumber()==magic)
           {
            switch(OrderType())
              {
               case OP_BUY:
                  if(OrderOpenPrice()+(trail_p*Point)<Ask && OrderStopLoss()+(trail_p*Point)<Ask)
                    {
                     if(!OrderModify(OrderTicket(),OrderOpenPrice(),Ask-(trail_p*Point),OrderTakeProfit(),OrderExpiration(),clrNONE))
                       {
                        Print("OrderModify завершилась с ошибкой #",GetLastError());
                       }
                    }
                  break;
               case OP_SELL:
                  if(OrderOpenPrice()-(trail_p*Point)>Bid && OrderStopLoss()-(trail_p*Point)>Bid)
                    {
                     if(!OrderModify(OrderTicket(),OrderOpenPrice(),Bid+(trail_p*Point),OrderTakeProfit(),OrderExpiration(),clrNONE))
                       {
                        Print("OrderModify завершилась с ошибкой #",GetLastError());
                       }
                    }
                  break;
              }
           }
        }
     }
  }
//+------------------------------------------------------------------+
