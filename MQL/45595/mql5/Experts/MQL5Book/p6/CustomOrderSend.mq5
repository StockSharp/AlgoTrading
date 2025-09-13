//+------------------------------------------------------------------+
//|                                              CustomOrderSend.mq5 |
//|                                  Copyright 2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright   "2022, MetaQuotes Ltd."
#property link        "https://www.mql5.com"
#property description "Construct MqlTradeRequest from user input and send it to server via OrderSend or OrderSendAsync."

#include <MQL5Book/PRTF.mqh>
#include <MQL5Book/TradeRetcode.mqh>
#include <MQL5Book/TradeUtils.mqh>

input bool Async = false;
input ENUM_TRADE_REQUEST_ACTIONS Action = TRADE_ACTION_DEAL;
input ulong Magic;
input ulong Order;
input string Symbol;    // Symbol (empty = current _Symbol)
input double Volume;    // Volume (0 = minimal lot)
input double Price;     // Price (0 = current Ask)
input double StopLimit;
input double SL;
input double TP;
input ulong Deviation;
input ENUM_ORDER_TYPE Type;
input ENUM_ORDER_TYPE_FILLING Filling;
input ENUM_ORDER_TYPE_TIME ExpirationType;
input datetime ExpirationTime;
input string Comment;
input ulong Position;
input ulong PositionBy;

//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
{
   if(AccountInfoInteger(ACCOUNT_TRADE_MODE) != ACCOUNT_TRADE_MODE_DEMO)
   {
      Alert("This is a test EA! Run it on a DEMO account only!");
      return INIT_FAILED;
   } 

   // setup timer for postponed execution
   EventSetTimer(1);
   return INIT_SUCCEEDED;
}

//+------------------------------------------------------------------+
//| Timer event handler                                              |
//+------------------------------------------------------------------+
void OnTimer()
{
   // once executed do nothing, wait for another setup from user
   EventKillTimer();
   
   // initialize structs with zeros
   MqlTradeRequest request = {};
   MqlTradeResult result = {};
   
   // default values
   const bool kindOfBuy = (Type & 1) == 0 && Type < ORDER_TYPE_CLOSE_BY;
   const string symbol = StringLen(Symbol) == 0 ? _Symbol : Symbol;
   const double volume = Volume == 0 ? SymbolInfoDouble(symbol, SYMBOL_VOLUME_MIN) : Volume;
   const double price = Price == 0 ? SymbolInfoDouble(symbol, kindOfBuy ? SYMBOL_ASK : SYMBOL_BID) : Price;
   
   TU::SymbolMetrics sm(symbol);
   
   // fill the struct
   request.action = Action;
   request.magic = Magic;
   request.order = Order;
   request.symbol = symbol;
   request.volume = sm.volume(volume);
   request.price = sm.price(price);
   request.stoplimit = sm.price(StopLimit);
   request.sl = sm.price(SL);
   request.tp = sm.price(TP);
   request.deviation = Deviation;
   request.type = Type;
   request.type_filling = Filling;
   request.type_time = ExpirationType;
   request.expiration = ExpirationTime;
   request.comment = Comment;
   request.position = Position;
   request.position_by = PositionBy;

   // send request and print out results
   ResetLastError();
   if(Async)
   {
      PRTF(OrderSendAsync(request, result));
   }
   else
   {
      PRTF(OrderSend(request, result));
   }
   Print(TU::StringOf(request));
   Print(TU::StringOf(result));
}

/* // Facultative study: uncomment to see trade events in the log
//+------------------------------------------------------------------+
//| Trade transactions handler                                       |
//+------------------------------------------------------------------+
void OnTradeTransaction(const MqlTradeTransaction &transaction,
   const MqlTradeRequest &request,
   const MqlTradeResult &result)
{
   static ulong count = 0;
   Print(count++);
   Print(TU::StringOf(transaction));
   Print(TU::StringOf(request));
   Print(TU::StringOf(result));
}
*/
//+------------------------------------------------------------------+
/*
   example output (default settings - Async=false):
   
      OrderSend(request,result)=true / ok
      TRADE_ACTION_DEAL, EURUSD, ORDER_TYPE_BUY, V=0.01, ORDER_FILLING_FOK, @ 1.12462
      DONE, D=1250236209, #=1267684253, V=0.01, @ 1.12462, Bid=1.12456, Ask=1.12462, Request executed, Req=1

   example output (altered settings - Async=true):
   
      OrderSendAsync(request,result)=true / ok
      TRADE_ACTION_DEAL, EURUSD, ORDER_TYPE_BUY, V=0.01, ORDER_FILLING_FOK, @ 1.12449
      PLACED, Order placed, Req=2
*/
//+------------------------------------------------------------------+
