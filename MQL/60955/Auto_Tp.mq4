//+------------------------------------------------------------------+
//|                                                      Auto_Tp.mq4 |
//|                                                Mir Mostofa Kamal |
//|                              https://www.mql5.com/en/users/bokul |
//+------------------------------------------------------------------+
#property copyright "Mir Mostofa Kamal"
#property link      "https://www.mql5.com/en/users/bokul"
#property version   "1.00"
#property description "Auto Take profit and stop loss set EA ."
#property description "This EA will work on all time frames. Good for Scalping."
#property description "WARNING: Use this software at your own risk."
#property description "The creator of this script cannot be held responsible for any damage or loss."
#property strict
//+------------------------------------------------------------------+
//Inputs
//+------------------------------------------------------------------+
extern double TakeProfitPips     = 30;    // Take Profit :
extern bool   UseStopLoss    = false;  // Use Stop Loss (Enable/Diseble):
extern double StopLossPips       = 200;    // Stop Loss :
extern bool   UseTrailingStop    = false;  // Use Trailing Stop (Enable/Diseble):
extern double TrailingStopPips   = 15;    //Trailing Stop:

extern bool   UseEquityProtection = false; //Use Equity Protection
extern double MinEquityPercent   = 50.0;  // Close trades if Equity < 50% of Balance
extern int    Slippage            = 3;

int MagicNumber = 0; // Only affect manual trades
//+------------------------------------------------------------------+
int OnInit()
  {
   Print("Auto SL/TP Manager EA initialized.");
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+
void OnTick()
  {
   ManageOpenTrades();
   CheckEquityLimit();
  }
//+------------------------------------------------------------------+
void ManageOpenTrades()
  {
   for(int i=0; i<OrdersTotal(); i++)
     {
      if(OrderSelect(i, SELECT_BY_POS, MODE_TRADES))
        {
         if(OrderMagicNumber() == MagicNumber && OrderSymbol() == Symbol())
           {
            bool needModify = false;
            double newSL = 0, newTP = 0;
            double pip = MarketInfo(Symbol(), MODE_POINT) * 10;
            double price = OrderOpenPrice();

            if(OrderType() == OP_BUY)
              {
               if(UseStopLoss)
                  newSL = NormalizeDouble(price - StopLossPips * pip, Digits);
               newTP = NormalizeDouble(price + TakeProfitPips * pip, Digits);
              }
            else
               if(OrderType() == OP_SELL)
                 {
                  if(UseStopLoss)
                     newSL = NormalizeDouble(price + StopLossPips * pip, Digits);
                  newTP = NormalizeDouble(price - TakeProfitPips * pip, Digits);
                 }

            // Check if SL/TP need modification
            if((UseStopLoss && OrderStopLoss() == 0) || OrderTakeProfit() == 0)
              {
               if(!UseStopLoss)
                  newSL = 0;
               OrderModify(OrderTicket(), price, newSL, newTP, 0, clrRed);
              }

            if(UseTrailingStop && UseStopLoss)
               ApplyTrailingStop();
           }
        }
     }
  }
//+------------------------------------------------------------------+
void ApplyTrailingStop()
  {
   double pip = MarketInfo(Symbol(), MODE_POINT) * 10;

   for(int i=0; i<OrdersTotal(); i++)
     {
      if(OrderSelect(i, SELECT_BY_POS, MODE_TRADES))
        {
         if(OrderMagicNumber() == MagicNumber && OrderSymbol() == Symbol())
           {
            double ts = TrailingStopPips * pip;

            if(OrderType() == OP_BUY && Bid - OrderOpenPrice() > ts)
              {
               double newSL = NormalizeDouble(Bid - ts, Digits);
               if(OrderStopLoss() < newSL)
                  OrderModify(OrderTicket(), OrderOpenPrice(), newSL, OrderTakeProfit(), 0, clrYellow);
              }

            else
               if(OrderType() == OP_SELL && OrderOpenPrice() - Ask > ts)
                 {
                  double newSL = NormalizeDouble(Ask + ts, Digits);
                  if(OrderStopLoss() > newSL || OrderStopLoss() == 0)
                     OrderModify(OrderTicket(), OrderOpenPrice(), newSL, OrderTakeProfit(), 0, clrYellow);
                 }
           }
        }
     }
  }
//+------------------------------------------------------------------+
void CheckEquityLimit()
  {
   if(!UseEquityProtection)
      return;

   double balance = AccountBalance();
   double equity  = AccountEquity();
   double minEquity = balance * MinEquityPercent / 100.0;

   if(equity <= minEquity)
     {
      for(int i=OrdersTotal()-1; i>=0; i--)
        {
         if(OrderSelect(i, SELECT_BY_POS, MODE_TRADES))
           {
            if(OrderSymbol() == Symbol() && OrderMagicNumber() == MagicNumber)
              {
               bool closed = OrderClose(OrderTicket(), OrderLots(), MarketInfo(Symbol(), MODE_BID), Slippage, clrViolet);
               if(closed)
                  Print("Closed trade due to low equity. Equity: ", equity);
              }
           }
        }
     }
  }
//+------------------------------------------------------------------+
//+------------------------------------------------------------------+
