//+------------------------------------------------------------------+
//|                                              MarketBookEvent.mq5 |
//|                                  Copyright 2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright   "2022, MetaQuotes Ltd."
#property link        "https://www.mql5.com"
#property description "On start-up subscribe to market book notifications for specified symbol (if not empty). Otherwise monitor notifications, initiated by other programs."
#property description "Events are shown in the Comment."

#property indicator_chart_window
#property indicator_plots 0

#include <MQL5Book/PRTF.mqh>
#include <MQL5Book/Comments.mqh>

input string WorkSymbol = ""; // WorkSymbol (if empty, intercept events initiated by others)

//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
void OnInit()
{
   if(StringLen(WorkSymbol))
   {
      PRTF(MarketBookAdd(WorkSymbol));
   }
   else
   {
      Print("Start listening to OnBookEvent initiated by other programs");
   }
}

//+------------------------------------------------------------------+
//| Market book notification handler                                 |
//+------------------------------------------------------------------+
void OnBookEvent(const string &symbol)
{
   ChronoComment(symbol + " " + (string)GetTickCount());
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
   if(StringLen(WorkSymbol))
   {
      PRTF(MarketBookRelease(WorkSymbol));
   }
}
//+------------------------------------------------------------------+
