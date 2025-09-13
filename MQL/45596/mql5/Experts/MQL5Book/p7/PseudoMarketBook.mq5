//+------------------------------------------------------------------+
//|                                             PseudoMarketBook.mq5 |
//|                                  Copyright 2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright   "Copyright 2022, MetaQuotes Ltd."
#property link        "https://www.mql5.com"
#property description "Generate pseudo market book from ticks for a custom symbol based on a symbol without market books."

#include <MQL5Book/PRTF.mqh>
#include <MQL5Book/MqlError.mqh>
#include <MQL5Book/Defines.mqh>
#include <MQL5Book/CustomSymbolMonitor.mqh>

input string CustomPath = "MQL5Book\\Part7"; // Custom Symbol Folder
input uint CustomBookDepth = 20;

string CustomSymbol;
int depth;
double contract;

//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
{
   CustomSymbol = _Symbol + ".Pseudo";
   
   bool custom = false;
   if(!PRTF(SymbolExist(CustomSymbol, custom)))
   {
      if(PRTF(CustomSymbolCreate(CustomSymbol, CustomPath, _Symbol)))
      {
         CustomSymbolSetString(CustomSymbol, SYMBOL_DESCRIPTION, "Pseudo book generator");
         CustomSymbolSetString(CustomSymbol, SYMBOL_FORMULA, "\"" + _Symbol + "\"");
      }
   }
   else if(!custom)
   {
      Alert("Standard symbol already exists");
      return INIT_FAILED;
   }
   else
   {
      if(IDYES == MessageBox(StringFormat("Delete existing custom symbol '%s'?", CustomSymbol),
         "Please, confirm", MB_YESNO))
      {
         PRTF(MarketBookRelease(CustomSymbol));
         PRTF(SymbolSelect(CustomSymbol, false));
         PRTF(CustomRatesDelete(CustomSymbol, 0, LONG_MAX));
         PRTF(CustomTicksDelete(CustomSymbol, 0, LONG_MAX));
         if(!PRTF(CustomSymbolDelete(CustomSymbol)))
         {
            Alert("Can't delete ", CustomSymbol, ", please, check up and delete manually");
         }
         return INIT_PARAMETERS_INCORRECT;
      }
   }
   
   if(PRTF(SymbolInfoInteger(_Symbol, SYMBOL_TICKS_BOOKDEPTH)) != CustomBookDepth
   && PRTF(SymbolInfoInteger(CustomSymbol, SYMBOL_TICKS_BOOKDEPTH)) != CustomBookDepth)
   {
      Print("Adjusting custom market book depth");
      PRTF(CustomSymbolSetInteger(CustomSymbol, SYMBOL_TICKS_BOOKDEPTH, CustomBookDepth));
   }

   PRTF(SymbolSelect(CustomSymbol, true));

   SymbolMonitor sm;
   CustomSymbolMonitor csm(CustomSymbol, &sm);
   int props[] = {SYMBOL_TRADE_TICK_VALUE, SYMBOL_TRADE_TICK_SIZE};
   const int d1 = csm.verify(props);
   if(d1)
   {
      Print("Number of found descrepancies: ", d1);
      if(csm.verify(props)) // check again
      {
         Alert("Custom symbol can not be created, internal error!");
         return INIT_FAILED;
      }
      Print("Fixed");
   }


   depth = (int)PRTF(SymbolInfoInteger(CustomSymbol, SYMBOL_TICKS_BOOKDEPTH));
   contract = PRTF(SymbolInfoDouble(CustomSymbol, SYMBOL_TRADE_CONTRACT_SIZE));
   
   return INIT_SUCCEEDED;
}

//+------------------------------------------------------------------+
//| Helper function to accumulate volumes by price levels            |
//+------------------------------------------------------------------+
void Place(double &array[], const int index, const double value = 1)
{
   const int size = ArraySize(array);
   if(index >= size)
   {
      ArrayResize(array, index + 1);
      for(int i = size; i <= index; ++i)
      {
         array[i] = 0;
      }
   }
   array[index] += value;
}

//+------------------------------------------------------------------+
//| Simulate market book based on previous ticks                     |
//| - what has been bought, will be sold                             |
//| - what has been sold, will be bought                             |
//+------------------------------------------------------------------+
bool GenerateMarketBook(const int count, MqlBookInfo &book[])
{
   MqlTick tick; // center of the book
   if(!SymbolInfoTick(_Symbol, tick)) return false;
   
   double buys[];  // buy volumes be price levels
   double sells[]; // sell volumes be price levels

   MqlTick ticks[];
   CopyTicks(_Symbol, ticks, COPY_TICKS_ALL, 0, count); // get tick history
   for(int i = 1; i < ArraySize(ticks); ++i)
   {
      // consider ask was moved up by buy
      int k = (int)MathRound((tick.ask - ticks[i].ask) / _Point);
      if(ticks[i].ask > ticks[i - 1].ask)
      {
         // already bought, will not buy again, probably will take profit by selling
         if(k <= 0)
         {
            Place(sells, -k, contract / sqrt(sqrt(ArraySize(ticks) - i)));
         }
      }
      
      // bid was moved down by sell
      k = (int)MathRound((tick.bid - ticks[i].bid) / _Point);
      if(ticks[i].bid < ticks[i - 1].bid)
      {
         // already sold, will not sell again, probably will take profit by buying
         if(k >= 0)
         {
            Place(buys, k, contract / sqrt(sqrt(ArraySize(ticks) - i)));
         }
      }
   }
   
   for(int i = 0, k = 0; i < ArraySize(sells) && k < depth; ++i) // upper half of the book
   {
      if(sells[i] > 0)
      {
         MqlBookInfo info = {};
         info.type = BOOK_TYPE_SELL;
         info.price = tick.ask + i * _Point;
         info.volume = (long)sells[i];
         info.volume_real = (double)(long)sells[i];
         PUSH(book, info);
         ++k;
      }
   }

   for(int i = 0, k = 0; i < ArraySize(buys) && k < depth; ++i) // bottom half of the book
   {
      if(buys[i] > 0)
      {
         MqlBookInfo info = {};
         info.type = BOOK_TYPE_BUY;
         info.price = tick.bid - i * _Point;
         info.volume = (long)buys[i];
         info.volume_real = (double)(long)buys[i];
         PUSH(book, info);
         ++k;
      }
   }
   
   return ArraySize(book) > 0;
}

//+------------------------------------------------------------------+
//| Tick event handler                                               |
//+------------------------------------------------------------------+
void OnTick()
{
   MqlBookInfo book[];
   if(GenerateMarketBook(2000, book))
   {
      ResetLastError();
      if(!CustomBookAdd(CustomSymbol, book))
      {
         Print("Can't add market books, ", E2S(_LastError));
         ExpertRemove();
      }
   }
}

//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int)
{
   // this call is required because CustomBookAdd implicitly locks
   // the symbol for which the books are generated,
   // without this call user will not be able to remove the symbol
   // from the Market Watch (where it is internally selected from
   // inside CustomBookAdd, even if it's not visible)
   // and delete it
   PRTF(MarketBookRelease(CustomSymbol));
}

//+------------------------------------------------------------------+
