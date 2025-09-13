//+------------------------------------------------------------------+
//|                                                 Daily Target.mq4 |
//|                                     Copyright 2021, Signal Forex |
//|                                           https://signalforex.id |
//+------------------------------------------------------------------+
#property copyright "Copyright 2021, Signal Forex"
#property link      "https://signalforex.id"
#property version   "1.00"
#property strict
#property description   "Group sharing t.me/codeMQL"

//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
input    double         inDailyTarget          = 10;       //Daily Target ($)
//if inDailyMaxLoss = 0.0 then cut loss function not activated
input    double         inDailyMaxLoss         = 0.0;      //Daily Max Losses ($)
input    int            inMagicNumber          = 0;        //Magic Number

//Global Variable
int      slippage       = 3;
double   PDailyMaxLoss  = 0.0;
bool     PFlagDailyStop = false;
int      PDOY           = 0;

//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
//---
   if(inDailyTarget <= 0)
     {
      Alert("Invalid input");
      return(INIT_PARAMETERS_INCORRECT);
     }

   PDailyMaxLoss = MathAbs(inDailyMaxLoss) * -1;

//---
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
//---
   Print("Thank you for using this EA");
  }
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
  {
//---
   if (!PFlagDailyStop){

      double   tFloating = 0.0, tHistory = 0.0, tDaily = 0.0;
      tHistory    = getTHistory();
      tFloating   = getTFloating();
      tDaily      = tHistory+tFloating;
   
      //Daily Target Reached
      if(tDaily >= inDailyTarget)
        {
         fCloseAllOrders();
         Alert ("Daily Target Reached, Profit : " + DoubleToStr(tDaily, 2));
         PFlagDailyStop = true;
        }
      
      //Limit Losses
      if (tDaily <= PDailyMaxLoss && PDailyMaxLoss < 0){
         fCloseAllOrders();
         Alert ("Daily Max Losses, Cutloss : " + DoubleToStr(tDaily, 2));
         PFlagDailyStop = true;
      }
   
   }
   
   if (PDOY != DayOfYear()) {
      PDOY=DayOfYear();
      PFlagDailyStop = false;
   }
  }
//+------------------------------------------------------------------+


double getTFloating(){
   double   tFloating = 0.0;
   int      tOrder  = OrdersTotal();
   for(int i=tOrder-1; i>=0; i--)
     {
      if(OrderSelect(i, SELECT_BY_POS, MODE_TRADES))
        {
         if(OrderMagicNumber() == inMagicNumber)
           {
            tFloating   += OrderProfit()+OrderCommission() + OrderSwap();
           }
        }
     }
   return(tFloating);
}

double getTHistory(){
   double   tHistory    = 0.0;
   int      tOrderHis   = OrdersHistoryTotal();
   string   strToday    = TimeToString(TimeCurrent(), TIME_DATE);
   for(int i=tOrderHis-1; i>=0; i--)
     {
      if(OrderSelect(i, SELECT_BY_POS, MODE_HISTORY))
        {
         if(OrderMagicNumber() == inMagicNumber && StringFind(TimeToString(OrderCloseTime(), TIME_DATE), strToday, 0) == 0)
           {
            tHistory   += OrderProfit()+OrderCommission() + OrderSwap();
           }
        }
     }
   return(tHistory);
}

//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void fCloseAllOrders()
  {
   double   priceClose = 0.0;
   int tOrders = OrdersTotal();
   for(int i=tOrders-1; i>=0; i--)
     {
      if(OrderSelect(i, SELECT_BY_POS, MODE_TRADES))
        {
         if(OrderMagicNumber() == inMagicNumber && (OrderType() == OP_BUY || OrderType() == OP_SELL))
           {
            priceClose  = (OrderType()==OP_BUY)?MarketInfo(OrderSymbol(), MODE_BID):MarketInfo(OrderSymbol(), MODE_ASK);
            if(!OrderClose(OrderTicket(), OrderLots(), priceClose, slippage, clrGold))
              {
               Print("WARNING: Close Failed");
              }
           }
        }
     }
  }
//+------------------------------------------------------------------+
