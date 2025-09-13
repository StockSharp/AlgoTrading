//+------------------------------------------------------------------+
//|                                              EqualVolumeBars.mq5 |
//|                           Copyright © 2008-2022, MetaQuotes Ltd. |
//|                                            https://www.mql5.com/ |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2008-2022, MetaQuotes Ltd."
#property link "https://www.mql5.com/"
#property description "Non-trading EA generating equivolume and/or range bars as a custom symbol.\n"

#define TICKS_ARRAY 10000 // size of tick buffer

//+------------------------------------------------------------------+
//| Supported types of custom charts                                 |
//+------------------------------------------------------------------+
enum mode
{
   EqualTickVolumes = 0,
   EqualRealVolumes = 1,
   RangeBars = 2
};

//+------------------------------------------------------------------+
//| Inputs                                                           |
//+------------------------------------------------------------------+
input mode WorkMode = EqualTickVolumes;
input int TicksInBar = 1000;
input datetime StartDate = 0; // StartDate (default: 30 days back)
input string CustomPath = "MQL5Book\\Part7";

const uint DailySeconds = 60 * 60 * 24;
const string Suffixes[] = {"_Eqv", "_Qrv", "_Rng"};
datetime Start;
string SymbolName;
int BarCount;
bool InitDone = false;

//+------------------------------------------------------------------+
//| Virtual time and OHLCTV values of current bar for custom symbol  |
//+------------------------------------------------------------------+
datetime now_time;
double now_close, now_open, now_low, now_high, now_real;
long now_volume;

//+------------------------------------------------------------------+
//| Custom symbol reset                                              |
//+------------------------------------------------------------------+
bool Reset()
{
   int size;
   do
   {
      ResetLastError();
      int deleted = CustomRatesDelete(SymbolName, 0, LONG_MAX);
      int err = GetLastError();
      if(err != ERR_SUCCESS)
      {
         Alert("CustomRatesDelete failed, ", err);
         return false;
      }
      else
      {
         Print("Rates deleted: ", deleted);
      }
  
      ResetLastError();
      deleted = CustomTicksDelete(SymbolName, 0, LONG_MAX);
      if(deleted == -1)
      {
         Print("CustomTicksDelete failed ", GetLastError());
         return false;
      }
      else
      {
         Print("Ticks deleted: ", deleted);
      }
    
      // wait for changes to take effect in the core threads
      Sleep(1000);

      MqlTick _array[];
      size = CopyTicks(SymbolName, _array, COPY_TICKS_ALL, 0, 10);
      Print("Remaining ticks: ", size);
   }
   while(size > 0 && !IsStopped());
   // NB. this can not work everytime as expected
   // if getting ERR_CUSTOM_TICKS_WRONG_ORDER or similar error - the last resort
   // is to wipe out the custom symbol manually from GUI, and then restart this EA

   return size > -1; // success
}

//+------------------------------------------------------------------+
//| Process history of real ticks                                    |
//+------------------------------------------------------------------+
void BuildHistory(const datetime start)
{
   ulong cursor = start * 1000;
   uint trap = GetTickCount();

   Print("Processing tick history...");
   Comment("Processing tick history, this may take a while...");
   TicksBuffer tb;
    
   while(tb.fill(cursor, true) && !IsStopped())
   {
      MqlTick t;
      while(tb.read(t))
      {
         HandleTick(t, true);
      }
   }
   Comment("");
   
   Print("Bar 0: ", now_time, " ", now_volume, " ", now_real);
   if(now_volume > 0)
   {
      // write latest (incomplete) bar to the chart
      WriteToChart(now_time, now_open, now_low, now_high, now_close, now_volume, (long)now_real);
   
      // show stats
      Print(BarCount, " bars written in ", (GetTickCount() - trap) / 1000, " sec");
   }
   else
   {
      Alert("No data");
   }
}

//+------------------------------------------------------------------+
//| Start from scratch                                               |
//+------------------------------------------------------------------+
datetime Init(const datetime start)
{
   now_time = start;
   now_close = 0;
   now_open = 0;
   now_low = DBL_MAX;
   now_high = 0;
   now_volume = 0;
   now_real = 0;
   return start;
}

//+------------------------------------------------------------------+
//| Rough estimation of continuation                                 |
//+------------------------------------------------------------------+
datetime Resume(const datetime start)
{
   MqlRates rates[2];
   if(CopyRates(SymbolName, PERIOD_M1, 0, 2, rates) != 2) return Init(start);
   
   ArrayPrint(rates); // tail
   
   // rescan the last bar
   // (but we don't know which tick inside the single minute rates[1].time
   // did actually form this equal volume bar)
   now_time = rates[1].time;
   now_close = rates[1].open;
   now_open = rates[1].open;
   now_low = rates[1].open;
   now_high = rates[1].open;
   now_volume = 0; // rates[1].tick_volume;
   now_real = 0; // (double)rates[1].real_volume;
   
   Print("Resuming from ", rates[1].time);
   
   return rates[1].time;
}

//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
{
   InitDone = false;
   EventSetTimer(1);
   return INIT_SUCCEEDED;
}

//+------------------------------------------------------------------+
//| Timer event handler                                              |
//+------------------------------------------------------------------+
void OnTimer()
{
   if(!TerminalInfoInteger(TERMINAL_CONNECTED))
   {
      Print("Waiting for connection...");
      return;
   }

   if(!SymbolIsSynchronized(_Symbol))
   {
      Print("Unsynchronized, skipping ticks...");
      return;
   }
   
   EventKillTimer();

   BarCount = 0;
   Start = StartDate == 0 ? TimeCurrent() - DailySeconds * 30 : StartDate;
   SymbolName = _Symbol + Suffixes[WorkMode] + (string)TicksInBar;

   bool justCreated = false;
   if(!SymbolSelect(SymbolName, true))
   {
      Print("Creating \"", SymbolName, "\"");

      if(!CustomSymbolCreate(SymbolName, CustomPath, _Symbol)
      && !SymbolSelect(SymbolName, true))
      {
         Alert("Can't select symbol:", SymbolName, " err:", GetLastError());
         return;
      }
      justCreated = true;
      Start = Init(Start);
   }
   else
   {
      if(IDYES == MessageBox(SymbolName + " exists. Rebuild?", NULL, MB_YESNO))
      {
         Print("Resetting \"", SymbolName, "\"");
         if(!Reset()) return;
         Start = Init(Start);
      }
      else
      {
         // find existing tail of custom quotes to supersede Start
         Start = Resume(Start);
      }
   }

   BuildHistory(Start);

   if(IsStopped())
   {
      Print("Interrupted. The custom symbol data is inconsistent - please, delete");
      return;
   }

   Print("Open \"", SymbolName, "\" chart to view results");

   if(justCreated)
   {
      OpenCustomChart();
      RefreshWindow(now_time);
   }

   InitDone = true;
}

//+------------------------------------------------------------------+
//| Ticks buffer (read from history by chunks)                       |
//+------------------------------------------------------------------+
class TicksBuffer
{
private:
   MqlTick array[];
   int tick;
  
public:
   bool fill(ulong &cursor, const bool history = false)
   {
      int size = history ? CopyTicks(_Symbol, array, COPY_TICKS_ALL, cursor, TICKS_ARRAY) :
         CopyTicksRange(_Symbol, array, COPY_TICKS_ALL, cursor);
      if(size == -1)
      {
         Print("CopyTicks failed: ", _LastError);
         return false;
      }
      else if(size == 0)
      {
         if(history)
         {
            Print("End of CopyTicks at ", (datetime)(cursor / 1000), " ", _LastError);
         }
         return false;
      }
      
      if((ulong)array[0].time_msc < cursor)
      {
         Print("Tick rewind bug, ", (datetime)(cursor / 1000));
         return false;
      }
      cursor = array[size - 1].time_msc + 1;
      tick = 0;
    
      return true;
   }
    
   bool read(MqlTick &t)
   {
      if(tick < ArraySize(array))
      {
         t = array[tick++];
         return true;
      }
      return false;
   }
};

//+------------------------------------------------------------------+
//| Helper function to open custom symbol chart                      |
//+------------------------------------------------------------------+
void OpenCustomChart()
{
   const long id = ChartOpen(SymbolName, PERIOD_M1);
   if(id == 0)
   {
      Alert("Can't open new chart for ", SymbolName, ", code: ", _LastError);
   }
   else
   {
      Sleep(1000);
      ChartSetSymbolPeriod(id, SymbolName, PERIOD_M1);
      ChartSetInteger(id, CHART_MODE, CHART_CANDLES);
   }
}

//+------------------------------------------------------------------+
//| Finalization handler                                             |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
{
   Comment("");
}

//+------------------------------------------------------------------+
//| Tick event handler                                               |
//+------------------------------------------------------------------+
void OnTick()
{
   if(!InitDone) return;

   static ulong cursor = 0;
   MqlTick t;
  
   if(cursor == 0)
   {
      if(SymbolInfoTick(_Symbol, t))
      {
         HandleTick(t);
         cursor = t.time_msc + 1;
      }
   }
   else
   {
      TicksBuffer tb;
      while(tb.fill(cursor))
      {
         while(tb.read(t))
         {
            HandleTick(t);
         }
      }
   }

   RefreshWindow(now_time);
}

//+------------------------------------------------------------------+
//| Process incoming ticks one by one                                |
//+------------------------------------------------------------------+
inline void HandleTick(const MqlTick &t, const bool history = false)
{
   now_volume++;
   now_real += t.volume_real;
   // (long)t.volume; // NB: use 'long volume' to eliminate floating point error accumulation
   const double bid = t.last != 0 ? t.last : t.bid;

   if(!IsNewBar()) // bar continues
   {
      if(bid < now_low) now_low = bid;
      if(bid > now_high) now_high = bid;
      now_close = bid;
    
      if(!history)
      {
         // write bar 0 to chart (-1 for volume stands for upcoming refresh)
         WriteToChart(now_time, now_open, now_low, now_high, now_close, now_volume - !history, (long)now_real);
      }
   }
   else // new bar tick
   {
      do
      {
         if(history)
         {
            BarCount++;
         
            if((BarCount % 10) == 0)
            {
               Comment(t.time, " -> ", now_time, " [", BarCount, "]");
            }
         }
         else
         {
            Comment("Complete bar: ", now_time);
         }
       
         if(WorkMode == RangeBars)
         {
            FixRange();
         }
         // write bar 1
         WriteToChart(now_time, now_open, now_low, now_high, now_close,
            WorkMode == EqualTickVolumes ? TicksInBar : now_volume,
            WorkMode == EqualRealVolumes ? TicksInBar : (long)now_real);
   
         // normalize down to a minute
         datetime time = t.time / 60 * 60;
   
         // eliminate bars with equal or too old times
         if(time <= now_time) time = now_time + 60;
   
         now_time = time;
         now_open = bid;
         now_low = bid;
         now_high = bid;
         now_close = bid;
         now_volume = 1;
         if(WorkMode == EqualRealVolumes) now_real -= TicksInBar;
   
         // write bar 0 (-1 for volume stands for upcoming refresh)
         WriteToChart(now_time, now_open, now_low, now_high, now_close, now_volume - !history, (long)now_real);
      }
      while(IsNewBar() && WorkMode == EqualRealVolumes);
   }
}

//+------------------------------------------------------------------+
//| Simulate new tick on custom symbol chart                         |
//+------------------------------------------------------------------+
void RefreshWindow(const datetime t)
{
   MqlTick ta[1];
   SymbolInfoTick(_Symbol, ta[0]);
   ta[0].time = t;
   ta[0].time_msc = t * 1000;
   if(CustomTicksAdd(SymbolName, ta) == -1) // NB! this call may increment number of ticks per bar
   {
      Print("CustomTicksAdd failed:", _LastError, " ", (long) ta[0].time);
      ArrayPrint(ta);
   }
}

//+------------------------------------------------------------------+
//| Add bar (MqlRates element) to custom symbol chart                |
//+------------------------------------------------------------------+
void WriteToChart(datetime t, double o, double l, double h, double c, long v, long m = 0)
{
   MqlRates r[1];

   r[0].time = t;
   r[0].open = o;
   r[0].low = l;
   r[0].high = h;
   r[0].close = c;
   r[0].tick_volume = v;
   r[0].spread = 0;
   r[0].real_volume = m;

   if(CustomRatesUpdate(SymbolName, r) < 1)
   {
      Print("CustomRatesUpdate failed: ", _LastError);
   }
}

//+------------------------------------------------------------------+
//| Check condition for new virtual bar formation according to mode  |
//+------------------------------------------------------------------+
bool IsNewBar()
{
   if(WorkMode == EqualTickVolumes)
   {
      if(now_volume > TicksInBar) return true;
   }
   else if(WorkMode == EqualRealVolumes)
   {
      if(now_real > TicksInBar) return true;
   }
   else if(WorkMode == RangeBars)
   {
      if((now_high - now_low) / _Point > TicksInBar) return true;
   }

   return false;
}

//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void FixRange()
{
   const int excess = (int)((now_high + (_Point / 2)) / _Point)
      - (int)((now_low + (_Point / 2)) / _Point) - TicksInBar;
   if(excess > 0)
   {
      if(now_close > now_open)
      {
         now_high -= excess * _Point;
         if(now_high < now_close) now_close = now_high;
      }
      else if(now_close < now_open)
      {
         now_low += excess * _Point;
         if(now_low > now_close) now_close = now_low;
      }
   }
}
//+------------------------------------------------------------------+
