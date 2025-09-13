//+------------------------------------------------------------------+
//|                                                     FatPanel.mq5 |
//|                                                     Igor Volodin |
//|                                       http://www.thefatpanel.com |
//+------------------------------------------------------------------+
#property copyright "Igor Volodin"
#property link      "http://www.thefatpanel.com"
//#property version   "0.2"

#property description "The First Algorithmic Trading (FAT) Panel allows you to create the automated trading strategies without writing the code"
#property tester_indicator "Fatpanel//panel.ex5"

#include <Arrays\List.mqh>
#include <Fatpanel\PanelDispatcher.mqh>

CPanelDispatcher _pdispatcher;
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit() 
  {
   EventSetTimer(1);
   _pdispatcher.init();
   return(0);
  }
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason) 
  {
   _pdispatcher.deinit();
   EventKillTimer();
  }
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTimer() 
  {
   _pdispatcher.control();
  }
//+------------------------------------------------------------------+
//| OnTrade                                                          |
//+------------------------------------------------------------------+
void OnTrade() 
  {
   _pdispatcher.trade();
  }
//+------------------------------------------------------------------+

//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick() 
  {
  }
//+------------------------------------------------------------------+
//| Expert Event function                                            |
//+------------------------------------------------------------------+
void OnChartEvent(const int event,const long &lparam,const double &dparam,const string &sparam) 
  {
   _pdispatcher.behavior(event,lparam,dparam,sparam);
  }
//+------------------------------------------------------------------+
