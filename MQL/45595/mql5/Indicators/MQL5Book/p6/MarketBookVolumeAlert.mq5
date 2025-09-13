//+------------------------------------------------------------------+
//|                                        MarketBookVolumeAlert.mq5 |
//|                                  Copyright 2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright   "2022, MetaQuotes Ltd."
#property link        "https://www.mql5.com"
#property description "Subscribe to market books for specified symbol and monitor changes with large volumes."

#property indicator_chart_window
#property indicator_plots 0

#define N_LINES 25                // number of lines in multicomment
#include <MQL5Book/Comments.mqh>

input string WorkSymbol = ""; // WorkSymbol (if empty, use current chart symbol)
input bool CountVolumeInLots = false;
input double VolumeLimit = 0;

const string _WorkSymbol = StringLen(WorkSymbol) == 0 ? _Symbol : WorkSymbol;
double contract;
int digits;

//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
void OnInit()
{
   MarketBookAdd(_WorkSymbol);
   contract = SymbolInfoDouble(_WorkSymbol, SYMBOL_TRADE_CONTRACT_SIZE);
   digits = (int)MathRound(MathLog10(contract));
   Print(SymbolInfoDouble(_WorkSymbol, SYMBOL_SESSION_BUY_ORDERS_VOLUME));
   Print(SymbolInfoDouble(_WorkSymbol, SYMBOL_SESSION_SELL_ORDERS_VOLUME));
}

#define VOL(V) (CountVolumeInLots ? V / contract : V)

//+------------------------------------------------------------------+
//| Market book notification handler                                 |
//+------------------------------------------------------------------+
void OnBookEvent(const string &symbol)
{
   if(symbol != _WorkSymbol) return;
   
   static MqlBookInfo mbp[];      // previous table
   MqlBookInfo mbi[];
   if(MarketBookGet(symbol, mbi)) // retrieve current book
   {
      if(ArraySize(mbp) == 0)
      {
         ArrayCopy(mbp, mbi);
         return;
      }
      
      int j = 0;
      for(int i = 0; i < ArraySize(mbi); ++i)
      {
         bool found = false;
         for( ; j < ArraySize(mbp); ++j)
         {
            if(MathAbs(mbp[j].price - mbi[i].price) < DBL_EPSILON * mbi[i].price) // mbp[j].price == mbi[i].price
            {
               if(VOL(mbi[i].volume_real - mbp[j].volume_real) >= VolumeLimit)
               {
                  NotifyVolumeChange("Enlarged", mbp[j].price, VOL(mbp[j].volume_real), VOL(mbi[i].volume_real));
               }
               else
               if(VOL(mbp[j].volume_real - mbi[i].volume_real) >= VolumeLimit)
               {
                  NotifyVolumeChange("Reduced", mbp[j].price, VOL(mbp[j].volume_real), VOL(mbi[i].volume_real));
               }
               found = true;
               ++j;
               break;
            }
            else if(mbp[j].price > mbi[i].price)
            {
               if(VOL(mbp[j].volume_real) >= VolumeLimit)
               {
                  NotifyVolumeChange("Removed", mbp[j].price, VOL(mbp[j].volume_real), 0.0);
               }
               // keep loop incrementing ++j to lower prices
            }
            else // mbp[j].price < mbi[i].price
            {
               break;
            }
         }
         if(!found) // unique price
         {
            if(VOL(mbi[i].volume_real) >= VolumeLimit)
            {
               NotifyVolumeChange("Added", mbi[i].price, 0.0, VOL(mbi[i].volume_real));
            }
         }
      }
      
      if(ArrayCopy(mbp, mbi) <= 0)
      {
         Print("ArrayCopy failed:", _LastError);
      }
      if(ArrayResize(mbp, ArraySize(mbi)) <= 0) // shrink if required
      {
         Print("ArrayResize failed:", _LastError);
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
   MarketBookRelease(_WorkSymbol);
   Comment("");
}

//+------------------------------------------------------------------+
//| Multisymbol tick custom event                                    |
//+------------------------------------------------------------------+
void NotifyVolumeChange(const string action, const double price,
   const double previous, const double volume)
{
   const string message = StringFormat("%s: %s %s -> %s",
      action,
      DoubleToString(price, (int)SymbolInfoInteger(_WorkSymbol, SYMBOL_DIGITS)),
      DoubleToString(previous, digits),
      DoubleToString(volume, digits));
   ChronoComment(message);
}
//+------------------------------------------------------------------+
