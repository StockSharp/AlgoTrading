//+------------------------------------------------------------------+
//|                                            PendingOrderGrid2.mq5 |
//|                                  Copyright 2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright   "2022, MetaQuotes Ltd."
#property link        "https://www.mql5.com"
#property description "Construct a grid consisting of limit and stoplimit orders, then maintain it with predefined size."
                       " Use time schedule to limit intra-day operations."
                       " Close positions when possible."

#define SHOW_WARNINGS  // output extended info into the log
#define WARNING Print  // use simple Print for warnings (instead of a built-in format with line numbers etc.)

// uncomment the following line to prevent early returns after
// failed checkups in MqlStructRequestSync methods, so
// incorrect requests will be sent and retcodes received from the server
// #define RETURN(X)

#include <MQL5Book/MqlTradeSync.mqh>
#include <MQL5Book/OrderFilter.mqh>
#include <MQL5Book/PositionFilter.mqh>
#include <MQL5Book/MapArray.mqh>
#include <MQL5Book/Tuples.mqh>

#define GRID_OK    +1
#define GRID_EMPTY  0

input double Volume;                                       // Volume (0 = minimal lot)
input uint GridSize = 6;                                   // GridSize (even number of price levels)
input uint GridStep = 200;                                 // GridStep (points)
input ENUM_ORDER_TYPE_TIME Expiration = ORDER_TIME_GTC;
input ENUM_ORDER_TYPE_FILLING Filling = ORDER_FILLING_FOK;
input datetime _StartTime = D'1970.01.01 00:00:00';        // StartTime (hh:mm:ss)
input datetime _StopTime = D'1970.01.01 09:00:00';         // StopTime (hh:mm:ss)
input ulong Magic = 1234567890;

ulong StartTime, StopTime;
const ulong DAYLONG = 60 * 60 * 24;
datetime lastBar = 0;

//+------------------------------------------------------------------+
//| Helper struct to support automatic data logging and magic        |
//+------------------------------------------------------------------+
struct MqlTradeRequestSyncLog: public MqlTradeRequestSync
{
   MqlTradeRequestSyncLog()
   {
      magic = Magic;
      type_filling = Filling;
      type_time = Expiration;
      if(Expiration == ORDER_TIME_SPECIFIED
         || Expiration == ORDER_TIME_SPECIFIED_DAY)
      {
         // always keep expiration within 24 hours
         expiration = (datetime)(TimeCurrent() / DAYLONG * DAYLONG + StopTime);
         if(StartTime > StopTime)
         {
            expiration = (datetime)(expiration + DAYLONG);
         }
      }
   }
   ~MqlTradeRequestSyncLog()
   {
      Print(TU::StringOf(this));
      Print(TU::StringOf(this.result));
   }
};

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
   if(GridSize < 2 || !!(GridSize % 2))
   {
      Alert("GridSize should be 2, 4, 6+ (even number)");
      return INIT_FAILED;
   }
   StartTime = _StartTime % DAYLONG;
   StopTime = _StopTime % DAYLONG;
   lastBar = 0;
   return INIT_SUCCEEDED;
}

//+------------------------------------------------------------------+
//| Find out if the grid is complete or incomplete and fix it        |
//+------------------------------------------------------------------+
uint CheckGrid()
{
   OrderFilter filter;
   ulong tickets[];
   
   filter.let(ORDER_SYMBOL, _Symbol).let(ORDER_MAGIC, Magic)
      .let(ORDER_TYPE, ORDER_TYPE_SELL, IS::GREATER)
      .select(tickets);
   const int n = ArraySize(tickets);
   if(!n) return GRID_EMPTY;
   
   MapArray<ulong,uint> levels; // price levels => order types mask
   const double point = SymbolInfoDouble(_Symbol, SYMBOL_POINT);
   int limits = 0;
   int stops = 0;
   for(int i = 0; i < n; ++i)
   {
      if(OrderSelect(tickets[i]))
      {
         const ulong level = (ulong)MathRound(OrderGetDouble(ORDER_PRICE_OPEN) / point);
         const ulong type = OrderGetInteger(ORDER_TYPE);
         if(type == ORDER_TYPE_BUY_LIMIT || type == ORDER_TYPE_SELL_LIMIT)
         {
            ++limits;
            levels.put(level, levels[level] | (1 << type));
         }
         else if(type == ORDER_TYPE_BUY_STOP_LIMIT || type == ORDER_TYPE_SELL_STOP_LIMIT)
         {
            ++stops;
            levels.put(level, levels[level] | (1 << type));
         }
      }
   }
   
   if(limits == stops)
   {
      if(limits == GridSize) return GRID_OK; // the grid is complete
      
      Alert("Error: Order number does not match requested");
      return TRADE_RETCODE_ERROR;
   }
   
   if(limits > stops)
   {
      const uint stopmask = (1 << ORDER_TYPE_BUY_STOP_LIMIT) | (1 << ORDER_TYPE_SELL_STOP_LIMIT);
      for(int i = 0; i < levels.getSize(); ++i)
      {
         if((levels[i] & stopmask) == 0) // no stop order on this price level
         {
            const bool buyLimit = (levels[i] & (1 << ORDER_TYPE_BUY_LIMIT));
            bool needNewStopLimit = false;
            
            if(buyLimit) // should check that sell-limit is not present on one level above
            {
               if((levels[levels.getKey(i) + GridStep] & (1 << ORDER_TYPE_SELL_LIMIT)) == 0)
               {
                  needNewStopLimit = true;
               }
               else
               {
                  //PrintFormat("WARNING1: Level %lld already occupied by ORDER_TYPE_SELL_LIMIT",
                  //   levels.getKey(i) + GridStep);
               }
            }
            else // check that buy-limit is not present on one level below
            {
               if((levels[levels.getKey(i) - GridStep] & (1 << ORDER_TYPE_BUY_LIMIT)) == 0)
               {
                  needNewStopLimit = true;
               }
               else
               {
                  //PrintFormat("WARNING2: Level %lld already occupied by ORDER_TYPE_BUY_LIMIT",
                  //   levels.getKey(i) - GridStep);
               }
            }
            
            if(needNewStopLimit)
            {
               const uint retcode = RepairGridLevel(levels.getKey(i), point, buyLimit);
               if(TradeCodeSeverity(retcode) > SEVERITY_NORMAL)
               {
                  return retcode;
               }
            }
         }
      }
      return GRID_OK;
   }
   
   Alert("Error: Orphaned Stop-Limit orders found");
   return TRADE_RETCODE_ERROR;
}

//+------------------------------------------------------------------+
//| Restore orders on vacant grid levels                             |
//+------------------------------------------------------------------+
uint RepairGridLevel(const ulong level, const double point, const bool buyLimit)
{
   const double price = level * point;
   const double volume = Volume == 0 ? SymbolInfoDouble(_Symbol, SYMBOL_VOLUME_MIN) : Volume;

   MqlTradeRequestSyncLog request;
   request.comment = "repair";
   
   // if unpaired buy-limit exists, set sell-stop-limit near it
   // if unpaired sell-limit exists, set buy-stop-limit near it
   const ulong order = (buyLimit ?
      request.sellStopLimit(volume, price, price + GridStep * point) :
      request.buyStopLimit(volume, price, price - GridStep * point));
   const bool result = (order != 0) && request.completed();
   if(!result) Alert("RepairGridLevel failed");
   return request.result.retcode;
}

//+------------------------------------------------------------------+
//| Create initial set of orders to form the grid                    |
//+------------------------------------------------------------------+
uint SetupGrid()
{
   const double current = SymbolInfoDouble(_Symbol, SYMBOL_BID);
   const double point = SymbolInfoDouble(_Symbol, SYMBOL_POINT);
   const double volume = Volume == 0 ? SymbolInfoDouble(_Symbol, SYMBOL_VOLUME_MIN) : Volume;
   
   const double base = ((ulong)MathRound(current / point / GridStep) * GridStep) * point;
   const string comment = "G[" + DoubleToString(base, (int)SymbolInfoInteger(_Symbol, SYMBOL_DIGITS)) + "]";
   const static string message = "SetupGrid failed: ";
   
   Print("Start setup at ", current);

   MqlTradeRequestSyncLog request[][2]; // limit and stoplimit
   ArrayResize(request, GridSize);      // 2 pending orders per each price level
   
   for(int i = 0; i < (int)GridSize / 2; ++i)
   {
      const int k = i + 1;
      
      // bottom half of the grid
      request[i][0].comment = comment;
      request[i][1].comment = comment;
      
      if(!(request[i][0].buyLimit(volume, base - k * GridStep * point)))
      {
         Alert(message + (string)i + "/BL");
         return request[i][0].result.retcode;
      }
      if(!(request[i][1].sellStopLimit(volume, base - k * GridStep * point, base - (k - 1) * GridStep * point)))
      {
         Alert(message + (string)i + "/SSL");
         return request[i][1].result.retcode;
      }

      // top half of the grid
      const int m = i + (int)GridSize / 2;

      request[m][0].comment = comment;
      request[m][1].comment = comment;
      
      if(!(request[m][0].sellLimit(volume, base + k * GridStep * point)))
      {
         Alert(message + (string)m + "/SL");
         return request[m][0].result.retcode;
      }
      if(!(request[m][1].buyStopLimit(volume, base + k * GridStep * point, base + (k - 1) * GridStep * point)))
      {
         Alert(message + (string)m + "/BSL");
         return request[m][1].result.retcode;
      }
   }

   for(int i = 0; i < (int)GridSize; ++i)
   {
      for(int j = 0; j < 2; ++j)
      {
         if(!request[i][j].completed())
         {
            Alert(message + (string)i + "/" + (string)j + " post-check");
            return request[i][j].result.retcode;
         }
      }
   }

   return GRID_OK;
}

//+------------------------------------------------------------------+
//| Helper function to find compatible positions                     |
//+------------------------------------------------------------------+
int GetMyPositions(const string s, const ulong m, Tuple2<ulong,ulong> &types4tickets[])
{
   int props[] = {POSITION_TYPE, POSITION_TICKET};
   PositionFilter filter;
   filter.let(POSITION_SYMBOL, s).let(POSITION_MAGIC, m)
      .select(props, types4tickets, true);
   return ArraySize(types4tickets);
}

//+------------------------------------------------------------------+
//| Mutual closure of two positions                                  |
//+------------------------------------------------------------------+
uint CloseByPosition(const ulong ticket1, const ulong ticket2)
{
   // define the struct
   MqlTradeRequestSyncLog request;
   // fill optional fields
   request.comment = "compacting";

   ResetLastError();
   // send request and wait for its completion
   if(request.closeby(ticket1, ticket2))
   {
      Print("Positions collapse initiated");
      if(request.completed())
      {
         Print("OK CloseBy Order/Deal/Position");
         return 0; // success
      }
   }

   return request.result.retcode; // error
}

//+------------------------------------------------------------------+
//| Close counter-positions pairwise, and optionally the rest ones   |
//+------------------------------------------------------------------+
uint CompactPositions(const bool cleanup = false)
{
   uint retcode = 0;
   Tuple2<ulong,ulong> types4tickets[];
   int i = 0, j = 0;
   int n = GetMyPositions(_Symbol, Magic, types4tickets);
   if(n > 0)
   {
      Print("CompactPositions: ", n);
      // traverse array from both ends pairwise
      for(i = 0, j = n - 1; i < j; ++i, --j)
      {
         if(types4tickets[i]._1 != types4tickets[j]._1) // until position types differ
         {
            retcode = CloseByPosition(types4tickets[i]._2, types4tickets[j]._2);
            if(retcode) return retcode; // error
         }
         else
         {
            break;
         }
      }
   }
   
   if(cleanup && j < n)
   {
      retcode = CloseAllPositions(types4tickets, i, j + 1);
   }
   
   return retcode; // can be success (0) or error
}

//+------------------------------------------------------------------+
//| Helper function to close specified positions                     |
//+------------------------------------------------------------------+
uint CloseAllPositions(const Tuple2<ulong,ulong> &types4tickets[], const int start = 0, const int end = 0)
{
   const int n = end == 0 ? ArraySize(types4tickets) : end;
   Print("CloseAllPositions ", n - start);
   for(int i = start; i < n; ++i)
   {
      MqlTradeRequestSyncLog request;
      request.comment = "close down " + (string)(i + 1 - start) + " of " + (string)(n - start);
      const ulong ticket = types4tickets[i]._2;
      if(!(request.close(ticket) && request.completed()))
      {
         Print("Error: position is not closed ", ticket);
         if(!PositionSelectByTicket(ticket)) continue;
         return request.result.retcode; // error
      }
   }
   return 0; // success
}

//+------------------------------------------------------------------+
//| Remove all compatible orders                                     |
//+------------------------------------------------------------------+
uint RemoveOrders()
{
   OrderFilter filter;
   ulong tickets[];
   filter.let(ORDER_SYMBOL, _Symbol).let(ORDER_MAGIC, Magic)
      .select(tickets);
   const int n = ArraySize(tickets);
   for(int i = 0; i < n; ++i)
   {
      MqlTradeRequestSyncLog request;
      request.comment = "removal " + (string)(i + 1) + " of " + (string)n;
      if(!(request.remove(tickets[i]) && request.completed()))
      {
         Print("Error: order is not removed ", tickets[i]);
         if(!OrderSelect(tickets[i]))
         {
            Sleep(100);
            Print("Order is in history? ", HistoryOrderSelect(tickets[i]));
            continue;
         }
         return request.result.retcode;
      }
   }
   return 0;
}

//+------------------------------------------------------------------+
//| Tick event handler                                               |
//+------------------------------------------------------------------+
void OnTick()
{
   // noncritical error handler by autoadjusted timeout
   const static int DEFAULT_RETRY_TIMEOUT = 1; // seconds
   static int RetryFrequency = DEFAULT_RETRY_TIMEOUT;
   static datetime RetryRecordTime = 0;
   if(TimeCurrent() - RetryRecordTime < RetryFrequency) return;

   // run once on a new bar
   if(iTime(_Symbol, _Period, 0) == lastBar) return;
   
   uint retcode = 0;
   bool tradeScheduled = true;
   
   // check if current time suitable for trading
   if(StartTime != StopTime)
   {
      const ulong now = TimeCurrent() % DAYLONG;
      
      if(StartTime < StopTime)
      {
         tradeScheduled = now >= StartTime && now < StopTime;
      }
      else
      {
         tradeScheduled = now >= StartTime || now < StopTime;
      }
   }
   
   // main functionality goes here
   if(tradeScheduled)
   {
      retcode = CheckGrid();
      
      if(retcode == GRID_EMPTY)
      {
         retcode = SetupGrid();
      }
      else
      {
         retcode = CompactPositions();
      }
   }
   else
   {
      retcode = CompactPositions(true);
      if(!retcode) retcode = RemoveOrders();
   }
   
   // now track errors and try to remedy them if possible
   const TRADE_RETCODE_SEVERITY severity = TradeCodeSeverity(retcode);
   if(severity >= SEVERITY_INVALID)
   {
      PrintFormat("Trying to recover...");
      if(CompactPositions(true) || RemoveOrders()) // try to rollback
      {
         Alert("Can't place/modify pending orders, EA is stopped");
         RetryFrequency = INT_MAX;
      }
      else
      {
         RetryFrequency += (int)sqrt(RetryFrequency + 1);
         RetryRecordTime = TimeCurrent();
      }
   }
   else if(severity >= SEVERITY_RETRY)
   {
      RetryFrequency += (int)sqrt(RetryFrequency + 1);
      RetryRecordTime = TimeCurrent();
      PrintFormat("Problems detected, waiting for better conditions (timeout enlarged to %d seconds)",
         RetryFrequency);
   }
   else
   {
      if(RetryFrequency > DEFAULT_RETRY_TIMEOUT)
      {
         RetryFrequency = DEFAULT_RETRY_TIMEOUT;
         PrintFormat("Timeout restored to %d second", RetryFrequency);
      }
      // if the grid was processed ok, skip all next ticks until the next bar
      lastBar = iTime(_Symbol, _Period, 0);
   }
}

//+------------------------------------------------------------------+
/*
   example output (EURUSD,H1 default settings):

   buy stop limit 0.01 EURUSD at 1.14200 (1.14000) (1.13923 / 1.13923)
   TRADE_ACTION_PENDING, EURUSD, ORDER_TYPE_BUY_STOP_LIMIT, V=0.01, ORDER_FILLING_FOK, @ 1.14200, X=1.14000, ORDER_TIME_GTC, M=1234567890, repair
   DONE, #=159, V=0.01, Bid=1.13923, Ask=1.13923, Request executed, Req=287
   CompactPositions, pairs: 1
   close position #152 sell 0.01 EURUSD by position #153 buy 0.01 EURUSD (1.13923 / 1.13923)
   deal #18 buy 0.01 EURUSD at 1.13996 done (based on order #160)
   deal #19 sell 0.01 EURUSD at 1.14202 done (based on order #160)
   Positions collapse initiated
   OK CloseBy Order/Deal/Position
   TRADE_ACTION_CLOSE_BY, EURUSD, ORDER_TYPE_BUY, ORDER_FILLING_FOK, P=152, b=153, M=1234567890, compacting
   DONE, D=18, #=160, Request executed, Req=288

*/
//+------------------------------------------------------------------+
