//+------------------------------------------------------------------+
//|                                        OnTick(string symbol).mq5 |
//|                                            Copyright 2010, Lizar |
//|                            https://login.mql5.com/ru/users/Lizar |
//+------------------------------------------------------------------+
#define VERSION       "1.00 Build 1 (01 Fab 2011)"

#property copyright   "Copyright 2010, Lizar"
#property link        "https://login.mql5.com/ru/users/Lizar"
#property version     VERSION
#property description "Template of the Expert Advisor"
#property description "with multicurrency OnTick(string symbol) event handler"

//+------------------------------------------------------------------+
//|                MULTICURRENCY MODE SETTINGS                       |
//|           of OnTick(string symbol) event handler                 |
//|                                                                  |
//| 1.1 List of symbols needed to proceed in the events:             |
#define  SYMBOLS_TRADING    "EURUSD","GBPUSD","USDJPY","USDCHF"
//| 1.2 If you want all symbols from Market Watch, use this:         |
//#define  SYMBOLS_TRADING    "MARKET_WATCH"
//|     Note: Select only one way from 1.1 or 1.2.                   |
//|                                                                  |
//| 2.  Event type for OnTick(string symbol):                        |
#define  CHART_EVENT_SYMBOL CHARTEVENT_TICK 
//|     Note: the event type must corresponds to the                 |
//|                 ENUM_CHART_EVENT_SYMBOL enumeration.             |
//|                                                                  |
//| 3.  Include file:                                                |
#include <OnTick(string symbol).mqh>
//+------------------------------------------------------------------+

//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//| This function must be declared, even if it empty.                |
//+------------------------------------------------------------------+
int OnInit()
  {
   //--- Add your code here...
   return(0);
  }
  
//+------------------------------------------------------------------+
//| Expert multi tick function                                       |
//| Use this function instead of the standard OnTick() function      |
//+------------------------------------------------------------------+
void OnTick(string symbol)
  {
   //--- Add your code here...
   Print("New event on symbol: ",symbol);
  }
  
//+------------------------------------------------------------------+
//| ChartEvent function                                              |
//| This function must be declared, even if it empty.                |
//+------------------------------------------------------------------+
void OnChartEvent(const int id,         // event id
                  const long& lparam,   // event param of long type
                  const double& dparam, // event param of double type
                  const string& sparam) // event param of string type
  {
   //--- Add your code here...
  }
  
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
   //--- Add your code here...
  }

//+------------------------------ end -------------------------------+