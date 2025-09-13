//+------------------------------------------------------------------+
//|                                                  SeriesStats.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property script_show_inputs

#include <MQL5Book/PRTF.mqh>

input string WorkSymbol = NULL; // Symbol (leave empty for current)
input ENUM_TIMEFRAMES TimeFrame = PERIOD_CURRENT;
input int BarOffset = 0;
input int BarCount = 10000;

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   MqlRates rates[];
   double range = 0, move = 0;
   
   PrintFormat("Requesting %d bars on %s %s",
      BarCount, StringLen(WorkSymbol) > 0 ? WorkSymbol : _Symbol,
      EnumToString(TimeFrame == PERIOD_CURRENT ? _Period : TimeFrame));
   
   // request complete BarCount bars as MqlRates array
   const int n = PRTF(CopyRates(WorkSymbol, TimeFrame, BarOffset, BarCount, rates));
   
   if(n <= 0)
   {
      return; // exit on error (details are shown in log by PRTF)
   }
   
   for(int i = 0; i < n; ++i)
   {
      range += (rates[i].high - rates[i].low) / n;
      move += (fmax(rates[i].open, rates[i].close) - fmin(rates[i].open, rates[i].close)) / n;
   }
   
   PrintFormat("Stats per bar: range=%f, movement=%f", range, move);
   PrintFormat("Dates: %s - %s", TimeToString(rates[0].time), TimeToString(rates[n - 1].time));
   
   /*
      output example 1:
      
      Requesting 10000 bars on EURUSD PERIOD_H1
      CopyRates(WorkSymbol,TimeFrame,BarOffset,BarCount,rates)=10000 / ok
      Stats per bar: range=0.001439, movement=0.000700
      Dates: 2020.03.03 14:00 - 2021.10.11 17:00
      
      output example 2 (hitting TERMINAL_MAXBARS limit (20000)):
      
      Requesting 100000 bars on EURUSD PERIOD_H1
      CopyRates(WorkSymbol,TimeFrame,BarOffset,BarCount,rates)=20018 / ok
      Stats per bar: range=0.001280, movement=0.000621
      Dates: 2018.07.19 15:00 - 2021.10.11 17:00
   */
}
//+------------------------------------------------------------------+
