//+------------------------------------------------------------------+
//|                                                         OCO2.mq5 |
//|                                  Copyright 2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright   "2022, MetaQuotes Ltd."
#property link        "https://www.mql5.com"
#property description "Implemenation of OCO (One Cancels Other) strategy with 2 pending stop orders."

#include <MQL5Book/OrderFilter.mqh>
#include <MQL5Book/PositionFilter.mqh>
#include <MQL5Book/SymbolMonitor.mqh>

#define SHOW_WARNINGS  // output extended info into the log, with changes in data state
#define WARNING Print  // use simple Print for warnings (instead of a built-in format with line numbers etc.)
#include <MQL5Book/MqlTradeSync.mqh>

enum EVENT_TYPE
{
   ON_TRANSACTION, // OnTradeTransaction
   ON_TRADE        // OnTrade
};

input double Volume;            // Volume (0 - minimal lot)
input uint Distance2SLTP = 500; // Distance Indent/SL/TP (points)
input ulong Magic = 1234567890;
input ulong Deviation = 10;
input ulong Expiration = 0;     // Expiration (seconds in future, 3600 - 1 hour, etc)
input EVENT_TYPE ActivationBy = ON_TRANSACTION;

//+------------------------------------------------------------------+
//| Custom struct for implicit initialization of many fields         |
//+------------------------------------------------------------------+
struct MqlTradeRequestSyncOCO: public MqlTradeRequestSync
{
   MqlTradeRequestSyncOCO()
   {
      symbol = _Symbol;
      magic = Magic;
      deviation = Deviation;
      if(Expiration > 0)
      {
         type_time = ORDER_TIME_SPECIFIED;
         expiration = (datetime)(TimeCurrent() + Expiration);
      }
   }
};

//+------------------------------------------------------------------+
//| Global variables to track trade environment                      |
//+------------------------------------------------------------------+
OrderFilter orders;        // helper order finder
PositionFilter trades;     // helper position finder
bool FirstTick = false;    // process OnTick only once
ulong ExecutionCount = 0;  // how many times RunStrategy() was called

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

   FirstTick = true;
   
   orders.let(ORDER_MAGIC, Magic).let(ORDER_SYMBOL, _Symbol)
      .let(ORDER_TYPE, (1 << ORDER_TYPE_BUY_STOP) | (1 << ORDER_TYPE_SELL_STOP), IS::OR_BITWISE);
   trades.let(POSITION_MAGIC, Magic).let(POSITION_SYMBOL, _Symbol);
   
   return INIT_SUCCEEDED;
}

//+------------------------------------------------------------------+
//| Main trading function                                            |
//+------------------------------------------------------------------+
void RunStrategy()
{
   ExecutionCount++;
   
   ulong tickets[];
   ulong states[];
   
   orders.select(ORDER_STATE, tickets, states);
   const int n = ArraySize(tickets);
   if(n == 2) return; // OK - standard state
   
   if(n > 0)          // 1 or 2+ in both cases need to remove them
   {
      if(n > 2)
      {
         Alert("Too many orders found: " + (string)n);
      }
      
      // remove all related orders
      MqlTradeRequestSyncOCO r;
      for(int i = 0; i < n; ++i)
      {
         if(states[i] != ORDER_STATE_PARTIAL) // keep partially filled orders
         {
            r.remove(tickets[i]) && r.completed();
         }
      }
   }
   else
   {
      // check if open positions exist, place 2 orders if not
      if(!trades.select(tickets))
      {
         MqlTradeRequestSyncOCO r;
         SymbolMonitor sm(_Symbol);
         
         const double point = sm.get(SYMBOL_POINT);
         const double lot = Volume == 0 ? sm.get(SYMBOL_VOLUME_MIN) : Volume;
         const double buy = sm.get(SYMBOL_BID) + point * Distance2SLTP;
         const double sell = sm.get(SYMBOL_BID) - point * Distance2SLTP;

         r.buyStop(lot, buy, buy - Distance2SLTP * point,
            buy + Distance2SLTP * point) && r.completed();
         r.sellStop(lot, sell, sell + Distance2SLTP * point,
            sell - Distance2SLTP * point) && r.completed();
      }
   }
}

//+------------------------------------------------------------------+
//| Tick event handler                                               |
//+------------------------------------------------------------------+
void OnTick()
{
   if(FirstTick)
   {
      RunStrategy();
      FirstTick = false;
   }
}

//+------------------------------------------------------------------+
//| General trade notification handler                               |
//+------------------------------------------------------------------+
void OnTrade()
{
   static ulong count = 0;
   PrintFormat("OnTrade(%d)", ++count);
   if(ActivationBy == ON_TRADE)
   {
      RunStrategy();
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
   PrintFormat("OnTradeTransaction(%d)", ++count);
   Print(TU::StringOf(transaction));

   if(ActivationBy != ON_TRANSACTION) return;
   
   if(transaction.type == TRADE_TRANSACTION_ORDER_DELETE)
   {
      // when order is deleted from online, it's temporary missing
      // both in online list and in the history, so we need to wait for
      // the next event TRADE_TRANSACTION_HISTORY_ADD
      // NB! In the tester when TRADE_TRANSACTION_ORDER_DELETE is fired
      // order is already in the history!
      /* // this does not work online:
         // m.isReady() == false, because
         // neither OrderSelect(), nor HistoryOrderSelect() has effect
         OrderMonitor m(transaction.order);
         if(m.isReady() && m.get(ORDER_MAGIC) == Magic && m.get(ORDER_SYMBOL) == _Symbol)
         {
            RunStrategy();
         }
      */
   }
   else if(transaction.type == TRADE_TRANSACTION_HISTORY_ADD)
   {
      OrderMonitor m(transaction.order);
      if(m.isReady() && m.get(ORDER_MAGIC) == Magic && m.get(ORDER_SYMBOL) == _Symbol)
      {
         // order state is unimportant - in any case we need to remove the remaining one
         // if(transaction.order_state == ORDER_STATE_FILLED
         // || transaction.order_state == ORDER_STATE_CANCELED ...)
         RunStrategy();
      }
   }
}

//+------------------------------------------------------------------+
//| Finalization handler                                             |
//+------------------------------------------------------------------+
void OnDeinit(const int r)
{
   Print("ExecutionCount = ", ExecutionCount);
}
//+------------------------------------------------------------------+
/*
   example output (default settings):
   
   buy stop 0.01 EURUSD at 1.11151 sl: 1.10651 tp: 1.11651 (1.10646 / 1.10683)
   sell stop 0.01 EURUSD at 1.10151 sl: 1.10651 tp: 1.09651 (1.10646 / 1.10683)
   OnTradeTransaction(1)
   TRADE_TRANSACTION_ORDER_ADD, #=2(ORDER_TYPE_BUY_STOP/ORDER_STATE_PLACED), ORDER_TIME_GTC, EURUSD, @ 1.11151, SL=1.10651, TP=1.11651, V=0.01
   OnTrade(1)
   OnTradeTransaction(2)
   TRADE_TRANSACTION_REQUEST
   OnTradeTransaction(3)
   TRADE_TRANSACTION_ORDER_ADD, #=3(ORDER_TYPE_SELL_STOP/ORDER_STATE_PLACED), ORDER_TIME_GTC, EURUSD, @ 1.10151, SL=1.10651, TP=1.09651, V=0.01
   OnTrade(2)
   OnTradeTransaction(4)
   TRADE_TRANSACTION_REQUEST
   order [#3 sell stop 0.01 EURUSD at 1.10151] triggered
   deal #2 sell 0.01 EURUSD at 1.10150 done (based on order #3)
   deal performed [#2 sell 0.01 EURUSD at 1.10150]
   order performed sell 0.01 at 1.10150 [#3 sell stop 0.01 EURUSD at 1.10151]
   OnTradeTransaction(5)
   TRADE_TRANSACTION_DEAL_ADD, D=2(DEAL_TYPE_SELL), #=3(ORDER_TYPE_BUY/ORDER_STATE_STARTED), EURUSD, @ 1.10150, SL=1.10651, TP=1.09651, V=0.01, P=3
   OnTrade(3)
   OnTradeTransaction(6)
   TRADE_TRANSACTION_ORDER_DELETE, #=3(ORDER_TYPE_SELL_STOP/ORDER_STATE_FILLED), ORDER_TIME_GTC, EURUSD, @ 1.10151, SL=1.10651, TP=1.09651, V=0.01, P=3
   OnTrade(4)
   OnTradeTransaction(7)
   TRADE_TRANSACTION_HISTORY_ADD, #=3(ORDER_TYPE_SELL_STOP/ORDER_STATE_FILLED), ORDER_TIME_GTC, EURUSD, @ 1.10151, SL=1.10651, TP=1.09651, P=3
   order canceled [#2 buy stop 0.01 EURUSD at 1.11151]
   OnTrade(5)
   OnTradeTransaction(8)
   TRADE_TRANSACTION_ORDER_DELETE, #=2(ORDER_TYPE_BUY_STOP/ORDER_STATE_CANCELED), ORDER_TIME_GTC, EURUSD, @ 1.11151, SL=1.10651, TP=1.11651, V=0.01
   OnTrade(6)
   OnTradeTransaction(9)
   TRADE_TRANSACTION_HISTORY_ADD, #=2(ORDER_TYPE_BUY_STOP/ORDER_STATE_CANCELED), ORDER_TIME_GTC, EURUSD, @ 1.11151, SL=1.10651, TP=1.11651, V=0.01
   OnTrade(7)
   OnTradeTransaction(10)
   TRADE_TRANSACTION_REQUEST
   
*/
//+------------------------------------------------------------------+
