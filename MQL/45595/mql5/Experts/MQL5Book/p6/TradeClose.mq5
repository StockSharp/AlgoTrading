//+------------------------------------------------------------------+
//|                                                   TradeClose.mq5 |
//|                                  Copyright 2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright   "2022, MetaQuotes Ltd."
#property link        "https://www.mql5.com"
#property description "Open and then close positions with current symbol and minimal lot according to a simple trending strategy working on per bar basis."

#define SHOW_WARNINGS  // output extended info into the log, with changes in data state
#define WARNING Print  // use simple Print for warnings (instead of a built-in format with line numbers etc.)
#include <MQL5Book/MqlTradeSync.mqh>

input ulong Deviation;
input ulong Magic = 1234567890;

ulong LastErrorCode = 0;

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
//| Helper function to find compatible position                      |
//+------------------------------------------------------------------+
bool GetMyPosition(const string s, const ulong m)
{
   for(int i = 0; i < PositionsTotal(); ++i)
   {
      if(PositionGetSymbol(i) == s && PositionGetInteger(POSITION_MAGIC) == m)
      {
         return true; // one is enough
      }
   }
   return false;
}

//+------------------------------------------------------------------+
//| Prepare MqlTradeRequestSync struct and send it to close position |
//+------------------------------------------------------------------+
ulong ClosePosition(const ulong ticket)
{
   // define the struct
   MqlTradeRequestSync request;

   // fill optional fields of the struct
   request.magic = Magic;
   request.deviation = Deviation;

   ResetLastError();
   // send request and wait for its completion
   if(request.close(ticket) && request.completed())
   {
      Print("OK Close Order/Deal/Position");
   }
   else
   {
      Print(TU::StringOf(request));
      Print(TU::StringOf(request.result));
      LastErrorCode = request.result.retcode;
      return 0; // error
   }
   
   return request.position; // success
}

//+------------------------------------------------------------------+
//| Prepare MqlTradeRequestSync struct and send it to create position|
//+------------------------------------------------------------------+
ulong OpenPosition(const ENUM_ORDER_TYPE type)
{
   // define the struct
   MqlTradeRequestSync request;
   
   // default values
   const bool wantToBuy = type == ORDER_TYPE_BUY;
   const double volume = SymbolInfoDouble(_Symbol, SYMBOL_VOLUME_MIN);

   // fill optional fields
   request.magic = Magic;
   request.deviation = Deviation;

   ResetLastError();
   // format parameters appropriately and send request
   if((bool)(wantToBuy ? request.buy(volume) : request.sell(volume))
      && request.completed())
   {
      Print("OK New Order/Deal/Position");
   }
   else
   {
      Print(TU::StringOf(request));
      Print(TU::StringOf(request.result));
      LastErrorCode = request.result.retcode;
   }
   
   return request.position; // nonzero on success
}

//+------------------------------------------------------------------+
//| Detect trading strategy signals to buy or sell                   |
//+------------------------------------------------------------------+
ENUM_ORDER_TYPE GetTradeDirection()
{
   if(iClose(_Symbol, _Period, 1) > iClose(_Symbol, _Period, 2)
      && iClose(_Symbol, _Period, 2) > iClose(_Symbol, _Period, 3))
   {
      return ORDER_TYPE_BUY; // open buy
   }

   if(iClose(_Symbol, _Period, 1) < iClose(_Symbol, _Period, 2)
      && iClose(_Symbol, _Period, 2) < iClose(_Symbol, _Period, 3))
   {
      return ORDER_TYPE_SELL; // open sell
   }
   
   return (ENUM_ORDER_TYPE)-1; // close all
}

//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
{
   static int errors = 0;
   static const int maxtrials = 10; // no more than this number of attempts per bar
   
   // wait for a new bar
   static datetime lastBar = 0;
   if(iTime(_Symbol, _Period, 0) == lastBar && errors == 0) return;
   lastBar = iTime(_Symbol, _Period, 0);
   
   // get trade signal
   const ENUM_ORDER_TYPE type = GetTradeDirection();
   
   if(GetMyPosition(_Symbol, Magic))
   {
      if(type != ORDER_TYPE_BUY && type != ORDER_TYPE_SELL)
      {
         if(!ClosePosition(PositionGetInteger(POSITION_TICKET)))
         {
            ++errors;
         }
         else
         {
            errors = 0;
         }
      }
   }
   else if(type == ORDER_TYPE_BUY || type == ORDER_TYPE_SELL)
   {
      if(!OpenPosition(type))
      {
         ++errors;
      }
      else
      {
         errors = 0;
      }
   }
   // too many errors per bar
   if(errors >= maxtrials) errors = 0;
   // error require a pause at least
   if(IS_TANGIBLE(LastErrorCode)) errors = 0;
}

//+------------------------------------------------------------------+
/*
   tester log example

   2022.01.03 01:05:00   instant buy 0.01 XAUUSD at 1831.36 (1830.63 / 1831.36)
   2022.01.03 01:05:00   deal #2 buy 0.01 XAUUSD at 1831.36 done (based on order #2)
   2022.01.03 01:05:00   deal performed [#2 buy 0.01 XAUUSD at 1831.36]
   2022.01.03 01:05:00   order performed buy 0.01 at 1831.36 [#2 buy 0.01 XAUUSD at 1831.36]
   2022.01.03 01:05:00   Waiting for position for deal D=2
   2022.01.03 01:05:00   OK New Order/Deal/Position
   2022.01.03 04:00:00   instant sell 0.01 XAUUSD at 1828.86, close #2 (1828.86 / 1829.13)
   2022.01.03 04:00:00   deal #3 sell 0.01 XAUUSD at 1828.86 done (based on order #3)
   2022.01.03 04:00:00   deal performed [#3 sell 0.01 XAUUSD at 1828.86]
   2022.01.03 04:00:00   order performed sell 0.01 at 1828.86 [#3 sell 0.01 XAUUSD at 1828.86]
   2022.01.03 04:00:00   OK Close Order/Deal/Position
   2022.01.03 05:00:01   instant sell 0.01 XAUUSD at 1826.22 (1826.22 / 1826.47)
   2022.01.03 05:00:01   deal #4 sell 0.01 XAUUSD at 1826.22 done (based on order #4)
   2022.01.03 05:00:01   deal performed [#4 sell 0.01 XAUUSD at 1826.22]
   2022.01.03 05:00:01   order performed sell 0.01 at 1826.22 [#4 sell 0.01 XAUUSD at 1826.22]
   2022.01.03 05:00:01   Waiting for position for deal D=4
   2022.01.03 05:00:01   OK New Order/Deal/Position
   2022.01.03 08:00:00   instant buy 0.01 XAUUSD at 1825.66, close #4 (1825.44 / 1825.66)
   2022.01.03 08:00:00   deal #5 buy 0.01 XAUUSD at 1825.66 done (based on order #5)
   2022.01.03 08:00:00   deal performed [#5 buy 0.01 XAUUSD at 1825.66]
   2022.01.03 08:00:00   order performed buy 0.01 at 1825.66 [#5 buy 0.01 XAUUSD at 1825.66]
   2022.01.03 08:00:00   OK Close Order/Deal/Position

*/
//+------------------------------------------------------------------+
