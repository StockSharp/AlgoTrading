//+------------------------------------------------------------------+
//|                                                     bobnaley.mq5 |
//|                                             Copyright 2010, AM2. |
//|                                   http://www.crossmaker.narod.ru |
//+------------------------------------------------------------------+

#property copyright "Copyright 2010, AM2."
#property link      "http://www.crossmaker.narod.ru"
#property version   "1.12"

#include <Trade\Trade.mqh>            

//--- input parameters
input double TakeProfit    =   0.007; // Take Profit
input double StopLoss      =   0.0035;// Stop Loss
input int MA_Period        =      76; // Moving Average period
input int Stoch_OverSold   =      30; // Stochastic oversold level
input int Stoch_OverBought =      70; // Stochastic overbought level
input double Lot           =       5; // Lots to trade

//--- global variables
int maHandle;                         // handle of the Moving Average indicator
int stochHandle;                      // handle of the Stochastic indicator
double maVal[3];                      // array for values of Moving Average
double stochVal[3];                   // array for values of Stochastic indicator
CTrade trade;                         // using CTrade class
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
//---get handle of Moving Average indicator
   maHandle=iMA(NULL,0,MA_Period,0,MODE_SMA,PRICE_CLOSE);
//--- get handle of Stochastic indicator
   stochHandle=iStochastic(NULL,0,5,3,3,MODE_EMA,STO_CLOSECLOSE);

//--- check handles
   if(maHandle<0 || stochHandle<0)
     {
      Alert("Error in creation of indicators - error no: ",GetLastError(),"!!");
      return(-1);
     }
   return(0);
  }
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
//--- release handles
   IndicatorRelease(maHandle);
   IndicatorRelease(stochHandle);
  }
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
  {

//--- do we have necessary number of bars
   int Mybars=Bars(_Symbol,_Period);
   if(Mybars<100) // if bars<100
     {
      Alert("We have less than 100 bars on the chart, the Expert Advisor will exit!!!");
      return;
     }
//--- get the indicator's data
   if(CopyBuffer(maHandle,0,0,3,maVal)<0 || CopyBuffer(stochHandle,0,0,3,stochVal)<0)
     {
      Alert("Error copying of the indicator's buffers - error no:",GetLastError());
      return;
     }

   double Ask = SymbolInfoDouble(_Symbol,SYMBOL_ASK); // ask price
   double Bid = SymbolInfoDouble(_Symbol,SYMBOL_BID); // bid price

/*
    1. Check conditions to open long position : MA increases, 
       Stochastic increases and located below the oversold level
*/

//--- boolean variable used to check buy conditions
   bool BuyCondition =(maVal[0]<maVal[1] && maVal[1]<maVal[2] && Ask > maVal[0]&&  // MA increases, price>MA
                       stochVal[1]>stochVal[2] && stochVal[0]<Stoch_OverSold);     // Stochastic increases, Stochastic<Stoch_OverSold 

//--- combine all togther
   if(BuyCondition)                                          // buy condition ok
      if(!PositionSelect(_Symbol))                           // no position yet
         if(AccountInfoDouble(ACCOUNT_FREEMARGIN)>5000)      // if we have enough money
           {
            trade.PositionOpen(_Symbol,                                          // symbol
                               ORDER_TYPE_BUY,                                   // buy order
                               Money_M(),                                        // lots to trade
                               Ask,                                              // last ask price
                               Ask - StopLoss,                                   // Stop Loss
                               Ask + TakeProfit,                                 // Take Profit 
                               " ");                                             // no comments
           }
/*
    2. Check conditions to open short position : MA decreases, 
       Stochastic decreases and located above the overbought level
*/

//--- boolean variable used to check buy conditions
   bool SellCondition = (maVal[0]>maVal[1]) && (maVal[1]>maVal[2]&& Bid < maVal[0]&&  // MA decreases, price<MA
                         stochVal[1]<stochVal[2] && stochVal[0]>Stoch_OverBought);    // Stochastic decreases, Stochastic>Stoch_OverBought

//--- סמבטנאול גסו גלוסעו
   if(SellCondition)                                         // sell condition ok
      if(!PositionSelect(_Symbol))                           // no position yet
         if(AccountInfoDouble(ACCOUNT_FREEMARGIN)>5000)      // if we have enough money
           {
            trade.PositionOpen(_Symbol,                                          // symbol
                               ORDER_TYPE_SELL,                                  // sell order
                               Money_M(),                                        // lots to trade
                               Bid,                                              // last bid price
                               Bid + StopLoss,                                   // Stop Loss
                               Bid - TakeProfit,                                 // Take Profit 
                               " ");                                             // no comments
           }
   return;
  }
//+------------------------------------------------------------------+
//|                     Returns the position volume                  |
//+------------------------------------------------------------------+
double Money_M()
  {
   double Lots=AccountInfoDouble(ACCOUNT_FREEMARGIN)/100000*50;
   Lots=MathMin(15,MathMax(0.1,Lots));
   if(Lots<0.1)
      Lots=NormalizeDouble(Lots,2);
   else
     {
      if(Lots<1) Lots=NormalizeDouble(Lots,1);
      else       Lots=NormalizeDouble(Lots,0);
     }
   return(Lots);
  }
//+------------------------------------------------------------------+
