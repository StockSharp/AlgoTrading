//+------------------------------------------------------------------+
//|                                                  CloseOrders.mq4 |
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
extern    double         inTargetProfitMoney     = 10;       //Target Profit ($)
extern    double         inCutLossMoney          = 0.0;      //Cut Loss ($)
extern    int            inMagicNumber           = 0;        //Magic Number

int      slippage    = 3;

//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
//---
   if(inTargetProfitMoney <= 0)
     {
      Alert("Invalid input");
      return(INIT_PARAMETERS_INCORRECT);
     }

   inCutLossMoney = MathAbs(inCutLossMoney) * -1;

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

   double   tFloating = 0.0;
   int tOrder  = OrdersTotal();
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

   if(tFloating >= inTargetProfitMoney || (tFloating <= inCutLossMoney && inCutLossMoney < 0))
     {
      fCloseAllOrders();
     }

  }
//+------------------------------------------------------------------+


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
