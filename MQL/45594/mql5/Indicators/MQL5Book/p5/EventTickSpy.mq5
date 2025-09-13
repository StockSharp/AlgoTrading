//+------------------------------------------------------------------+
//|                                                 EventTickSpy.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright   "2021, MetaQuotes Ltd."
#property link        "https://www.mql5.com"
#property description "This indicator sends custom events to specified chart upon receiving new ticks (OnCalculate calls).\n"

#property indicator_chart_window
#property indicator_plots 0
#property tester_everytick_calculate

#define TICKSPY 0xFEED // 65261

input string SymbolList = "EURUSD,GBPUSD,XAUUSD,USDJPY"; // List of symbols, comma separated (example)
input ushort Message = TICKSPY;                          // Custom message id
input long Chart = 0;                                    // Receiving chart id (do not edit)

string Symbols[];

//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
void OnInit()
{
   PrintFormat("Starting for chart %lld, msg=0x%X [%s]", Chart, Message, SymbolList);
   if(Chart == 0)
   {
      if(StringLen(SymbolList) > 0)
      {
         const int n = StringSplit(SymbolList, ',', Symbols);
         for(int i = 0; i < n; ++i)
         {
            if(Symbols[i] != _Symbol)
            {
               ResetLastError();
               // run this indicator once again on different symbols with different settings,
               // specifically we pass ChartID to get notifications back from other symbols
               iCustom(Symbols[i], PERIOD_CURRENT, MQLInfoString(MQL_PROGRAM_NAME),
                  "", Message, ChartID());
               // Alternatively we could use a range of custom events, calling it
               // with Message + i in the parameter second from the end
               if(_LastError != 0)
               {
                  PrintFormat("The symbol '%s' seems incorrect", Symbols[i]);
               }
            }
         }
      }
      else
      {
         Print("SymbolList is empty: tracking current symbol only!");
         Print("To monitor other symbols, fill in SymbolList, i.e. 'EURUSD,GBPUSD,XAUUSD,USDJPY'");
      }
   }
}

//+------------------------------------------------------------------+
//| Custom indicator iteration function                              |
//+------------------------------------------------------------------+
int OnCalculate(const int rates_total, const int prev_calculated, const int, const double &price[])
{
   if(prev_calculated)
   {
      ArraySetAsSeries(price, true);
      if(Chart > 0)
      {
         // send tick notification to requesting chart
         EventChartCustom(Chart, Message, 0, price[0], _Symbol);
      }
      else
      {
         OnSymbolTick(_Symbol, price[0]);
      }
   }
  
   return rates_total;
}

//+------------------------------------------------------------------+
//| Chart event handler                                              |
//+------------------------------------------------------------------+
void OnChartEvent(const int id, const long &lparam, const double &dparam, const string &sparam)
{
   if(id >= CHARTEVENT_CUSTOM + Message)
   {
      OnSymbolTick(sparam, dparam);
      // OR (if we use a range of custom events):
      // OnSymbolTick(Symbols[id - CHARTEVENT_CUSTOM - Message], dparam);
   }
}

//+------------------------------------------------------------------+
//| Multisymbol tick custom event                                    |
//+------------------------------------------------------------------+
void OnSymbolTick(const string &symbol, const double price)
{
   Print(symbol, " ", DoubleToString(price, (int)SymbolInfoInteger(symbol, SYMBOL_DIGITS)));
}
//+------------------------------------------------------------------+
