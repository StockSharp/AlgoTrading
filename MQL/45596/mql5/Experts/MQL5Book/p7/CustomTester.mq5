//+------------------------------------------------------------------+
//|                                                 CustomTester.mq5 |
//|                                  Copyright 2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property description "Generates custom symbol from real Ticks of current chart's symbol.\n"
#property description "Ticks are coming slowly, producing a kind of visual back test chart, but with full support of interactive events and up to the current day (that's not available in the standard tester).\n"

#include <MQL5Book/PRTF.mqh>
#include <MQL5Book/Defines.mqh>
#include <VirtualKeys.mqh>
#include <MQL5Book/CustomSymbolMonitor.mqh>

#define EVENT_KEY 0xDED

//+------------------------------------------------------------------+
//| Inputs                                                           |
//+------------------------------------------------------------------+
input string CustomPath = "MQL5Book\\Part7"; // Custom Symbol Folder
input datetime _Start;                       // Start (default: 120 days back)
input ENUM_TIMEFRAMES Timeframe = PERIOD_H1;

//+------------------------------------------------------------------+
//| Globals (names in CamelCase)                                     |
//+------------------------------------------------------------------+
string CustomSymbol = _Symbol + ".Tester";
const uint DailySeconds = 60 * 60 * 24;
datetime Start = _Start == 0 ? TimeCurrent() - DailySeconds * 120 : _Start;
bool FirstCopy = true;
// step back 1 day because without this new ticks will not update the chart
datetime Cursor = (Start / DailySeconds - 1) * DailySeconds; // round up to a day boundary
MqlTick Ticks[];       // ticks for current day
int Index = 0;         // position inside the day
int Step = 32;         // advancing by 32 Ticks at once (by default)
int StepRestore = 0;   // remember recent speed during pause
long Chart = 0;        // newly created chart with custom symbol
bool InitDone = false;

//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
void OnInit()
{
   EventSetMillisecondTimer(100);
}

//+------------------------------------------------------------------+
//| Timer handler                                                    |
//+------------------------------------------------------------------+
void OnTimer()
{
   if(!GenerateData())
   {
      EventKillTimer();
   }
}

//+------------------------------------------------------------------+
//| Helper function to speed up/slow down Ticks replay by keys       |
//+------------------------------------------------------------------+
//| ATTENTION! Keyboard works only when the chart has focus,         |
//| so you need to click it by mouse for the keys to take effect.    |
//+------------------------------------------------------------------+
void CheckKeys(const long key)
{
   if(key == VK_DOWN)
   {
      Step /= 2;
      if(Step > 0)
      {
         Print("Slow down: ", Step);
         ChartSetString(Chart, CHART_COMMENT, "Speed: " + (string)Step);
      }
      else
      {
         Print("Paused");
         ChartSetString(Chart, CHART_COMMENT, "Paused");
         ChartRedraw(Chart);
      }
   }
   else if(key == VK_UP)
   {
      if(Step == 0)
      {
         Step = 1;
         Print("Resumed");
         ChartSetString(Chart, CHART_COMMENT, "Resumed");
      }
      else
      {
         Step *= 2;
         Print("Spead up: ", Step);
         ChartSetString(Chart, CHART_COMMENT, "Speed: " + (string)Step);
      }
   }
   else if(key == VK_PAUSE)
   {
      if(Step > 0)
      {
         StepRestore = Step;
         Step = 0;
         Print("Paused");
         ChartSetString(Chart, CHART_COMMENT, "Paused");
         ChartRedraw(Chart);
      }
      else
      {
         Step = StepRestore;
         Print("Resumed");
         ChartSetString(Chart, CHART_COMMENT, "Speed: " + (string)Step);
      }
   }
}

//+------------------------------------------------------------------+
//| Main function to produce the custom symbol and emit Ticks        |
//+------------------------------------------------------------------+
bool GenerateData()
{
   if(!InitDone)
   {
      bool custom = false;
      if(PRTF(SymbolExist(CustomSymbol, custom)) && custom)
      {
         if(IDYES == MessageBox(StringFormat("Clean up existing custom symbol '%s'?", CustomSymbol),
            "Please, confirm", MB_YESNO))
         {
            PRTF(CustomRatesDelete(CustomSymbol, 0, LONG_MAX));
            PRTF(CustomTicksDelete(CustomSymbol, 0, LONG_MAX));
            Sleep(1000);
            MqlRates rates[1];
            MqlTick tcks[];
            if(PRTF(CopyRates(CustomSymbol, PERIOD_M1, 0, 1, rates)) == 1
            || PRTF(CopyTicks(CustomSymbol, tcks) > 0))
            {
               Alert("Can't delete rates and Ticks, internal error");
               ExpertRemove();
            }
         }
         else
         {
            return false;
         }
      }
      else
      if(!PRTF(CustomSymbolCreate(CustomSymbol, CustomPath, _Symbol)))
      {
         return false;
      }
      
      // some properties, including very important ones can be not applied
      // right away after calling CustomSymbolCreate, so we need to check them
      // and try to apply "manually"
      SymbolMonitor sm;
      CustomSymbolMonitor csm(CustomSymbol, &sm);
      int props[] = {SYMBOL_TRADE_TICK_VALUE, SYMBOL_TRADE_TICK_SIZE};
      const int d1 = csm.verify(props);
      if(d1)
      {
         Print("Number of found descrepancies: ", d1);
         if(csm.verify(props)) // check again
         {
            Alert("Custom symbol can not be created, internal error!");
            return false;
         }
         Print("Fixed");
      }
      
      Print(TimeToString(SymbolInfoInteger(CustomSymbol, SYMBOL_TIME)));
      SymbolSelect(CustomSymbol, true);
      Chart = ChartOpen(CustomSymbol, Timeframe);
      const int handle = iCustom(CustomSymbol, Timeframe, "MQL5Book/p7/KeyboardSpy", ChartID(), EVENT_KEY);
      ChartIndicatorAdd(Chart, 0, handle);
      ChartSetString(Chart, CHART_COMMENT, "Custom Tester");
      ChartSetInteger(Chart, CHART_SHOW_OBJECT_DESCR, true);
      ChartRedraw(Chart);

      InitDone = true;
   }
   else
   {
      for(int i = 0; i <= (Step - 1) / 256; ++i)
      if(Step > 0 && !GenerateTicks())
      {
         return false;
      }
   }
   return true;
}

//+------------------------------------------------------------------+
//| Helper function to read real symbol Ticks by chunks day by day   |
//+------------------------------------------------------------------+
bool FillTickBuffer()
{
   int r;
   ArrayResize(Ticks, 0);
   do
   {
      r = PRTF(CopyTicksRange(_Symbol, Ticks, COPY_TICKS_ALL, Cursor * 1000L, (Cursor + DailySeconds) * 1000L - 1));
      if(r > 0 && FirstCopy)
      {
         // MQL5 bug workaround: this preliminary call is needed for the chart to leave "Waiting for Update" state
         PRTF(CustomTicksReplace(CustomSymbol, Cursor * 1000L, (Cursor + DailySeconds) * 1000L - 1, Ticks));
         FirstCopy = false;
         r = 0;
      }
      Cursor += DailySeconds;
   }
   while(r == 0 && Cursor < TimeCurrent()); // skip non-trading days
   Index = 0;
   return r > 0;
}

//+------------------------------------------------------------------+
//| Helper function to emit Ticks by small packets to custom symbol  |
//+------------------------------------------------------------------+
bool GenerateTicks()
{
   if(Index >= ArraySize(Ticks))
   {
      if(!FillTickBuffer()) return false;
   }
   
   const int m = ArraySize(Ticks);
   MqlTick array[];
   const int n = ArrayCopy(array, Ticks, 0, Index, fmin(fmin(Step, 256), m));
   if(n <= 0) return false;
   
   ResetLastError();
   if(CustomTicksAdd(CustomSymbol, array) != ArraySize(array) || _LastError != 0)
   {
      Print(_LastError); // in case we get ERR_CUSTOM_TICKS_WRONG_ORDER (5310)
      ExpertRemove();
   }

   Comment("Spead: ", (string)Step, " / ", STR_TIME_MSC(array[n - 1].time_msc));
   Index += Step;

   return true;
}

//+------------------------------------------------------------------+
//| Chart events handler:                                            |
//| - interactive keyboard presses (when the chart is active)        |
//| - remote keyboard events on dependent custom symbol chart        |
//+------------------------------------------------------------------+
void OnChartEvent(const int id, const long &lparam, const double &dparam, const string &sparam)
{
   if(id == CHARTEVENT_CUSTOM + EVENT_KEY) // this notification comes from dependent chart
   {
      // NB: MT5 limitation: since our MQL-program generates custom symbol
      // and normally does this in background (its chart is not an active chart),
      // TerminalInfoInteger(TERMINAL_KEYSTATE_) does not work,
      // that is it returns 0 always, hence we can't detect
      // Ctrl/Shift and other key states, and use only plain alphanumeric keys
      CheckKeys(lparam);
   }
   else if(id == CHARTEVENT_KEYDOWN) // this only fires when this chart is active
   {
      // when the chart is active, only at that mements we could
      // get meaningful data from TerminalInfoInteger(TERMINAL_KEYSTATE_),
      // but since it's unavailable via iCustom indicators, we don't use it here as well 
      CheckKeys(lparam);
   }
}

//+------------------------------------------------------------------+
//| Finalization handler                                             |
//+------------------------------------------------------------------+
void OnDeinit(const int)
{
   if(Chart != 0)
   {
      ChartClose(Chart);
   }
   Comment("");
}
//+------------------------------------------------------------------+
