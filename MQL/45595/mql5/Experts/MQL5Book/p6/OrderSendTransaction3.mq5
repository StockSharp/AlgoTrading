//+------------------------------------------------------------------+
//|                                        OrderSendTransaction3.mq5 |
//|                                  Copyright 2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright   "2022, MetaQuotes Ltd."
#property link        "https://www.mql5.com"
#property description "Send 3 trade requests (open, adjust sl/tp, close) and confirmed by transaction events.\n"
                      "Events are imported via reading of a special indicator buffer, collecting there trade request results!"

#include <MQL5Book/OrderMonitor.mqh>
#include <MQL5Book/DealMonitor.mqh>
#include <MQL5Book/PositionMonitor.mqh>
#include <MQL5Book/ConverterT.mqh>

#define SHOW_WARNINGS  // output extended info into the log, with changes in data state
#define WARNING Print  // use simple Print for warnings (instead of a built-in format with line numbers etc.)
#include <MQL5Book/MqlTradeSync.mqh>

#define FIELD_NUM   6  // most important fieds from MqlTradeResult
#define TIMEOUT  1000  // 1 second

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

int handle = 0;

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

   const static string indicator = "MQL5Book/p6/TradeTransactionRelay";
   handle = iCustom(_Symbol, PERIOD_D1, indicator);
   if(handle == INVALID_HANDLE)
   {
      Alert("Can't start indicator ", indicator);
      return INIT_FAILED;
   }
   
   // setup timer for postponed execution
   EventSetTimer(1);
   return INIT_SUCCEEDED;
}

//+------------------------------------------------------------------+
//| Import transaction data from helper indicator running in parallel|
//+------------------------------------------------------------------+
bool AwaitAsync(MqlTradeRequestSync &r, const int _handle)
{
   Converter<ulong,double> cnv;
   const int offset = (int)((r.result.request_id * FIELD_NUM)
      % (Bars(_Symbol, _Period) / FIELD_NUM * FIELD_NUM));

   const uint start = GetTickCount();
   // keep looping until results arrive or timeout
   while(!IsStopped() && GetTickCount() - start < TIMEOUT)
   {
      double array[];
      if((CopyBuffer(_handle, 0, offset, FIELD_NUM, array)) == FIELD_NUM)
      {
         ArraySetAsSeries(array, true);
         // when request_id is found, the result is ready
         if((uint)MathRound(array[0]) == r.result.request_id)
         {
            r.result.retcode = (uint)MathRound(array[1]);
            r.result.deal = cnv[array[2]];
            r.result.order = cnv[array[3]];
            r.result.volume = array[4];
            r.result.price = array[5];
            PrintFormat("Got Req=%d at %d ms",
               r.result.request_id, GetTickCount() - start);
            Print(TU::StringOf(r.result));
            return true;
         }
      }
   }
   Print("Timeout for: ");
   Print(TU::StringOf(r));
   return false;
}

//+------------------------------------------------------------------+
//| Timer event handler                                              |
//+------------------------------------------------------------------+
void OnTimer()
{
   // once executed do nothing
   EventKillTimer();
   
   MqlTradeRequestSync::AsyncEnabled = true;

   // define the struct
   MqlTradeRequestSync request;

   // fill optional fields of the struct
   request.magic = Magic;
   request.deviation = Deviation;

   // default values
   const double volume = Volume == 0 ? SymbolInfoDouble(_Symbol, SYMBOL_VOLUME_MIN) : Volume;

   Print("Start trade");
   ResetLastError();
   if((bool)(Type == MARKET_BUY ? request.buy(volume) : request.sell(volume)))
   {
      Print("OK Open?");
   }
   
   if(!(AwaitAsync(request, handle) && request.completed()))
   {
      Print("Failed Open");
      return;
   }
   
   Print("SL/TP modification");
   const double price = PositionGetDouble(POSITION_PRICE_OPEN);
   const double point = SymbolInfoDouble(_Symbol, SYMBOL_POINT);
   TU::TradeDirection dir((ENUM_ORDER_TYPE)Type);
   const double SL = dir.negative(price, Distance2SLTP * point);
   const double TP = dir.positive(price, Distance2SLTP * point);
   if(request.adjust(SL, TP))
   {
      Print("OK Adjust?");
   }
   
   if(!(AwaitAsync(request, handle) && request.completed()))
   {
      Print("Failed Adjust");
   }

   Print("Close down");
   if(request.close(request.result.position))
   {
      Print("OK Close?");
   }

   if(!(AwaitAsync(request, handle) && request.completed()))
   {
      Print("Failed Close");
   }

   Print("Finish");
}

/*
//+------------------------------------------------------------------+
//| Trade transactions handler                                       |
//| (it's left here for debugging only: uncomment to see             |
//| transaction log _after_ completion of entire trade plan,         |
//| because this OnTradeTransaction is still called after OnTimer)   |
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
}
*/
//+------------------------------------------------------------------+
/*
   example output (default settings, EURUSD):
   note how MqlTradeResults, received from helper indicator,
   printed during streamlined trading process (DONE statuses)

   Start trade
   OK Open?
   Got Req=1 at 16 ms
   DONE, D=1282677007, #=1300045365, V=0.01, @ 1.10564, Bid=1.10564, Ask=1.10564, Request executed, Req=1
   Waiting for position for deal D=1282677007
   SL/TP modification
   OK Adjust?
   Got Req=2 at 16 ms
   DONE, Request executed, Req=2
   Close down
   OK Close?
   Got Req=3 at 0 ms
   DONE, D=1282677008, #=1300045366, V=0.01, @ 1.10564, Bid=1.10564, Ask=1.10564, Request executed, Req=3
   Finish


   this is debug output of the transactions above
   
   >>>     1
   TRADE_TRANSACTION_ORDER_ADD, #=1300045365(ORDER_TYPE_BUY/ORDER_STATE_STARTED), EURUSD, @ 1.10564, V=0.01
   >>>     2
   TRADE_TRANSACTION_ORDER_DELETE, #=1300045365(ORDER_TYPE_BUY/ORDER_STATE_FILLED), EURUSD, @ 1.10564, P=1300045365
   >>>     3
   TRADE_TRANSACTION_DEAL_ADD, D=1282677007(DEAL_TYPE_BUY), #=1300045365(ORDER_TYPE_BUY/ORDER_STATE_STARTED), EURUSD, @ 1.10564, V=0.01, P=1300045365
   >>>     4
   TRADE_TRANSACTION_HISTORY_ADD, #=1300045365(ORDER_TYPE_BUY/ORDER_STATE_FILLED), EURUSD, @ 1.10564, P=1300045365
   >>>     5
   TRADE_TRANSACTION_REQUEST
   TRADE_ACTION_DEAL, EURUSD, ORDER_TYPE_BUY, V=0.01, ORDER_FILLING_FOK, @ 1.10564, D=10, #=1300045365, M=1234567890
   DONE, D=1282677007, #=1300045365, V=0.01, @ 1.10564, Bid=1.10564, Ask=1.10564, Req=1
   >>>     6
   TRADE_TRANSACTION_POSITION, EURUSD, @ 1.10564, SL=1.09564, TP=1.11564, V=0.01, P=1300045365
   >>>     7
   TRADE_TRANSACTION_REQUEST
   TRADE_ACTION_SLTP, EURUSD, ORDER_TYPE_BUY, V=0.01, ORDER_FILLING_FOK, SL=1.09564, TP=1.11564, D=10, P=1300045365, M=1234567890
   DONE, Req=2
   >>>     8
   TRADE_TRANSACTION_ORDER_ADD, #=1300045366(ORDER_TYPE_SELL/ORDER_STATE_STARTED), EURUSD, @ 1.10564, V=0.01, P=1300045365
   >>>     9
   TRADE_TRANSACTION_ORDER_DELETE, #=1300045366(ORDER_TYPE_SELL/ORDER_STATE_FILLED), EURUSD, @ 1.10564, P=1300045365
   >>>    10
   TRADE_TRANSACTION_HISTORY_ADD, #=1300045366(ORDER_TYPE_SELL/ORDER_STATE_FILLED), EURUSD, @ 1.10564, P=1300045365
   >>>    11
   TRADE_TRANSACTION_DEAL_ADD, D=1282677008(DEAL_TYPE_SELL), #=1300045366(ORDER_TYPE_BUY/ORDER_STATE_STARTED), EURUSD, @ 1.10564, SL=1.09564, TP=1.11564, V=0.01, P=1300045365
   >>>    12
   TRADE_TRANSACTION_REQUEST
   TRADE_ACTION_DEAL, EURUSD, ORDER_TYPE_SELL, V=0.01, ORDER_FILLING_FOK, @ 1.10564, D=10, #=1300045366, P=1300045365, M=1234567890
   DONE, D=1282677008, #=1300045366, V=0.01, @ 1.10564, Bid=1.10564, Ask=1.10564, Req=3
   
*/
//+------------------------------------------------------------------+
