//+------------------------------------------------------------------+
//|                                         MarketBookAddRelease.mq5 |
//|                                  Copyright 2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright   "2022, MetaQuotes Ltd."
#property link        "https://www.mql5.com"
#property description "On start-up subscribe to market book notifications for specified symbol (empty string means current chart symbol)."
#property description "On exit unsubscribe from market book notifications. Otherwise does nothing. See logs for success/errors."

#property indicator_chart_window
#property indicator_plots 0

#include <MQL5Book/PRTF.mqh>

input string WorkSymbol = ""; // WorkSymbol (empty means current chart symbol)

const string _WorkSymbol = StringLen(WorkSymbol) == 0 ? _Symbol : WorkSymbol;
string symbols[];

//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
void OnInit()
{
   const int n = StringSplit(_WorkSymbol, ',', symbols);
   for(int i = 0; i < n; ++i)
   {
      if(!PRTF(MarketBookAdd(symbols[i])))
         PrintFormat("MarketBookAdd(%s) failed", symbols[i]);
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
   for(int i = 0; i < ArraySize(symbols); ++i)
   {
      if(!PRTF(MarketBookRelease(symbols[i])))
         PrintFormat("MarketBookRelease(%s) failed", symbols[i]);
   }
}
//+------------------------------------------------------------------+
/*

   example output 1 (market book is available):
   
      MarketBookAdd(symbols[i])=true / ok
      MarketBookRelease(symbols[i])=true / ok
      
   example output 2 (no market book for specific symbol):
   
      MarketBookAdd(symbols[i])=false / BOOKS_CANNOT_ADD(4901)
      MarketBookAdd(XPDUSD) failed
      MarketBookRelease(symbols[i])=false / BOOKS_CANNOT_DELETE(4902)
      MarketBookRelease(XPDUSD) failed

*/
//+------------------------------------------------------------------+
