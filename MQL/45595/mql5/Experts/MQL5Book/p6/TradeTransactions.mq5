//+------------------------------------------------------------------+
//|                                            TradeTransactions.mq5 |
//|                                  Copyright 2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright   "2022, MetaQuotes Ltd."
#property link        "https://www.mql5.com"
#property description "Output trade transaction in the log"

#include <MQL5Book/TradeUtils.mqh>
#include <MQL5Book/OrderMonitor.mqh>
#include <MQL5Book/DealMonitor.mqh>
#include <MQL5Book/PositionMonitor.mqh>

input bool DetailedLog = false; // DetailedLog ('true' shows order/deal/position details)

//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
{
   return INIT_SUCCEEDED;
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
   }
   
   if(DetailedLog)
   {
      if(transaction.order != 0)
      {
         OrderMonitor m(transaction.order);
         m.print();
      }
      if(transaction.deal != 0)
      {
         DealMonitor m(transaction.deal);
         m.print();
      }
      if(transaction.position != 0)
      {
         PositionMonitor m(transaction.position);
         m.print();
      }
   }
}

//+------------------------------------------------------------------+
/*
   example output (default settings, manual trading):

   [open long position 0.01 EURUSD]
   
   >>>      1
   TRADE_TRANSACTION_ORDER_ADD, #=1296991463(ORDER_TYPE_BUY/ORDER_STATE_STARTED), EURUSD, @ 1.10947, V=0.01
   >>>      2
   TRADE_TRANSACTION_DEAL_ADD, D=1279627746(DEAL_TYPE_BUY), #=1296991463(ORDER_TYPE_BUY/ORDER_STATE_STARTED), EURUSD, @ 1.10947, V=0.01, P=1296991463
   >>>      3
   TRADE_TRANSACTION_ORDER_DELETE, #=1296991463(ORDER_TYPE_BUY/ORDER_STATE_FILLED), EURUSD, @ 1.10947, P=1296991463
   >>>      4
   TRADE_TRANSACTION_HISTORY_ADD, #=1296991463(ORDER_TYPE_BUY/ORDER_STATE_FILLED), EURUSD, @ 1.10947, P=1296991463
   >>>      5
   TRADE_TRANSACTION_REQUEST
   TRADE_ACTION_DEAL, EURUSD, ORDER_TYPE_BUY, V=0.01, ORDER_FILLING_FOK, @ 1.10947, #=1296991463
   DONE, D=1279627746, #=1296991463, V=0.01, @ 1.10947, Bid=1.10947, Ask=1.10947, Req=7
   
   [sell 0.02 EURUSD]
   
   >>>      6
   TRADE_TRANSACTION_ORDER_ADD, #=1296992157(ORDER_TYPE_SELL/ORDER_STATE_STARTED), EURUSD, @ 1.10964, V=0.02
   >>>      7
   TRADE_TRANSACTION_DEAL_ADD, D=1279628463(DEAL_TYPE_SELL), #=1296992157(ORDER_TYPE_BUY/ORDER_STATE_STARTED), EURUSD, @ 1.10964, V=0.02, P=1296992157
   >>>      8
   TRADE_TRANSACTION_ORDER_DELETE, #=1296992157(ORDER_TYPE_SELL/ORDER_STATE_FILLED), EURUSD, @ 1.10964, P=1296992157
   >>>      9
   TRADE_TRANSACTION_HISTORY_ADD, #=1296992157(ORDER_TYPE_SELL/ORDER_STATE_FILLED), EURUSD, @ 1.10964, P=1296992157
   >>>     10
   TRADE_TRANSACTION_REQUEST
   TRADE_ACTION_DEAL, EURUSD, ORDER_TYPE_SELL, V=0.02, ORDER_FILLING_FOK, @ 1.10964, #=1296992157
   DONE, D=1279628463, #=1296992157, V=0.02, @ 1.10964, Bid=1.10964, Ask=1.10964, Req=8
   
   [close by 2 positions - short position for 0.01 EURUSD will remain]
   
   >>>     11
   TRADE_TRANSACTION_ORDER_ADD, #=1296992548(ORDER_TYPE_CLOSE_BY/ORDER_STATE_STARTED), EURUSD, @ 1.10964, V=0.01, P=1296991463, b=1296992157
   >>>     12
   TRADE_TRANSACTION_DEAL_ADD, D=1279628878(DEAL_TYPE_SELL), #=1296992548(ORDER_TYPE_BUY/ORDER_STATE_STARTED), EURUSD, @ 1.10964, V=0.01, P=1296991463
   >>>     13
   TRADE_TRANSACTION_POSITION, EURUSD, @ 1.10947, P=1296991463
   >>>     14
   TRADE_TRANSACTION_DEAL_ADD, D=1279628879(DEAL_TYPE_BUY), #=1296992548(ORDER_TYPE_BUY/ORDER_STATE_STARTED), EURUSD, @ 1.10947, V=0.01, P=1296992157
   >>>     15
   TRADE_TRANSACTION_ORDER_DELETE, #=1296992548(ORDER_TYPE_CLOSE_BY/ORDER_STATE_FILLED), EURUSD, @ 1.10964, P=1296991463, b=1296992157
   >>>     16
   TRADE_TRANSACTION_HISTORY_ADD, #=1296992548(ORDER_TYPE_CLOSE_BY/ORDER_STATE_FILLED), EURUSD, @ 1.10964, P=1296991463, b=1296992157
   >>>     17
   TRADE_TRANSACTION_REQUEST
   TRADE_ACTION_CLOSE_BY, EURUSD, ORDER_TYPE_BUY, V=0.01, ORDER_FILLING_FOK, #=1296992548, P=1296991463, b=1296992157
   DONE, D=1279628878, #=1296992548, V=0.01, @ 1.10964, Bid=1.10961, Ask=1.10965, Req=9
   
   [close position]
   
   >>>     18
   TRADE_TRANSACTION_ORDER_ADD, #=1297002683(ORDER_TYPE_BUY/ORDER_STATE_STARTED), EURUSD, @ 1.10964, V=0.01, P=1296992157
   >>>     19
   TRADE_TRANSACTION_ORDER_DELETE, #=1297002683(ORDER_TYPE_BUY/ORDER_STATE_FILLED), EURUSD, @ 1.10964, P=1296992157
   >>>     20
   TRADE_TRANSACTION_HISTORY_ADD, #=1297002683(ORDER_TYPE_BUY/ORDER_STATE_FILLED), EURUSD, @ 1.10964, P=1296992157
   >>>     21
   TRADE_TRANSACTION_DEAL_ADD, D=1279639132(DEAL_TYPE_BUY), #=1297002683(ORDER_TYPE_BUY/ORDER_STATE_STARTED), EURUSD, @ 1.10964, V=0.01, P=1296992157
   >>>     22
   TRADE_TRANSACTION_REQUEST
   TRADE_ACTION_DEAL, EURUSD, ORDER_TYPE_BUY, V=0.01, ORDER_FILLING_FOK, @ 1.10964, #=1297002683, P=1296992157
   DONE, D=1279639132, #=1297002683, V=0.01, @ 1.10964, Bid=1.10964, Ask=1.10964, Req=10
   
*/
//+------------------------------------------------------------------+
