//+------------------------------------------------------------------+
//|                                      CustomSymbolFilterTicks.mq5 |
//|                                  Copyright 2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright   "Copyright 2022, MetaQuotes Ltd."
#property link        "https://www.mql5.com"
#property description "Create specified custom symbol based on current chart's symbol with prunned ticks for faster tests and optimizations."
#property script_show_inputs

//#include <MQL5Book/PRTF.mqh>
#define PRTF
#include <MQL5Book/TickFilter.mqh>
#include <MQL5Book/Timing.mqh>
#include <MQL5Book/CustomSymbolMonitor.mqh>

//+------------------------------------------------------------------+
//| Inputs                                                           |
//+------------------------------------------------------------------+
input string CustomPath = "MQL5Book\\Part7"; // Custom Symbol Folder
input datetime _Start;                       // Start (default: 120 days back)
input TickFilter::FILTER_MODE Mode = TickFilter::SEQUENCE;

//+------------------------------------------------------------------+
//| Globals                                                          |
//+------------------------------------------------------------------+
string CustomSymbol = _Symbol + ".TckFltr" + "-" + EnumToString(Mode);
const uint DailySeconds = 60 * 60 * 24;
datetime Start = _Start == 0 ? TimeCurrent() - DailySeconds * 120 : _Start;

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   bool custom = false;
   if(PRTF(SymbolExist(CustomSymbol, custom)) && custom)
   {
      if(IDYES == MessageBox(StringFormat("Delete existing custom symbol '%s'?", CustomSymbol),
         "Please, confirm", MB_YESNO))
      {
         PRTF(SymbolSelect(CustomSymbol, false));
         PRTF(CustomRatesDelete(CustomSymbol, 0, LONG_MAX));
         PRTF(CustomTicksDelete(CustomSymbol, 0, LONG_MAX));
         PRTF(CustomSymbolDelete(CustomSymbol));
      }
      else
      {
         return;
      }
   }

   if(IDYES == MessageBox(StringFormat("Create new custom symbol '%s'?", CustomSymbol),
      "Please, confirm", MB_YESNO))
   {
      if(PRTF(CustomSymbolCreate(CustomSymbol, CustomPath, _Symbol)))
      {

         //PRTF(CustomSymbolSetDouble(CustomSymbol, SYMBOL_TRADE_TICK_VALUE_PROFIT, 1.0));

         // BUG: some properties are not set as expected, including very important ones:
         // SYMBOL_TRADE_TICK_VALUE, SYMBOL_TRADE_TICK_SIZE, etc. which remain zeros,
         // so we need to edit them "manually"
         SymbolMonitor sm;
         CustomSymbolMonitor csm(CustomSymbol, &sm);
         int props[] = {SYMBOL_TRADE_TICK_VALUE, SYMBOL_TRADE_TICK_SIZE/*,
            SYMBOL_TRADE_TICK_VALUE_PROFIT, SYMBOL_TRADE_TICK_VALUE_LOSS*/};
         const int d1 = csm.verify(props);
         if(d1)
         {
            Print("Number of found descrepancies: ", d1);
            if(csm.verify(props)) // check again
            {
               Alert("Custom symbol can not be created, internal error!");
               return;
            }
            Print("Fixed");
         }
         
         CustomSymbolSetString(CustomSymbol, SYMBOL_DESCRIPTION, "Prunned ticks by " + EnumToString(Mode));
         
         if(GenerateTickData())
         {
            SymbolSelect(CustomSymbol, true);
            ChartOpen(CustomSymbol, PERIOD_H1);
         }
      }
   }
}

//+------------------------------------------------------------------+
//| Day-by-day tick data processor                                   |
//+------------------------------------------------------------------+
bool GenerateTickData()
{
   bool result = true;
   datetime from = Start / DailySeconds * DailySeconds; // round up to a day boundary
   ulong read = 0, written = 0;
   uint day = 0;
   const uint total = (uint)((TimeCurrent() - from) / DailySeconds + 1);
   Timing timing;
   MqlTick array[];
   
   while(!IsStopped() && from < TimeCurrent())
   {
      Comment(TimeToString(from, TIME_DATE), " ", day++, "/", total,
         " elapsed: ", timing.elapsed(), ", remain: ", timing.remain(day * 1.0f / total));

      const int r = PRTF(CopyTicksRange(_Symbol, array, COPY_TICKS_ALL,
         from * 1000L, (from + DailySeconds) * 1000L - 1));
      if(r < 0)
      {
         Alert("Error reading ticks at ", TimeToString(from, TIME_DATE));
         result = false;
         break;
      }
      read += r;
      
      if(r > 0)
      {
         const int t = PRTF(TickFilter::filter(Mode, array));
         const int w = PRTF(CustomTicksReplace(CustomSymbol,
            from * 1000L, (from + DailySeconds) * 1000L - 1, array));
         if(w <= 0)
         {
            Alert("Error writing custom ticks at ", TimeToString(from, TIME_DATE));
            result = false;
            break;
         }
         written += w;
      }
      from += DailySeconds;
   }
   
   if(read > 0)
   {
      PrintFormat("Done ticks - read: %lld, written: %lld, ratio: %.1f%%",
         read, written, written * 100.0 / read);
   }
   Comment("");
   return result;
}
//+------------------------------------------------------------------+
