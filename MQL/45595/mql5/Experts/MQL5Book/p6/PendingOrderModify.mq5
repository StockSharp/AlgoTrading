//+------------------------------------------------------------------+
//|                                           PendingOrderModify.mq5 |
//|                                  Copyright 2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright   "2022, MetaQuotes Ltd."
#property link        "https://www.mql5.com"
#property description "Construct MqlTradeRequest for specified pending order type and call OrderSend with TRADE_ACTION_PENDING."
#property description "Modify the order by TRADE_ACTION_MODIFY on every new day with Stop Loss and Take Profit autodetected from previous day range."

#define SHOW_WARNINGS  // output extended info into the log
#define WARNING Print  // use simple Print for warnings (instead of a built-in format with line numbers etc.)

// uncomment the following line to prevent early returns after
// failed checkups in MqlStructRequestSync methods, so
// incorrect requests will be sent and retcodes received from the server
// #define RETURN(X)

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
input ENUM_ORDER_TYPE_TIME Expiration = ORDER_TIME_GTC;
input datetime Until = 0;
input ulong Magic = 1234567890;

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
   return INIT_SUCCEEDED;
}

//+------------------------------------------------------------------+
//| Helper function to find a compatible order                       |
//+------------------------------------------------------------------+
ulong GetMyOrder(const string name, const ulong magic)
{
   for(int i = 0; i < OrdersTotal(); ++i)
   {
      ulong t = OrderGetTicket(i);
      if(OrderGetInteger(ORDER_MAGIC) == magic
         && OrderGetString(ORDER_SYMBOL) == name)
      {
         return t;
      }
   }

   return 0;
}

// distance for orders of different types from current market price,
// indices are ENUM_ORDER_TYPE, values to multiply by daily range
static double Coefficients[] = 
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

//+------------------------------------------------------------------+
//| Prepare MqlTradeRequest struct and call OrderSend with it        |
//| to place a pending order of specific type and properties         |
//+------------------------------------------------------------------+
uint PlaceOrder(const ENUM_ORDER_TYPE type,
   const string symbol, const double lot, const double range,
   ENUM_ORDER_TYPE_TIME expiration, datetime until,
   const ulong magic = 0)
{
   // default values
   const double volume = lot == 0 ? SymbolInfoDouble(symbol, SYMBOL_VOLUME_MIN) : lot;
   const double point = SymbolInfoDouble(symbol, SYMBOL_POINT);

   // price-related values
   const double price = TU::GetCurrentPrice(type, symbol) + range * Coefficients[type];
   // origin is filled only for *_STOP_LIMIT-orders
   const bool stopLimit = type == ORDER_TYPE_BUY_STOP_LIMIT || type == ORDER_TYPE_SELL_STOP_LIMIT;
   const double origin = stopLimit ? TU::GetCurrentPrice(type, symbol) : 0;
   
   TU::TradeDirection dir(type);
   const int sltp = (int)(range / 2 / point);
   
   const double stop = sltp == 0 ? 0 : dir.negative(stopLimit ? origin : price, sltp * point);
   const double take = sltp == 0 ? 0 : dir.positive(stopLimit ? origin : price, sltp * point);
   
   MqlTradeRequestSync request(symbol);

   // fill optional fields
   request.magic = magic;

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
   return request.result.retcode;
}

//+------------------------------------------------------------------+
//| Prepare MqlTradeRequest struct and call OrderSend with it        |
//| to place a pending order of specific type and properties         |
//+------------------------------------------------------------------+
uint ModifyOrder(const ulong ticket, const double range,
   ENUM_ORDER_TYPE_TIME expiration, datetime until)
{
   // default values
   const string symbol = OrderGetString(ORDER_SYMBOL);
   const double point = SymbolInfoDouble(symbol, SYMBOL_POINT);

   // price-related values
   const ENUM_ORDER_TYPE type = (ENUM_ORDER_TYPE)OrderGetInteger(ORDER_TYPE);
   const double price = TU::GetCurrentPrice(type, symbol) + range * Coefficients[type];
   // origin is filled only for *_STOP_LIMIT-orders
   const bool stopLimit = type == ORDER_TYPE_BUY_STOP_LIMIT || type == ORDER_TYPE_SELL_STOP_LIMIT;
   const double origin = stopLimit ? TU::GetCurrentPrice(type, symbol) : 0; 
   
   TU::TradeDirection dir(type);
   const int sltp = (int)(range / 2 / point);
   const double stop = sltp == 0 ? 0 : dir.negative(stopLimit ? origin : price, sltp * point);
   const double take = sltp == 0 ? 0 : dir.positive(stopLimit ? origin : price, sltp * point);
   
   MqlTradeRequestSync request(symbol);
   
   /*
   // uncomment this fragment to alter filling mode in a round robin manner
   ENUM_ORDER_TYPE_FILLING filling =
      (ENUM_ORDER_TYPE_FILLING)((OrderGetInteger(ORDER_TYPE_FILLING) + 1) % 3);
   request.type_filling = filling;
   // NB: make sure all filling modes are allowed for selected symbol
   */

   ResetLastError();
   // fill and check relevant fields, send request
   if(request.modify(ticket, price, stop, take, expiration, until, origin)
      && request.completed())
   {
      Print("OK order modified: #=", ticket);
   }
   
   Print(TU::StringOf(request));
   Print(TU::StringOf(request.result));
   return request.result.retcode;
}

//+------------------------------------------------------------------+
//| Tick event handler                                               |
//+------------------------------------------------------------------+
void OnTick()
{
   // run once on a new day 
   static datetime lastDay = 0;
   static const uint DAYLONG = 60 * 60 * 24; // seconds per day (24 hours)
   if(TimeTradeServer() / DAYLONG * DAYLONG == lastDay) return;

   // noncritical error handler by autoadjusted timeout
   const static int DEFAULT_RETRY_TIMEOUT = 1; // seconds
   static int RetryFrequency = DEFAULT_RETRY_TIMEOUT;
   static datetime RetryRecordTime = 0;
   if(TimeTradeServer() - RetryRecordTime < RetryFrequency) return;

   const string symbol = StringLen(Symbol) == 0 ? _Symbol : Symbol;
   const double range = iHigh(symbol, PERIOD_D1, 1) - iLow(symbol, PERIOD_D1, 1);
   Print("Autodetected daily range: ", (float)range);
   
   uint retcode = 0;
   ulong ticket = GetMyOrder(symbol, Magic);
   if(!ticket)
   {
      retcode = PlaceOrder((ENUM_ORDER_TYPE)Type, symbol, Volume,
         range, Expiration, Until, Magic);
   }
   else
   {
      retcode = ModifyOrder(ticket, range, Expiration, Until);
   }
   const TRADE_RETCODE_SEVERITY severity = TradeCodeSeverity(retcode);
   if(severity >= SEVERITY_INVALID)
   {
      Alert("Can't place/modify pending order, EA is stopped");
      RetryFrequency = INT_MAX;
   }
   else if(severity >= SEVERITY_RETRY)
   {
      RetryFrequency += (int)sqrt(RetryFrequency + 1);
      RetryRecordTime = TimeTradeServer();
      PrintFormat("Problems detected, waiting for better conditions (timeout enlarged to %d seconds)",
         RetryFrequency);
   }
   else
   {
      if(RetryFrequency > DEFAULT_RETRY_TIMEOUT)
      {
         RetryFrequency = DEFAULT_RETRY_TIMEOUT;
         PrintFormat("Timeout restored to %d second", RetryFrequency);
      }
      lastDay = TimeTradeServer() / DAYLONG * DAYLONG;
   }
}
//+------------------------------------------------------------------+
/*
   example output (XAUUSD, default settings):

   2022.01.03 01:05:00   Autodetected daily range: 14.37
   2022.01.03 01:05:00   buy stop 0.01 XAUUSD at 1845.73 sl: 1838.55 tp: 1852.91 (1830.63 / 1831.36)
   2022.01.03 01:05:00   OK order placed: #=2
   2022.01.03 01:05:00   TRADE_ACTION_PENDING, XAUUSD, ORDER_TYPE_BUY_STOP, V=0.01, ORDER_FILLING_FOK, @ 1845.73, SL=1838.55, TP=1852.91, ORDER_TIME_GTC, M=1234567890
   2022.01.03 01:05:00   DONE, #=2, V=0.01, Bid=1830.63, Ask=1831.36, Request executed
   2022.01.04 01:05:00   Autodetected daily range: 33.5
   2022.01.04 01:05:00   order modified [#2 buy stop 0.01 XAUUSD at 1836.56]
   2022.01.04 01:05:00   OK order modified: #=2
   2022.01.04 01:05:00   TRADE_ACTION_MODIFY, XAUUSD, ORDER_TYPE_BUY_STOP, V=0.01, ORDER_FILLING_FOK, @ 1836.56, SL=1819.81, TP=1853.31, ORDER_TIME_GTC, #=2
   2022.01.04 01:05:00   DONE, #=2, @ 1836.56, Bid=1819.81, Ask=1853.31, Request executed, Req=1
   2022.01.05 01:05:00   Autodetected daily range: 18.23
   2022.01.05 01:05:00   order modified [#2 buy stop 0.01 XAUUSD at 1832.56]
   2022.01.05 01:05:00   OK order modified: #=2
   2022.01.05 01:05:00   TRADE_ACTION_MODIFY, XAUUSD, ORDER_TYPE_BUY_STOP, V=0.01, ORDER_FILLING_FOK, @ 1832.56, SL=1823.45, TP=1841.67, ORDER_TIME_GTC, #=2
   2022.01.05 01:05:00   DONE, #=2, @ 1832.56, Bid=1823.45, Ask=1841.67, Request executed, Req=2
   ...
   2022.01.11 01:05:00   Autodetected daily range: 11.96
   2022.01.11 01:05:00   order modified [#2 buy stop 0.01 XAUUSD at 1812.91]
   2022.01.11 01:05:00   OK order modified: #=2
   2022.01.11 01:05:00   TRADE_ACTION_MODIFY, XAUUSD, ORDER_TYPE_BUY_STOP, V=0.01, ORDER_FILLING_FOK, @ 1812.91, SL=1806.93, TP=1818.89, ORDER_TIME_GTC, #=2
   2022.01.11 01:05:00   DONE, #=2, @ 1812.91, Bid=1806.93, Ask=1818.89, Request executed, Req=6
   2022.01.11 18:10:58   order [#2 buy stop 0.01 XAUUSD at 1812.91] triggered
   2022.01.11 18:10:58   deal #2 buy 0.01 XAUUSD at 1812.91 done (based on order #2)
   2022.01.11 18:10:58   deal performed [#2 buy 0.01 XAUUSD at 1812.91]
   2022.01.11 18:10:58   order performed buy 0.01 at 1812.91 [#2 buy stop 0.01 XAUUSD at 1812.91]
   2022.01.11 20:28:59   take profit triggered #2 buy 0.01 XAUUSD 1812.91 sl: 1806.93 tp: 1818.89 [#3 sell 0.01 XAUUSD at 1818.89]
   2022.01.11 20:28:59   deal #3 sell 0.01 XAUUSD at 1818.91 done (based on order #3)
   2022.01.11 20:28:59   deal performed [#3 sell 0.01 XAUUSD at 1818.91]
   2022.01.11 20:28:59   order performed sell 0.01 at 1818.91 [#3 sell 0.01 XAUUSD at 1818.89]
   2022.01.12 01:05:00   Autodetected daily range: 23.28
   2022.01.12 01:05:00   buy stop 0.01 XAUUSD at 1843.77 sl: 1832.14 tp: 1855.40 (1820.14 / 1820.49)
   2022.01.12 01:05:00   OK order placed: #=4
   2022.01.12 01:05:00   TRADE_ACTION_PENDING, XAUUSD, ORDER_TYPE_BUY_STOP, V=0.01, ORDER_FILLING_FOK, @ 1843.77, SL=1832.14, TP=1855.40, ORDER_TIME_GTC, M=1234567890
   2022.01.12 01:05:00   DONE, #=4, V=0.01, Bid=1820.14, Ask=1820.49, Request executed, Req=7

*/
//+------------------------------------------------------------------+
