//+------------------------------------------------------------------+
//|                    Close_on_PROFIT_or_LOSS_inAccont_Currency.mq4 |
//|                                  Copyright 2023, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
//+------------------------------------------------------------------------------------------------------------------------------+
//|At 0 set EA will do nothing.   
//|
//|Positive_Closure_in_Account_Currency must be higher than the current Equity amount, otherwise, the trades will be executed immediately.
//|Example: Equity is 55000$ and Positive_Closure_in_Account_Currency set to 55500$ to gain 500$
//|
//|Negative_Closure_in_Account_Currency must be lower than the current Equity amount, otherwise, the trades will be executed immediately.
//|Example: Equity is 55000$ and Negative_Closure_in_Account_Currency set to 54500$ to loose only 500$ 
//|
//|Spread spikes can be avoided by reducing the spread number but the market will do what it wants and higher gains or losses can occure. 
//|
//|Also if the spread is set lower than the average spread for the pairs traded those positions will not be executed. 
//|
//|WARNING: Use this software at your own risk. The Forex market is very volatile! 
//+------------------------------------------------------------------------------------------------------------------------------+


#property copyright     "Copyright 2024, MetaQuotes Ltd."
#property link          "https://www.mql5.com"
#property version       "1.01"
#property description   "persinaru@gmail.com"
#property description   "IP 2024 - free open source"
#property description   "This EA closes all trades on Profit and Losses calculated in Account Currency."
#property description   ""
#property description   "WARNING: Use this software at your own risk."
#property description   "The creator of this script cannot be held responsible for any damage or loss."
#property description   ""
#property strict
#property show_inputs


extern string  Closures = "EA closes all trades and pending orders when a profit or loss is reached. Profit and Losses are calculated in Account Currency."; 


extern int Positive_Closure_in_Account_Currency     = 0; 
//$ Positive_Closure_in_Account_Currency
extern int Negative_Closure_in_Account_Currency     = 0; 
//$ Positive_Closure_in_Account_Currency

extern int Spread = 10;
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
  
  stat();

   if (Positive_Closure_in_Account_Currency > 0) {
   if (AccountEquity()>= Positive_Closure_in_Account_Currency) {

   for(int Simple=OrdersTotal()-1; Simple>=0; Simple--){
   
   if(OrderSelect(Simple, SELECT_BY_POS==true, MODE_TRADES)){

   if(OrderType()==OP_BUY) {int OP_Buy  = OrderClose(OrderTicket(),OrderLots(),MarketInfo(OrderSymbol(),MODE_ASK),Spread,clrNONE);}
   if(OrderType()==OP_SELL){int OP_Sell = OrderClose(OrderTicket(),OrderLots(),MarketInfo(OrderSymbol(),MODE_BID),Spread,clrNONE);}
   
   if(OrderType()==OP_BUYLIMIT){int OP_BuyLimit = OrderDelete(OrderTicket());}
   if(OrderType()==OP_BUYSTOP){int OP_BuyStop = OrderDelete(OrderTicket());}
   if(OrderType()==OP_SELLLIMIT){int OP_SellLimit = OrderDelete(OrderTicket());}
   if(OrderType()==OP_SELLSTOP){int OP_SellStop = OrderDelete(OrderTicket());}}}
   if (OrdersTotal()==0) {ExpertRemove();}
}}

   if (Negative_Closure_in_Account_Currency > 0) {
   if (AccountEquity()<= Negative_Closure_in_Account_Currency) {

   for(int Simple=OrdersTotal()-1; Simple>=0; Simple--){
   
   if(OrderSelect(Simple, SELECT_BY_POS==true, MODE_TRADES)){

   if(OrderType()==OP_BUY) {int OP_Buy  = OrderClose(OrderTicket(),OrderLots(),MarketInfo(OrderSymbol(),MODE_ASK),Spread,clrNONE);}
   if(OrderType()==OP_SELL){int OP_Sell = OrderClose(OrderTicket(),OrderLots(),MarketInfo(OrderSymbol(),MODE_BID),Spread,clrNONE);}
   
   if(OrderType()==OP_BUYLIMIT){int OP_BuyLimit = OrderDelete(OrderTicket());}
   if(OrderType()==OP_BUYSTOP){int OP_BuyStop = OrderDelete(OrderTicket());}
   if(OrderType()==OP_SELLLIMIT){int OP_SellLimit = OrderDelete(OrderTicket());}
   if(OrderType()==OP_SELLSTOP){int OP_SellStop = OrderDelete(OrderTicket());}}}
   if (OrdersTotal()==0) {ExpertRemove();}
}}

}        
//+------------------------------------------------------------------+
int stat()
  {
   Comment("     ",AccountName(),"              ACCOUNT  ",AccountNumber(),"           FREE MARGIN  ",AccountFreeMargin(),"          EQUITY  ",AccountEquity(),"            BALANCE  ",AccountBalance());
   return(0);
  }
