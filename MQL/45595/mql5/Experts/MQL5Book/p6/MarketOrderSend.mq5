//+------------------------------------------------------------------+
//|                                              MarketOrderSend.mq5 |
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
   example output (default settings: buy minimal lot of current symbol):
   
   OK Order: #=218966930
   Waiting for position for deal D=215494463
   OK Position: P=218966930
   TRADE_ACTION_DEAL, XTIUSD.c, ORDER_TYPE_BUY, V=0.01, ORDER_FILLING_FOK, @ 109.340, P=218966930, M=1234567890
   DONE, D=215494463, #=218966930, V=0.01, @ 109.35, Request executed, Req=8
      
   exampe output (change settings to sell double minimal lot (0.02) - flip position on the netting account):

   OK Order: #=218966932
   Waiting for position for deal D=215494468
   Position ticket <> id: 218966932, 218966930
   OK Position: P=218966932
   TRADE_ACTION_DEAL, XTIUSD.c, ORDER_TYPE_SELL, V=0.02, ORDER_FILLING_FOK, @ 109.390, P=218966932, M=1234567890
   DONE, D=215494468, #=218966932, V=0.02, @ 109.39, Request executed, Req=9

   example output (use Distance2SLTP=500 points):

   OK Order: #=1273913958
   Waiting for position for deal D=1256506526
   OK Position: P=1273913958
   OK Adjust
   TRADE_ACTION_SLTP, EURUSD, ORDER_TYPE_BUY, V=0.01, ORDER_FILLING_FOK, @ 1.10889, SL=1.10389, TP=1.11389, P=1273913958, M=1234567890
   DONE, Bid=1.10389, Ask=1.11389, Request executed, Req=26

*/
//+------------------------------------------------------------------+
