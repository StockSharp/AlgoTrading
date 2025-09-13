//+------------------------------------------------------------------+
//|                                                 Code Checker.mq5 |
//|                                                  by H A T Lakmal |
//|                                           https://t.me/Lakmal846 |
//+------------------------------------------------------------------+
#property copyright "by H A T Lakmal"
#property link      "https://t.me/Lakmal846"
#property version   "1.00"

bool NewBarRecived = false; // Falg for controll.

//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
//--- create timer
   EventSetTimer(60);

//---
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
//--- destroy timer
   EventKillTimer();

  }


//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
  {
   datetime TimePreviousBar = iTime(_Symbol,PERIOD_M1,1);
   datetime TimeCurrentClose = TimePreviousBar + 60; // Closing Time of the current bar.
   datetime Time_Current = TimeCurrent();

   if(Time_Current == TimeCurrentClose && NewBarRecived == false)
     {
      PlaySound("ok.wav");   // For the statement work of not.
      NewBarRecived = true; // Update the flag to avoide multiple  calls.


      // Your Code goes here ----- (Do Something)

     }
   else
      if(Time_Current > TimeCurrentClose)
        {
         NewBarRecived = false; // Rest the flag for next bar open.



         // Your Code goes here ----- (Do Something)


        }


   Comment("\n" +  "\n" +  "Time Current Bar -: " + TimeToString(TimePreviousBar,TIME_DATE|TIME_MINUTES|TIME_SECONDS) +
           "\n" + "Time Current Close -: " +TimeToString(TimeCurrentClose,TIME_DATE|TIME_MINUTES|TIME_SECONDS) +
           "\n" + "Time Current -: " + TimeToString(Time_Current,TIME_DATE|TIME_MINUTES|TIME_SECONDS) + "\n" +"\n" + "A New Bar Recived -: " + NewBarRecived); // For check calculations


  }
//+------------------------------------------------------------------+
//| Timer function                                                   |
//+------------------------------------------------------------------+
void OnTimer()
  {
//---

  }
//+------------------------------------------------------------------+
//| Trade function                                                   |
//+------------------------------------------------------------------+
void OnTrade()
  {
//---

  }
//+------------------------------------------------------------------+
//| ChartEvent function                                              |
//+------------------------------------------------------------------+
void OnChartEvent(const int id,
                  const long &lparam,
                  const double &dparam,
                  const string &sparam)
  {
//---

  }
//+------------------------------------------------------------------+
