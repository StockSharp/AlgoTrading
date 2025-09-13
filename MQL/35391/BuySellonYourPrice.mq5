//+------------------------------------------------------------------+
//|                                                     BuyLimit.mq5 |
//|                                       Copyright 2021, Dark Ryd3r |
//|                                   https://twitter.com/DarkrRyd3r |
//+------------------------------------------------------------------+
#property copyright "Copyright 2021, Dark Ryd3r"
#property link      "https://twitter.com/DarkrRyd3r"
#property version   "1.00"
#property description   "Entry Price is not effective if Market order is selected"
#property description   "Select Type of Order and enter correct buy price, Stop Loss and Take Profit"
#property description   "This EA will send your orders and comments are also passed on Strategy Tester"

#include <Trade\Trade.mqh>
CTrade trade;

input double qty = 1.2; //Enter qunatity for order
input double entryprice = 1.23239; //Enter Entry Price
input double Sl = 1.231;// Enter Stop Loss
input double Tp = 1.245; // Enter Take Profit

enum BuyorSell {
   None,
   Buy, //Market Order Buy
   Sell, //Market Order Sell
   BuyLimit, //Limit Buy Order
   SellLimit, //Limit Sell Order
   BuyStop, //Buy Stop Order
   SellStop //Sell Stop Order
};

input BuyorSell SelectMode = None;
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit() {
//---



//---
   return(INIT_SUCCEEDED);
}
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason) {
//---

}
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick() {
//---
   double Ask = NormalizeDouble(SymbolInfoDouble(_Symbol,SYMBOL_ASK),_Digits);
   double Bid = NormalizeDouble(SymbolInfoDouble(_Symbol,SYMBOL_BID),_Digits);
   
   if (SelectMode== None) {
     Print("Please select order type");
   }
   
//Market
   if (SelectMode== Buy) {
      if( (OrdersTotal()==0 ) && (PositionsTotal()==0) ) {
         trade.Buy(qty,_Symbol,Ask,Sl,Tp,"Market order : Buy Executed");
      }
   }
   if (SelectMode== Sell) {
      if( (OrdersTotal()==0 ) && (PositionsTotal()==0) ) {
         trade.Sell(qty,_Symbol,Bid,Sl,Tp,"Market order : Sell Executed");
      }
   }

//Limit
   if (SelectMode== BuyLimit) {
      if( (OrdersTotal()==0 ) && (PositionsTotal()==0) ) {
         trade.BuyLimit(qty, entryprice,_Symbol,Sl,Tp,ORDER_TIME_GTC,0,"Limit Order : Buy Executed");
      }
   }
   if (SelectMode== SellLimit) {
      if( (OrdersTotal()==0 ) && (PositionsTotal()==0) ) {
         trade.SellLimit(qty, entryprice,_Symbol,Sl,Tp,ORDER_TIME_GTC,0,"Limit Order : Sell Executed");
      }
   }

//Stop
   if (SelectMode== BuyStop) {
      if( (OrdersTotal()==0 ) && (PositionsTotal()==0) ) {
         trade.BuyStop(qty, entryprice,_Symbol,Sl,Tp,ORDER_TIME_GTC,0,"Stop Order : Buy Executed");
      }
   }
   if (SelectMode== SellStop) {
      if( (OrdersTotal()==0 ) && (PositionsTotal()==0) ) {
         trade.SellStop(qty, entryprice,_Symbol,Sl,Tp,ORDER_TIME_GTC,0,"Stop Order : Sell Executed");
      }
   }

}
//+------------------------------------------------------------------+
