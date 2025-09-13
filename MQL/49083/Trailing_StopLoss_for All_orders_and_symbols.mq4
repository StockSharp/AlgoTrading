//+------------------------------------------------------------------+
//|                 Trailing_StopLoss_for All_orders_and_symbols.mq4 |
//|                                  Copyright 2023, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
//+------------------------------------------------------------------+
//|This script will trail the stop loss of all open buy and sell orders. 
//|Adjust the TrailStart and TrailStop parameters to your preferred levels. 
//|This is a basic example and can be further optimized and customized 
//|based on your specific requirements.
//+------------------------------------------------------------------+
#property copyright     "Copyright 2024, MetaQuotes Ltd."
#property link          "https://www.mql5.com"
#property version       "1.01"
#property description   "persinaru@gmail.com"
#property description   "IP 2024 - free open source"
#property description   "Trailing_StopLoss_for All_orders_and_symbols"
#property description   ""
#property description   "WARNING: Use this software at your own risk."
#property description   "The creator of this script cannot be held responsible for any damage or loss."
#property description   ""
#property strict
#property show_inputs
#property script_show_inputs

// Input parameters
extern int TrailStart = 20;            // Trailing start level in pips
extern int TrailStop = 10;             // Trailing stop level in pips

//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
void OnInit()
  {
  }

//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
  {
    TrailingStop();
  }

//+------------------------------------------------------------------+
//| Trailing stop function                                          |
//+------------------------------------------------------------------+
void TrailingStop()
  {
    for(int i = OrdersTotal() - 1; i >= 0; i--)
      {
        if(OrderSelect(i, SELECT_BY_POS, MODE_TRADES))
          {
            if(OrderType() <= OP_SELL)
              {
                double currentProfit = OrderProfit();
                double currentStopLoss = OrderStopLoss();
                double currentPrice = OrderOpenPrice();

                if(currentProfit > 0) // Only trailing if the trade is in profit
                  {
                    double trailPrice = 0;

                    // Calculate the trailing stop price
                    if(OrderType() == OP_BUY)
                      {
                        trailPrice = currentPrice + TrailStart * Point;
                        trailPrice = MathMax(trailPrice, currentStopLoss + TrailStop * Point);
                      }
                    else if(OrderType() == OP_SELL)
                      {
                        trailPrice = currentPrice - TrailStart * Point;
                        trailPrice = MathMin(trailPrice, currentStopLoss - TrailStop * Point);
                      }

                    // Update stop loss if the new price is better
                    if(trailPrice != currentStopLoss)
                      {
                        bool res = OrderModify(OrderTicket(), OrderOpenPrice(), trailPrice, OrderTakeProfit(), 0, clrNONE);
                        if(res)
                          {
                            Print("Trailing stop for order ", OrderTicket(), " updated successfully.");
                          }
                        else
                          {
                            Print("Failed to update trailing stop for order ", OrderTicket());
                          }
                      }
                  }
              }
          }
      }
  }
