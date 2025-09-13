//+------------------------------------------------------------------+
//|                                                   MultiTrend.mq5 |
//|                                            Copyright 2010, Lizar |
//|                            https://login.mql5.com/ru/users/Lizar |
//+------------------------------------------------------------------+
#define VERSION       "1.00 Build 2 (09 Dec 2010)"

#property copyright   "Copyright 2010, Lizar"
#property link        "https://login.mql5.com/ru/users/Lizar"
#property version     VERSION
#property description "The Expert Advisor is a demonstration of MCM Control Panel."

input color bg_color=Gray;        // Menu background color
input color font_color=Gainsboro; // Font color
input color select_color=Yellow;  // Selected text color
input int   font_size=10;         // Font size

#include <Control panel MCM.mqh> //<--- include file

//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
   //--- MCM Control panel initialization. 
   //--- If colors is not specified, default colors will be used
   InitControlPanelMCM(bg_color,font_color,select_color,font_size);   
   //---
   return(0);
  }
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
   DeinitControlPanelMCM();   //<--- MCM Control panel deinitalization:
  }
  
//+------------------------------------------------------------------+
//| Standard event processing function                               |
//| It can be useful in multicurrency trading                        |
//+------------------------------------------------------------------+
void OnChartEvent(const int id,         // event id:
                                        // if id=CHARTEVENT_CUSTOM==0 - initialization, when prev_calculated==0
                                        // if id=CHARTEVENT_CUSTOM!=0 - symbol position in Market Watch
                  const long&   lparam, // chart timeframe
                  const double& dparam, // price
                  const string& sparam  // symbol
                 )
  {
      if(id>=CHARTEVENT_CUSTOM)      
        {
         Print(TimeToString(TimeCurrent(),TIME_SECONDS)," -> id=",id-CHARTEVENT_CUSTOM,":  ",sparam," ",EventDescription(lparam)," price=",dparam);
        }
  }
//+------------------------------------------------------------------+
