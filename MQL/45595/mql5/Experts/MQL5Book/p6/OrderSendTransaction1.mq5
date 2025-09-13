//+------------------------------------------------------------------+
//|                                        OrderSendTransaction1.mq5 |
//|                                  Copyright 2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright   "2022, MetaQuotes Ltd."
#property link        "https://www.mql5.com"
#property description "Send 3 trade requests (open, adjust sl/tp, close) and wait for transaction events.\n"
                      "Events are triggered in this EA only after full completion of its main algorithm because of synchronization!"

#include <MQL5Book/OrderMonitor.mqh>
#include <MQL5Book/DealMonitor.mqh>
#include <MQL5Book/PositionMonitor.mqh>

#define SHOW_WARNINGS  // output extended info into the log, with changes in data state
#define WARNING Print  // use simple Print for warnings (instead of a built-in format with line numbers etc.)
#include <MQL5Book/MqlTradeSync.mqh>

enum ENUM_ORDER_TYPE_MARKET
{
   MARKET_BUY = ORDER_TYPE_BUY,    // ORDER_TYPE_BUY
   MARKET_SELL = ORDER_TYPE_SELL   // ORDER_TYPE_SELL
};

input ENUM_ORDER_TYPE_MARKET Type;
input double Volume;               // Volume (0 - minimal lot)
input uint Distance2SLTP = 1000;
input ulong Magic = 1234567890;
input ulong Deviation = 10;

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
   // once executed do nothing
   EventKillTimer();
   
   // define the struct
   MqlTradeRequestSync request;

   // fill optional fields of the struct
   request.magic = Magic;
   request.deviation = Deviation;

   // default values
   const double volume = Volume == 0 ? SymbolInfoDouble(_Symbol, SYMBOL_VOLUME_MIN) : Volume;

   Print("Start trade");
   ResetLastError();
   const ulong order = (Type == MARKET_BUY ?
      request.buy(volume) : 
      request.sell(volume));
   if(order == 0 || !request.completed())
   {
      Print("Failed Open");
      return;
   }
   
   Print("OK Open");
   
   Sleep(5000); // wait 5 seconds (you can try to modify manually)
   Print("SL/TP modification");
   const double price = PositionGetDouble(POSITION_PRICE_OPEN);
   const double point = SymbolInfoDouble(_Symbol, SYMBOL_POINT);
   TU::TradeDirection dir((ENUM_ORDER_TYPE)Type);
   const double SL = dir.negative(price, Distance2SLTP * point);
   const double TP = dir.positive(price, Distance2SLTP * point);
   if(request.adjust(SL, TP) && request.completed())
   {
      Print("OK Adjust");
   }
   else
   {
      Print("Failed Adjust");
   }
   
   Sleep(5000); // wait 5 seconds more
   Print("Close down");
   if(request.close(request.result.position) && request.completed())
   {
      Print("Finish");
   }
   else
   {
      Print("Failed Close");
   }
}

//+------------------------------------------------------------------+
//| Trade transactions handler                                       |
//+------------------------------------------------------------------+
void OnTradeTransaction(const MqlTradeTransaction &transaction,
   const MqlTradeRequest &request,
   const MqlTradeResult &result)
{
   static ulong count = 0;
   PrintFormat(">>>% 6d", ++count);
   Print(TU::StringOf(transaction));
   
   if(transaction.type == TRADE_TRANSACTION_REQUEST)
   {
      Print(TU::StringOf(request));
      Print(TU::StringOf(result));
   }
}

//+------------------------------------------------------------------+
/*
   example output (default settings, EURUSD):
   note how transaction events are postponed because MQL programs are single-threaded

   Start trade
   Waiting for position for deal D=1280661362
   OK Open
   SL/TP modification
   OK Adjust
   Close down
   Finish
   >>>     1
   TRADE_TRANSACTION_ORDER_ADD, #=1298021794(ORDER_TYPE_BUY/ORDER_STATE_STARTED), EURUSD, @ 1.10913, V=0.01
   >>>     2
   TRADE_TRANSACTION_DEAL_ADD, D=1280661362(DEAL_TYPE_BUY), #=1298021794(ORDER_TYPE_BUY/ORDER_STATE_STARTED), EURUSD, @ 1.10913, V=0.01, P=1298021794
   >>>     3
   TRADE_TRANSACTION_ORDER_DELETE, #=1298021794(ORDER_TYPE_BUY/ORDER_STATE_FILLED), EURUSD, @ 1.10913, P=1298021794
   >>>     4
   TRADE_TRANSACTION_HISTORY_ADD, #=1298021794(ORDER_TYPE_BUY/ORDER_STATE_FILLED), EURUSD, @ 1.10913, P=1298021794
   >>>     5
   TRADE_TRANSACTION_REQUEST
   TRADE_ACTION_DEAL, EURUSD, ORDER_TYPE_BUY, V=0.01, ORDER_FILLING_FOK, @ 1.10913, D=10, #=1298021794, M=1234567890
   DONE, D=1280661362, #=1298021794, V=0.01, @ 1.10913, Bid=1.10913, Ask=1.10913, Req=9
   >>>     6
   TRADE_TRANSACTION_POSITION, EURUSD, @ 1.10913, SL=1.09913, TP=1.11913, V=0.01, P=1298021794
   >>>     7
   TRADE_TRANSACTION_REQUEST
   TRADE_ACTION_SLTP, EURUSD, ORDER_TYPE_BUY, V=0.01, ORDER_FILLING_FOK, SL=1.09913, TP=1.11913, D=10, P=1298021794, M=1234567890
   DONE, Req=10
   >>>     8
   TRADE_TRANSACTION_ORDER_ADD, #=1298022443(ORDER_TYPE_SELL/ORDER_STATE_STARTED), EURUSD, @ 1.10901, V=0.01, P=1298021794
   >>>     9
   TRADE_TRANSACTION_DEAL_ADD, D=1280661967(DEAL_TYPE_SELL), #=1298022443(ORDER_TYPE_BUY/ORDER_STATE_STARTED), EURUSD, @ 1.10901, SL=1.09913, TP=1.11913, V=0.01, P=1298021794
   >>>    10
   TRADE_TRANSACTION_ORDER_DELETE, #=1298022443(ORDER_TYPE_SELL/ORDER_STATE_FILLED), EURUSD, @ 1.10901, P=1298021794
   >>>    11
   TRADE_TRANSACTION_HISTORY_ADD, #=1298022443(ORDER_TYPE_SELL/ORDER_STATE_FILLED), EURUSD, @ 1.10901, P=1298021794
   >>>    12
   TRADE_TRANSACTION_REQUEST
   TRADE_ACTION_DEAL, EURUSD, ORDER_TYPE_SELL, V=0.01, ORDER_FILLING_FOK, @ 1.10901, D=10, #=1298022443, P=1298021794, M=1234567890
   DONE, D=1280661967, #=1298022443, V=0.01, @ 1.10901, Bid=1.10901, Ask=1.10901, Req=11
   
*/
//+------------------------------------------------------------------+
