//+------------------------------------------------------------------+
//|                                    A very simple sample of clock |
//|                                                   LoongClock.mq5 |
//|                             Copyright 2010, Loong@forum.mql4.com |
//|                             http://login.mql5.com/en/users/Loong |
//+------------------------------------------------------------------+
#property copyright "2010, Loong@forum.mql4.com"
#property link      "http://login.mql5.com/en/users/Loong"
#property version   "1.00"

#include "CLoongClock.mqh"

input ENUM_TIME_FUNC  inp_tf = TIME_FUNC_LOCAL;

CLoongClock c1;
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
//---
   EventSetTimer(1); // 1 second
   c1.SetTimeFunc(inp_tf);
//---
   return(0);
  }
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
//---
   EventKillTimer();
  }
//+------------------------------------------------------------------+
//| Expert timer function                                            |
//+------------------------------------------------------------------+
void OnTimer()
  {
//---
   c1.Timer();
   ChartRedraw();
  }
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
  {
//---
   
  }
//+------------------------------------------------------------------+
