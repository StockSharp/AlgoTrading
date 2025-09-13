//+------------------------------------------------------------------+
//|                                                 TrailingStop.mq5 |
//|                                  Copyright 2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright   "2022, MetaQuotes Ltd."
#property link        "https://www.mql5.com"
#property description "Open new position with minimal lot on current symbol (if not exist), then monitor and move stop loss according to specified or daily range."

#define SHOW_WARNINGS  // output extended info into the log, with changes in data state
#define WARNING Print  // use simple Print for warnings (instead of a built-in format with line numbers etc.)
#include <MQL5Book/MqlTradeSync.mqh>
#include <MQL5Book/AutoPtr.mqh>
#include <MQL5Book/TrailingStop.mqh>

enum ENUM_ORDER_TYPE_MARKET
{
   MARKET_BUY = ORDER_TYPE_BUY,   // ORDER_TYPE_BUY
   MARKET_SELL = ORDER_TYPE_SELL  // ORDER_TYPE_SELL
};

input int TrailingDistance = 0;   // Distance to Stop Loss in points (0 = autodetect)
input int TrailingStep = 10;      // Trailing Step in points
input int MATrailingPeriod = 0;   // Period for Trailing by MA (0 = disabled)
input ENUM_ORDER_TYPE_MARKET Type;
input string Comment;
input ulong Deviation;
input ulong Magic = 1234567890;

AutoPtr<TrailingStop> tr;

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
//| Helper function to find a compatible position                    |
//+------------------------------------------------------------------+
bool GetMyPosition(const string s, const ulong m)
{
   for(int i = 0; i < PositionsTotal(); ++i)
   {
      if(PositionGetSymbol(i) == s && PositionGetInteger(POSITION_MAGIC) == m)
      {
         return true;
      }
   }
   return false;
}

//+------------------------------------------------------------------+
//| Prepare MqlTradeRequestSync struct, send it to create a position |
//+------------------------------------------------------------------+
ulong OpenPosition()
{
   // define the struct
   MqlTradeRequestSync request;
   
   // default values
   const bool wantToBuy = Type == MARKET_BUY;
   const double volume = SymbolInfoDouble(_Symbol, SYMBOL_VOLUME_MIN);

   // fill optional fields of the struct
   request.magic = Magic;
   request.deviation = Deviation;
   request.comment = Comment;

   ResetLastError();
   // execute a trade and wait its completion
   if((bool)(wantToBuy ? request.buy(volume) : request.sell(volume))
      && request.completed())
   {
      Print("OK Order/Deal/Position");
   }
   
   return request.position;
}

//+------------------------------------------------------------------+
//| Start trailing existing position or create a new one             |
//+------------------------------------------------------------------+
void Setup()
{
   int distance = 0;
   const double point = SymbolInfoDouble(_Symbol, SYMBOL_POINT);
   
   if(MATrailingPeriod == 0)
   {
      if(TrailingDistance == 0) // autodetect daily range
      {
         distance = (int)((iHigh(_Symbol, PERIOD_D1, 1) - iLow(_Symbol, PERIOD_D1, 1))
            / point / 2);
         Print("Autodetected daily distance (points): ", distance);
      }
      else
      {
         distance = TrailingDistance;
      }
   }

   // only process positions on current symbol and our magic id
   if(GetMyPosition(_Symbol, Magic))
   {
      const ulong ticket = PositionGetInteger(POSITION_TICKET);
      Print("The next position found: ", ticket);
      tr = MATrailingPeriod > 0 ?
         new TrailingStopByMA(ticket, MATrailingPeriod) :
         new TrailingStop(ticket, distance, TrailingStep);
   }
   else // no our position
   {
      Print("No positions found, lets open it...");
      const ulong ticket = OpenPosition();
      if(ticket)
      {
         tr = MATrailingPeriod > 0 ?
            new TrailingStopByMA(ticket, MATrailingPeriod) :
            new TrailingStop(ticket, distance, TrailingStep);
      }
   }
   
   if(tr[] != NULL)
   {
      tr[].trail(); // 1-st time trail after position creation or acqusition
   }
}

//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
{
   if(tr[] == NULL || !tr[].trail())
   {
      // if there is no tracked position yet, create it or find among existing
      Setup();
   }
}
//+------------------------------------------------------------------+
/*
   example output (default settings, tester)
   
   2022.01.10 00:02:00   Autodetected daily distance (points): 373
   2022.01.10 00:02:00   No positions found, lets open it...
   2022.01.10 00:02:00   instant buy 0.01 EURUSD at 1.13612 (1.13550 / 1.13612 / 1.13550)
   2022.01.10 00:02:00   deal #2 buy 0.01 EURUSD at 1.13612 done (based on order #2)
   2022.01.10 00:02:00   deal performed [#2 buy 0.01 EURUSD at 1.13612]
   2022.01.10 00:02:00   order performed buy 0.01 at 1.13612 [#2 buy 0.01 EURUSD at 1.13612]
   2022.01.10 00:02:00   Waiting for position for deal D=2
   2022.01.10 00:02:00   OK Order/Deal/Position
   2022.01.10 00:02:00   Initial SL: 1.131770
   2022.01.10 00:02:00   position modified [#2 buy 0.01 EURUSD 1.13612 sl: 1.13177]
   2022.01.10 00:02:00   OK Trailing: 1.13177
   2022.01.10 00:06:13   SL: 1.131770 -> 1.131880
   2022.01.10 00:06:13   position modified [#2 buy 0.01 EURUSD 1.13612 sl: 1.13188]
   2022.01.10 00:06:13   OK Trailing: 1.13188
   2022.01.10 00:09:17   SL: 1.131880 -> 1.131990
   2022.01.10 00:09:17   position modified [#2 buy 0.01 EURUSD 1.13612 sl: 1.13199]
   2022.01.10 00:09:17   OK Trailing: 1.13199
   2022.01.10 00:09:26   SL: 1.131990 -> 1.132110
   2022.01.10 00:09:26   position modified [#2 buy 0.01 EURUSD 1.13612 sl: 1.13211]
   2022.01.10 00:09:26   OK Trailing: 1.13211
   2022.01.10 00:09:35   SL: 1.132110 -> 1.132240
   2022.01.10 00:09:35   position modified [#2 buy 0.01 EURUSD 1.13612 sl: 1.13224]
   2022.01.10 00:09:35   OK Trailing: 1.13224
   2022.01.10 10:06:38   stop loss triggered #2 buy 0.01 EURUSD 1.13612 sl: 1.13224 [#3 sell 0.01 EURUSD at 1.13224]
   2022.01.10 10:06:38   deal #3 sell 0.01 EURUSD at 1.13221 done (based on order #3)
   2022.01.10 10:06:38   deal performed [#3 sell 0.01 EURUSD at 1.13221]
   2022.01.10 10:06:38   order performed sell 0.01 at 1.13221 [#3 sell 0.01 EURUSD at 1.13224]
   2022.01.10 10:06:38   Autodetected daily distance (points): 373
   2022.01.10 10:06:38   No positions found, lets open it...

*/
//+------------------------------------------------------------------+
