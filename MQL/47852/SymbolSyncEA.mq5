//+------------------------------------------------------------------+
//|                                                 SymbolSyncEA.mq5 |
//|                                  Copyright 2024, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
//+------------------------------------------------------------------+
//| Expert Advisor to monitor chart symbol change                    |
//+------------------------------------------------------------------+
string initialSymbol;
string SyncSymbol;
//+------------------------------------------------------------------+
//| Expert initialization function      
//+------------------------------------------------------------------+
//seems everytime symbol is changed oninit is called so its easy to sync here
int OnInit()
  {
// Store the initial symbol
   initialSymbol = Symbol();
   PrintFormat(" Expert oninit with symbol: %s", initialSymbol);
   SyncSymbol=initialSymbol;
   SyncSymbols();

// Set a timer to check for symbol change every 500ms
 //  EventSetTimer(500);

   return(INIT_SUCCEEDED);
  }

//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
// Remove the timer when the EA is removed
 //  EventKillTimer();
  }

//+------------------------------------------------------------------+
//| Timer event handler          (not needed here just for my own reference                                     |
//+------------------------------------------------------------------+
void OnTimer()
  {
// Check if the symbol has changed
   if(Symbol() != initialSymbol)
     {
      // Symbol has changed, set a flag (e.g., global variable)
      // GlobalVariableSet("SymbolChanged", 1);
      string newSymbol=Symbol();
      PrintFormat("Symbol changed from %s to %s ",initialSymbol,newSymbol);
      initialSymbol = newSymbol; // Update the initial symbol
      SyncSymbol=newSymbol;
      SyncSymbols();

      // Optionally reset the global variable after a certain time or condition
      // GlobalVariableDel("SymbolChanged");
     }
  }

// Function to change chart symbol
void ChangeChartSymbol(long chartID, string symbol)
  {
   ENUM_TIMEFRAMES chartTF = ChartPeriod(chartID);

   if(ChartSymbol(chartID) != symbol)
     {
      ChartSetSymbolPeriod(chartID, symbol, chartTF);
      PrintFormat("Change symbol of the chart %s. New symbol %s", symbol, chartID);
     }
  }

//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool SyncSymbols()
  {
   int chartlimit=10;
// Get the first chart ID
   long chartID = ChartFirst();
   int count = 0;

// Loop through charts
   while(chartID >= 0 && count < chartlimit)
     {
      ChangeChartSymbol(chartID, SyncSymbol);

      // Get the next chart ID
      chartID = ChartNext(chartID);
      count++;
     }
   return true;
  }
//+------------------------------------------------------------------+
