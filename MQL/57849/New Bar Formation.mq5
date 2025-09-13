//+------------------------------------------------------------------+
//|                                            New Bar Formation.mq5 |
//|                                  Copyright 2025, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
//--- code by Clinton Dennis Email: Clintondennis911@gmail.com
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
//---
   
//---
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
//---
   
  }
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
  {
//--- detect newly formed candles
//-------------------------------------------------------------------------------------------+
   static datetime previous_time = 0; //--- store previous bar open datetime
   datetime current_time = iTime(_Symbol, PERIOD_CURRENT, 0); //-- return current bar datetime
   if(previous_time != current_time) //--- check change in bar open datetime
     {
      /*
       your one time run code per new bar formed should be in here
      the PERIOD_CURRENT is your current chart TimeFrame
      and can be changed to any TimeFrame of your choice.
      */
      Print("new Bar Formed"); //--- print when a new bar is formed.
      previous_time = current_time; //--- reset the previous datetime to the new bar datetime
     } //--- end of if statement
//-------------------------------------------------------------------------------------------+
  }