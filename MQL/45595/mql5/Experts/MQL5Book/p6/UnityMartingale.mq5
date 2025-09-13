//+------------------------------------------------------------------+
//|                                              UnityMartingale.mq5 |
//|                              Copyright (c) 2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright "Copyright (c) 2022, MetaQuotes Ltd."
#property link "https://www.mql5.com"
#property description "Multi-currency expert adviser based on Unity indicator and reversal strategy with martingale."

#property tester_set "UnityMartingale-eurusd.set"
#property tester_set "UnityMartingale-gbpchf.set"
#property tester_set "UnityMartingale-audjpy.set"
#property tester_set "UnityMartingale-combo.set"
#property tester_no_cache

#resource "OptReportPage.htm" as string OptReportPageTemplate
#resource "OptReportElement.htm" as string OptReportElementTemplate
#resource "\\Indicators\\MQL5Book\\p6\\UnityPercentEvent.ex5"

#define MINIWIDTH  400
#define MINIHEIGHT 200
#define MINIMARGIN 20

#include <MQL5Book/DateTime.mqh>
#include <MQL5Book/SymbolMonitor.mqh>
#include <MQL5Book/PositionFilter.mqh>
#include <MQL5Book/DealFilter.mqh>
#include <MQL5Book/MqlTradeSync.mqh>
#include <MQL5Book/TradeState.mqh>
#include <MQL5Book/AutoPtr.mqh>
#include <MQL5Book/TrailingStop.mqh>
#include <MQL5Book/MultiSymbolMonitor.mqh>
#include <MQL5Book/Tuples.mqh>
#include <MQL5Book/RSquared.mqh>
#include <MQL5Book/TradeReport.mqh>
#include <MQL5Book/TradeReportWriter.mqh>
#include <MQL5Book/TickModel.mqh>
#include <MQL5Book/Periods.mqh>

enum ERROR_TIMEOUT
{
   bt_NONE = 0,                  // none
   bt_SECOND = 1,                // second
   bt_MINUTE = 60,               // minute (M1)
   bt_HOUR = 60 * 60,            // hour (H1)
   bt_SESSION = 60 * 60 * 4,     // session (H4)
   bt_DAY = 60 * 60 * 24,        // day (D1)
   bt_MONTH = 60 * 60 * 24 * 30, // month (MN)
   bt_YEAR = 60 * 60 * 24 * 365, // year
   bt_FOREVER = UINT_MAX         // forever
};

enum SIGNAL_TYPE
{
   BREAKOUT,
   PULLBACK
};

//+------------------------------------------------------------------+
//| Inputs                                                           |
//+------------------------------------------------------------------+

input group "S Y M B O L   S E T T I N G S"
input bool UseTime = true;       // UseTime (HourStart and HourEnd)
input uint HourStart = 2;        // HourStart (0...23)
input uint HourEnd = 22;         // HourEnd (0...23)
input double Lots = 0.01;        // Lots (initial)
input double Factor = 2.0;       // Factor (lot multiplication)
input uint Limit = 5;            // Limit (max number of multiplications)
input uint StopLoss = 500;       // StopLoss (points)
input uint TakeProfit = 500;     // TakeProfit (points)
input SIGNAL_TYPE StartType = 0; // SignalType

input group "C O M M O N   S E T T I N G S"
sinput ulong Magic = 1234567890; // Magic
input ERROR_TIMEOUT SkipTimeOnError = bt_SECOND; // SkipTimeOnError
input bool Trailing = true;      // Trailing
input string WorkSymbols = "";   // WorkSymbols (name±lots*factor^limit(sl,tp)[start,stop];...)

input group "U N I T Y   S E T T I N G S"
input string UnitySymbols = "EURUSD,GBPUSD,USDCHF,USDJPY,AUDUSD,USDCAD,NZDUSD";
input int UnityBarLimit = 10;
input ENUM_APPLIED_PRICE UnityPriceType = PRICE_CLOSE;
input ENUM_MA_METHOD UnityPriceMethod = MODE_EMA;
input int UnityPricePeriod = 1;

#define SLTP_DEFAULT 1000

//+------------------------------------------------------------------+
//| Complete set of settings for one symbol                          |
//+------------------------------------------------------------------+
struct Settings
{
   bool useTime;
   uint hourStart;
   uint hourEnd;
   double lots;
   double factor;
   uint limit;
   uint stopLoss;
   uint takeProfit;
   SIGNAL_TYPE startType;
   ulong magic;
   ERROR_TIMEOUT skipTimeOnError;
   bool trailing;
   string symbol;

   void defaults()
   {
      useTime = false;
      hourStart = hourEnd = 0;
      lots = 0.01;
      factor = 1;
      limit = 1;
      stopLoss = SLTP_DEFAULT;
      takeProfit = SLTP_DEFAULT;
      startType = BREAKOUT;
      magic = 0;
      skipTimeOnError = bt_NONE;
      symbol = _Symbol;
      trailing = false;
   }
   
   int range(const string &line, const string opener, const string closer,
      uint &min, uint &max)
   {
      int p, q;
      q = StringFind(line, opener);
      if(q == -1)
      {
         return 0;
      }
      p = q + 1;
      q = StringFind(line, closer, p);
      if(q == -1)
      {
         PrintFormat("WARNING: Range has no closing brace %s for %s", closer, opener);
         return -1; // no pair brace
      }
      
      string elements[];
      const string substr = StringSubstr(line, p, q - p);
      const int r = StringSplit(substr, ',', elements);
      if(r == 2)
      {
         min = (int)elements[0];
         max = (int)elements[1];
      }
      else
      {
         PrintFormat("WARNING: Range within %s%s should contain 2 elements, %d given",
            opener, closer, r);
      }
      
      return r;
   }
    
   // syntax: name±lots*factor^limit(sl,tp)[start,stop];...
   //         parentheses and brackets are optional
   // examples: EURUSD+0.01*2^5
   //           EURUSD+0.01*2^5(500,1000)[2,22]
   //           EURUSD+0.01*2.0^7(500,500)[2,22];AUDJPY+0.01*2.0^8(300,500)[2,22];GBPCHF+0.01*1.7^8(1000,2000)[2,22]
   
   bool parse(const string &line)
   {
      defaults();

      // obligatory part
      int p = StringFind(line, "+");
      if(p == -1) p = StringFind(line, "-");
      if(p == -1) return false;
      
      symbol = StringSubstr(line, 0, p);
      startType = line[p] == '+' ? BREAKOUT : PULLBACK;
      int q = StringFind(line, "*", ++p);
      if(q == -1) return false;
      lots = (double)StringSubstr(line, p, q - p);
      p = q + 1;
      q = StringFind(line, "^", p);
      if(q == -1) return false;
      factor = (double)StringSubstr(line, p, q - p);
      p = q + 1;
      limit = (int)StringSubstr(line, p);
      
      // optional part
      
      if(range(line, "(", ")", stopLoss, takeProfit) == -1)
      {
         return false; // error
      }
      const int plan = range(line, "[", "]", hourStart, hourEnd);
      if(plan == 2)
      {
         useTime = true;
      }
      else if(plan == -1)
      {
         return false; // error
      }
      
      return true;
   }

   bool static parseAll(const string &line, Settings &settings[])
   {
      string symbols[];
      int n = StringSplit(line, ';', symbols);
      ArrayResize(settings, n);
      
      string hash = "";
      
      for(int i = 0; i < n; i++)
      {
         if(!settings[i].parse(symbols[i]))
         {
            return false;
         }
         string signature = "^" + settings[i].symbol + "$";
         if(StringFind(hash, signature) > -1)
         {
            Print("WARNING: Duplicate of symbol ", settings[i].symbol, " found");
         }

         settings[i].print();
         if(settings[i].validate())
         {
            hash += signature;
         }
         else
         {
            Print("Invalid settings: trade system will be ignored");
            settings[i].symbol = NULL;
         }
      }
      
      return StringLen(hash) > 0; // some settings are correct
   }
    
   bool validate()
   {
      SymbolMonitor s(symbol);
      
      if(takeProfit == 0 && !trailing)
      {
         Print("Either TakeProfit or Trailing should be applied");
         return false;
      }

      if(stopLoss == 0 && trailing)
      {
         Print("StopLoss required for Trailing");
         return false;
      }
      
      const double minLot = s.get(SYMBOL_VOLUME_MIN);
      if(lots < minLot)
      {
         lots = minLot;
         Print("Minimal lot ", (float)minLot, " is applied for ", symbol);
      }
      
      const double maxLot = s.get(SYMBOL_VOLUME_MAX);
      if(lots > maxLot)
      {
         lots = maxLot;
         Print("Maximal lot ", (float)maxLot, " is applied for ", symbol);
      }
      
      if(hourStart == hourEnd && hourStart != 0)
      {
         Print("For 24-hour schedule use 0-0 hours or disable UseTime");
         return false;
      }
      
      // check if specified symbol exists
      // AND initiate its history loading into the tester (when running in the tester)
      // without this the tester loads only one symbol selected for the chart
      double rates[1];
      const bool success = CopyClose(symbol, PERIOD_CURRENT, 0, 1, rates) > -1;
      if(!success)
      {
         Print("Unknown symbol: ", symbol, " ", E2S(_LastError));
      }
      return success;
   }
    
   void print() const
   {
      Print(symbol, (startType == BREAKOUT ? "+" : "-"), (float)lots,
        "*", (float)factor,
        "^", limit,
        "(", stopLoss, ",", takeProfit, ")",
        useTime ? "[" + (string)hourStart + "," + (string)hourEnd + "]": "");
   }
};

//+------------------------------------------------------------------+
//| Simple common interface for trading                              |
//+------------------------------------------------------------------+
interface TradingStrategy
{
   virtual bool trade(void);
   virtual bool statement(TradeReportWriter *writer = NULL);
};

//+------------------------------------------------------------------+
//| Simple common interface for trading signals                      |
//+------------------------------------------------------------------+
interface TradingSignal
{
   virtual int signal(void);
};

//+------------------------------------------------------------------+
//| Helper class to manage and read data from Unity indicator        |
//+------------------------------------------------------------------+
class UnityController
{
   int handle;          // indicator handle
   int buffers;         // number of buffers, registered in the indicator
   const int bar;       // bar number where to read values
   double data[];       // current values read from the buffers
   datetime lastRead;   // last time values were read
   const bool tickwise; // work by ticks (true) or by bars (false)
   MultiSymbolMonitor sync;
   
public:
   UnityController(const string symbolList, const int offset, const int limit,
      const ENUM_APPLIED_PRICE type, const ENUM_MA_METHOD method, const int period):
      bar(offset), tickwise(!offset)
   {
      handle = iCustom(_Symbol, _Period, "::Indicators\\MQL5Book\\p6\\UnityPercentEvent.ex5",
         symbolList, limit, type, method, period);
      lastRead = 0;
      
      string symbols[];
      const int n = StringSplit(symbolList, ',', symbols);
      for(int i = 0; i < n; ++i)
      {
         sync.attach(symbols[i]);
      }
   }
   
   ~UnityController()
   {
      IndicatorRelease(handle);
   }
   
   void attached(const int b)
   {
      buffers = b;
      ArrayResize(data, buffers);
   }
   
   bool isReady()
   {
      return sync.check(true) == 0;
   }
   
   bool isNewTime() const
   {
      return lastRead != lastTime();
   }
   
   datetime lastTime() const
   {
      // when in sync, all symbols have the same 0-th bar time
      return tickwise ? TimeTradeServer() : iTime(_Symbol, _Period, 0);
   }

   bool getOuterIndices(int &min, int &max)
   {
      if(isNewTime())
      {
         if(!read()) return false;
      }
      max = ArrayMaximum(data);
      min = ArrayMinimum(data);
      return true;
   }
   
   double operator[](const int buffer)
   {
      if(isNewTime())
      {
         if(!read())
         {
            return EMPTY_VALUE;
         }
      }
      return data[buffer];
   }
   
   bool read()
   {
      if(!buffers) return false;
      for(int i = 0; i < buffers; ++i)
      {
         double temp[1];
         if(CopyBuffer(handle, i, bar, 1, temp) == 1)
         {
            data[i] = temp[0];
         }
         else
         {
            return false;
         }
      }
      lastRead = lastTime();
      return true;
   }
};

//+------------------------------------------------------------------+
//| Concrete trading signal based on Unity indicator                 |
//+------------------------------------------------------------------+
class UnitySignal: public TradingSignal
{
   UnityController *controller;
   const int currency1;
   const int currency2;
   
public:
   UnitySignal(UnityController *parent, const int c1, const int c2):
      controller(parent), currency1(c1), currency2(c2) { }
   
   virtual int signal(void) override
   {
      if(!controller.isReady()) return 0; // wait until go out of sync
      if(!controller.isNewTime()) return 0;
      
      int min, max;
      if(!controller.getOuterIndices(min, max)) return 0;
      
      // overbought - can be breakout or pullback
      if(currency1 == max && currency2 == min) return +1;
      // oversold - can be breakout or pullback
      if(currency2 == max && currency1 == min) return -1;
      
      // can emit early exit signals as well
      // if(controller[currency1] > controller[currency2]) return +2; // exit sells
      // if(controller[currency1] < controller[currency2]) return -2; // exit buys
      
      return 0;
   }
};

//+------------------------------------------------------------------+
//| Special PositionState with on-the-fly equity monitoring          |
//+------------------------------------------------------------------+
class PositionStateWithEquity: public PositionState
{
   TradeReport *report;
   
public:
   PositionStateWithEquity(const long t, TradeReport *r): PositionState(t), report(r)
   {
   }
   
   ~PositionStateWithEquity()
   {
      if(HistorySelectByPosition(get(POSITION_IDENTIFIER)))
      {
         double result = 0;
         DealFilter filter;
         int props[] = {DEAL_PROFIT, DEAL_SWAP, DEAL_COMMISSION, DEAL_FEE};
         Tuple4<double, double, double, double> overheads[];
         if(filter.select(props, overheads))
         {
            for(int i = 0; i < ArraySize(overheads); ++i)
            {
               result += NormalizeDouble(overheads[i]._1, 2) + NormalizeDouble(overheads[i]._2, 2)
                  + NormalizeDouble(overheads[i]._3, 2) + NormalizeDouble(overheads[i]._4, 2);
            }
         }
         if(CheckPointer(report) != POINTER_INVALID) report.addBalance(result);
      }
   }
};

//+------------------------------------------------------------------+
//| Main class with trading strategy                                 |
//+------------------------------------------------------------------+
class UnityMartingale: public TradingStrategy
{
protected:
   Settings settings;
   SymbolMonitor symbol;
   AutoPtr<PositionState> position;
   AutoPtr<TrailingStop> trailing;
   AutoPtr<TradingSignal> command;

   TradeReport report;
   TradeReport::DrawDown equity;
   const double deposit;
   const datetime epoch;
    
   double lotsStep;
   double lotsLimit;
   double takeProfit, stopLoss;
    
   bool paused;
   datetime badConditions;

public:
   UnityMartingale(const Settings &state, TradingSignal *signal):
      symbol(state.symbol), deposit(AccountInfoDouble(ACCOUNT_BALANCE)), epoch(TimeCurrent())
   {
      settings = state;
      paused = false;
      badConditions = 0;
      command = signal;
      equity.calcDrawdown(deposit);

      // assign member variables
      const double point = symbol.get(SYMBOL_POINT);
      takeProfit = settings.takeProfit * point;
      stopLoss = settings.stopLoss * point;
      lotsLimit = settings.lots;
      lotsStep = symbol.get(SYMBOL_VOLUME_STEP);
      
      // calculate maximal lot after predefined number of multiplications
      for(int pos = 0; pos < (int)settings.limit; pos++)
      {
         lotsLimit = MathFloor((lotsLimit * settings.factor) / lotsStep) * lotsStep;
      }

      double maxLot = symbol.get(SYMBOL_VOLUME_MAX);
      if(lotsLimit > maxLot)
      {
         lotsLimit = maxLot;
      }

      // pick up existing positions (if any)
      PositionFilter positions;
      ulong tickets[];
      positions.let(POSITION_MAGIC, settings.magic).let(POSITION_SYMBOL, settings.symbol)
         .select(tickets);
      const int n = ArraySize(tickets);
      if(n > 1)
      {
         Alert(StringFormat("Too many positions: %d", n));
         // TODO: close old positions
      }
      else if(n > 0)
      {
         position = MQLInfoInteger(MQL_TESTER) ? new PositionStateWithEquity(tickets[0], &report) : new PositionState(tickets[0]);
         if(settings.stopLoss && settings.trailing)
         {
           trailing = new TrailingStop(tickets[0], settings.stopLoss,
              ((int)symbol.get(SYMBOL_SPREAD) + 1) * 2);
         }
      }
   }
   
   virtual bool statement(TradeReportWriter *writer = NULL) override
   {
      if(MQLInfoInteger(MQL_TESTER))
      {
         Print("Separate trade report for ", settings.symbol);
         // equity drawdown should have been already calculated on-the-fly
         Print("Equity DD:");
         equity.print();
         
         // balance drawdown is calculated along with resulting report
         Print("Trade Statistics (with Balance DD):");
         DealFilter filter;
         filter.let(DEAL_SYMBOL, settings.symbol)
            .let(DEAL_MAGIC, settings.magic, IS::EQUAL_OR_ZERO);
            // we need ZERO magic for last exit made by the tester
         HistorySelect(0, LONG_MAX);
         TradeReport::GenericStats stats =
            report.calcStatistics(filter, deposit, epoch);
         stats.print();
         if(CheckPointer(writer) != POINTER_INVALID)
         {
            double data[];               // balance values
            datetime time[];             // keep time sync in curves
            report.getCurve(data, time);
            return writer.addCurve(stats, data, time, settings.symbol);
         }
         return true;
      }
      return false;
   }

   bool scheduled(const datetime now)
   {
      const long hour = (now % 86400) / 3600;
      if(settings.hourStart < settings.hourEnd)
      {
         return hour >= settings.hourStart && hour < settings.hourEnd;
      }
      else
      {
         return hour >= settings.hourStart || hour < settings.hourEnd;
      }
      return true;
   }

   virtual bool trade() override
   {
      // if an error occured in the recent past, wait a predefined period
      if(settings.skipTimeOnError > 0 && badConditions ==
         TimeCurrent() / settings.skipTimeOnError * settings.skipTimeOnError)
      {
         return false;
      }
      
      if(MQLInfoInteger(MQL_TESTER))
      {
         if(position[])
         {
            report.resetFloatingPL(); // after reset we need to sum up all floating PLs
            // calling addFloatingPL for every position,
            // but in this strategy we have only 1 position at a time
            report.addFloatingPL(position[].get(POSITION_PROFIT) + position[].get(POSITION_SWAP));
            equity.calcDrawdown(report.getCurrent()); // once all floating PL added - calculate DD
         }
      }

      // work hours
      if(settings.useTime && !scheduled(TimeCurrent()))
      {
         // if position is open - close it
         if(position[] && position[].isReady())
         {
            if(close(position[].get(POSITION_TICKET)))
            {
               // NB: we could keep position in cache to find new direction
               // and continue series of lot multiplication between schedules,
               // then do not do "NULLifing" on the next line
               position = NULL;
            }
            else
            {
               // errors are handled inside 'close', i.e. trading is paused for a while
               position[].refresh(); // make sure 'ready' flag is dropped or kept by actual state
            }
         }
         return false;
      }
      
      int s = command[].signal();
      if(s != 0)
      {
         if(settings.startType == PULLBACK) s *= -1; // reverse logic
      }
      
      ulong ticket = 0;

      if(position[] == NULL) // fresh start - no position existing or existed
      {
         if(s == +1)
         {
            ticket = openBuy(settings.lots);
         }
         else if(s == -1)
         {
            ticket = openSell(settings.lots);
         }
      }
      else
      {
         if(position[].refresh()) // position still exists
         {
            if((position[].get(POSITION_TYPE) == POSITION_TYPE_BUY && s == -1)
            || (position[].get(POSITION_TYPE) == POSITION_TYPE_SELL && s == +1))
            {
               PrintFormat("Opposite signal: %d for position %d %lld",
                  s, position[].get(POSITION_TYPE), position[].get(POSITION_TICKET));
               if(close(position[].get(POSITION_TICKET)))
               {
                  // position = NULL; - keep position info in the cache
               }
               else
               {
                  // errors are handled inside 'close', i.e. trading is paused for a while
                  position[].refresh(); // make sure 'ready' flag is dropped if closed anyway
               }
            }
            else
            {
               position[].update();
               if(trailing[]) trailing[].trail();
            }
         }
         else // position closed - let open next one
         {
            if(s == 0) // while no signals keep martingale
            {
               // use cached object to read former position properties: profit and lot
               if(position[].get(POSITION_PROFIT) >= 0.0) 
               {
                  // keep previous trade direction
                  if(position[].get(POSITION_TYPE) == POSITION_TYPE_BUY)
                     ticket = openBuy(settings.lots); // BUY in case of previous profitable BUY
                  else
                     ticket = openSell(settings.lots); // SELL in case of previous profitable SELL
               }
               else
               {
                  double lots = MathFloor((position[].get(POSITION_VOLUME) * settings.factor) / lotsStep) * lotsStep;
      
                  if(lotsLimit < lots)
                  {
                     lots = settings.lots;
                  }
                
                  // change trade direction
                  if(position[].get(POSITION_TYPE) == POSITION_TYPE_BUY)
                     ticket = openSell(lots); // SELL in case of previous lossy BUY
                  else
                     ticket = openBuy(lots); // BUY in case of previous lossy SELL
               }
            }
            else // if a signal exists do multiplied lots after a loss
            {
               double lots;
               if(position[].get(POSITION_PROFIT) >= 0.0)
               {
                  lots = settings.lots;
               }
               else
               {
                  lots = MathFloor((position[].get(POSITION_VOLUME) * settings.factor) / lotsStep) * lotsStep;
      
                  if(lotsLimit < lots)
                  {
                     lots = settings.lots;
                  }               
               }
               
               ticket = (s == +1) ? openBuy(lots) : openSell(lots);
            }
         }
      }
      
      if(ticket > 0) // new position is just opened
      {
         position = MQLInfoInteger(MQL_TESTER) ? new PositionStateWithEquity(ticket, &report) : new PositionState(ticket);
         if(settings.stopLoss && settings.trailing)
         {
            trailing = new TrailingStop(ticket, settings.stopLoss,
               ((int)symbol.get(SYMBOL_SPREAD) + 1) * 2);
         }
      }
  
      return true;
    }

protected:
   bool checkFreeMargin(const ENUM_ORDER_TYPE type, const double price, double &lots) const
   {
      double margin;
      if(OrderCalcMargin(type, settings.symbol, lots, price, margin))
      {
         if(AccountInfoDouble(ACCOUNT_MARGIN_FREE) > margin)
         {
            return true;
         }
      }

      // fallback to starting lot due to insufficient margin
      lots = settings.lots;

      if(OrderCalcMargin(type, settings.symbol, lots, price, margin))
      {
         return AccountInfoDouble(ACCOUNT_MARGIN_FREE) > margin;
      }
      
      return false;
   }
    
   void prepare(MqlTradeRequestSync &request)
   {
      request.deviation = (int)(symbol.get(SYMBOL_SPREAD) + 1) * 2;
      request.magic = settings.magic;
   }
    
   ulong postprocess(MqlTradeRequestSync &request)
   {
      if(request.result.order == 0)
      {
         badConditions = TimeCurrent() / settings.skipTimeOnError * settings.skipTimeOnError;
         if(!paused)
         {
            Print("Pausing due to bad conditions: ", badConditions);
            paused = true;
         }
      }
      else
      {
         if(request.completed())
         {
            paused = false;
            return request.result.position;
         }
      }
      return 0;
   }
    
   ulong openBuy(double lots)
   {
      const double price = symbol.get(SYMBOL_ASK);
      
      if(!checkFreeMargin(ORDER_TYPE_BUY, price, lots)) return 0;
      
      MqlTradeRequestSync request;
      prepare(request);
      if(request.buy(settings.symbol, lots, price,
         stopLoss ? price - stopLoss : 0,
         takeProfit ? price + takeProfit : 0))
      {
         return postprocess(request);
      }
      return 0;
   }
    
   ulong openSell(double lots)
   {
      const double price = symbol.get(SYMBOL_BID);

      if(!checkFreeMargin(ORDER_TYPE_SELL, price, lots)) return 0;
      
      MqlTradeRequestSync request;
      prepare(request);
      if(request.sell(settings.symbol, lots, price,
         stopLoss ? price + stopLoss : 0,
         takeProfit ? price - takeProfit : 0))
      {
         return postprocess(request);
      }
      return 0;
   }
    
   bool close(const ulong ticket)
   {
      MqlTradeRequestSync request;
      prepare(request);
      return request.close(ticket) && postprocess(request);
   }
};

//+------------------------------------------------------------------+
//| Manager for a set of TradingStrategy instances                   |
//+------------------------------------------------------------------+
class TradingStrategyPool: public TradingStrategy
{
private:
   AutoPtr<TradingStrategy> pool[];

public:
   TradingStrategyPool(const int reserve = 0)
   {
      ArrayResize(pool, 0, reserve);
   }

   TradingStrategyPool(TradingStrategy *instance)
   {
      push(instance);
   }

   void push(TradingStrategy *instance)
   {
      int n = ArraySize(pool);
      ArrayResize(pool, n + 1);
      pool[n] = instance;
   }
    
   virtual bool trade() override
   {
      for(int i = 0; i < ArraySize(pool); i++)
      {
         pool[i][].trade();
      }
      return true;
   }
   
   virtual bool statement(TradeReportWriter *writer) override
   {
      bool result = false;
      for(int i = 0; i < ArraySize(pool); i++)
      {
         result = pool[i][].statement(writer) || result;
      }
      return result;
   }
};

//+------------------------------------------------------------------+
//| Global pointer for the pool of strategies                        |
//+------------------------------------------------------------------+
AutoPtr<TradingStrategyPool> pool;
AutoPtr<UnityController> controller;

int currenciesCount;
string currencies[];

//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
{
   currenciesCount = 0;
   ArrayResize(currencies, 0);

   if(!StartUp(true)) return INIT_PARAMETERS_INCORRECT;
   
   TesterHideIndicators(true);

   const bool barwise = UnityPriceType == PRICE_CLOSE && UnityPricePeriod == 1;
   controller = new UnityController(UnitySymbols, barwise,
      UnityBarLimit, UnityPriceType, UnityPriceMethod, UnityPricePeriod);
   // now waiting for indicator messages about buffers/currencies
   return INIT_SUCCEEDED;
}

//+------------------------------------------------------------------+
//| Get indices of 2 currencies building up given forex pair         |
//+------------------------------------------------------------------+
bool SplitSymbolToCurrencyIndices(const string symbol, int &first, int &second)
{
   const string s1 = SymbolInfoString(symbol, SYMBOL_CURRENCY_BASE);
   const string s2 = SymbolInfoString(symbol, SYMBOL_CURRENCY_PROFIT);
   first = second = -1;
   for(int i = 0; i < ArraySize(currencies); ++i)
   {
      if(currencies[i] == s1) first = i;
      else if(currencies[i] == s2) second = i;
   }
   
   return first != -1 && second != -1;
}

//+------------------------------------------------------------------+
//| Applied initialization function (called on different events)     |
//+------------------------------------------------------------------+
bool StartUp(const bool init = false)
{
   if(WorkSymbols == "")
   {
      Settings settings =
      {
         UseTime, HourStart, HourEnd,
         Lots, Factor, Limit,
         StopLoss, TakeProfit,
         StartType, Magic, SkipTimeOnError, Trailing, _Symbol
      };
      
      if(settings.validate())
      {
         if(init)
         {
            Print("Input settings:");
            settings.print();
         }
      }
      else
      {
         if(init) Print("Wrong settings, please fix");
         return false;
      }
   
      if(!init)
      {
         controller[].attached(currenciesCount);
         // split _Symbol to 2 of currencies[]
         int first, second;
         if(!SplitSymbolToCurrencyIndices(_Symbol, first, second))
         {
            PrintFormat("Can't find currencies (%s %s) for %s",
               (first == -1 ? "base" : ""), (second == -1 ? "profit" : ""), _Symbol);
            return false;
         }
         pool = new TradingStrategyPool(new UnityMartingale(settings,
            new UnitySignal(controller[], first, second)));
      }
   }
   else
   {
      Print("Parsed settings:");
      Settings settings[];

      if(!Settings::parseAll(WorkSymbols, settings))
      {
         if(init) Print("Settings are incorrect, can't start up");
         return false;
      }
      
      if(!init)
      {
         controller[].attached(currenciesCount);
      
         const int n = ArraySize(settings);
         pool = new TradingStrategyPool(n);
         for(int i = 0; i < n; i++)
         {
            if(settings[i].symbol == NULL) continue; // skip incorrect settings
            
            settings[i].skipTimeOnError = SkipTimeOnError;
            settings[i].trailing = Trailing;
            // support many subsystems on the same symbol on a hedge account
            settings[i].magic = Magic + i;  // different magic for every subsystem
            
            // split settings[i].symbol to 2 of currencies[]
            int first, second;
            if(!SplitSymbolToCurrencyIndices(settings[i].symbol, first, second))
            {
               PrintFormat("Can't find currencies (%s %s) for %s",
                  (first == -1 ? "base" : ""), (second == -1 ? "profit" : ""), settings[i].symbol);
            }
            else
            {
               pool[].push(new UnityMartingale(settings[i],
                  new UnitySignal(controller[], first, second)));
            }
         }
      }
   }
   
   return true;
}

//+------------------------------------------------------------------+
//| Chart event handler                                              |
//+------------------------------------------------------------------+
void OnChartEvent(const int id, const long &lparam, const double &dparam, const string &sparam)
{
   if(id == CHARTEVENT_CUSTOM + UnityBarLimit)
   {
      PrintFormat("%lld %f '%s'", lparam, dparam, sparam);
      if(lparam == 0) ArrayResize(currencies, 0);
      currenciesCount = (int)MathRound(dparam);
      PUSH(currencies, sparam);

      if(ArraySize(currencies) == currenciesCount)
      {
         if(pool[] == NULL)
         {
            StartUp(); // confirms that indicator is ready, we can start trading
         }
         else
         {
            Alert("Repeated initialization!");
         }
      }
   }
}

//+------------------------------------------------------------------+
//| Tick event handler                                               |
//+------------------------------------------------------------------+
void OnTick()
{
   CheckTickModel();
   if(pool[] != NULL)
   {
      pool[].trade();
   }
}

//+------------------------------------------------------------------+
//| Tester event handler                                             |
//+------------------------------------------------------------------+
string status = "status";

#define STAT_PROPS 5

double OnTester()
{
   if(IsStopped())
   {
      FrameAdd(status, 1, 0, NULL);
      return 0;
   }
   
   HistorySelect(0, LONG_MAX);
   
   const ENUM_DEAL_PROPERTY_DOUBLE props[STAT_PROPS] =
   {
      DEAL_PROFIT, DEAL_SWAP, DEAL_COMMISSION, DEAL_FEE, DEAL_VOLUME
   };
   double expenses[][STAT_PROPS];
   ulong tickets[]; // used here only to match 'select' prototype, but helpful for debug
   
   DealFilter filter;
   filter.let(DEAL_TYPE, (1 << DEAL_TYPE_BUY) | (1 << DEAL_TYPE_SELL), IS::OR_BITWISE)
      .let(DEAL_ENTRY, (1 << DEAL_ENTRY_OUT) | (1 << DEAL_ENTRY_INOUT) | (1 << DEAL_ENTRY_OUT_BY), IS::OR_BITWISE)
      .select(props, tickets, expenses);

   const int n = ArraySize(tickets);
   const bool singleSymbol = WorkSymbols == "";
   
   double balance[];  // adjusted by trade volumes for using R2 criterion
   double curve[];    // plain balance curve for visualization
   datetime stamps[]; // datetime per point on the balance curve
   
   ArrayResize(balance, n + 1);
   balance[0] = TesterStatistics(STAT_INITIAL_DEPOSIT);
   
   if(!singleSymbol) // overall balance is showing only when many symbols involved
   {
      ArrayResize(curve, n + 1);
      ArrayResize(stamps, n + 1);
      curve[0] = TesterStatistics(STAT_INITIAL_DEPOSIT);
      
      // MQL5 does not provide starting time of the test right away,
      // we could find it in the 1-st deal, but it's outside our filter by deals
      // so using 0 means just "don't count on the time of this point"
      stamps[0] = 0;
   }
   
   for(int i = 0; i < n; ++i)
   {
      double result = 0;
      for(int j = 0; j < STAT_PROPS - 1; ++j)
      {
         result += expenses[i][j];
      }
      if(!singleSymbol)
      {
         curve[i + 1] = result + curve[i];
         stamps[i + 1] = (datetime)HistoryDealGetInteger(tickets[i], DEAL_TIME);
      }
      result /= expenses[i][STAT_PROPS - 1]; // normalize by volume
      balance[i + 1] = result + balance[i];
   }

   const double r2 = RSquaredTest(balance);
   const static string tempfile = "temp.html";
   
   // generate single pass report with multiple symbol support
   HTMLReportWriter writer(tempfile, MINIWIDTH, MINIHEIGHT);
   if(pool[] != NULL)
   {
      pool[].statement(&writer); // ask trading systems to output their results
   }
   if(!singleSymbol)
   {
      TradeReport::GenericStats stats;
      stats.fillByTester();
      // we can't add overall 'balance' on the same graph because
      // it's normalized by trade volumes, so we use another array 'curve'
      writer.addCurve(stats, curve, stamps, "Overall", clrBlack, 2);
   }
   writer.render();
   writer.close();
   
   // send the file via frame during optimization
   // terminal will save those with better performance
   if(MQLInfoInteger(MQL_OPTIMIZATION))
   {
      FrameAdd(tempfile, 0, r2 * 100, tempfile);
   }
   
   return r2 * 100;
}

//+------------------------------------------------------------------+
//| Struct with most important info about a single tester pass       |
//+------------------------------------------------------------------+
struct Pass
{
   ulong id;
   double value;
   string parameters; // optimized 'name=value' pairs
   string preset;     // text for set-file (all parameters)
};

Pass TopPasses[];     // stack of gradually improving passes (the last the best)
Pass BestPass;        // currently best pass
string ReportPath;    // dedicated folder for all html-files for this run

//+------------------------------------------------------------------+
//| Optimization initialization                                      |
//+------------------------------------------------------------------+
void OnTesterInit()
{
   BestPass.value = -DBL_MAX;
   ReportPath = _Symbol + "-" + PeriodToString(_Period) + "-" + MQLInfoString(MQL_PROGRAM_NAME) + "/";
}

//+------------------------------------------------------------------+
//| Optimization deinitialization                                    |
//+------------------------------------------------------------------+
void OnTesterDeinit()
{
   // read last 100 records in TopPasses and generate overall.htm file for them
   int handle = FileOpen(ReportPath + "overall.htm", FILE_WRITE | FILE_TXT | FILE_ANSI, 0, CP_UTF8);
   string headerAndFooter[2];
   StringSplit(OptReportPageTemplate, '~', headerAndFooter);
   StringReplace(headerAndFooter[0], "%MINIWIDTH%", (string)MINIWIDTH);
   StringReplace(headerAndFooter[0], "%MINIHEIGHT%", (string)(MINIHEIGHT + MINIMARGIN));
   FileWriteString(handle, headerAndFooter[0]);
   for(int i = ArraySize(TopPasses) - 1, k = 0; i >= 0 && k < 100; --i, ++k)
   {
      string p = TopPasses[i].parameters;
      StringReplace(p, "&", " ");
      const string filename = StringFormat("%06.3f-%lld.htm",
         TopPasses[i].value, TopPasses[i].id);
      string element = OptReportElementTemplate;
      StringReplace(element, "%FILENAME%", filename);
      StringReplace(element, "%PARAMETERS%", TopPasses[i].parameters);
      StringReplace(element, "%PARAMETERS_SPACED%", p);
      StringReplace(element, "%PASS%", IntegerToString(TopPasses[i].id));
      StringReplace(element, "%PRESET%", TopPasses[i].preset);
      StringReplace(element, "%MINIWIDTH%", (string)(MINIWIDTH + 2 * MINIMARGIN));
      StringReplace(element, "%MINIHEIGHT%", (string)(MINIHEIGHT + MINIMARGIN));
      FileWriteString(handle, element);
   }
   FileWriteString(handle, headerAndFooter[1]);
   FileClose(handle);
}

//+------------------------------------------------------------------+
//| Optimization pass (frame), used for error detection here         |
//+------------------------------------------------------------------+
void OnTesterPass()
{
   ulong   pass;
   string  name;
   long    id;
   double  value;
   uchar   data[];

   // input parameters for the pass where the frame belongs
   string  params[];
   uint    count;

   while(FrameNext(pass, name, id, value, data))
   {
      if(name == status && id == 1)
      {
         Alert("Please stop optimization!");
         Alert("Tick model is incorrect: OHLC M1 or better is required");
         // could be great if this call would stop optimization itself,
         // but it is not
         ExpertRemove();
      }
      else
      {
         // collect passes with improved stats
         if(value > BestPass.value && FrameInputs(pass, params, count))
         {
            BestPass.preset = "";
            BestPass.parameters = "";
            // extract optimized and overall parameters to form a set-file
            for(uint i = 0; i < count; i++)
            {
               string name2value[];
               int n = StringSplit(params[i], '=', name2value);
               if(n == 2)
               {
                  long pvalue, pstart, pstep, pstop;
                  bool enabled = false;
                  if(ParameterGetRange(name2value[0], enabled, pvalue, pstart, pstep, pstop))
                  {
                     if(enabled)
                     {
                        if(StringLen(BestPass.parameters)) BestPass.parameters += "&";
                        BestPass.parameters += params[i];
                     }
                     
                     BestPass.preset += params[i] + "||"
                       + (string)pstart + "||" + (string)pstep + "||" + (string)pstop + "||"
                       + (enabled ? "Y" : "N") + "<br>\n";
                  }
                  else
                  {
                     BestPass.preset += params[i] + "<br>\n";
                  }
               }
            }
         
            BestPass.value = value;
            BestPass.id = pass;
            PUSH(TopPasses, BestPass);
            // write received frame with html-report into a file
            const string text = CharArrayToString(data);
            int handle = FileOpen(StringFormat(ReportPath + "%06.3f-%lld.htm", value, pass),
               FILE_WRITE | FILE_TXT | FILE_ANSI);
            FileWriteString(handle, text);
            FileClose(handle);
         }
      }
   }
}

//+------------------------------------------------------------------+
//| Proper tick model guard                                          |
//+------------------------------------------------------------------+
void CheckTickModel()
{
   if(MQLInfoInteger(MQL_TESTER))
   {
      static const TICK_MODEL minimalRequiredQuality = TICK_MODEL_OHLC_M1;

      static ulong count = 0;
      static const ulong detector = 3;
      if(count++ < detector)
      {
         const TICK_MODEL model = getTickModel();
         if(count >= 2)
         {
            if(minimalRequiredQuality < model)
            {
               PrintFormat("Tick model is incorrect (%s %sis required), terminating",
                  EnumToString(minimalRequiredQuality),
                  (minimalRequiredQuality != TICK_MODEL_REAL ? "or better " : ""));
               ExpertRemove();  // will set _StopFlag flag
               // TesterStop(); // will NOT set _StopFlag flag
            }
         }
      }
   }
}
//+------------------------------------------------------------------+
