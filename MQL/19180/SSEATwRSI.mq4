//+------------------------------------------------------------------+
//|                         Super Simple Expert Advisor Template.mq4 |
//|                                                       John Davis |
//|                                          http://www.tidyneat.com |
//+------------------------------------------------------------------+

#property copyright     "Johnthe"
#property link          "https://www.mql5.com/en/users/johnthe"
#property description   "Just gives an idea! "
#property description   "Opens buys on bulish engulfing"
#property description   "Opens sells on bearish engulfing"
#property strict
extern double    Lots=0.1;
extern double ProfitGoal=190.00; // Profit goal in deposit currency
extern double MaxLoss=-10;
sinput string RSI_Settings;
input int period=7; // Averagin period for calculation
input ENUM_APPLIED_PRICE appPrice=PRICE_HIGH; // Applied price
input int OverBought=88; // Over bought level
input int OverSold=37; // Over sold level

datetime NewTime=0;
//+------------------------------------------------------------------+
//| Begin main function                                              |
//+------------------------------------------------------------------+
int start()
  {
   if(NewTime!=Time[0]) // Run only on new bars
     {
      NewTime=Time[0];
      double rsi=iRSI(_Symbol,PERIOD_CURRENT,period,appPrice,1);
      bool orderOpended=false;
      if(rsi>OverBought && 
         iOpen(NULL,0,2)>iClose(NULL,0,2) && // Place Buy logic in if statemet
         iOpen(NULL,0,1)<iClose(NULL,0,1) &&        // Buy logic
         iOpen(NULL,0,2)<iClose(NULL,0,1))          // Buy logic
         orderOpended=OrderSend(Symbol(),OP_BUY,Lots,Ask,3,0,0,NULL,0,0,Blue);

      else if(rsi<OverSold && 
         iOpen(NULL,0,2)<iClose(NULL,0,2) && // Place Sell logic in if satement
         iOpen(NULL,0,1)>iClose(NULL,0,1) &&       // Sell logic
         iOpen(NULL,0,2)>iClose(NULL,0,1))         // Sell logic
         orderOpended=OrderSend(Symbol(),OP_SELL,Lots,Bid,3,0,0,NULL,0,0,Red);
     }
// Checks if you reached your profit goal or loss limit
   
   if(AccountProfit()>=ProfitGoal || AccountProfit()<=MaxLoss )
     {
      // Closes all orders oldest first inline with FIFO
      for(int i=0; i<OrdersTotal(); i++)
        {
         if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES))
            if(OrderClose(OrderTicket(),OrderLots(),OrderClosePrice(),10,clrNONE))
               i--; // Decrement by one since closed orders are removed from que
        }
     }
   return(0);
  }
//+------------------------------------------------------------------+
