//+------------------------------------------------------------------+
//|                    Virtual_SL_TP_Pending_with_SL_Trailing.mq4    |
//|                        Copyright 2024, MetaQuotes Software Corp. |
//|                                       http://www.metaquotes.net/ |
//+------------------------------------------------------------------+

#property copyright     "Copyright 2024, MetaQuotes Ltd."
#property link          "https://www.mql5.com"
#property version       "1.01"
#property description   "persinaru@gmail.com"
#property description   "IP 2024 - free open source"
#property description   "Virtual_SL_TP_Pending_with_SL_Trailing"
#property description   ""
#property description   "WARNING: Use this software at your own risk."
#property description   "The creator of this script cannot be held responsible for any damage or loss."
#property description   ""
#property strict
#property show_inputs
#property script_show_inputs

// Input parameters
extern int StopLossPoints = 20;          // Initial Stop Loss in points
extern int TakeProfitPoints = 40;        // Initial Take Profit in points
extern double SpreadThreshold = 2.0;    // Spread threshold for virtual stop loss/take profit in points
extern int TrailingStopPoints = 10;      // Trailing stop in points for virtual pending order
extern bool EnableTrailing = false ;       // Enable or disable trailing stop

// Global variables
double initialSpread;
double virtualStopLoss, virtualTakeProfit;
double pendingOrderPrice;

//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
{
    // Store initial spread in pips
    initialSpread = MarketInfo(Symbol(), MODE_SPREAD) / Point;

    // Calculate initial virtual stop loss and take profit in pips
    virtualStopLoss = NormalizeDouble(Ask - StopLossPoints * Point, Digits) / Point;
    virtualTakeProfit = NormalizeDouble(Ask + TakeProfitPoints * Point, Digits) / Point;

    // Set pending order price
    pendingOrderPrice = NormalizeDouble(Ask + TrailingStopPoints * Point, Digits);

    return(INIT_SUCCEEDED);
}

//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
{
    // Check if spread has increased beyond threshold
    double currentSpread = MarketInfo(Symbol(), MODE_SPREAD) / Point;
    if (currentSpread > initialSpread + SpreadThreshold)
    {
        // Adjust virtual stop loss and take profit in pips
        virtualStopLoss = NormalizeDouble(virtualStopLoss + (currentSpread - initialSpread), Digits);
        virtualTakeProfit = NormalizeDouble(virtualTakeProfit + (currentSpread - initialSpread), Digits);

        // Adjust pending order price
        pendingOrderPrice = NormalizeDouble(pendingOrderPrice + (currentSpread - initialSpread), Digits);
    }

    // Check if the price hits the virtual stop loss or take profit
    if (Bid <= virtualStopLoss || Bid >= virtualTakeProfit)
    {
        // Close the position
        ClosePosition();
    }

    // Check if trailing stop is enabled and the price reaches the pending order price
    if (EnableTrailing && Ask >= pendingOrderPrice)
    {
        // Place the virtual pending order with trailing stop
        PlacePendingOrder();
    }
}

//+------------------------------------------------------------------+
//| Close the position                                               |
//+------------------------------------------------------------------+
void ClosePosition()
{
    if (OrderSelect(0, SELECT_BY_POS) && OrderClose(OrderTicket(), OrderLots(), MarketInfo(OrderSymbol(), MODE_BID), 3))
    {
        Print("Position closed at virtual stop loss or take profit");
    }
}

//+------------------------------------------------------------------+
//| Place the virtual pending order with trailing stop               |
//+------------------------------------------------------------------+
void PlacePendingOrder()
{
    double stopLoss = pendingOrderPrice - TrailingStopPoints * Point;
    double takeProfit = pendingOrderPrice + TakeProfitPoints * Point;

    if (OrderSend(Symbol(), OP_BUYSTOP, 0.1, pendingOrderPrice, 3, stopLoss, takeProfit, "Virtual Pending Order", 0, 0, clrNONE) > 0)
    {
        Print("Virtual pending order placed at ", pendingOrderPrice);
    }
    else
    {
        Print("Failed to place virtual pending order: ", GetLastError());
    }
}
