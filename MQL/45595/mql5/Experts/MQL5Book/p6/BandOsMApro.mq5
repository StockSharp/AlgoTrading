//+------------------------------------------------------------------+
//|                                                  BandOsMApro.mq5 |
//|                              Copyright (c) 2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright "Copyright (c) 2022, MetaQuotes Ltd."
#property link "https://www.mql5.com"
#property description "Trading strategy based on OsMA, BBands and MA indicators."

#include <MQL5Book/SymbolMonitor.mqh>
#include <MQL5Book/PositionFilter.mqh>
#include <MQL5Book/DealFilter.mqh>
#include <MQL5Book/MqlTradeSync.mqh>
#include <MQL5Book/TradeState.mqh>
#include <MQL5Book/AutoPtr.mqh>
#include <MQL5Book/TrailingStop.mqh>

#define USE_R2_CRITERION
#ifdef USE_R2_CRITERION
#include <MQL5Book/RSquared.mqh>
#endif

//+------------------------------------------------------------------+
//| Inputs                                                           |
//+------------------------------------------------------------------+

input group "C O M M O N   S E T T I N G S"
sinput ulong Magic = 1234567890;
input double Lots = 0.01;
input int StopLoss = 1000;

input group "O S M A   S E T T I N G S"
input int FastOsMA = 12;
input int SlowOsMA = 26;
input int SignalOsMA = 9;
input ENUM_APPLIED_PRICE PriceOsMA = PRICE_TYPICAL;

input group "B B A N D S   S E T T I N G S"
input int BandsMA = 26;
input int BandsShift = 0;
input double BandsDeviation = 2.0;

input group "M A   S E T T I N G S"
input int PeriodMA = 10;
input int ShiftMA = 0;
input ENUM_MA_METHOD MethodMA = MODE_SMA;

input group "A U X I L I A R Y"
sinput int FastSlowCombo4Optimization = 0;   // (reserved for optimization)
sinput ulong FastShadow4Optimization = 0;    // (reserved for optimization)
sinput ulong SlowShadow4Optimization = 0;    // (reserved for optimization)
sinput ulong StepsShadow4Optimization = 0;   // (reserved for optimization)


//+------------------------------------------------------------------+
//| Simple common interface for trading                              |
//+------------------------------------------------------------------+
interface TradingStrategy
{
   virtual bool trade(void);
};

//+------------------------------------------------------------------+
//| Simple common interface for trading signals                      |
//+------------------------------------------------------------------+
interface TradingSignal
{
   virtual int signal(void);
};

//+------------------------------------------------------------------+
//| Trade signal detector based on 3 indicators                      |
//+------------------------------------------------------------------+
class BandOsMaSignal: public TradingSignal
{
   int hOsMA, hBands, hMA;
   int direction;
public:
   BandOsMaSignal(const int fast, const int slow, const int signal, const ENUM_APPLIED_PRICE price,
      const int bands, const int shift, const double deviation,
      const int period, const int x, ENUM_MA_METHOD method)
   {
      hOsMA = iOsMA(_Symbol, _Period, fast, slow, signal, price);
      hBands = iBands(_Symbol, _Period, bands, shift, deviation, hOsMA);
      hMA = iMA(_Symbol, _Period, period, x, method, hOsMA);
      direction = 0;
   }
   
   ~BandOsMaSignal()
   {
      IndicatorRelease(hMA);
      IndicatorRelease(hBands);
      IndicatorRelease(hOsMA);
   }
   
   virtual int signal(void) override
   {
      double osma[2], upper[2], lower[2], ma[2];
      // copy 2 values from bars 1 and 2 for every indicator
      if(CopyBuffer(hOsMA, 0, 1, 2, osma) != 2) return 0;
      if(CopyBuffer(hBands, UPPER_BAND, 1, 2, upper) != 2) return 0;
      if(CopyBuffer(hBands, LOWER_BAND, 1, 2, lower) != 2) return 0;
      if(CopyBuffer(hMA, 0, 1, 2, ma) != 2) return 0;
      
      // if there was a signal, check if it's over
      if(direction != 0)
      {
         if(direction > 0)
         {
            if(osma[0] >= ma[0] && osma[1] < ma[1])
            {
               direction = 0;
            }
         }
         else
         {
            if(osma[0] <= ma[0] && osma[1] > ma[1])
            {
               direction = 0;
            }
         }
      }
      
      // in any case check for new signals      
      if(osma[0] <= lower[0] && osma[1] > lower[1])
      {
         direction = +1;
      }
      else if(osma[0] >= upper[0] && osma[1] < upper[1])
      {
         direction = -1;
      }
      
      return direction;
   }
};

//+------------------------------------------------------------------+
//| Main class with trading strategy                                 |
//+------------------------------------------------------------------+
class SimpleStrategy: public TradingStrategy
{
protected:
   AutoPtr<PositionState> position;
   AutoPtr<TrailingStop> trailing;
   AutoPtr<TradingSignal> command;

   const int stopLoss;
   const ulong magic;
   const double lots;
   
   datetime lastBar;
    
public:
   SimpleStrategy(TradingSignal *signal, const ulong m, const int sl, const double v):
      command(signal), magic(m), stopLoss(sl), lots(v), lastBar(0)
   {
      // pick up existing positions (if any)
      PositionFilter positions;
      ulong tickets[];
      positions.let(POSITION_MAGIC, magic).let(POSITION_SYMBOL, _Symbol).select(tickets);
      const int n = ArraySize(tickets);
      if(n > 1)
      {
         Alert(StringFormat("Too many positions: %d", n));
         // TODO: close old positions
      }
      else if(n > 0)
      {
         position = new PositionState(tickets[0]);
         if(stopLoss)
         {
           trailing = new TrailingStop(tickets[0], stopLoss, stopLoss / 50);
         }
      }
   }
   
   virtual bool trade() override
   {
      // work only once per bar, at its opening
      if(lastBar == iTime(_Symbol, _Period, 0)) return false;
      
      int s = command[].signal(); // get the signal
      
      ulong ticket = 0;
      
      if(position[] != NULL)
      {
         if(position[].refresh()) // position still exists
         {
            // the signal is reversed or does not exist anymore
            if((position[].get(POSITION_TYPE) == POSITION_TYPE_BUY && s != +1)
            || (position[].get(POSITION_TYPE) == POSITION_TYPE_SELL && s != -1))
            {
               PrintFormat("Signal lost: %d for position %d %lld",
                  s, position[].get(POSITION_TYPE), position[].get(POSITION_TICKET));
               if(close(position[].get(POSITION_TICKET)))
               {
                  position = NULL;
               }
               else
               {
                  position[].refresh(); // make sure 'ready' flag is dropped if closed anyway
               }
            }
            else
            {
               position[].update();
               if(trailing[]) trailing[].trail();
            }
         }
         else // position closed
         {
            position = NULL;
         }
      }
      
      if(position[] == NULL)
      {
         if(s != 0)
         {
            ticket = (s == +1) ? openBuy() : openSell();
         }
      }
      
      if(ticket > 0) // new position is just opened
      {
         position = new PositionState(ticket);
         if(stopLoss)
         {
            trailing = new TrailingStop(ticket, stopLoss, stopLoss / 50);
         }
      }
      
      lastBar = iTime(_Symbol, _Period, 0);
      
      return true;
   }

protected:
   void prepare(MqlTradeRequestSync &request)
   {
      request.deviation = stopLoss / 50;
      request.magic = magic;
   }
    
   ulong postprocess(MqlTradeRequestSync &request)
   {
      if(request.completed())
      {
         return request.result.position;
      }
      return 0;
   }
    
   ulong openBuy()
   {
      SymbolMonitor m(_Symbol);
      const double price = m.get(SYMBOL_ASK);
      const double point = m.get(SYMBOL_POINT);
      
      MqlTradeRequestSync request;
      prepare(request);
      if(request.buy(_Symbol, lots, price,
         stopLoss ? price - stopLoss * point : 0, 0))
      {
         return postprocess(request);
      }
      return 0;
   }
    
   ulong openSell()
   {
      SymbolMonitor m(_Symbol);
      const double price = m.get(SYMBOL_BID);
      const double point = m.get(SYMBOL_POINT);
      
      MqlTradeRequestSync request;
      prepare(request);
      if(request.sell(_Symbol, lots, price,
         stopLoss ? price + stopLoss * point : 0, 0))
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
//| Global pointer for the pool of strategies                        |
//+------------------------------------------------------------------+
AutoPtr<TradingStrategy> strategy;

//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
{
   if(FastOsMA >= SlowOsMA) return INIT_PARAMETERS_INCORRECT;
   
   // during optimization we require shadow parameters
   if(MQLInfoInteger(MQL_OPTIMIZATION) && StepsShadow4Optimization == 0)
   {
      return INIT_PARAMETERS_INCORRECT;
   }

   PairOfPeriods p = {FastOsMA, SlowOsMA}; // try original parameters by default
   if(FastShadow4Optimization && SlowShadow4Optimization && StepsShadow4Optimization)
   {
      // if shadow copies are assigned, decode them and override periods settings
      int FastStart = (int)(FastShadow4Optimization & 0xFFFF);
      int FastStop = (int)((FastShadow4Optimization >> 16) & 0xFFFF);
      int SlowStart = (int)(SlowShadow4Optimization & 0xFFFF);
      int SlowStop = (int)((SlowShadow4Optimization >> 16) & 0xFFFF);
      int FastStep = (int)(StepsShadow4Optimization & 0xFFFF);
      int SlowStep = (int)((StepsShadow4Optimization >> 16) & 0xFFFF);
      
      p = Iterate(FastStart, FastStop, FastStep,
         SlowStart, SlowStop, SlowStep, FastSlowCombo4Optimization);
      PrintFormat("MA periods are restored from shadow: FastOsMA=%d SlowOsMA=%d",
         p.fast, p.slow);
   }
   
   strategy = new SimpleStrategy(
      new BandOsMaSignal(p.fast, p.slow, SignalOsMA, PriceOsMA,
         BandsMA, BandsShift, BandsDeviation,
         PeriodMA, ShiftMA, MethodMA),
         Magic, StopLoss, Lots);
   return INIT_SUCCEEDED;
}

//+------------------------------------------------------------------+
//| Tick event handler                                               |
//+------------------------------------------------------------------+
void OnTick()
{
   if(strategy[] != NULL)
   {
      strategy[].trade();
   }
}

//+------------------------------------------------------------------+
//| Helper struct to hold and request all tester stats               |
//+------------------------------------------------------------------+
struct TesterRecord
{
   string feature;
   double value;
      
   static void fill(TesterRecord &stats[])
   {
      ResetLastError();
      for(int i = 0; ; ++i)
      {
         const double v = TesterStatistics((ENUM_STATISTICS)i);
         if(_LastError) return;
         TesterRecord t = {EnumToString((ENUM_STATISTICS)i), v};
         PUSH(stats, t);
      }
   }
};

//+------------------------------------------------------------------+
//| Finalization handler                                             |
//+------------------------------------------------------------------+
void OnDeinit(const int)
{
   TesterRecord stats[];
   TesterRecord::fill(stats);
   ArrayPrint(stats, 2);
}

//+------------------------------------------------------------------+
double sign(const double x)
{
   return x > 0 ? +1 : (x < 0 ? -1 : 0);
}

//+------------------------------------------------------------------+
//| Tester event handler                                             |
//+------------------------------------------------------------------+
double OnTester()
{
#ifdef USE_R2_CRITERION
   return GetR2onBalanceCurve();
#else
   const double profit = TesterStatistics(STAT_PROFIT);
   return sign(profit) * sqrt(fabs(profit))
      * sqrt(TesterStatistics(STAT_PROFIT_FACTOR))
      * sqrt(TesterStatistics(STAT_TRADES))
      * sqrt(fabs(TesterStatistics(STAT_SHARPE_RATIO)));
#endif      
}

#ifdef USE_R2_CRITERION

#define STAT_PROPS 4

//+------------------------------------------------------------------+
//| Build balance curve and estimate R2 for it                       |
//+------------------------------------------------------------------+
double GetR2onBalanceCurve()
{
   HistorySelect(0, LONG_MAX);
   
   const ENUM_DEAL_PROPERTY_DOUBLE props[STAT_PROPS] =
   {
      DEAL_PROFIT, DEAL_SWAP, DEAL_COMMISSION, DEAL_FEE
   };
   double expenses[][STAT_PROPS];
   ulong tickets[]; // used here only to match 'select' prototype, but helpful for debug
   
   DealFilter filter;
   filter.let(DEAL_TYPE, (1 << DEAL_TYPE_BUY) | (1 << DEAL_TYPE_SELL), IS::OR_BITWISE)
      .let(DEAL_ENTRY, (1 << DEAL_ENTRY_OUT) | (1 << DEAL_ENTRY_INOUT) | (1 << DEAL_ENTRY_OUT_BY), IS::OR_BITWISE)
      .select(props, tickets, expenses);

   const int n = ArraySize(tickets);
   
   double balance[];
   
   ArrayResize(balance, n + 1);
   balance[0] = TesterStatistics(STAT_INITIAL_DEPOSIT);
   
   for(int i = 0; i < n; ++i)
   {
      double result = 0;
      for(int j = 0; j < STAT_PROPS; ++j)
      {
         result += expenses[i][j];
      }
      balance[i + 1] = result + balance[i];
   }
   const double r2 = RSquaredTest(balance);
   return r2 * 100;
}
#endif
//+------------------------------------------------------------------+

//+------------------------------------------------------------------+
//| Struct for pair of MA periods (checked for correctness)          |
//+------------------------------------------------------------------+
struct PairOfPeriods
{
   int fast;
   int slow;
};

//+------------------------------------------------------------------+
//| Get a pair of MA periods according to given ranges and steps     |
//+------------------------------------------------------------------+
PairOfPeriods Iterate(const long start1, const long stop1, const long step1,
   const long start2, const long stop2, const long step2,
   const long find = -1)
{
   int count = 0;
   for(int i = (int)start1; i <= (int)stop1; i += (int)step1)
   {
      for(int j = (int)start2; j <= (int)stop2; j += (int)step2)
      {
         if(i < j)
         {
            if(count == find)
            {
               PairOfPeriods p = {i, j};
               return p;
            }
            ++count;
         }
      }
   }
   PairOfPeriods p = {count, 0};
   return p;
}

//+------------------------------------------------------------------+
//| Optimization initialization handler                              |
//+------------------------------------------------------------------+
void OnTesterInit()
{
   bool enabled1, enabled2;
   long value1, start1, step1, stop1;
   long value2, start2, step2, stop2;
   // obtain optimization settings for FastOsMA and SlowOsMA
   if(ParameterGetRange("FastOsMA", enabled1, value1, start1, step1, stop1)
   && ParameterGetRange("SlowOsMA", enabled2, value2, start2, step2, stop2))
   {
      if(enabled1 && enabled2)
      {
         // disable them
         if(!ParameterSetRange("FastOsMA", false, value1, start1, step1, stop1)
         || !ParameterSetRange("SlowOsMA", false, value2, start2, step2, stop2))
         {
            Print("Can't disable optimization by FastOsMA and SlowOsMA: ",
               E2S(_LastError));
            return;
         }
         // find out a number of correct combinations of 2 periods
         PairOfPeriods p = Iterate(start1, stop1, step1, start2, stop2, step2);
         const int count = p.fast;
         // request optimization for this range for new shadow parameter
         ParameterSetRange("FastSlowCombo4Optimization", true, 0, 0, 1, count);
         PrintFormat("Parameter FastSlowCombo4Optimization is enabled with maximum: %d",
            count);
         // send required settings to our copy running on the agent
         const ulong fast = start1 | (stop1 << 16);
         const ulong slow = start2 | (stop2 << 16);
         const ulong step = step1 | (step2 << 16);
         ParameterSetRange("FastShadow4Optimization", false, fast, fast, 1, fast);
         ParameterSetRange("SlowShadow4Optimization", false, slow, slow, 1, slow);
         ParameterSetRange("StepsShadow4Optimization", false, step, step, 1, step);
      }
   }
   else
   {
      Print("Can't adjust optimization by FastOsMA and SlowOsMA: ", E2S(_LastError));
   }
}

//+------------------------------------------------------------------+
//| Optimization finalization handler                                |
//| (for some reason compiler requires this handler as well,         |
//| but it's useless here)                                           |
//+------------------------------------------------------------------+
void OnTesterDeinit()
{
}
//+------------------------------------------------------------------+
