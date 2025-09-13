//+------------------------------------------------------------------+
//|                                      CustomSymbolRandomRates.mq5 |
//|                                  Copyright 2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright   "Copyright 2022, MetaQuotes Ltd."
#property link        "https://www.mql5.com"
#property description "Create custom symbol with randomized quotes based on current chart's symbol."
#property script_show_inputs

#include <MQL5Book/PRTF.mqh>

enum RANDOMIZATION
{
   ORIGINAL,
   RANDOM_WALK,
   FUZZY_WEAK,
   FUZZY_STRONG,
};

input string CustomPath = "MQL5Book\\Part7";    // Custom Symbol Folder
input RANDOMIZATION RandomFactor = RANDOM_WALK;
input datetime _From;                           // From (default: 120 days back)
input datetime _To;                             // To (default: up to now)
input uint RandomSeed = 0;                      // Random Seed (0 means random)

datetime From;
datetime To;
const string CustomSymbol = _Symbol + "." + EnumToString(RandomFactor)
   + (RandomSeed ? "_" + (string)RandomSeed : "");

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   From = _From == 0 ? TimeCurrent() - 60 * 60 * 24 * 120 : _From; // 120 days by default
   To = _To == 0 ? TimeCurrent() / 60 * 60 : _To; // till current time
   if(From > To)
   {
      Alert("Date range must include From <= To");
      return;
   }
   
   if(RandomSeed != 0) MathSrand(RandomSeed);

   bool custom = false;
   if(PRTF(SymbolExist(CustomSymbol, custom)) && custom)
   {
      if(IDYES == MessageBox(StringFormat("Delete custom symbol '%s'?", CustomSymbol),
         "Please, confirm", MB_YESNO))
      {
         if(CloseChartsForSymbol(CustomSymbol))
         {
            Sleep(500); // wait changes to take effect (unreliable)
            // we need to wipe out rates because otherwise the rates
            // remain in the base file on the disk for some time
            // and can be picked up by the following creation procedure
            PRTF(CustomRatesDelete(CustomSymbol, 0, LONG_MAX));
            PRTF(SymbolSelect(CustomSymbol, false));
            PRTF(CustomSymbolDelete(CustomSymbol));
         }
      }
   }
   
   if(IDYES == MessageBox(StringFormat("Create new custom symbol '%s'?", CustomSymbol),
      "Please, confirm", MB_YESNO))
   {
      if(PRTF(CustomSymbolCreate(CustomSymbol, CustomPath, _Symbol)))
      {
         if(RandomFactor == RANDOM_WALK)
         {
            CustomSymbolSetInteger(CustomSymbol, SYMBOL_DIGITS, 8);
         }
         
         CustomSymbolSetString(CustomSymbol, SYMBOL_DESCRIPTION, "Randomized quotes");
      
         const int n = GenerateQuotes();
         Print("Bars M1 generated: ", n);
         if(n > 0)
         {
            SymbolSelect(CustomSymbol, true);
            ChartOpen(CustomSymbol, PERIOD_M1);
         }
      }
   }
}

//+------------------------------------------------------------------+
//| Chart management function                                        |
//+------------------------------------------------------------------+
bool CloseChartsForSymbol(const string symbol)
{
   long id = ChartFirst();
   long match = -1;
   while(id != -1)
   {
      if(ChartSymbol(id) == symbol)
      {
         if(id == ChartID())
         {
            Alert("Can't close itself: start this script on another chart");
            return false;
         }
         else
         {
            match = id;
         }
      }
      id = ChartNext(id);
      if(match != -1)
      {
         ChartClose(match);
         match = -1;
      }
   }
   ResetLastError(); // clear CHART_NOT_FOUND (4103)
   return true;
}

//+------------------------------------------------------------------+
//| Get value mangled with nonuniform random noice                   |
//+------------------------------------------------------------------+
double RandomWalk(const double p)
{
   const static double factor[] = {0.0, 0.1, 0.01, 0.05};
   const static double f = factor[RandomFactor] / 100;
   const double r = (rand() - 16383.0) / 16384.0; // [-1,+1]
   const int sign = r >= 0 ? +1 : -1;

   if(r != 0)
   {
      return p + p * sign * f * sqrt(-log(sqrt(fabs(r))));
   }
   return p;
}

//+------------------------------------------------------------------+
//| Request, modify, commit MqlRates for custom symbol               |
//+------------------------------------------------------------------+
int GenerateQuotes()
{
   MqlRates rates[];
   MqlRates zero = {};
   datetime start;
   double price;
   
   if(RandomFactor != RANDOM_WALK)
   {
      // NB: terminal settings are in effect here, including bar limit
      if(PRTF(CopyRates(_Symbol, PERIOD_M1, From, To, rates)) <= 0)
      {
         return 0; // error
      }
      if(RandomFactor == ORIGINAL)
      {
         return PRTF(CustomRatesReplace(CustomSymbol, From, To, rates));
      }
      price = rates[0].open;
      start = rates[0].time;
   }
   else
   {
      ArrayResize(rates, (int)((To - From) / 60) + 1);
      price = 1.0;
      start = From - 60;
   }
   
   const int size = ArraySize(rates);
   
   double hlc[3];
   for(int i = 0; i < size; ++i)
   {
      if(RandomFactor == RANDOM_WALK)
      {
         rates[i] = zero;
         rates[i].time = start += 60;
         rates[i].open = price;
         hlc[0] = RandomWalk(price);
         hlc[1] = RandomWalk(price);
         hlc[2] = RandomWalk(price);
      }
      else
      {
         double delta = 0;
         if(i > 0)
         {
            delta = rates[i].open - price; // cumulative correction
         }
         rates[i].open = price;
         hlc[0] = RandomWalk(rates[i].high - delta);
         hlc[1] = RandomWalk(rates[i].low - delta);
         hlc[2] = RandomWalk(rates[i].close - delta);
      }
      ArraySort(hlc);
      
      rates[i].high = fmax(hlc[2], rates[i].open);
      rates[i].low = fmin(hlc[0], rates[i].open);
      rates[i].close = price = hlc[1];
      rates[i].tick_volume = 4;
   }
   
   return PRTF(CustomRatesReplace(CustomSymbol, From, To, rates));
}
//+------------------------------------------------------------------+
