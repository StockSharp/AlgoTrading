//+------------------------------------------------------------------+
//|                                                  Regression .mq4 |
//|                                                      |
//|                                                                  |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2010, MartyP"

//---- scalper
extern double      buy_bewegung = 10;
extern int         buy_period = 2;
extern double      buy_breakdown = 5;
extern double      buy_RSIv = 30;

extern double      sell_bewegung = 0.0040;
extern int         sell_period = 2;
extern double      sell_breakdown = 0.0030;
extern double      sell_RSIv = 30;

extern int buy_StopLoss=60;
extern int buy_TakeProfit=3;
extern int sell_StopLoss=60;
extern int sell_TakeProfit=3;

extern int buy_MA = 14;
extern int sell_MA = 14;

extern bool        enable_buy = true;
extern bool        enable_sell = true;

datetime lasttime = 0;

#include <ea.mqh>
#include <stdlib.mqh>


//+------------------------------------------------------------------+
//| expert initialization function                                   |
//+------------------------------------------------------------------+
int init()
  {
//----
   SetComment("RSI_Regression");   
//----
   return(0);
  }
//+------------------------------------------------------------------+
//| expert deinitialization function                                 |
//+------------------------------------------------------------------+
int deinit()
  {
//----
   
//----
   return(0);
  }
//+------------------------------------------------------------------+
//| expert start function                                            |
//+------------------------------------------------------------------+

int Signal(int oldSignal)
{
   return (SignalScalp(oldSignal));
}

int SignalScalp(int oldSignal)
{
   int signal = oldSignal;
   

   if (enable_buy){
      // Buy-Condition
      if (iRSI(Symbol(), 0, buy_MA, PRICE_CLOSE, buy_period)-iRSI(Symbol(), 0, buy_MA, PRICE_CLOSE, 0) >= buy_bewegung){
         if (iRSI(Symbol(), 0, buy_MA, PRICE_CLOSE, 1) - iRSI(Symbol(), 0, buy_MA, PRICE_CLOSE, 0) > buy_breakdown){
            if (iRSI(Symbol(), 0, buy_MA, PRICE_CLOSE, 0) < buy_RSIv){
               signal = OP_BUY;
            }
         }
      }
   }   
   
   if (enable_sell){  
      // Sell-Condition
      if (iRSI(Symbol(), 0, sell_MA, PRICE_CLOSE, 0)-iRSI(Symbol(), 0, sell_MA, PRICE_CLOSE, sell_period) >= sell_bewegung){
         if (iRSI(Symbol(), 0, sell_MA, PRICE_CLOSE, 0) - iRSI(Symbol(), 0, sell_MA, PRICE_CLOSE, 1) > sell_breakdown){
            if (iRSI(Symbol(), 0, sell_MA, PRICE_CLOSE, 0) > sell_RSIv){
               signal = OP_SELL;
            }
         }
      }      
   }
   
   return(signal);
}

int start()
  {
//----
  
   int    signal,oldSignal;
 
   oldSignal = 6;
   FindOrders(true, buy_StopLoss, sell_StopLoss);
   int numTrades = GetNumTickets();
  
   signal = Signal(oldSignal);     
  
   if (TimeCurrent()-lasttime > 360 && numTrades < 3){
      if (signal == OP_BUY){
          Buy(GetLots(), buy_StopLoss, buy_TakeProfit);
          lasttime = TimeCurrent();
      }
      if (signal == OP_SELL){
          Sell(GetLots(), sell_StopLoss, sell_TakeProfit);
          lasttime = TimeCurrent();
      }
   }  
  
   
//----
   return(0);
  }

