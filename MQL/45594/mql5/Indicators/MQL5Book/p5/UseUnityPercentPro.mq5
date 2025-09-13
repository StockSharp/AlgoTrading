//|------------------------------------------------------------------+
//|                                           UseUnityPercentPro.mq5 |
//|                               Copyright (c) 2018-2021, Marketeer |
//|                          https://www.mql5.com/en/users/marketeer |
//|------------------------------------------------------------------+
#property copyright "2018-2021 © Marketeer"
#property link      "https://www.mql5.com/en/users/marketeer"
#property version   "1.0"
#property description "Multi-asset cluster indicator taking all changes"
                      " of assets' values as a sum of squares equalling to 1.0,"
                      " hence forming overall picture of the market"

#property indicator_separate_window

#define BUF_NUM 15 // max number of entities (currencies)

// reserve up to BUF_NUM lines (can use less)
#property indicator_buffers BUF_NUM
#property indicator_plots BUF_NUM

// predefined set of colors
#property indicator_color1 Green
#property indicator_color2 DarkBlue
#property indicator_color3 Red
#property indicator_color4 Gray
#property indicator_color5 Peru
#property indicator_color6 Gold
#property indicator_color7 Purple
#property indicator_color8 Teal
#property indicator_color9 LightGreen
#property indicator_color10 LightBlue
#property indicator_color11 Orange
#property indicator_color12 LightGray
#property indicator_color13 Brown
#property indicator_color14 Yellow
#property indicator_color15 Magenta

// simplify usage of array of buffers
#include <MQL5Book/IndBufArray.mqh>
#include <MQL5Book/MapArray.mqh>

// inputs
input string Instruments = "EURUSD,GBPUSD,USDCHF,USDJPY,AUDUSD,USDCAD,NZDUSD";
input int BarLimit = 500;
input ENUM_APPLIED_PRICE PriceType = PRICE_CLOSE;
input ENUM_MA_METHOD PriceMethod = MODE_EMA;
input int PricePeriod = 1;

// globals
string Symbols[];
int Direction[];
int SymbolCount;
int Handles[];

// indicator buffers are inside this array
BufferArray buffers(BUF_NUM, true);

// stats by currencies for given list of instruments
MapArray<string,int> workCurrencies;

//+------------------------------------------------------------------+
//| Helper function to parse symbols, find common currency and       |
//| detect direction of every symbol against the common              |
//+------------------------------------------------------------------+
string InitSymbols()
{
   SymbolCount = StringSplit(Instruments, ',', Symbols);
   if(SymbolCount >= BUF_NUM)
   {
     SymbolCount = BUF_NUM - 1;
     ArrayResize(Symbols, SymbolCount);
   }
   else if(SymbolCount == 0)
   {
     SymbolCount = 1;
     ArrayResize(Symbols, SymbolCount);
     Symbols[0] = Symbol();
   }
  
   ArrayResize(Direction, SymbolCount);
   ArrayInitialize(Direction, 0);
   ArrayResize(Handles, SymbolCount);
   ArrayInitialize(Handles, INVALID_HANDLE);
  
   string common = NULL; // common currency for all symbols
  
   for(int i = 0; i < SymbolCount; i++)
   {
      // ensure that symbol is present in Market Watch
      if(!SymbolSelect(Symbols[i], true))
      {
         Print("Can't select ", Symbols[i]);
         return NULL;
      }
      
      // get currencies constructing this symbol
      string first, second;
      first = SymbolInfoString(Symbols[i], SYMBOL_CURRENCY_BASE);
      second = SymbolInfoString(Symbols[i], SYMBOL_CURRENCY_PROFIT);
      
      // count the number of uses for every currency
      if(first != second)
      {
         workCurrencies.inc(first);
         workCurrencies.inc(second);
      }
      else
      {
         workCurrencies.inc(Symbols[i]);
      }
   }
  
   if(workCurrencies.getSize() >= BUF_NUM)
   {
      Print("Too many symbols, max ", (BUF_NUM - 1));
      return NULL;
   }
   
   // find a common currency based on collected usage stats
   for(int i = 0; i < workCurrencies.getSize(); i++)
   {
      if(workCurrencies[i] > 1) // count is more than 1
      {
         if(common == NULL)
         {
            common = workCurrencies.getKey(i); // get i-th name
         }
         else
         {
            Print("Collision: multiple common symbols: ",
               common, "[", workCurrencies[common], "] ",
               workCurrencies.getKey(i), "[", workCurrencies[i], "]");
            return NULL;
         }
      }
   }
  
   if(common == NULL) common = workCurrencies.getKey(0);
  
   Print("Common currency=", common);
   
   // using common currency we can detect every symbol direction
   for(int i = 0; i < SymbolCount; i++)
   {
      if(SymbolInfoString(Symbols[i], SYMBOL_CURRENCY_PROFIT) == common) Direction[i] = +1;
      else if(SymbolInfoString(Symbols[i], SYMBOL_CURRENCY_BASE) == common) Direction[i] = -1;
      else
      {
         Print("Ambiguous symbol direction ", Symbols[i], ", defaults used");
         Direction[i] = +1;
      }
      Handles[i] = iMA(Symbols[i], PERIOD_CURRENT, PricePeriod, 0, PriceMethod, PriceType);      
   }
  
   return common;
}

//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
int OnInit()
{
   const string common = InitSymbols();
   if(common == NULL) return INIT_PARAMETERS_INCORRECT;
   
   string base = SymbolInfoString(_Symbol, SYMBOL_CURRENCY_BASE);
   string profit = SymbolInfoString(_Symbol, SYMBOL_CURRENCY_PROFIT);
   
   int replaceIndex = -1;
   // setup active plots
   for(int i = 0; i <= SymbolCount; i++)
   {
      string name;
      // rearrange labels in such way that base currency is always at index 0
      if(i == 0)
      {
         name = common;
         if(name != workCurrencies.getKey(i))
         {
            replaceIndex = i;
         }
      }
      else
      {
         if(common == workCurrencies.getKey(i) && replaceIndex > -1)
         {
            name = workCurrencies.getKey(replaceIndex);
         }
         else
         {
            name = workCurrencies.getKey(i);
         }
      }
    
      PlotIndexSetString(i, PLOT_LABEL, name);
      PlotIndexSetInteger(i, PLOT_DRAW_TYPE, DRAW_LINE);
      PlotIndexSetInteger(i, PLOT_SHOW_DATA, true);
      PlotIndexSetInteger(i, PLOT_LINE_WIDTH, 1 + (name == base || name == profit));
   }
  
   // hide excessive buffers in Data Window
   for(int i = SymbolCount + 1; i < BUF_NUM; i++)
   {
      PlotIndexSetInteger(i, PLOT_SHOW_DATA, false);
   }
  
   // single 1.0-level setup
   IndicatorSetInteger(INDICATOR_LEVELS, 1);
   IndicatorSetDouble(INDICATOR_LEVELVALUE, 0, 1.0);
  
   // user-friendly name
   IndicatorSetString(INDICATOR_SHORTNAME,
      StringFormat("Unity [%d] %s(%d,%s)", workCurrencies.getSize(),
      StringSubstr(EnumToString(PriceMethod), 5), PricePeriod,
      StringSubstr(EnumToString(PriceType), 6)));
  
   // accuracy
   IndicatorSetInteger(INDICATOR_DIGITS, 5);
  
   Print("Unity ", __FUNCTION__, " ", Bars(_Symbol, PERIOD_CURRENT));
   return INIT_SUCCEEDED;
}

//+------------------------------------------------------------------+
//| Main algorithm by formulae                                       |
//+------------------------------------------------------------------+
bool Calculate(const int bar)
{
   const datetime time0 = iTime(_Symbol, _Period, bar);
   const datetime time1 = iTime(_Symbol, _Period, bar + 1);
   
   double w[]; // receiving array for values by bar
   double v[]; // values by symbol
   ArrayResize(v, SymbolCount);

   // find change in quotes for every symbol
   for(int j = 0; j < SymbolCount; j++)
   {
      // try to get at least 2 bars on j-th symbol
      // corresponding to 2 nearest bars on current chart symbol
      int x = CopyBuffer(Handles[j], 0, time0, time1, w);
      if(x < 2)
      {
         // if bars are missing, try to get any nearest bar in past
         if(CopyBuffer(Handles[j], 0, time0, 1, w) != 1)
         {
            Print("No data for ", Symbols[j], " at ", (string)time0);
            return false; // problem
         }
         // then duplicate it as indication of no changes
         x = 2;
         ArrayResize(w, 2);
         w[1] = w[0];
      }

      // find inverted rate if required
      if(Direction[j] == -1)
      {
         w[x - 1] = 1.0 / w[x - 1];
         w[x - 2] = 1.0 / w[x - 2];
      }

      // now evaluate the change as ratio of 2 most recent values
      v[j] = w[x - 1] / w[x - 2]; // last / previous
   }
   
   // Calculate Unity formulae
   double sum = 1.0;
   for(int j = 0; j < SymbolCount; j++)
   {
      sum += v[j];
   }
   
   const double base_0 = (1.0 / sum);
   buffers[0][bar] = base_0 * (SymbolCount + 1);
   for(int j = 1; j <= SymbolCount; j++)
   {
      buffers[j][bar] = base_0 * v[j - 1] * (SymbolCount + 1);
   }
   
   return true; // success
}

//+------------------------------------------------------------------+
//| Timer function                                                   |
//+------------------------------------------------------------------+
void OnTimer()
{
   EventKillTimer();
   Print("Refresh");
   ChartSetSymbolPeriod(0, _Symbol, _Period);
}

//+------------------------------------------------------------------+
//| Custom indicator iteration function                              |
//+------------------------------------------------------------------+
int OnCalculate(const int rates_total,
                const int prev_calculated,
                const int begin,
                const double& price[])
{
   if(prev_calculated == 0)
   {
      Print("Empty");
      buffers.empty();
   }
   
   // main loop in reverse "as timeseries" direction, from now to past
   const int limit = MathMin(rates_total - prev_calculated + 1, BarLimit);
   for(int i = 0; i < limit; i++)
   {
      if(!Calculate(i))
      {
         Print("Timer");
         EventSetTimer(1); // give 1 more second to download and build data
         return 0; // will retry on next call
      }
   }
   
   return rates_total;
}
//+------------------------------------------------------------------+
