//+------------------------------------------------------------------+
//|                                              TrailingStop_EA.mq4 |
//|                                              Mustafa Doruk Basar |
//+------------------------------------------------------------------+
#property copyright "Mustafa Doruk Basar"
#property link      ""
#property version   "1.00"
#property description "Set the ticket and trailing stop and The EA manages the open position"
#property strict

extern int ticket_of_open_pos=1;    // the ticket number of the open pos that you want the EA to manage it
extern int trailing_points=200;     // trailing stop in points

int k;
int i;
int tic;
double StopLoss;
bool otic;

//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
//---

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
//---
   if(OrdersTotal()>0)
   {
      k=0;
      for(i=0;i<OrdersTotal();i++)
      {
         tic=OrderSelect(k,SELECT_BY_POS,MODE_TRADES);
         
         if(trailing_points<MarketInfo(OrderSymbol(),MODE_STOPLEVEL) && trailing_points>0)
         {
            trailing_points=MarketInfo(OrderSymbol(),MODE_STOPLEVEL);  
            Alert(OrderSymbol()+": You entered a lower trailing stop level than allowed. It will be changed to the minimum allowed level");
         }
               
         if (OrderTicket()==ticket_of_open_pos)
         {
            if(OrderType()==OP_BUY)
            {
               if(MarketInfo(OrderSymbol(),MODE_BID)-OrderOpenPrice()>=trailing_points*Point)
               {
                  TrailStop(OrderTicket());   
               }
            }
            if(OrderType()==OP_SELL)
            {
               if(OrderOpenPrice()-MarketInfo(OrderSymbol(),MODE_ASK)>=trailing_points*Point)
               {
                  TrailStop(OrderTicket());   
               }
            } 
         }
         k++;
      }
   }
   
   
  }
//+------------------------------------------------------------------+

void TrailStop(int orderticket)
{
   tic=OrderSelect(orderticket, SELECT_BY_TICKET);

   if(OrderType()==OP_BUY) 
   {
      StopLoss = MarketInfo(OrderSymbol(),MODE_BID)-(trailing_points*Point);
      if(StopLoss>OrderStopLoss()) 
      {
         otic=OrderModify(OrderTicket(),OrderOpenPrice(),StopLoss,OrderTakeProfit(),0,CLR_NONE);
      }
   }

   if(OrderType()==OP_SELL) 
   {
      StopLoss = MarketInfo(OrderSymbol(),MODE_ASK)+(trailing_points*Point);
      if(StopLoss<OrderStopLoss()) 
      {
         otic=OrderModify(OrderTicket(),OrderOpenPrice(),StopLoss,OrderTakeProfit(),0,CLR_NONE);
      }
   }

return;

}