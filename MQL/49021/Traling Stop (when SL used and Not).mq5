//+------------------------------------------------------------------+
//|                                                    Code Base.mq5 |
//|                                                  by H A T Lakmal |
//|                                           https://t.me/Lakmal846 |
//+------------------------------------------------------------------+


#include <Trade\Trade.mqh> // <<------------------------------------------ Include this "Trade.mqh" to access the CTrade Class 

//+------------------------------------------------------------------+
//| Expert Inputs                                                    |
//+------------------------------------------------------------------+
input double Traling_Step = 3.0;


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


CTrade trade; // <<------------------------------------------ Declare the "CTrade" calss. you can replace "trade" win any name you want

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

   if(PositionsTotal() > 0) // calls to trailing stop function for every tick if there is / are positions runing.
     {
      Check_TralingStop();
     }


  }
//+------------------------------------------------------------------+
//| Timer function                                                   |
//+------------------------------------------------------------------+
void OnTimer()
  {

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
//+------------------------------------------------------------------+
//| Custom Function 00 - Check and Do trailing stop                  |
//+------------------------------------------------------------------+
void Check_TralingStop()
  {
   int totalPositions = PositionsTotal();
   for(int count =0; count < totalPositions; count++)
     {
      ulong TicketNo = PositionGetTicket(count); // Get Position Ticket number using the 'index' of the position.

      if(PositionSelectByTicket(TicketNo)) // Select a position using the ticket number (we already picked the tickt no.)
        {
         if(PositionGetInteger(POSITION_TYPE) == POSITION_TYPE_BUY) // Check the position Type.
           {
            double openPrice = PositionGetDouble(POSITION_PRICE_OPEN);
            double stopLoss  = PositionGetDouble(POSITION_SL);       // <<-------------------Get Position Current Stop Loss
            double takeProfit = PositionGetDouble(POSITION_TP);
            double bidPrice  = SymbolInfoDouble(_Symbol,SYMBOL_BID);
            ulong ticket = PositionGetTicket(count);
            double trailingLevel = NormalizeDouble(bidPrice - (Traling_Step * Point()),_Digits);

            if(stopLoss < openPrice) // No stop loss is true.
              {
               if(bidPrice > openPrice && trailingLevel > openPrice) // Runs only once per position. Sets the first SL.

                  trade.PositionModify(ticket,trailingLevel,takeProfit); // Modify trailing Stop using "CTrade.trade"
              }


            if(bidPrice > openPrice && trailingLevel > stopLoss) // check trailing level is above the previos level.
              {
               trade.PositionModify(ticket,trailingLevel,takeProfit); // Modify trailing Stop using "CTrade.trade"
              }

           }
         if(PositionGetInteger(POSITION_TYPE) == POSITION_TYPE_SELL)
           {
            double openPrice = PositionGetDouble(POSITION_PRICE_OPEN);
            double stopLoss  = PositionGetDouble(POSITION_SL);
            double takeProfit = PositionGetDouble(POSITION_TP);
            double bidPrice  = SymbolInfoDouble(_Symbol,SYMBOL_BID);
            double askPrice  = SymbolInfoDouble(_Symbol,SYMBOL_ASK);
            ulong ticket = PositionGetTicket(count);
            double trailingLevel = NormalizeDouble(askPrice + (Traling_Step * Point()),_Digits);

            if(stopLoss < openPrice) // No stop loss is true.
              {
               if(askPrice < openPrice && trailingLevel < openPrice) // Runs only once per position. Sets the first SL.

                  trade.PositionModify(ticket,trailingLevel,takeProfit); // Modify trailing Stop using "CTrade.trade"
              }

            if(askPrice < openPrice && trailingLevel < stopLoss) // check trailing level is above the previos level.
              {
               trade.PositionModify(ticket,trailingLevel,takeProfit); // Modify trailing Stop using "CTrade.trade"
              }
           }

        }
     }
  }
//+------------------------------------------------------------------+
