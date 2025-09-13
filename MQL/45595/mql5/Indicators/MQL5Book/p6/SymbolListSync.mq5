//+------------------------------------------------------------------+
//|                                               SymbolListSync.mq5 |
//|                                  Copyright 2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright   "2022, MetaQuotes Ltd."
#property link        "https://www.mql5.com"
#property description "Check all symbols in Market Watch for synchronization. Display lagging symbols in Comment.\n"

#property indicator_chart_window
#property indicator_plots 0

input int SyncCheckupPeriod = 1; // SyncCheckupPeriod (seconds)

//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
void OnInit()
{
   EventSetTimer(SyncCheckupPeriod);
}

//+------------------------------------------------------------------+
//| Timer event handler                                              |
//+------------------------------------------------------------------+
void OnTimer()
{
   string unsynced;
   const int n = SymbolsTotal(true);
   // check all symbols in Market Watch
   for(int i = 0; i < n; ++i)
   {
      const string s = SymbolName(i, true);
      if(!SymbolIsSynchronized(s))
      {
         unsynced += s + "\n";
      }
   }
   
   if(StringLen(unsynced) > 0)
   {
      Comment("Unsynced symbols:\n" + unsynced);
      Print("Unsynced symbols:\n" + unsynced);
      string t = TimeToString(TimeCurrent(), TIME_DATE | TIME_SECONDS);
      StringReplace(t, ".", "");
      StringReplace(t, ":", "");
      StringReplace(t, " ", "");
      ChartScreenShot(0, t + ".png", 600, 400);
   }
   else
   {
      Comment("All Market Watch is in sync");
   }
}

//+------------------------------------------------------------------+
//| Custom indicator iteration function (dummy)                      |
//+------------------------------------------------------------------+
int OnCalculate(const int rates_total, const int prev_calculated, const int, const double &price[])
{
   return rates_total;
}

//+------------------------------------------------------------------+
//| Finalization handler                                             |
//+------------------------------------------------------------------+
void OnDeinit(const int)
{
   Comment("");
}
//+------------------------------------------------------------------+
