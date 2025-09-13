//+------------------------------------------------------------------+
//|                                                 TradeCloseBy.mq5 |
//|                                  Copyright 2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright   "2022, MetaQuotes Ltd."
#property link        "https://www.mql5.com"
#property description "Open positions on new bars according to current trend continuation. Keep up to predefined number of positions, then close excessive ones by 'close by' operation."

#define PUSH(A,V) (A[ArrayResize(A, ArraySize(A) + 1, ArraySize(A) * 2) - 1] = V)

#define SHOW_WARNINGS  // output extended info into the log, with changes in data state
#define WARNING Print  // use simple Print for warnings (instead of a built-in format with line numbers etc.)
#include <MQL5Book/MqlTradeSync.mqh>

input uint PositionLimit = 5;
input ulong Deviation;
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
   
   if(AccountInfoInteger(ACCOUNT_MARGIN_MODE) != ACCOUNT_MARGIN_MODE_RETAIL_HEDGING)
   {
      Alert("An account with hedging is required for this EA!");
      return INIT_FAILED;
   }
   
   if((SymbolInfoInteger(_Symbol, SYMBOL_ORDER_MODE) & SYMBOL_ORDER_CLOSEBY) == 0)
   {
      Alert("'Close By' mode is not supported for ", _Symbol);
      return INIT_FAILED;
   }
   
   return INIT_SUCCEEDED;
}

//+------------------------------------------------------------------+
//| Helper function to find compatible positions                     |
//+------------------------------------------------------------------+
int GetMyPositions(const string s, const ulong m, ulong &ticketsLong[], ulong &ticketsShort[])
{
   for(int i = 0; i < PositionsTotal(); ++i)
   {
      if(PositionGetSymbol(i) == s && PositionGetInteger(POSITION_MAGIC) == m)
      {
         if((ENUM_POSITION_TYPE)PositionGetInteger(POSITION_TYPE) == POSITION_TYPE_BUY)
            PUSH(ticketsLong, PositionGetInteger(POSITION_TICKET));
         else
            PUSH(ticketsShort, PositionGetInteger(POSITION_TICKET));
      }
   }
   
   const int min = fmin(ArraySize(ticketsLong), ArraySize(ticketsShort));
   if(min == 0) return -fmax(ArraySize(ticketsLong), ArraySize(ticketsShort));
   return min;
}

//+------------------------------------------------------------------+
//| Prepare MqlTradeRequestSync struct and send it to close position |
//+------------------------------------------------------------------+
bool CloseByPosition(const ulong ticket1, const ulong ticket2)
{
   // define the struct
   MqlTradeRequestSync request;

   // fill optional fields
   request.magic = Magic;

   ResetLastError();
   // send request and wait for its completion
   if(request.closeby(ticket1, ticket2))
   {
      Print("Positions collapse initiated");
      if(request.completed())
      {
         Print("OK CloseBy Order/Deal/Position");
         return true; // success
      }
   }

   Print(TU::StringOf(request));
   Print(TU::StringOf(request.result));
   
   return false; // error
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
   }
   
   return request.position; // nonzero on success
}

//+------------------------------------------------------------------+
//| Detect trading strategy signals to buy or sell                   |
//+------------------------------------------------------------------+
ENUM_ORDER_TYPE GetTradeDirection()
{
   if(iClose(_Symbol, _Period, 1) > iClose(_Symbol, _Period, 2)
      && iClose(_Symbol, _Period, 2) > iClose(_Symbol, _Period, 3)
      /* // uncomment this for more strict strategy: only bullish candles here
      && iClose(_Symbol, _Period, 2) >= iOpen(_Symbol, _Period, 2)
      && iClose(_Symbol, _Period, 1) >= iOpen(_Symbol, _Period, 1)*/)
   {
      return ORDER_TYPE_BUY; // open buy
   }

   if(iClose(_Symbol, _Period, 1) < iClose(_Symbol, _Period, 2)
      && iClose(_Symbol, _Period, 2) < iClose(_Symbol, _Period, 3)
      /* // uncomment this for more strict strategy: only bearish candles here
      && iClose(_Symbol, _Period, 2) <= iOpen(_Symbol, _Period, 2)
      && iClose(_Symbol, _Period, 1) <= iOpen(_Symbol, _Period, 1)*/)
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
   static bool error = false;

   // wait for a new bar
   static datetime lastBar = 0;
   if(iTime(_Symbol, _Period, 0) == lastBar && !error) return;
   lastBar = iTime(_Symbol, _Period, 0);
   
   // get trade signal
   const ENUM_ORDER_TYPE type = GetTradeDirection();
   
   ulong ticketsLong[], ticketsShort[];
   const int n = GetMyPositions(_Symbol, Magic, ticketsLong, ticketsShort);
   if(n > 0)
   {
      for(int i = 0; i < n; ++i)
      {
         error = !CloseByPosition(ticketsShort[i], ticketsLong[i]) && error;
      }
   }
   else if(type == ORDER_TYPE_BUY || type == ORDER_TYPE_SELL)
   {
      error = !OpenPosition(type);
   }
   else if(n < 0)
   {
      if(-n >= (int)PositionLimit) // open opposite position to close by at next bar
      {
         if(ArraySize(ticketsLong) > 0)
         {
            error = !OpenPosition(ORDER_TYPE_SELL);
         }
         else // (ArraySize(ticketsShort) > 0)
         {
            error = !OpenPosition(ORDER_TYPE_BUY);
         }
      }
   }
}
//+------------------------------------------------------------------+
/*
   tester log example
   
   2022.01.03 01:05:00   instant buy 0.01 XAUUSD at 1831.13 (1830.63 / 1831.13 / 1830.63)
   2022.01.03 01:05:00   deal #2 buy 0.01 XAUUSD at 1831.13 done (based on order #2)
   2022.01.03 01:05:00   deal performed [#2 buy 0.01 XAUUSD at 1831.13]
   2022.01.03 01:05:00   order performed buy 0.01 at 1831.13 [#2 buy 0.01 XAUUSD at 1831.13]
   2022.01.03 01:05:00   Waiting for position for deal D=2
   2022.01.03 01:05:00   OK New Order/Deal/Position
   2022.01.03 02:00:00   instant buy 0.01 XAUUSD at 1828.77 (1828.47 / 1828.77 / 1828.47)
   2022.01.03 02:00:00   deal #3 buy 0.01 XAUUSD at 1828.77 done (based on order #3)
   2022.01.03 02:00:00   deal performed [#3 buy 0.01 XAUUSD at 1828.77]
   2022.01.03 02:00:00   order performed buy 0.01 at 1828.77 [#3 buy 0.01 XAUUSD at 1828.77]
   2022.01.03 02:00:00   Waiting for position for deal D=3
   2022.01.03 02:00:00   OK New Order/Deal/Position
   2022.01.03 03:00:00   instant buy 0.01 XAUUSD at 1830.40 (1830.16 / 1830.40 / 1830.16)
   2022.01.03 03:00:00   deal #4 buy 0.01 XAUUSD at 1830.40 done (based on order #4)
   2022.01.03 03:00:00   deal performed [#4 buy 0.01 XAUUSD at 1830.40]
   2022.01.03 03:00:00   order performed buy 0.01 at 1830.40 [#4 buy 0.01 XAUUSD at 1830.40]
   2022.01.03 03:00:00   Waiting for position for deal D=4
   2022.01.03 03:00:00   OK New Order/Deal/Position
   2022.01.03 05:00:00   instant sell 0.01 XAUUSD at 1826.22 (1826.22 / 1826.45 / 1826.22)
   2022.01.03 05:00:00   deal #5 sell 0.01 XAUUSD at 1826.22 done (based on order #5)
   2022.01.03 05:00:00   deal performed [#5 sell 0.01 XAUUSD at 1826.22]
   2022.01.03 05:00:00   order performed sell 0.01 at 1826.22 [#5 sell 0.01 XAUUSD at 1826.22]
   2022.01.03 05:00:00   Waiting for position for deal D=5
   2022.01.03 05:00:00   OK New Order/Deal/Position
   2022.01.03 06:00:00   close position #5 sell 0.01 XAUUSD by position #2 buy 0.01 XAUUSD (1825.64 / 1825.86 / 1825.64)
   2022.01.03 06:00:00   deal #6 buy 0.01 XAUUSD at 1831.13 done (based on order #6)
   2022.01.03 06:00:00   deal #7 sell 0.01 XAUUSD at 1826.22 done (based on order #6)
   2022.01.03 06:00:00   Positions collapse initiated
   2022.01.03 06:00:00   OK CloseBy Order/Deal/Position

*/
//+------------------------------------------------------------------+
