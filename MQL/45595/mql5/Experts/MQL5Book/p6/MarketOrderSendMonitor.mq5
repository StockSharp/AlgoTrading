//+------------------------------------------------------------------+
//|                                       MarketOrderSendMonitor.mq5 |
//|                                  Copyright 2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright   "2022, MetaQuotes Ltd."
#property link        "https://www.mql5.com"
#property description "Construct MqlTradeRequest from user input and make buy/sell (TRADE_ACTION_DEAL) via OrderSend."
#property description "Stop Loss and Take Profit can be set optionally via additional request (TRADE_ACTION_SLTP) to modify open position."

#define SHOW_WARNINGS  // output extended info into the log, with changes in data state
#define WARNING Print  // use simple Print for warnings (instead of a built-in format with line numbers etc.)
#include <MQL5Book/MqlTradeSync.mqh>
#include <MQL5Book/OrderMonitor.mqh>

enum ENUM_ORDER_TYPE_MARKET
{
   MARKET_BUY = ORDER_TYPE_BUY,  // ORDER_TYPE_BUY
   MARKET_SELL = ORDER_TYPE_SELL // ORDER_TYPE_SELL
};

input string Symbol;             // Symbol (empty = current _Symbol)
input double Volume;             // Volume (0 = minimal lot)
input double Price;              // Price (0 = current Bid/Ask)
input ENUM_ORDER_TYPE_MARKET Type;
input string Comment;
input ulong Magic = 1234567890;
input ulong Deviation;
input int Distance2SLTP = 0;     // Distance to SL/TP in points (0 = no)

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
   
   // default values
   const bool wantToBuy = Type == MARKET_BUY;
   const string symbol = StringLen(Symbol) == 0 ? _Symbol : Symbol;
   const double volume = Volume == 0 ? SymbolInfoDouble(symbol, SYMBOL_VOLUME_MIN) : Volume;

   // define the struct
   MqlTradeRequestSync request(symbol);

   // fill optional fields of the struct
   request.magic = Magic;
   request.deviation = Deviation;
   request.comment = Comment;

   // send request and wait for results
   ResetLastError();
   const ulong order = (wantToBuy ?
      request.buy(volume, Price) :  // we could pass SL, TP here
      request.sell(volume, Price)); // we could pass SL, TP here
   if(order != 0)
   {
      Print("OK Order: #=", order);
      if(request.completed()) // waiting for position for market buy/sell
      {
         Print("OK Position: P=", request.result.position);
         
         OrderMonitor m(order);
         m.print();
         
         if(Distance2SLTP != 0)
         {
            // position is already selected inside 'complete' call,
            // but you could confirm it in a demonstrative way
            // PositionSelectByTicket(request.result.position);
            
            const double price = PositionGetDouble(POSITION_PRICE_OPEN);
            const double point = SymbolInfoDouble(symbol, SYMBOL_POINT);
            TU::TradeDirection dir((ENUM_ORDER_TYPE)Type);
            const double SL = dir.negative(price, Distance2SLTP * point);
            const double TP = dir.positive(price, Distance2SLTP * point);
            if(request.adjust(SL, TP) && request.completed())
            {
               Print("OK Adjust");
            }
         }
      }
   }
   Print(TU::StringOf(request));
   Print(TU::StringOf(request.result));
}
//+------------------------------------------------------------------+
/*
   example output (default settings):
   
   OK Order: #=1287846602
   Waiting for position for deal D=1270417032
   OK Position: P=1287846602
   MonitorInterface<ENUM_ORDER_PROPERTY_INTEGER,ENUM_ORDER_PROPERTY_DOUBLE,ENUM_ORDER_PROPERTY_STRING>
   ENUM_ORDER_PROPERTY_INTEGER Count=14
     0 ORDER_TIME_SETUP=2022.03.21 13:28:59
     1 ORDER_TIME_EXPIRATION=1970.01.01 00:00:00
     2 ORDER_TIME_DONE=2022.03.21 13:28:59
     3 ORDER_TYPE=ORDER_TYPE_BUY
     4 ORDER_TYPE_FILLING=ORDER_FILLING_FOK
     5 ORDER_TYPE_TIME=ORDER_TIME_GTC
     6 ORDER_STATE=ORDER_STATE_FILLED
     7 ORDER_MAGIC=1234567890
     8 ORDER_POSITION_ID=1287846602
     9 ORDER_TIME_SETUP_MSC=2022.03.21 13:28:59'572
    10 ORDER_TIME_DONE_MSC=2022.03.21 13:28:59'572
    11 ORDER_POSITION_BY_ID=0
    12 ORDER_TICKET=1287846602
    13 ORDER_REASON=ORDER_REASON_EXPERT
   ENUM_ORDER_PROPERTY_DOUBLE Count=7
     0 ORDER_VOLUME_INITIAL=0.01
     1 ORDER_VOLUME_CURRENT=0.0
     2 ORDER_PRICE_OPEN=1.10275
     3 ORDER_PRICE_CURRENT=1.10275
     4 ORDER_PRICE_STOPLIMIT=0.0
     5 ORDER_SL=0.0
     6 ORDER_TP=0.0
   ENUM_ORDER_PROPERTY_STRING Count=3
     0 ORDER_SYMBOL=EURUSD
     1 ORDER_COMMENT=
     2 ORDER_EXTERNAL_ID=
   TRADE_ACTION_DEAL, EURUSD, ORDER_TYPE_BUY, V=0.01, ORDER_FILLING_FOK, @ 1.10275, P=1287846602, M=1234567890
   DONE, D=1270417032, #=1287846602, V=0.01, @ 1.10275, Bid=1.10275, Ask=1.10275, Request executed, Req=3

*/
//+------------------------------------------------------------------+
