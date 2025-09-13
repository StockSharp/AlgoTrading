//+------------------------------------------------------------------+
//|                                             SimpleHedgePanel.mq5 |
//|                                            Copyright 2013, Rone. |
//|                                            rone.sergey@gmail.com |
//+------------------------------------------------------------------+
#property copyright "Copyright 2013, Rone."
#property link      "rone.sergey@gmail.com"
#property version   "1.00"
#property description "Simple Hedge Panel"
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
#include "HedgePanel.mqh"
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
input uint InpBoxes = 3;    // Boxes
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
CHedgePanel panel;
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit() {
//---
   if ( !panel.Init(InpBoxes, 20, 20) ) {
      return(INIT_FAILED);
   }   
   panel.Run();
//---
   return(INIT_SUCCEEDED);
}
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason) {
//---
   panel.Destroy(reason);
//---   
}
//+------------------------------------------------------------------+
//| Expert chart event function                                      |
//+------------------------------------------------------------------+
void OnChartEvent(const int id,         // event ID  
                  const long& lparam,   // event parameter of the long type
                  const double& dparam, // event parameter of the double type
                  const string& sparam) // event parameter of the string type
{
   panel.ChartEvent(id, lparam, dparam, sparam);
}
//+------------------------------------------------------------------+
