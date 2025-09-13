//+------------------------------------------------------------------+
//|                                             CustomOrderCheck.mq5 |
//|                                  Copyright 2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright   "2022, MetaQuotes Ltd."
#property link        "https://www.mql5.com"
#property description "Construct MqlTradeRequest from user input and call OrderCheck for it."

#include <MQL5Book/PRTF.mqh>
#include <MQL5Book/StructPrint.mqh>
#include <MQL5Book/TradeRetcode.mqh>

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
void OnInit()
{
   // setup timer for postponed execution
   EventSetTimer(1);
}

//+------------------------------------------------------------------+
//| Timer event handler                                              |
//+------------------------------------------------------------------+
void OnTimer()
{
   // once executed do nothing, wait for another setup from user
   EventKillTimer();
   
   // output current account state for reference
   PRTF(AccountInfoDouble(ACCOUNT_EQUITY));
   PRTF(AccountInfoDouble(ACCOUNT_PROFIT));
   PRTF(AccountInfoDouble(ACCOUNT_MARGIN));
   PRTF(AccountInfoDouble(ACCOUNT_MARGIN_FREE));
   PRTF(AccountInfoDouble(ACCOUNT_MARGIN_LEVEL));
   
   // initialize structs with zeros
   MqlTradeRequest request = {};
   MqlTradeCheckResult result = {};
   
   // default values
   const bool kindOfBuy = (Type & 1) == 0 && Type < ORDER_TYPE_CLOSE_BY;
   const string symbol = StringLen(Symbol) == 0 ? _Symbol : Symbol;
   const double volume = Volume == 0 ? SymbolInfoDouble(symbol, SYMBOL_VOLUME_MIN) : Volume;
   const double price = Price == 0 ? SymbolInfoDouble(symbol, kindOfBuy ? SYMBOL_ASK : SYMBOL_BID) : Price;
   
   // fill the struct
   request.action = Action;
   request.magic = Magic;
   request.order = Order;
   request.symbol = symbol;
   request.volume = volume;
   request.price = price;
   request.stoplimit = StopLimit;
   request.sl = SL;
   request.tp = TP;
   request.deviation = Deviation;
   request.type = Type;
   request.type_filling = Filling;
   request.type_time = ExpirationType;
   request.expiration = ExpirationTime;
   request.comment = Comment;
   request.position = Position;
   request.position_by = PositionBy;

   // check-up and print out results
   ResetLastError();
   PRTF(OrderCheck(request, result));
   StructPrint(request, ARRAYPRINT_HEADER);
   Print(TRCSTR(result.retcode));
   StructPrint(result, ARRAYPRINT_HEADER, 2);

   // call OrderCalcMargin for reference
   double margin = 0;
   ResetLastError();
   PRTF(OrderCalcMargin(Type, symbol, volume, price, margin));
   PRTF(margin);
}
//+------------------------------------------------------------------+
/*
   example output:
   
      AccountInfoDouble(ACCOUNT_EQUITY)=15565.22 / ok
      AccountInfoDouble(ACCOUNT_PROFIT)=0.0 / ok
      AccountInfoDouble(ACCOUNT_MARGIN)=0.0 / ok
      AccountInfoDouble(ACCOUNT_MARGIN_FREE)=15565.22 / ok
      AccountInfoDouble(ACCOUNT_MARGIN_LEVEL)=0.0 / ok
      OrderCheck(request,result)=true / ok
      [action] [magic] [order] [symbol] [volume] [price] [stoplimit] [sl] [tp] [deviation] [type] [type_filling] [type_time]        [expiration] [comment] [position] [position_by] [reserved]
             1       0       0 "XAUUSD"     0.01 1899.97        0.00 0.00 0.00           0      0              0           0 1970.01.01 00:00:00 ""                 0             0          0
      OK_0
      [retcode] [balance] [equity] [profit] [margin] [margin_free] [margin_level] [comment] [reserved]
              0  15565.22 15565.22     0.00    19.00      15546.22       81922.21 "Done"             0
      OrderCalcMargin(Type,symbol,volume,price,margin)=true / ok
      margin=19.0 / ok
      
      AccountInfoDouble(ACCOUNT_EQUITY)=9999.540000000001 / ok
      AccountInfoDouble(ACCOUNT_PROFIT)=-0.83 / ok
      AccountInfoDouble(ACCOUNT_MARGIN)=79.22 / ok
      AccountInfoDouble(ACCOUNT_MARGIN_FREE)=9920.32 / ok
      AccountInfoDouble(ACCOUNT_MARGIN_LEVEL)=12622.49431961626 / ok
      OrderCheck(request,result)=true / ok
      [action] [magic] [order]  [symbol] [volume] [price] [stoplimit] [sl] [tp] [deviation] [type] [type_filling] [type_time]        [expiration] [comment] [position] [position_by] [reserved]
             1       0       0 "PLZL.MM"      1.0 12642.0         0.0  0.0  0.0           0      0              0           0 1970.01.01 00:00:00 " "                0             0          0
      OK_0
      [retcode] [balance] [equity] [profit] [margin] [margin_free] [margin_level] [comment] [reserved]
              0  10000.87  9999.54    -0.83   158.26       9841.28        6318.43 "Done"             0
      OrderCalcMargin(Type,symbol,volume,price,margin)=true / ok
      margin=79.04000000000001 / ok

*/
//+------------------------------------------------------------------+
