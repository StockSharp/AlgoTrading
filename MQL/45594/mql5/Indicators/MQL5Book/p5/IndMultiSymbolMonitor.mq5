//+------------------------------------------------------------------+
//|                                        IndMultiSymbolMonitor.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property indicator_chart_window
#property indicator_buffers 0
#property indicator_plots   0

input string Instruments = "EURUSD,GBPUSD,USDCHF,USDJPY,AUDUSD,USDCAD,NZDUSD";

#include <MQL5Book/MultiSymbolMonitor.mqh>

MultiSymbolMonitor monitor;

//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
void OnInit()
{
   string symbols[];
   const int n = StringSplit(Instruments, ',', symbols);
   for(int i = 0; i < n; ++i)
   {
      monitor.attach(symbols[i]);
   }
}

//+------------------------------------------------------------------+
//| Custom indicator iteration function                              |
//+------------------------------------------------------------------+
int OnCalculate(const int rates_total,
                const int prev_calculated,
                const int begin,
                const double &price[])
{
   const ulong changes = monitor.check(true);
   if(changes != 0)
   {
      Print("New bar(s) on: ", monitor.describe(changes), ", in-sync:", monitor.inSync());
   }
   return rates_total;
}

//+------------------------------------------------------------------+
//| Finalization function                                            |
//+------------------------------------------------------------------+
void OnDeinit(const int)
{
   Print(__FUNCSIG__);
}
//+------------------------------------------------------------------+
