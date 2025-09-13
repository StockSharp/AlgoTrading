//+------------------------------------------------------------------+
//|                                        OrderSendTransaction2.mq5 |
//|                                  Copyright 2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright   "2022, MetaQuotes Ltd."
#property link        "https://www.mql5.com"
#property description "Send 3 trade requests (open, adjust sl/tp, close) chained by transaction events."

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
//| Global variables to track requests in async manner               |
//| (NB: most straightforward and routine way)                       |
//| see OrderSendTransaction3.mq5 for a workaround solution          |
//+------------------------------------------------------------------+
uint RequestID = 0;
ulong PositionTicket = 0;

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
   
   MqlTradeRequestSync::AsyncEnabled = true;

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
   // send TRADE_ACTION_DEAL request
   const ulong order = (Type == MARKET_BUY ?
      request.buy(volume) : 
      request.sell(volume));
   if(order) // this is actually request_id in async mode
   {
      Print("OK Open?");
      RequestID = request.result.request_id;
   }
   else
   {
      Print("Failed Open");
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
      
      if(result.request_id == RequestID)
      {
         MqlTradeRequestSync next;
         next.magic = Magic;
         next.deviation = Deviation;
         
         if(request.action == TRADE_ACTION_DEAL)
         {
            if(PositionTicket == 0) // did't have a position so far, it was opening
            {
               // TODO: handle other errors!
               if(next.requote(result.retcode))
               {
                  // repeat sending on requotes
                  OnTimer();
                  return;
               }
               // analyse results obtained from transaction
               if(!HistoryOrderSelect(result.order))
               {
                  Print("Can't select order in history");
                  RequestID = 0;
                  return;
               }
               const ulong posid = HistoryOrderGetInteger(result.order, ORDER_POSITION_ID);
               PositionTicket = TU::PositionSelectById(posid);
               
               if(!PositionTicket)
               {
                  Print("Can't find position by ticket");
                  RequestID = 0;
                  return;
               }
               
               const double price = PositionGetDouble(POSITION_PRICE_OPEN);
               const double point = SymbolInfoDouble(_Symbol, SYMBOL_POINT);
               TU::TradeDirection dir((ENUM_ORDER_TYPE)Type);
               const double SL = dir.negative(price, Distance2SLTP * point);
               const double TP = dir.positive(price, Distance2SLTP * point);
               // send TRADE_ACTION_SLTP request
               if(next.adjust(PositionTicket, SL, TP))
               {
                  Print("OK Adjust?");
                  RequestID = next.result.request_id;
               }
               else
               {
                  Print("Failed Adjust");
                  RequestID = 0;
               }
            }
            else // already have a position, it means it was a close
            {
               if(!PositionSelectByTicket(PositionTicket))
               {
                  Print("Finish");
                  RequestID = 0;
                  PositionTicket = 0;
               }
               else
               {
                  if(next.requote(result.retcode))
                  {
                     // TODO: repeat closing on requotes
                  }
               }
            }
         }
         else if(request.action == TRADE_ACTION_SLTP)
         {
            // send another TRADE_ACTION_DEAL request - this time to close position
            if(next.close(PositionTicket))
            {
               Print("OK Close?");
               RequestID = next.result.request_id;
            }
            else
            {
               PrintFormat("Failed Close %lld", PositionTicket);
            }
         }
      }
   }
}

//+------------------------------------------------------------------+
/*
   example output (default settings) :
   
   Start trade
   OK Open?
   >>>     1
   TRADE_TRANSACTION_ORDER_ADD, #=1299508203(ORDER_TYPE_BUY/ORDER_STATE_STARTED), EURUSD, @ 1.10640, V=0.01
   >>>     2
   TRADE_TRANSACTION_DEAL_ADD, D=1282135720(DEAL_TYPE_BUY), #=1299508203(ORDER_TYPE_BUY/ORDER_STATE_STARTED), EURUSD, @ 1.10640, V=0.01, P=1299508203
   >>>     3
   TRADE_TRANSACTION_ORDER_DELETE, #=1299508203(ORDER_TYPE_BUY/ORDER_STATE_FILLED), EURUSD, @ 1.10640, P=1299508203
   >>>     4
   TRADE_TRANSACTION_HISTORY_ADD, #=1299508203(ORDER_TYPE_BUY/ORDER_STATE_FILLED), EURUSD, @ 1.10640, P=1299508203
   >>>     5
   TRADE_TRANSACTION_REQUEST
   TRADE_ACTION_DEAL, EURUSD, ORDER_TYPE_BUY, V=0.01, ORDER_FILLING_FOK, @ 1.10640, D=10, #=1299508203, M=1234567890
   DONE, D=1282135720, #=1299508203, V=0.01, @ 1.1064, Bid=1.1064, Ask=1.1064, Req=7
   OK Adjust?
   >>>     6
   TRADE_TRANSACTION_POSITION, EURUSD, @ 1.10640, SL=1.09640, TP=1.11640, V=0.01, P=1299508203
   >>>     7
   TRADE_TRANSACTION_REQUEST
   TRADE_ACTION_SLTP, EURUSD, ORDER_TYPE_BUY, V=0.01, ORDER_FILLING_FOK, SL=1.09640, TP=1.11640, D=10, P=1299508203, M=1234567890
   DONE, Req=8
   OK Close?
   >>>     8
   TRADE_TRANSACTION_ORDER_ADD, #=1299508215(ORDER_TYPE_SELL/ORDER_STATE_STARTED), EURUSD, @ 1.10638, V=0.01, P=1299508203
   >>>     9
   TRADE_TRANSACTION_ORDER_DELETE, #=1299508215(ORDER_TYPE_SELL/ORDER_STATE_FILLED), EURUSD, @ 1.10638, P=1299508203
   >>>    10
   TRADE_TRANSACTION_HISTORY_ADD, #=1299508215(ORDER_TYPE_SELL/ORDER_STATE_FILLED), EURUSD, @ 1.10638, P=1299508203
   >>>    11
   TRADE_TRANSACTION_DEAL_ADD, D=1282135730(DEAL_TYPE_SELL), #=1299508215(ORDER_TYPE_BUY/ORDER_STATE_STARTED), EURUSD, @ 1.10638, SL=1.09640, TP=1.11640, V=0.01, P=1299508203
   >>>    12
   TRADE_TRANSACTION_REQUEST
   TRADE_ACTION_DEAL, EURUSD, ORDER_TYPE_SELL, V=0.01, ORDER_FILLING_FOK, @ 1.10638, D=10, #=1299508215, P=1299508203, M=1234567890
   DONE, D=1282135730, #=1299508215, V=0.01, @ 1.10638, Bid=1.10638, Ask=1.10638, Req=9
   Finish
   
*/
//+------------------------------------------------------------------+
