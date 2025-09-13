//+------------------------------------------------------------------+
//|                                         MarketBookQuasiTicks.mq5 |
//|                                  Copyright 2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright   "2022, MetaQuotes Ltd."
#property link        "https://www.mql5.com"
#property description "Subscribe to market books for specified list of symbols."
#property description "Extract Ask/Bid prices from received books and produce quasi-tick events for multiple symbols."

#property indicator_chart_window
#property indicator_plots 0

#define N_LINES 25                // number of lines in multicomment
#include <MQL5Book/Comments.mqh>

input string SymbolList = "EURUSD,GBPUSD,XAUUSD,USDJPY"; // SymbolList (comma,separated,list)

const string WorkSymbols = StringLen(SymbolList) == 0 ? _Symbol : SymbolList;
string symbols[];

//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
void OnInit()
{
   const int n = StringSplit(WorkSymbols, ',', symbols);
   for(int i = 0; i < n; ++i)
   {
      if(!MarketBookAdd(symbols[i]))
      {
         PrintFormat("MarketBookAdd(%s) failed with code %d", symbols[i], _LastError);
      }
   }
}

//+------------------------------------------------------------------+
//| Market book notification handler                                 |
//+------------------------------------------------------------------+
void OnBookEvent(const string &symbol)
{
   MqlBookInfo mbi[];
   if(MarketBookGet(symbol, mbi)) // retrieve current book
   {
      int half = ArraySize(mbi) / 2; // estimation of the middle slot
      bool correct = true;
      for(int i = 0; i < ArraySize(mbi); ++i)
      {
         if(i > 0) // find the middle of the book
         {
            if(mbi[i - 1].type == BOOK_TYPE_SELL
               && mbi[i].type == BOOK_TYPE_BUY)
            {
               half = i;
            }
            
            if(mbi[i - 1].price <= mbi[i].price)
            {
               correct = false;
            }
         }
      }
      
      if(correct) // extract best Bid/Ask prices from proper book 
      {
         // mbi[half - 1].price // Ask
         // mbi[half].price     // Bid
         OnSymbolTick(symbol, mbi[half].price);
      }
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
      if(!MarketBookRelease(symbols[i]))
      {
         PrintFormat("MarketBookRelease(%s) failed with code %d", symbols[i], _LastError);
      }
   }
   Comment("");
}

//+------------------------------------------------------------------+
//| Multisymbol tick custom event                                    |
//+------------------------------------------------------------------+
void OnSymbolTick(const string &symbol, const double price)
{
   const string message = StringFormat("%s %s",
      symbol, DoubleToString(price, (int)SymbolInfoInteger(symbol, SYMBOL_DIGITS)));
   ChronoComment(message);
}
//+------------------------------------------------------------------+
