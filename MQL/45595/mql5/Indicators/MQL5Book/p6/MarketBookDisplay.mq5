//+------------------------------------------------------------------+
//|                                            MarketBookDisplay.mq5 |
//|                                  Copyright 2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright   "2022, MetaQuotes Ltd."
#property link        "https://www.mql5.com"
#property description "Subscribe to market book of specified symbol and display its updates in the comment."
#property description "Show buy and sell volumes as a histogram in a subwindow."

#property indicator_separate_window
#property indicator_plots 2
#property indicator_buffers 2

// plot settings
#property indicator_type1   DRAW_HISTOGRAM
#property indicator_color1  clrDodgerBlue
#property indicator_width1  2
#property indicator_label1  "Buys"

#property indicator_type2   DRAW_HISTOGRAM
#property indicator_color2  clrOrangeRed
#property indicator_width2  2
#property indicator_label2  "Sells"

#include <MQL5Book/PRTF.mqh>

input string WorkSymbol = ""; // WorkSymbol (if empty, use current chart symbol)
input bool AdvancedMode = false;
input bool ShowVolumeInLots = false;

const string _WorkSymbol = StringLen(WorkSymbol) == 0 ? _Symbol : WorkSymbol;

double buys[], sells[];
int depth, digits;
double tick, contract;

//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
int OnInit()
{
   // indicator buffers setup
   SetIndexBuffer(0, buys);
   SetIndexBuffer(1, sells);
   ArraySetAsSeries(buys, true);
   ArraySetAsSeries(sells, true);
   // obtain symbol properties
   depth = (int)PRTF(SymbolInfoInteger(_WorkSymbol, SYMBOL_TICKS_BOOKDEPTH));
   digits = (int)PRTF(SymbolInfoInteger(_WorkSymbol, SYMBOL_DIGITS));
   tick = PRTF(SymbolInfoDouble(_WorkSymbol, SYMBOL_TRADE_TICK_SIZE));
   contract = PRTF(SymbolInfoDouble(_WorkSymbol, SYMBOL_TRADE_CONTRACT_SIZE));

   if(tick > 0)
   {
      // enable market book events
      PRTF(MarketBookAdd(_WorkSymbol));
   }
   
   return tick > 0 ? INIT_SUCCEEDED : INIT_FAILED;
}

#define VOL(V) (ShowVolumeInLots ? V / contract : V)

//+------------------------------------------------------------------+
//| Market book notification handler                                 |
//+------------------------------------------------------------------+
void OnBookEvent(const string &symbol)
{
   if(symbol == _WorkSymbol) // process only book for requested symbol
   {
      MqlBookInfo mbi[];
      if(MarketBookGet(symbol, mbi)) // retrieve current book
      {
         // clean up all slots (multiplied by 10 to take some
         // additional slots which may be affected because of AdvancedMode
         for(int i = 0; i <= fmin(depth * 10, Bars(_Symbol, _Period) - 1); ++i)
         {
            buys[i] = EMPTY_VALUE;
            sells[i] = EMPTY_VALUE;
         }
         int half = ArraySize(mbi) / 2; // estimation of the middle slot
         bool correct = true;
         // collect all levels into a string to display
         string s = "";
         for(int i = 0; i < ArraySize(mbi); ++i)
         {
            s += StringFormat("%02d %s %s %d %g\n", i,
               (mbi[i].type == BOOK_TYPE_BUY ? "B" :
               (mbi[i].type == BOOK_TYPE_SELL ? "S" : "?")),
               DoubleToString(mbi[i].price, digits),
               mbi[i].volume, mbi[i].volume_real);
               
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
         // NB: Comment is important for implicit chart refreshing!
         Comment(s + (!correct ? "\nINCORRECT BOOK" : ""));
         
         if(!correct) return;

         // now fill indicator buffers with data
         if(AdvancedMode) // show empty slots
         {
            for(int i = 0; i < ArraySize(mbi); ++i)
            {
               if(i < half)
               {
                  const int x = (int)MathRound((mbi[i].price - mbi[half - 1].price) / tick);
                  if(x < Bars(_Symbol, _Period))
                  {
                     sells[x] = -VOL(mbi[i].volume_real);
                  }
               }
               else
               {
                  const int x = (int)MathRound((mbi[half].price - mbi[i].price) / tick);
                  if(x < Bars(_Symbol, _Period))
                  {
                     buys[x] = VOL(mbi[i].volume_real);
                  }
               }
            }
         }
         else // standard mode: skip empty slots
         {
            for(int i = 0; i < ArraySize(mbi); ++i)
            {
               if(i < half)
               {
                  sells[half - i - 1] = -VOL(mbi[i].volume_real);
               }
               else
               {
                  buys[i - half] = VOL(mbi[i].volume_real);
               }
            }
         }
      }
   }
}

//+------------------------------------------------------------------+
//| Custom indicator iteration function (dummy)                      |
//+------------------------------------------------------------------+
int OnCalculate(const int rates_total, const int prev_calculated, const int, const double &price[])
{
   if(prev_calculated == 0)
   {
      ArrayInitialize(buys, EMPTY_VALUE);
      ArrayInitialize(sells, EMPTY_VALUE);
   }
   return rates_total;
}

//+------------------------------------------------------------------+
//| Finalization handler                                             |
//+------------------------------------------------------------------+
void OnDeinit(const int)
{
   Comment("");
   PRTF(MarketBookRelease(_WorkSymbol));
}
//+------------------------------------------------------------------+
