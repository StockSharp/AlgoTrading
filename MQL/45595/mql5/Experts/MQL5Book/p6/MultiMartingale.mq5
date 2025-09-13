//+------------------------------------------------------------------+
//|                                              MultiMartingale.mq5 |
//|                              Copyright (c) 2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright "Copyright (c) 2022, MetaQuotes Ltd."
#property link "https://www.mql5.com"
#property description "Multi-currency expert adviser based on reversal strategy with martingale (Attention: high risks!)."

#define EXTENDED_SETTINGS "MultiMartingale-WorkSymbols.txt"
// #property tester_file EXTENDED_SETTINGS
// NB! If you add the tester_file directive
// when your EA is already selected as active EA in the tester,
// simple recompilation will be not enough to make it work!
// You should de-select your EA in the tester and then select anew -
// only after this the tester will respect your directive and
// pick up the file.

#include <MQL5Book/DateTime.mqh>
#include <MQL5Book/SymbolMonitor.mqh>
#include <MQL5Book/PositionFilter.mqh>
#include <MQL5Book/MqlTradeSync.mqh>
#include <MQL5Book/TradeState.mqh>
#include <MQL5Book/AutoPtr.mqh>
#include <MQL5Book/TrailingStop.mqh>

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

//+------------------------------------------------------------------+
//| Inputs                                                           |
//+------------------------------------------------------------------+

input group "S Y M B O L   S E T T I N G S"
input bool UseTime = true;      // UseTime (HourStart and HourEnd)
input uint HourStart = 2;       // HourStart (0...23)
input uint HourEnd = 22;        // HourEnd (0...23)
input double Lots = 0.01;       // Lots (initial)
input double Factor = 2.0;      // Factor (lot multiplication)
input uint Limit = 5;           // Limit (max number of multiplications)
input uint StopLoss = 500;      // StopLoss (points)
input uint TakeProfit = 500;    // TakeProfit (points)
input ENUM_POSITION_TYPE StartType = 0;    // StartType (first order type: BUY or SELL)

input group "C O M M O N   S E T T I N G S"
sinput ulong Magic = 1234567890; // Magic
input ERROR_TIMEOUT SkipTimeOnError = bt_SECOND; // SkipTimeOnError
input bool Trailing = true;      // Trailing
input string WorkSymbols = "";   // WorkSymbols (name±lots*factor^limit(sl,tp)[start,stop];...)

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
   ENUM_POSITION_TYPE startType;
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
      startType = POSITION_TYPE_BUY;
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
      startType = line[p] == '+' ? POSITION_TYPE_BUY : POSITION_TYPE_SELL;
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
      string filename = NULL;
      if(line == EXTENDED_SETTINGS)
      {
         int h = FileOpen(line, FILE_READ | FILE_TXT | FILE_ANSI | FILE_SHARE_READ | FILE_SHARE_WRITE, '\t', CP_UTF8);
         if(h != INVALID_HANDLE)
         {
            filename = FileReadString(h);
            StringTrimLeft(filename);
            StringTrimRight(filename);
            FileClose(h);
            if(StringLen(filename) == 0)
            {
               PrintFormat("File '%s' is empty", line);
               return false;
            }
         }
         else
         {
            PrintFormat("Can't open file '%s' (%d)", line, _LastError);
            return false;
         }
      }

      string symbols[];
      int n = StringSplit(filename == NULL ? line : filename, ';', symbols);
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
      Print(symbol, (startType == POSITION_TYPE_BUY ? "+" : "-"), (float)lots,
        "*", (float)factor,
        "^", limit,
        "(", stopLoss, ",", takeProfit, ")",
        useTime ? "[" + (string)hourStart + "," + (string)hourEnd + "]": "");
   }
};

//+------------------------------------------------------------------+
//| Simple common interface for trading strategies                   |
//+------------------------------------------------------------------+
interface TradingStrategy
{
   virtual bool trade(void);
};

//+------------------------------------------------------------------+
//| Main class with trading strategy                                 |
//+------------------------------------------------------------------+
class SimpleMartingale: public TradingStrategy
{
protected:
   Settings settings;
   SymbolMonitor symbol;
   AutoPtr<PositionState> position;
   AutoPtr<TrailingStop> trailing;
    
   double lotsStep;
   double lotsLimit;
   double takeProfit, stopLoss;
    
   bool paused;
   datetime badConditions;

public:
   SimpleMartingale(const Settings &state) : symbol(state.symbol)
   {
      settings = state;
      paused = false;
      badConditions = 0;

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
         position = new PositionState(tickets[0]);
         if(settings.stopLoss && settings.trailing)
         {
           trailing = new TrailingStop(tickets[0], settings.stopLoss,
              ((int)symbol.get(SYMBOL_SPREAD) + 1) * 2);
         }
      }
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
      
      ulong ticket = 0;

      if(position[] == NULL) // fresh start - no position existing or existed
      {
         if(settings.startType == POSITION_TYPE_BUY)
         {
            ticket = openBuy(settings.lots);
         }
         else
         {
            ticket = openSell(settings.lots);
         }
      }
      else
      {
         if(position[].refresh()) // position still exists
         {
            position[].update();
            if(trailing[]) trailing[].trail();
         }
         else // position closed - let open next one
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
      }
      
      if(ticket > 0) // new position is just opened
      {
         position = new PositionState(ticket);
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
};

//+------------------------------------------------------------------+
//| Global pointer for the pool of strategies                        |
//+------------------------------------------------------------------+
AutoPtr<TradingStrategyPool> pool;

//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
{
   if(WorkSymbols == "")
   {
      Print("Input settings:");

      Settings settings =
      {
         UseTime, HourStart, HourEnd,
         Lots, Factor, Limit,
         StopLoss, TakeProfit,
         StartType, Magic, SkipTimeOnError, Trailing, _Symbol
      };
      
      if(settings.validate())
      {
         settings.print();
         pool = new TradingStrategyPool(new SimpleMartingale(settings));
         return INIT_SUCCEEDED;
      }
      return INIT_FAILED;
   }
   else
   {
      Print("Parsed settings:");
      Settings settings[];

      Settings::parseAll(WorkSymbols, settings);
      const int n = ArraySize(settings);
      pool = new TradingStrategyPool(n);
      for(int i = 0; i < n; i++)
      {
         if(settings[i].symbol == NULL) continue; // skip incorrect settings
         
         settings[i].skipTimeOnError = SkipTimeOnError;
         settings[i].trailing = Trailing;
         // support many subsystems on the same symbol on a hedge account
         settings[i].magic = Magic + i;  // different magic for every subsystem
         pool[].push(new SimpleMartingale(settings[i]));
      }
   }
  
   return INIT_SUCCEEDED;
}

//+------------------------------------------------------------------+
//| Tick event handler                                               |
//+------------------------------------------------------------------+
void OnTick()
{
   if(pool[] != NULL)
   {
      pool[].trade();
   }
}

//+------------------------------------------------------------------+
