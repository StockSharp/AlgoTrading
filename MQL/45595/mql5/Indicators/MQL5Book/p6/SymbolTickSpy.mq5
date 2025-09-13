//+------------------------------------------------------------------+
//|                                                SymbolTickSpy.mq5 |
//|                                  Copyright 2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright   "2022, MetaQuotes Ltd."
#property link        "https://www.mql5.com"
#property description "Send custom events to specified chart upon receiving new ticks on symbols from a list. "
                      "Fill MqlTick structures with price/volume data on every tick.\n"

#property indicator_chart_window
#property indicator_plots 0

#define TICKSPY 0xFEED // 65261

input string SymbolList = 
   "EURUSD,GBPUSD,XAUUSD,USDJPY,USDCHF"; // List of symbols, comma separated (example)
input ushort Message = TICKSPY;          // Custom message id
input long Chart = 0;                    // Receiving chart id (do not edit)
input int Index = 0;                     // Index in symbol list (do not edit)

string Symbols[];
int Spreads[];
int SelfIndex = -1;

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
         ArrayResize(Spreads, n);
         for(int i = 0; i < n; ++i)
         {
            if(Symbols[i] != _Symbol)
            {
               ResetLastError();
               // run this indicator on different symbols with different settings,
               // specifically we pass ChartID to get notifications back from other symbols
               iCustom(Symbols[i], PERIOD_CURRENT, MQLInfoString(MQL_PROGRAM_NAME),
                  "", Message, ChartID(), i);
               if(_LastError != 0)
               {
                  PrintFormat("The symbol '%s' seems incorrect", Symbols[i]);
               }
            }
            else
            {
               SelfIndex = i;
            }
            Spreads[i] = 0;
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
      if(Chart > 0)
      {
         // send tick notification to requesting chart
         EventChartCustom(Chart, Message, Index, 0, NULL);
      }
      else if(SelfIndex > -1)
      {
         OnSymbolTick(SelfIndex);
      }
   }
  
   return rates_total;
}

//+------------------------------------------------------------------+
//| Chart event handler                                              |
//+------------------------------------------------------------------+
void OnChartEvent(const int id, const long &lparam, const double &dparam, const string &sparam)
{
   if(id == CHARTEVENT_CUSTOM + Message)
   {
      OnSymbolTick((int)lparam);
   }
}

//+------------------------------------------------------------------+
//| Multisymbol tick custom event                                    |
//+------------------------------------------------------------------+
void OnSymbolTick(const int index)
{
   const string symbol = Symbols[index];
   
   MqlTick tick;
   if(SymbolInfoTick(symbol, tick))
   {
      Spreads[index] = (int)MathRound((tick.ask - tick.bid)
         / SymbolInfoDouble(symbol, SYMBOL_POINT));
         
      string message = "";
      for(int i = 0; i < ArraySize(Spreads); ++i)
      {
         message += Symbols[i] + "=" + (string)Spreads[i] + "\n";
      }
      Comment(message);
   }
}

//+------------------------------------------------------------------+
//| Finalization handler                                             |
//+------------------------------------------------------------------+
void OnDeinit(const int)
{
   Comment("");
}
//+------------------------------------------------------------------+
