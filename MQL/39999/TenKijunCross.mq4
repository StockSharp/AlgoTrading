//+------------------------------------------------------------------+
//|                                                     TenKijun.mq4 |
//|                                                  ALI Hassanzadeh |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright "ALI Hassanzadeh"
#property link      "https://www.mql5.com/en/users/shadowtp"
#property version   "1.00"
#property strict



input int StartHour = 00; // Start operation hour
input int LastHour = 20; // Last operation hour

int totalBars;


//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
//---
   totalBars = iBars(_Symbol, PERIOD_CURRENT);
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
//---
   int bars = iBars(_Symbol, PERIOD_CURRENT);
   if(totalBars != bars){
   
       totalBars = bars;
       
   
      if(CheckActiveHours()){
        
         signalGenerator();

      } else Comment("It's Outside of Active Trading Time. No New Position will open");
       
       
   }
  }
//+------------------------------------------------------------------+

// This Section Is for Checking the Expert Operational Activity 
bool CheckActiveHours()
{
  // Set operations disabled by default.
   bool OperationsAllowed = false;
   if(Hour() >= StartHour && Hour() <= LastHour) OperationsAllowed = true;
   return OperationsAllowed;
}


// This Section Checks for TenkanSen and KijunSen Cross in your Prefered TimeFrame and Sends you the notification Accordingly 
void signalGenerator(){

   double t1 = iIchimoku(_Symbol, PERIOD_CURRENT, 9, 26, 52, MODE_TENKANSEN,0);
   double t2 = iIchimoku(_Symbol, PERIOD_CURRENT, 9, 26, 52, MODE_TENKANSEN,1);
   double k1 = iIchimoku(_Symbol, PERIOD_CURRENT, 9, 26, 52,MODE_KIJUNSEN, 0);
   double k2 = iIchimoku(_Symbol, PERIOD_CURRENT, 9, 26, 52, MODE_KIJUNSEN, 1);
   
   if(t1 < k1 && t2 >= k2){
       string text = _Symbol + " Sell Cross on " + PERIOD_CURRENT;
       SendNotification(text);
   }
   else if(t1 > k1 && t2 <= k2){
       string text = _Symbol + " Buy Cross on " + PERIOD_CURRENT;
       SendNotification(text);
   }
   
   
}