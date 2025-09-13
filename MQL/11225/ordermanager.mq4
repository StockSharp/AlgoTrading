//+------------------------------------------------------------------+
//|                                                 OrderManager.mq4 |
//|                        Copyright 2014, MetaQuotes Software Corp. |
//|                                              http://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright "Copyright 2014, MetaQuotes Software Corp."
#property link      "http://www.mql5.com"
#property version   "1.00"
#property strict

extern bool ManageAllSymbol = false;
extern bool UseStopLoss = true;
extern double StopLossPercent = 0.02;
extern bool UseTakeProfit = true;
extern double TakeProfitPercent = 0.06;
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
//---
   EventSetTimer(1);
//---
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
//---
   EventKillTimer();
  }
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
  {
//---      
  }
//+------------------------------------------------------------------+
void OnTimer()
{
   if(!IsTradeAllowed()) return;
   double profit;   
   double percent ,risk;
   bool reponse = false;
   int cmd = 0;
   double price;
   
   for(int i = OrdersTotal()-1; i>=0; i--)
   {
      if( OrderSelect( i, SELECT_BY_POS, MODE_TRADES ) == false ) break;
      
      if(!ManageAllSymbol && OrderSymbol()!= Symbol())break;
        
      RefreshRates();
      profit = OrderProfit();
      cmd = OrderType();
      bool close = false;
      price = (cmd == OP_BUY) ? MarketInfo(OrderSymbol(),MODE_ASK) : MarketInfo(OrderSymbol(),MODE_BID);
      if(profit < 0)
      {
         double tempProfit =  MathAbs(profit);
         risk = tempProfit / AccountBalance();
         if(UseStopLoss && risk > StopLossPercent)
         {
            close = true;
         }
      }
      else if(profit > 0)
      {
         risk = profit / AccountBalance();
         if(UseTakeProfit && risk >= TakeProfitPercent)
         {
            close = true;   
         }
      }
      if(close) reponse = OrderClose(OrderTicket(),OrderLots(),price,3,0);
   }
}
