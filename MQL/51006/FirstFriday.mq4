//+------------------------------------------------------------------+
//|                                                  FirstFriday.mq4 |
//|                                  Copyright 2023, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright "Copyright 2023, MetaQuotes Ltd."
#property link      "https://www.mql5.com"
#property version   "1.00"
#property strict


datetime lastTime = 0; // Variable to store the time of the last detected candle

//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
   lastTime = iTime(Symbol(),PERIOD_D1,0);
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
   datetime currentTime = iTime(NULL, PERIOD_D1, 0); // Get the time of the current candle
   if(IsFirstFriday() && currentTime != lastTime)
     {
      Print("This is Friday of The First Week of The Month");
      lastTime = currentTime; // Update the lastTime to the current candle time

     }

  }
//+------------------------------------------------------------------+


//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool IsFirstFriday()
  {
// Get the current day of the week (0=Sunday, 1=Monday, ..., 5=Friday, 6=Saturday)
   int dayOfWeek = TimeDayOfWeek(TimeCurrent());

// Get the current day of the month
   int dayOfMonth = TimeDay(TimeCurrent());

// Check if today is Friday
   if(dayOfWeek == 5)
     {
      // Check if the day of the month is between 1 and 7
      if(dayOfMonth >= 1 && dayOfMonth <= 7)
        {
         return(true);
        }
     }
   return(false);
  }
//+------------------------------------------------------------------+
//+------------------------------------------------------------------+
