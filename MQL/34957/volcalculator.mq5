//+------------------------------------------------------------------+
//|                                             volumecalculator.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright "Copyright 2021, MetaQuotes Ltd."
#property link      "https://www.mql5.com"
#property version   "1.00"
//--- input parameters
input double      StopLossPrice;                //Set Stop Loss Price
input double      TakeProfitPrice;              // Set Take Profit Price
input double      MaxLossPercent=5;             // Set Maximum Percent of your count that you can  endure to loss per each trade (Default = 1)
input bool        Buy=true;                     // Set True if possitin is Long, and False if possition is short
double MaxLossValue = AccountInfoDouble(ACCOUNT_BALANCE)*(MaxLossPercent/100); // Max amount of money you may lose in this trade
double pipValue;                                //each pip value in this trade
double AllowedVolume;                           //Sauggested Volume for this trade
double TakePofitValue;                          //Maximum profit you can take in this trade
double RTR;                                     //Risk to Reward value
double TPpip;                                   //pip amount of Take Profit
double STpip;                                   //pip amount of Stop Loss


//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
//---
   double CurrentClose = iClose(_Symbol,_Period,0);
   if(Buy==true)
     {
      TPpip = MathCeil((TakeProfitPrice - CurrentClose)*10000);
      STpip = MathCeil((CurrentClose - StopLossPrice)*10000);
     }
   else
     {
      TPpip = MathCeil((CurrentClose - TakeProfitPrice)*10000);
      STpip= MathCeil((StopLossPrice - CurrentClose)*10000);
     }
   pipValue = MaxLossValue/STpip;
   TakePofitValue = pipValue*TPpip;
   AllowedVolume = pipValue/10;
   RTR = TPpip/STpip ;
   Print("-------------- New Calculations for New Position --------------");
   Print("Stop Loss pip = "+ DoubleToString(STpip,0));
   Print("Take Profit pip = "+ DoubleToString(TPpip,0));
   Print("Max Possible Loss Value = " + DoubleToString(MaxLossValue,2));
   Print("Take Profit Value = " + DoubleToString(TakePofitValue,2));
   Print("Risk to Reward Ratio = " + DoubleToString(RTR,2));
   Print("Allowed Volume = " + DoubleToString(AllowedVolume,4));
   if(RTR>=3)
      Print("You Can Trade");
   else
      Print("High Risk Possition, DO NOT TRADE");

//---s
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

  }
//+------------------------------------------------------------------+
