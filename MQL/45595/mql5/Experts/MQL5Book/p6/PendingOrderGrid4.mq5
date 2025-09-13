//+------------------------------------------------------------------+
//|                                            PendingOrderGrid4.mq5 |
//|                                  Copyright 2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright   "2022, MetaQuotes Ltd."
#property link        "https://www.mql5.com"
#property description "Construct a grid consisting of limit orders, using sync or async batch requests."
                       " Then maintain it with predefined size by monitoring trade events."
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
#include <MQL5Book/DealMonitor.mqh>
#include <MQL5Book/MapArray.mqh>
#include <MQL5Book/Tuples.mqh>
#include <MQL5Book/ConverterT.mqh>

#define GRID_OK    +1
#define GRID_EMPTY  0
#define FIELD_NUM   6  // most important fields from MqlTradeResult
#define TIMEOUT  1000  // 1 second

input double Volume;                                       // Volume (0 = minimal lot)
input uint GridSize = 6;                                   // GridSize (even number of price levels)
input uint GridStep = 200;                                 // GridStep (points)
input ENUM_ORDER_TYPE_TIME Expiration = ORDER_TIME_GTC;
input ENUM_ORDER_TYPE_FILLING Filling = ORDER_FILLING_FOK;
input datetime _StartTime = D'1970.01.01 00:00:00';        // StartTime (hh:mm:ss)
input datetime _StopTime = D'1970.01.01 09:00:00';         // StopTime (hh:mm:ss)
input ulong Magic = 1234567890;
input bool EnableAsyncSetup = false;

ulong StartTime, StopTime;
const ulong DAYLONG = 60 * 60 * 24;
datetime lastBar = 0;
int handle;

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
   
   if(EnableAsyncSetup)
   {
      if(!MQLInfoInteger(MQL_TESTER))
      {
         const uint start = GetTickCount();
         const static string indicator = "MQL5Book/p6/TradeTransactionRelay";
         handle = iCustom(_Symbol, PERIOD_D1, indicator);
         if(handle == INVALID_HANDLE)
         {
            Alert("Can't start indicator ", indicator);
            return INIT_FAILED;
         }
         PrintFormat("Started in %d ms", GetTickCount() - start);
      }
      else
      {
         Print("WARNINIG! Async mode is incompatible with the tester, EnableAsyncSetup is disabled");
      }
   }
   lastBar = 0;
   return INIT_SUCCEEDED;
}

//+------------------------------------------------------------------+
//| Find out if the grid is complete or incomplete and fix it        |
//+------------------------------------------------------------------+
uint CheckGrid()
{
   OrderFilter filter;
   Tuple2<double,ulong> tickets[];
   int props[] = {ORDER_PRICE_OPEN, ORDER_TYPE};
   
   filter.let(ORDER_SYMBOL, _Symbol).let(ORDER_MAGIC, Magic)
      .let(ORDER_TYPE, ORDER_TYPE_SELL, IS::GREATER)
      .select(props, tickets);
   const int n = ArraySize(tickets);
   if(!n) return GRID_EMPTY;
   
   MapArray<ulong,uint> levels; // price levels => order types mask
   const double point = SymbolInfoDouble(_Symbol, SYMBOL_POINT);
   ulong min = ULONG_MAX, max = 0;
   int limits = 0;
   for(int i = 0; i < n; ++i)
   {
      const ulong level = (ulong)MathRound(tickets[i]._1 / point);
      const ulong type = tickets[i]._2;
      if(type == ORDER_TYPE_BUY_LIMIT || type == ORDER_TYPE_SELL_LIMIT)
      {
         ++limits;
         levels.put(level, levels[level] | (1 << type));
      }
      max = fmax(max, level);
      min = fmin(min, level);
   }
   
   if(limits == GridSize) return GRID_OK; // the grid is complete
   if(limits > (int)GridSize)
   {
      Alert("Error: Too many orders");
      return TRADE_RETCODE_ERROR;
   }
   
   /*
      LEGEND:
       '-'     - levels/orders
       '/' '\' - price moves
       '+'     - repairs
      
                   buy&close_by
                   |   buy&close_by
                   |   |   buy
                   |   |   |
       - - / v     * + - - - - -
       - v     + \ ^   * + - - -
       /   + - - - - \ ^     + -
       - - - - - - - - - \ ^
       - - - - - - - - - - - - -
         |   |
         |   sell
         sell
   */
   
   const double price = SymbolInfoDouble(_Symbol, SYMBOL_BID);
   const ulong current = ((ulong)MathRound(price / point / GridStep) * GridStep);

   if((max - min) / GridStep == GridSize) // gap is inside
   {
      for(int i = 0; i < (int)GridSize; ++i)
      {
         if(min + i * GridStep != current && levels[min + i * GridStep] == 0)
         {
            if(min + i * GridStep < current)
            {
               RepairGridLevel(min + i * GridStep, point, true);
            }
            else
            {
               RepairGridLevel(min + i * GridStep, point, false);
            }
         }
      }
   }
   else // gap is outside
   {
      if(current > max) // add buy limits below
      {
         for(int i = 1; i < (int)GridSize; ++i)
         {
            if(levels[min + i * GridStep] == 0)
            {
               RepairGridLevel(min + i * GridStep, point, true);
            }
         }
         
      }
      else if(current < min) // add sell limits above
      {
         for(int i = 1; i < (int)GridSize; ++i)
         {
            if(levels[max - i * GridStep] == 0)
            {
               RepairGridLevel(max - i * GridStep, point, false);
            }
         }
      }
   }
   
   // re-check pending orders
   ArrayResize(tickets, 0);
   if(filter.select(props, tickets))
   {
      const int added = ArraySize(tickets) - n;
      PrintFormat("%d orders added during checkup", added);
      // number of added orders matches previous odds
      if(added == GridSize - limits) return GRID_OK; 
   }
   
   Alert("Error: Order number does not match requested");
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
      request.buyLimit(volume, price) :
      request.sellLimit(volume, price));
   const bool result = (order != 0) && request.completed();
   if(!result) Alert("RepairGridLevel failed " + (string)level + (buyLimit ? "/BL" : "/SL"));
   return request.result.retcode;
}

//+------------------------------------------------------------------+
//| Use an external indicator (such as TradeTransactionRelay) with   |
//| given handle to access transaction results on-the-fly via buffer |
//+------------------------------------------------------------------+
bool AwaitAsync(MqlTradeRequestSyncLog &r[], const int _handle)
{
   Converter<ulong,double> cnv;
   int offset[];
   const int n = ArraySize(r);
   int done = 0;
   ArrayResize(offset, n);

   for(int i = 0; i < n; ++i)
   {
      offset[i] = (int)((r[i].result.request_id * FIELD_NUM)
         % (Bars(_Symbol, _Period) / FIELD_NUM * FIELD_NUM));
   }
   
   const uint start = GetTickCount();
   while(!IsStopped() && done < n && GetTickCount() - start < TIMEOUT)
   for(int i = 0; i < n; ++i)
   {
      if(offset[i] == -1) continue; // skip empty elements
      double array[];
      if((CopyBuffer(_handle, 0, offset[i], FIELD_NUM, array)) == FIELD_NUM)
      {
         ArraySetAsSeries(array, true);
         if((uint)MathRound(array[0]) == r[i].result.request_id)
         {
            r[i].result.retcode = (uint)MathRound(array[1]);
            r[i].result.deal = cnv[array[2]];
            r[i].result.order = cnv[array[3]];
            r[i].result.volume = array[4];
            r[i].result.price = array[5];
            PrintFormat("Got Req=%d at %d ms", r[i].result.request_id, GetTickCount() - start);
            Print(TU::StringOf(r[i].result));
            offset[i] = -1; // mark as processed
            done++;
         }
      }
   }
   return done == n;
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

   MqlTradeRequestSyncLog request[];    // limit orders only
   ArrayResize(request, GridSize);      // 1 pending order per each price level
   
   AsyncSwitcher sync(EnableAsyncSetup && !MQLInfoInteger(MQL_TESTER));
   const uint start = GetTickCount();
   
   for(int i = 0; i < (int)GridSize / 2; ++i)
   {
      const int k = i + 1;
      
      // bottom half of the grid
      request[i].comment = comment;
      
      if(!(request[i].buyLimit(volume, base - k * GridStep * point)))
      {
         Alert(message + (string)i + "/BL");
         return request[i].result.retcode;
      }

      // top half of the grid
      const int m = i + (int)GridSize / 2;

      request[m].comment = comment;
      
      if(!(request[m].sellLimit(volume, base + k * GridStep * point)))
      {
         Alert(message + (string)m + "/SL");
         return request[m].result.retcode;
      }
   }
   
   if(EnableAsyncSetup && !MQLInfoInteger(MQL_TESTER))
   {
      if(!AwaitAsync(request, handle))
      {
         Print("Timeout");
         return TRADE_RETCODE_ERROR;
      }
   }
   
   PrintFormat("Done %d requests in %d ms (%d ms/request)",
      GridSize, GetTickCount() - start, (GetTickCount() - start) / GridSize);

   for(int i = 0; i < (int)GridSize; ++i)
   {
      if(!request[i].completed())
      {
         Alert(message + (string)i + " post-check");
         return request[i].result.retcode;
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
   if(n > 1)
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
//| Trade transactions handler                                       |
//| (can be commented out because CheckGrid fixes the same issues)   |
//+------------------------------------------------------------------+
void OnTradeTransaction(const MqlTradeTransaction &transaction,
   const MqlTradeRequest &,
   const MqlTradeResult &)
{
   if(transaction.type == TRADE_TRANSACTION_DEAL_ADD)
   {
      if(transaction.symbol == _Symbol)
      {
         DealMonitor dm(transaction.deal); // select the deal
         if(dm.get(DEAL_MAGIC) == Magic)
         {
            CheckGrid();
         }
      }
   }
}

//+------------------------------------------------------------------+
