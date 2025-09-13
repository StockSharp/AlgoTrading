//+------------------------------------------------------------------+
//|                                           PendingOrderDelete.mq5 |
//|                                  Copyright 2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright   "2022, MetaQuotes Ltd."
#property link        "https://www.mql5.com"
#property description "Construct MqlTradeRequest for specified pending order type and call OrderSend twice:\n"
                      "- once atfer start-up with TRADE_ACTION_PENDING to create an order,\n"
                      "- second in OnDeinit with TRADE_ACTION_REMOVE to remove it."

#define SHOW_WARNINGS  // output extended info into the log, with changes in data state
#define WARNING Print  // use simple Print for warnings (instead of a built-in format with line numbers etc.)
#include <MQL5Book/MqlTradeSync.mqh>

enum ENUM_ORDER_TYPE_PENDING
{                                                        // Captions in UI
   PENDING_BUY_STOP = ORDER_TYPE_BUY_STOP,               // ORDER_TYPE_BUY_STOP
   PENDING_SELL_STOP = ORDER_TYPE_SELL_STOP,             // ORDER_TYPE_SELL_STOP
   PENDING_BUY_LIMIT = ORDER_TYPE_BUY_LIMIT,             // ORDER_TYPE_BUY_LIMIT
   PENDING_SELL_LIMIT = ORDER_TYPE_SELL_LIMIT,           // ORDER_TYPE_SELL_LIMIT
   PENDING_BUY_STOP_LIMIT = ORDER_TYPE_BUY_STOP_LIMIT,   // ORDER_TYPE_BUY_STOP_LIMIT
   PENDING_SELL_STOP_LIMIT = ORDER_TYPE_SELL_STOP_LIMIT, // ORDER_TYPE_SELL_STOP_LIMIT
};

input string Symbol;             // Symbol (empty = current _Symbol)
input double Volume;             // Volume (0 = minimal lot)
input ENUM_ORDER_TYPE_PENDING Type = PENDING_BUY_STOP;
input int Distance2SLTP = 0;     // Distance to SL/TP in points (0 = no)
input ENUM_ORDER_TYPE_TIME Expiration = ORDER_TIME_GTC;
input datetime Until = 0;
input ulong Magic = 1234567890;
input string Comment;

ulong OwnOrder = 0; // ticket

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
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int)
{
   if(OwnOrder != 0)
   {
      RemoveOrder(OwnOrder);
   }
}

//+------------------------------------------------------------------+
//| Prepare MqlTradeRequest and call OrderSend to remove given order |
//+------------------------------------------------------------------+
void RemoveOrder(const ulong ticket)
{
   MqlTradeRequestSync request;
   if(request.remove(ticket) && request.completed())
   {
      Print("OK order removed");
   }
   Print(TU::StringOf(request));
   Print(TU::StringOf(request.result));
}

//+------------------------------------------------------------------+
//| Prepare MqlTradeRequest struct and call OrderSend with it        |
//| to place a pending order of specific type and properties         |
//+------------------------------------------------------------------+
ulong PlaceOrder(const ENUM_ORDER_TYPE type,
   const string symbol, const double lot,
   const int sltp, ENUM_ORDER_TYPE_TIME expiration, datetime until,
   const ulong magic = 0, const string comment = NULL)
{
   // distance for orders of different types from current market price,
   // indices are ENUM_ORDER_TYPE, values to multiply by daily range
   static double coefficients[] = 
   {
      0  ,   // ORDER_TYPE_BUY - not used
      0  ,   // ORDER_TYPE_SELL - not used
     -0.5,   // ORDER_TYPE_BUY_LIMIT - below price
     +0.5,   // ORDER_TYPE_SELL_LIMIT - above price
     +1.0,   // ORDER_TYPE_BUY_STOP - far above price
     -1.0,   // ORDER_TYPE_SELL_STOP - far below price
     +0.7,   // ORDER_TYPE_BUY_STOP_LIMIT - middle price above current 
     -0.7,   // ORDER_TYPE_SELL_STOP_LIMIT - middle price below current
      0  ,   // ORDER_TYPE_CLOSE_BY - not used
   };

   const double range = iHigh(symbol, PERIOD_D1, 1) - iLow(symbol, PERIOD_D1, 1);
   Print("Autodetected daily range: ", (float)range);

   // default values
   const double volume = lot == 0 ? SymbolInfoDouble(symbol, SYMBOL_VOLUME_MIN) : lot;
   const double point = SymbolInfoDouble(symbol, SYMBOL_POINT);

   // price-related values
   const double price = TU::GetCurrentPrice(type, symbol) + range * coefficients[type];
   // origin is filled only for *_STOP_LIMIT-orders
   const bool stopLimit = type == ORDER_TYPE_BUY_STOP_LIMIT || type == ORDER_TYPE_SELL_STOP_LIMIT;
   const double origin = stopLimit ? TU::GetCurrentPrice(type, symbol) : 0; 
   
   TU::TradeDirection dir(type);
   const double stop = sltp == 0 ? 0 : dir.negative(stopLimit ? origin : price, sltp * point);
   const double take = sltp == 0 ? 0 : dir.positive(stopLimit ? origin : price, sltp * point);
   
   MqlTradeRequestSync request(symbol);

   // fill optional fields
   request.magic = magic;
   request.comment = comment;
   // you can customize filling mode as well, by default MqlTradeRequestSync will
   // automatically select first supported mode
   // request.type_filling = SYMBOL_FILLING_FOK;

   ResetLastError();
   // fill and check relevant fields, send request
   ulong order = 0;
   switch(type)
   {
   case ORDER_TYPE_BUY_STOP:
      order = request.buyStop(volume, price, stop, take, expiration, until);
      break;
   case ORDER_TYPE_SELL_STOP:
      order = request.sellStop(volume, price, stop, take, expiration, until);
      break;
   case ORDER_TYPE_BUY_LIMIT:
      order = request.buyLimit(volume, price, stop, take, expiration, until);
      break;
   case ORDER_TYPE_SELL_LIMIT:
      order = request.sellLimit(volume, price, stop, take, expiration, until);
      break;
   case ORDER_TYPE_BUY_STOP_LIMIT:
      order = request.buyStopLimit(volume, price, origin, stop, take, expiration, until);
      break;
   case ORDER_TYPE_SELL_STOP_LIMIT:
      order = request.sellStopLimit(volume, price, origin, stop, take, expiration, until);
      break;
   }
   
   if(order != 0 && request.completed())
   {
      Print("OK order placed: #=", order);
   }
   Print(TU::StringOf(request));
   Print(TU::StringOf(request.result));
   return order;
}

//+------------------------------------------------------------------+
//| Timer event handler                                              |
//+------------------------------------------------------------------+
void OnTimer()
{
   // once executed, wait for another setup from user
   EventKillTimer();

   const string symbol = StringLen(Symbol) == 0 ? _Symbol : Symbol;
   OwnOrder = PlaceOrder((ENUM_ORDER_TYPE)Type, symbol, Volume,
      Distance2SLTP, Expiration, Until, Magic, Comment);
}
//+------------------------------------------------------------------+
/*
   example output (EURUSD, default settings, then switched to XAUUSD, then the expert removed):

   (EURUSD,H1)	Autodetected daily range: 0.0094
   (EURUSD,H1)	OK order placed: #=1284920879
   (EURUSD,H1)	TRADE_ACTION_PENDING, EURUSD, ORDER_TYPE_BUY_STOP, V=0.01, ORDER_FILLING_FOK, @ 1.11011, ORDER_TIME_GTC, M=1234567890
   (EURUSD,H1)	DONE, #=1284920879, V=0.01, Request executed, Req=1
   (EURUSD,H1)	OK order removed
   (EURUSD,H1)	TRADE_ACTION_REMOVE, EURUSD, ORDER_TYPE_BUY, ORDER_FILLING_FOK, #=1284920879
   (EURUSD,H1)	DONE, #=1284920879, Request executed, Req=2
   (XAUUSD,H1)	Autodetected daily range: 47.45
   (XAUUSD,H1)	OK order placed: #=1284921672
   (XAUUSD,H1)	TRADE_ACTION_PENDING, XAUUSD, ORDER_TYPE_BUY_STOP, V=0.01, ORDER_FILLING_FOK, @ 1956.68, ORDER_TIME_GTC, M=1234567890
   (XAUUSD,H1)	DONE, #=1284921672, V=0.01, Request executed, Req=3
   (XAUUSD,H1)	OK order removed
   (XAUUSD,H1)	TRADE_ACTION_REMOVE, XAUUSD, ORDER_TYPE_BUY, ORDER_FILLING_FOK, #=1284921672
   (XAUUSD,H1)	DONE, #=1284921672, Request executed, Req=4

*/
//+------------------------------------------------------------------+
