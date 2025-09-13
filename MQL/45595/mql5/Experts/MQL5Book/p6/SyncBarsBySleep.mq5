//+------------------------------------------------------------------+
//|                                              SyncBarsBySleep.mq5 |
//|                              Copyright (c) 2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#include <MQL5Book/TickModel.mqh>

input uint Pause = 1;                   // Pause (seconds)
input string OtherSymbol = "USDJPY";

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
   return INIT_SUCCEEDED;
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
   
   // timestamp of this _Symbol latest known bar
   static datetime lastBarTime = 0;
   static bool synchonized = false;
   static BarTimeStatistics stats;

   const datetime currentTime = iTime(_Symbol, _Period, 0);

   // if not initialize yet or bar changes
   if(lastBarTime != currentTime)
   {
      stats.total++;
      lastBarTime = currentTime;
      PrintFormat("Last bar on %s is %s", _Symbol, TimeToString(lastBarTime));
      synchonized = false;
   }

   // time of the last known bar on the other symbol
   datetime otherTime;
   bool late = false;

   // wait until bar times are equal on both symbols
   while(currentTime != (otherTime = iTime(OtherSymbol, _Period, 0)) && !IsStopped())
   {
      late = true;
      PrintFormat("Wait %d seconds...", Pause);
      Sleep(Pause * 1000);
   }

   if(late) stats.late++;
   
   if(IsStopped()) ExpertRemove();

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
