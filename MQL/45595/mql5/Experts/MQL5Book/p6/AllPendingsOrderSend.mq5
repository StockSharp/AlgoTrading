//+------------------------------------------------------------------+
//|                                         AllPendingsOrderSend.mq5 |
//|                                  Copyright 2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright   "2022, MetaQuotes Ltd."
#property link        "https://www.mql5.com"
#property description "Construct MqlTradeRequest for all pending order types and call OrderSend with TRADE_ACTION_PENDING."
#property description "Expiration, Stop Loss and Take Profit can be set optionally."

#define SHOW_WARNINGS  // output extended info into the log, with changes in data state
#define WARNING Print  // use simple Print for warnings (instead of a built-in format with line numbers etc.)
#include <MQL5Book/MqlTradeSync.mqh>

input ulong Magic = 1234567890;
input int Distance2SLTP = 0;     // Distance to SL/TP in points (0 = no)
input ENUM_ORDER_TYPE_TIME Expiration = ORDER_TIME_GTC;
input datetime Until = 0;

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
//| Prepare MqlTradeRequest struct and call OrderSend with it        |
//| to place a pending order of specific type with respect to        |
//| given daily price range                                          |
//+------------------------------------------------------------------+
bool PlaceOrder(const ENUM_ORDER_TYPE type, const double range)
{
   bool success = false;
   MqlTradeRequestSync request;
   
   // distance for orders of different types from current market price,
   // indices are ENUM_ORDER_TYPE
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
   
   // default values
   const double volume = SymbolInfoDouble(_Symbol, SYMBOL_VOLUME_MIN);
   const double price = TU::GetCurrentPrice(type) + range * coefficients[type];
   const double point = SymbolInfoDouble(_Symbol, SYMBOL_POINT);

   // origin is filled only for *_STOP_LIMIT-orders
   const bool stopLimit = type == ORDER_TYPE_BUY_STOP_LIMIT || type == ORDER_TYPE_SELL_STOP_LIMIT;
   const double origin = stopLimit ? TU::GetCurrentPrice(type) : 0; 
   
   TU::TradeDirection dir(type);
   const double stop = Distance2SLTP == 0 ? 0 :
      dir.negative(stopLimit ? origin : price, Distance2SLTP * point);
   const double take = Distance2SLTP == 0 ? 0 :
      dir.positive(stopLimit ? origin : price, Distance2SLTP * point);
   
   // Fill the struct:
   // Here we assign the required field 'type' directly, but do it only
   // to place many orders of different types in a loop,
   // and don't want a switch to call different methods like
   // buyStop/sellStop/buyLimit/sellLimit etc.
   // Instead we call the underlying '_pending' method.
   request.type = type;   // required field (here filled directly only for the purpose of this demo)
   request.magic = Magic; // optional field (always filled directly, when necessary)

   // Normally you should call specific public methods -
   // buyStop/sellStop/buyLimit/sellLimit etc (see PendingOrderSend.mq5)
   // to prepare an order of corresponding type

   ResetLastError();
   // send request and wait for results
   const ulong order = request._pending(_Symbol, volume, price, stop, take, Expiration, Until, origin);
   if(order != 0 && request.completed())
   {
      Print("OK order placed: #=", order);
      success = true;
   }
   Print(TU::StringOf(request));
   Print(TU::StringOf(request.result));
   return success;
}

//+------------------------------------------------------------------+
//| Timer event handler                                              |
//+------------------------------------------------------------------+
void OnTimer()
{
   // once executed, wait for another setup from user
   EventKillTimer();

   const double range = iHigh(_Symbol, PERIOD_D1, 1) - iLow(_Symbol, PERIOD_D1, 1);
   Print("Autodetected daily range: ", TU::StringOf(range));
   
   int count = 0;
   for(ENUM_ORDER_TYPE i = ORDER_TYPE_BUY_LIMIT; i <= ORDER_TYPE_SELL_STOP_LIMIT; ++i)
   {
      count += PlaceOrder(i, range);
   }
   
   if(count > 0)
   {
      Alert(StringFormat("%d pending orders placed - remove them manually, please", count));
   }
}
//+------------------------------------------------------------------+
/*
   example output (EURUSD, default settings):

   Autodetected daily range: 0.01413
   OK order placed: #=1282032135
   TRADE_ACTION_PENDING, EURUSD, ORDER_TYPE_BUY_LIMIT, V=0.01, ORDER_FILLING_FOK, @ 1.08824, ORDER_TIME_GTC, M=1234567890
   DONE, #=1282032135, V=0.01, Request executed, Req=73
   OK order placed: #=1282032136
   TRADE_ACTION_PENDING, EURUSD, ORDER_TYPE_SELL_LIMIT, V=0.01, ORDER_FILLING_FOK, @ 1.10238, ORDER_TIME_GTC, M=1234567890
   DONE, #=1282032136, V=0.01, Request executed, Req=74
   OK order placed: #=1282032138
   TRADE_ACTION_PENDING, EURUSD, ORDER_TYPE_BUY_STOP, V=0.01, ORDER_FILLING_FOK, @ 1.10944, ORDER_TIME_GTC, M=1234567890
   DONE, #=1282032138, V=0.01, Request executed, Req=75
   OK order placed: #=1282032141
   TRADE_ACTION_PENDING, EURUSD, ORDER_TYPE_SELL_STOP, V=0.01, ORDER_FILLING_FOK, @ 1.08118, ORDER_TIME_GTC, M=1234567890
   DONE, #=1282032141, V=0.01, Request executed, Req=76
   OK order placed: #=1282032142
   TRADE_ACTION_PENDING, EURUSD, ORDER_TYPE_BUY_STOP_LIMIT, V=0.01, ORDER_FILLING_FOK, @ 1.10520, X=1.09531, ORDER_TIME_GTC, M=1234567890
   DONE, #=1282032142, V=0.01, Request executed, Req=77
   OK order placed: #=1282032144
   TRADE_ACTION_PENDING, EURUSD, ORDER_TYPE_SELL_STOP_LIMIT, V=0.01, ORDER_FILLING_FOK, @ 1.08542, X=1.09531, ORDER_TIME_GTC, M=1234567890
   DONE, #=1282032144, V=0.01, Request executed, Req=78
   Alert: 6 pending orders placed - remove them manually, please

*/
//+------------------------------------------------------------------+
