//+----------------------------------------------------------------------------------------------------------------------------------------+
//|                                                                                                                     AnalyzeHistory.mq4 |
//|                                                                                                       Copyright 2014, Dorian Ocsovszki |
//|                                                                                                                                        |
//| Launch the EA in the strategy tester with 'Use date' checkbox unticked and 'Open prices only' modeling                                 |
//| The EA will print in the log all the history gaps                                                                                      |
//| Gaps over the weekend are ignored (assuming broker closes on Friday and reopens on Sunday), but gaps over festivities are reported     |
//|                                                                                                                                        |
//+----------------------------------------------------------------------------------------------------------------------------------------+
#property copyright "Copyright 2014, Puglios"
#property version   "1.00"

#define  Sunday   0     // as returned by TimeDayOfWeek()
#define  Friday   5     // as returned by TimeDayOfWeek()

input  uint    MinGapInBars               = 10;                                                 // minimum gap detected (in number of bars)

// print starting date on initialization
int OnInit() 
{        
   Print("History starts from ",TimeToStr(Time[0],TIME_DATE));
   return(INIT_SUCCEEDED);
}

// scan history looking for gaps
void OnTick(void)
{  
   if(Time[0]-Time[1]>MinGapInBars*60*Period())                                                 // gap detected
      if(TimeDayOfWeek(Time[0]) != Sunday  ||  TimeDayOfWeek(Time[1]) != Friday)                // gap not due to the weekend
         Print("gap from ",TimeToStr(Time[1])," to ",TimeToStr(Time[0]));            
}


// print ending date on deinitialization
void OnDeinit(const int reason) 
{
   Print("History ends on ",TimeToStr(Time[0],TIME_DATE));
   return;
}