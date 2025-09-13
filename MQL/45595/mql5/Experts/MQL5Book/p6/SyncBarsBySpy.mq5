//+------------------------------------------------------------------+
//|                                                SyncBarsBySpy.mq5 |
//|                              Copyright (c) 2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//| NB: Visual and nonvisual modes produce different results!        |
//+------------------------------------------------------------------+
#include <MQL5Book/TickModel.mqh>

input string OtherSymbol = "USDJPY";

#define TICKSPY 0xFEED // 65261

const int Message = TICKSPY;
int handle;

//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
{
   if(_Symbol == OtherSymbol)
   {
      Alert("Please specify a symbol other than the work symbol of the chart!");
      return INIT_PARAMETERS_INCORRECT;
   }
   if(!SymbolSelect(OtherSymbol, true))
   {
      Alert("Wrong other symbol: ", OtherSymbol);
      return INIT_PARAMETERS_INCORRECT;
   }
   // run this indicator to report us every tick on OtherSymbol via chart events
   handle = iCustom(OtherSymbol, _Period, "MQL5Book/p5/EventTickSpy",
                  "", Message, ChartID());
   return handle != INVALID_HANDLE ? INIT_SUCCEEDED : INIT_FAILED;
}

//+------------------------------------------------------------------+
//| Finalization handler                                             |
//+------------------------------------------------------------------+
void OnDeinit(const int)
{
   IndicatorRelease(handle);
}

//+------------------------------------------------------------------+
//| Helper class to collect bar timing statistics                    |
//+------------------------------------------------------------------+
class BarTimeStatistics
{
public:
   int total;
   int late;
   
   BarTimeStatistics(): total(0), late(0) { }
   
   ~BarTimeStatistics()
   {
      PrintFormat("%d bars on %s was late among %d total bars on %s (%2.1f%%)",
         late, OtherSymbol, total, _Symbol, late * 100.0 / total);
   }
};

//+------------------------------------------------------------------+
//| Chart event handler                                              |
//+------------------------------------------------------------------+
void OnChartEvent(const int id, const long &lparam, const double &dparam, const string &sparam)
{
   if(id == CHARTEVENT_CUSTOM + Message)
   {
      OnSymbolTick(sparam);
   }
}

//+------------------------------------------------------------------+
//| Custom multisymbol tick event handler                            |
//+------------------------------------------------------------------+
void OnSymbolTick(const string symbol)
{
   // timestamp of this _Symbol latest known bar
   static datetime lastBarTime = 0;
   static bool synchonized = false;
   static BarTimeStatistics stats;

   const datetime currentTime = iTime(_Symbol, _Period, 0);
   
   if(symbol == _Symbol)
   {
      // if not initialize yet or bar changes
      if(lastBarTime != currentTime)
      {
         stats.total++;
         lastBarTime = currentTime;
         PrintFormat("Last bar on %s is %s", _Symbol, TimeToString(lastBarTime));
         synchonized = false;
      }
   }
   
   static bool late = false;
   
   // wait until bar times are equal on both symbols
   if(currentTime != iTime(OtherSymbol, _Period, 0))
   {
      if(symbol == OtherSymbol)
      {
         late = true;
         Print(OtherSymbol, " bar delayed...");
      }
      return; // wait for next tick or chart event
   }
   
   if(late)
   {
      stats.late++;
      late = false;
   }

   // we are here after sync is done
   
   if(!synchonized)
   {
      // use TimeTradeServer() because TimeCurrent() doesn't change without ticks
      Print("Bars are in sync at ", TimeToString(TimeTradeServer(), TIME_DATE | TIME_SECONDS));
      // prevent the message output until next unsynced state
      synchonized = true;
   }
   
   // here do your synced job
   // ...
}

//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
{
   if(MQLInfoInteger(MQL_TESTER))
   {
      const TICK_MODEL model = getTickModel();
      if(model != TICK_MODEL_OPEN_PRICES)
      {
         static bool shownOnce = false;
         if(!shownOnce)
         {
            Print("This EA is intended to run in \"Open Prices\" mode");
            shownOnce = true;
         }
      }
   }
   
   OnSymbolTick(_Symbol);
}
//+------------------------------------------------------------------+
