//+------------------------------------------------------------------+
//|                                          scControl panel MCM.mq5 |
//|                                            Copyright 2010, Lizar |
//|                            https://login.mql5.com/ru/users/Lizar |
//+------------------------------------------------------------------+
#define VERSION         "1.00 Build 1 (06 Nov 2010)"

#property copyright   "Copyright 2010, Lizar"
#property link        "Lizar-2010@mail.ru"
#property version     VERSION
#property description "The script is a demonstration of MCM Control Panel."

input color bg_color=Gray;          // Menu color
input color font_color=Gainsboro;   // Text color
input color select_color=Yellow;    // Selected color
input int   font_size=10;           // Font size

#include <Control panel MCM.mqh> //<--- include file "Control panel MCM.mqh"
//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
  {
   //--- MCM Control panel initialization:
   InitControlPanelMCM(bg_color,font_color,select_color,font_size);   
   //---
  }
//+------------------------------------------------------------------+
