//+------------------------------------------------------------------+
//|                                   _HPCS_Inter5_MT4_EA_V01_WE.mq4 |
//|                        Copyright 2021, MetaQuotes Software Corp. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright "Copyright 2021, MetaQuotes Software Corp."
#property link      "https://www.mql5.com"
#property version   "1.00"
#property strict
#property script_show_inputs
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+

input int ii_stoploss = 10;
input int ii_TakeProfit = 10;

int OnInit()
  {
//---
      
      int li_ticket,li_factor = 1;
   double ld_stoploss,ld_Takeprofit;
   
   if(Digits ==5 || Digits == 3)
   { li_factor = 10;}
   
   ld_Takeprofit = Ask + ii_TakeProfit*Point()*li_factor;
   ld_stoploss = Ask - ii_stoploss*Point()*li_factor;
   
   if(ld_stoploss > (Bid - MarketInfo(_Symbol,MODE_STOPLEVEL )*Point()))
   {
      ld_stoploss = (Bid - MarketInfo(_Symbol,MODE_STOPLEVEL )*Point());
   } 
   
      if(Close[5] > Close[1])
      {
         li_ticket = OrderSend(_Symbol,OP_BUY,1,Ask,10,ld_stoploss,ld_Takeprofit,NULL,2233);
         if(li_ticket < 0)
         {
            Print("Order not placed with Error: ",GetLastError());
         }
         if(OrderSelect(li_ticket,SELECT_BY_TICKET,MODE_TRADES))
         {
            OrderPrint();
            Print("Order Open Time is: ",OrderOpenTime());
         }
      }
      
   
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
   
  }
//+------------------------------------------------------------------+
