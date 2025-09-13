#property copyright "Copyright 2022, Tradecian Algo"
#property link      "https://www.fiverr.com/biswait50/write-code-for-forex-expert-advisor-ea-on-mt4-and-mt5"
#property description  "Contact Mail  : help.tradecian@gmail.com"
#property description  "Telegram  : @pops1990"
#property version   "1.00"
#property  strict

input int  MAGIC_NUMBER =  20131111; // Magic Number
input string PAUSE_SETTINGS =" _______________PAUSE Settings _______________" ; //  _______________PAUSE Settings _______________
input int NUM_STOPS= 3; // Number of stops
input int WITHIN_MIN=20; // Within x minute
input int PAUSE_MIN=20; // Pause x minute
datetime times[];
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {

   ArrayResize(times,NUM_STOPS);
   return(INIT_SUCCEEDED);
  }

//+------------------------------------------------------------------+
//|             Check last N losses                                  |
//+------------------------------------------------------------------+
bool CheckLastNLossDifference()
  {

   int lossCount=0;
   for(int i=OrdersHistoryTotal()-1; i>=0; i--)
     {
      if(OrderSelect(i,SELECT_BY_POS,MODE_HISTORY)&&
         OrderMagicNumber()==MAGIC_NUMBER &&
         OrderSymbol()==Symbol())
        {
         if(OrderProfit()<0)
           {
            if(lossCount>=NUM_STOPS)
               break;
            times[lossCount]= OrderOpenTime();
            lossCount++;
           }
         else
            break;

        }
     }

   if(lossCount>=NUM_STOPS)
     {
      int timeDiff= (times[NUM_STOPS-1]-times[0])/60;
      if(timeDiff<=WITHIN_MIN)
         return true;
     }
   return false;
  }


//+------------------------------------------------------------------+
//|     Get the last Order time                      |
//+------------------------------------------------------------------+
datetime lastOrderCloseTime()
  {
   datetime time;
   for(int i = (OrdersHistoryTotal() - 1); i >= 0; i--)
     {
      // If the order cannot be selected, throw and log an error.
      if(OrderSelect(i, SELECT_BY_POS, MODE_HISTORY)  && OrderSymbol()==Symbol() && OrderMagicNumber()==MAGIC_NUMBER)
        {
         time=OrderCloseTime();
         break;
        }

     }
   return time;
  }


//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
  {
   if(CheckLastNLossDifference())
     {
      int timeDiff= (TimeCurrent()-lastOrderCloseTime())/60;
      Print("timeDiff "+timeDiff);
      if(timeDiff<PAUSE_MIN)
        {
         Comment("Trading stops due to consecutive loss");
         return;

        }

     }
}