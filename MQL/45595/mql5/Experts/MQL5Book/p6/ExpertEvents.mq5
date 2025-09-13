//+------------------------------------------------------------------+
//|                                                 ExpertEvents.mq5 |
//|                                  Copyright 2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright   "2022, MetaQuotes Ltd."
#property link        "https://www.mql5.com"
#property description "Display events of common types in multiline comments."

#define N_LINES 25
#include <MQL5Book/Comments.mqh>

//+------------------------------------------------------------------+
//| Custom multiline comment                                         |
//+------------------------------------------------------------------+
void Display(const string message)
{
   ChronoComment((string)GetTickCount() + ": " + message);
}

//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
void OnInit()
{
   Print(__FUNCTION__);
   EventSetTimer(2);
   if(!MarketBookAdd(_Symbol))
   {
      Print("MarketBookAdd failed:", _LastError);
   }
}

//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
{
   Display(__FUNCTION__);
}

//+------------------------------------------------------------------+
//| Timer event handler                                              |
//+------------------------------------------------------------------+
void OnTimer()
{
   Display(__FUNCTION__);
}

//+------------------------------------------------------------------+
//| Market book handler                                              |
//+------------------------------------------------------------------+
void OnBookEvent(const string &symbol)
{
   if(symbol == _Symbol) // process only book for requested symbol
   {
      Display(__FUNCTION__);
   }
}

//+------------------------------------------------------------------+
//| Chart event handler                                              |
//+------------------------------------------------------------------+
void OnChartEvent(const int id, const long &lparam, const double &dparam, const string &sparam)
{
   Display(__FUNCTION__);
}

//+------------------------------------------------------------------+
//| Finalization function                                            |
//+------------------------------------------------------------------+
void OnDeinit(const int)
{
   Print(__FUNCTION__);
   MarketBookRelease(_Symbol);
   Comment("");
}
//+------------------------------------------------------------------+
