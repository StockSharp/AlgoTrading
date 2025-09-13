//+------------------------------------------------------------------+
//|                                              One Price SL TP.mq4 |
//|                        Copyright 2021, MetaQuotes Software Corp. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright "Copyright 2021, SpaceX Software Corp."
#property link      "https://www.mql5.com"
#property version   "1.00"
#property strict
 #property strict
 #property show_inputs

input double ZEN = 0;
//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
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
      int total = OrdersTotal();
  for(int b=total-1;b>=0;b--)
  {
     if(OrderSelect(b, SELECT_BY_POS)==false) break;
     if(OrderCloseTime()!=0 || OrderSymbol()!=Symbol()) continue;
     
     if(OrderType()==OP_BUY)
       if(ZEN>0 && ZEN>Ask)
         if(OrderTakeProfit()>=0)
                 {
                 if(!OrderModify(OrderTicket(),OrderOpenPrice(),OrderStopLoss(),ZEN,0,clrNONE))
                        Print("Take Profit Updated TO New Level ",OrderTicket());
                     
                    }
                  
         if(ZEN>0 && ZEN<Bid)
         if(OrderStopLoss()>=0)
                 {
                 if(!OrderModify(OrderTicket(),OrderOpenPrice(),ZEN,OrderTakeProfit(),0,clrNONE))
                        Print("Stop Loss Updated TO New Level ",OrderTicket());
                     
                    }
                    
      if(OrderType()==OP_SELL)
       if(ZEN>0 && ZEN<Bid)
         if(OrderTakeProfit()>=0)
                 {
                 if(!OrderModify(OrderTicket(),OrderOpenPrice(),OrderStopLoss(),ZEN,0,clrNONE))
                        Print("Take Profit Updated TO New Level ",OrderTicket());
                     
                    }
                  
         if(ZEN>0 && ZEN>Ask)
         if(OrderStopLoss()>=0)
                 {
                 if(!OrderModify(OrderTicket(),OrderOpenPrice(),ZEN,OrderTakeProfit(),0,clrNONE))
                        Print("Stop Loss Updated TO New Level ",OrderTicket());
                     
                    }
//---
   
  }
//+------------------------------------------------------------------+
 
  }
//+------------------------------------------------------------------+