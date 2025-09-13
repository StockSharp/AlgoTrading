//+------------------------------------------------------------------+
//|                                                  maybeawo222.mq4 |
//|                                  Copyright 2023, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright "Copyright 2023, MetaQuotes Ltd."
#property link      "https://www.mql5.com"
#property version   "1.00"
#property strict

// Define input parameters
input int MovingPeriod = 14;
input int MovingShift = 0;
input int MagicNumber = 12345;
input double LotSize = 0.5;                      // Lot size for trading
input double StopLossPips = 100;                 // Stop loss in pips
input double TakeProfitPips = 800;               // Take profit in pips
input double BreakevenPips1 = 180.0;             // Breakeven level 1 in pips
input double BreakevenPips2 = 500.0;             // Breakeven level 2 in pips
input double DesiredBreakevenDistancePips1 = 60.0; // Desired breakeven distance for Breakeven 1
input double DesiredBreakevenDistancePips2 = 350.0; // Desired breakeven distance for Breakeven 2

input int StartHour = 3;                         // Trading start hour (0-23)
input int EndHour = 22;                          // Trading end hour (0-23)

// Define global variables
double lotSize = LotSize;

//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
{
    double minLotSize = SymbolInfoDouble(Symbol(), SYMBOL_VOLUME_MIN);
    double maxLotSize = SymbolInfoDouble(Symbol(), SYMBOL_VOLUME_MAX);

    if (lotSize < minLotSize)
    {
        Print("Lot size is below the minimum allowed. Adjusting to minimum lot size: ", minLotSize);
        lotSize = minLotSize;
    }
    else if (lotSize > maxLotSize)
    {
        Print("Lot size is above the maximum allowed. Adjusting to maximum lot size: ", maxLotSize);
        lotSize = maxLotSize;
    }

    return(INIT_SUCCEEDED);
}

//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
{
    // Place any cleanup code here if needed
}

//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
{
    // Check if the current time is within the specified trading hours
    int currentHour = Hour();
    if (currentHour >= StartHour && currentHour < EndHour)
    {
        // Check for open order conditions
        CheckForOpen();
        // Check for close order conditions
        CheckForClose();
    }
}

//+------------------------------------------------------------------+
//| Check for open order conditions                                  |
//+------------------------------------------------------------------+
void CheckForOpen()
{
    double ma;
    int res;

    //--- go trading only for the first ticks of a new bar
    if (Volume[0] > 1)
        return;

    //--- get Moving Average
    ma = iMA(Symbol(), 0, MovingPeriod, MovingShift, MODE_SMA, PRICE_CLOSE, 0);

    //--- sell conditions
    if (Open[1] > ma && Close[1] < ma)
    {
        // Add stop loss and take profit levels
        double stopLossPrice = NormalizeDouble(Bid + StopLossPips * MarketInfo(Symbol(), MODE_POINT), _Digits);
        double takeProfitPrice = NormalizeDouble(Bid - TakeProfitPips * MarketInfo(Symbol(), MODE_POINT), _Digits);

        res = OrderSend(Symbol(), OP_SELL, lotSize, Bid, 0, stopLossPrice, takeProfitPrice, "", MagicNumber, 0, clrRed);
        if (res < 0)
            Print("OrderSend error ", GetLastError());
        return;
    }

    //--- buy conditions
    if (Open[1] < ma && Close[1] > ma)
    {
        // Add stop loss and take profit levels
        double stopLossPrice = NormalizeDouble(Ask - StopLossPips * MarketInfo(Symbol(), MODE_POINT), _Digits);
        double takeProfitPrice = NormalizeDouble(Ask + TakeProfitPips * MarketInfo(Symbol(), MODE_POINT), _Digits);

        res = OrderSend(Symbol(), OP_BUY, lotSize, Ask, 0, stopLossPrice, takeProfitPrice, "", MagicNumber, 0, clrBlue);
        if (res < 0)
            Print("OrderSend error ", GetLastError());
        return;
    }
    
    // Check if breakeven condition is met for Breakeven 1
    double openPrice = OrderOpenPrice();
    double breakevenPrice1 = OrderType() == OP_BUY ? openPrice + DesiredBreakevenDistancePips1 * MarketInfo(Symbol(), MODE_POINT) :
                                                     openPrice - DesiredBreakevenDistancePips1 * MarketInfo(Symbol(), MODE_POINT);
    
    if (OrderType() == OP_BUY && Bid >= breakevenPrice1)
    {
        // Move stop loss to breakeven for the buy order
        double newStopLoss1 = breakevenPrice1 - StopLossPips * MarketInfo(Symbol(), MODE_POINT);
        if (OrderModify(OrderTicket(), OrderOpenPrice(), breakevenPrice1, newStopLoss1, 0, clrWhite))
        {
            Print("Moved Buy Order #", OrderTicket(), " to Breakeven 1 at Price: ", breakevenPrice1);
        }
    }
    else if (OrderType() == OP_SELL && Ask <= breakevenPrice1)
    {
        // Move stop loss to breakeven for the sell order
        double newStopLoss1 = breakevenPrice1 + StopLossPips * MarketInfo(Symbol(), MODE_POINT);
        if (OrderModify(OrderTicket(), OrderOpenPrice(), breakevenPrice1, newStopLoss1, 0, clrWhite))
        {
            Print("Moved Sell Order #", OrderTicket(), " to Breakeven 1 at Price: ", breakevenPrice1);
        }
    }

    // Check if breakeven condition is met for Breakeven 2
    double breakevenPrice2 = OrderType() == OP_BUY ? openPrice + DesiredBreakevenDistancePips2 * MarketInfo(Symbol(), MODE_POINT) :
                                                     openPrice - DesiredBreakevenDistancePips2 * MarketInfo(Symbol(), MODE_POINT);
    
    if (OrderType() == OP_BUY && Bid >= breakevenPrice2)
    {
        // Move stop loss to breakeven for the buy order
        double newStopLoss2 = breakevenPrice2 - StopLossPips * MarketInfo(Symbol(), MODE_POINT);
        if (OrderModify(OrderTicket(), OrderOpenPrice(), breakevenPrice2, newStopLoss2, 0, clrWhite))
        {
            Print("Moved Buy Order #", OrderTicket(), " to Breakeven 2 at Price: ", breakevenPrice2);
        }
    }
    else if (OrderType() == OP_SELL && Ask <= breakevenPrice2)
    {
        // Move stop loss to breakeven for the sell order
        double newStopLoss2 = breakevenPrice2 + StopLossPips * MarketInfo(Symbol(), MODE_POINT);
        if (OrderModify(OrderTicket(), OrderOpenPrice(), breakevenPrice2, newStopLoss2, 0, clrWhite))
        {
            Print("Moved Sell Order #", OrderTicket(), " to Breakeven 2 at Price: ", breakevenPrice2);
        }
    }
}

//+------------------------------------------------------------------+
//| Check for close order conditions                                 |
//+------------------------------------------------------------------+
void CheckForClose()
{
    // Define variables
    double ma = iMA(Symbol(), 0, MovingPeriod, MovingShift, MODE_SMA, PRICE_CLOSE, 0);

    // Loop through open orders
    for (int i = 0; i < OrdersTotal(); i++)
    {
        if (OrderSelect(i, SELECT_BY_POS, MODE_TRADES) == false)
            break;

        if (OrderMagicNumber() != MagicNumber || OrderSymbol() != Symbol())
            continue;

        // Check if stop loss or take profit is hit
        if (OrderStopLoss() != 0.0 && ((OrderType() == OP_BUY && Bid <= OrderStopLoss()) || (OrderType() == OP_SELL && Ask >= OrderStopLoss())))
        {
            // Close the order when stop loss is hit
            if (OrderClose(OrderTicket(), OrderLots(), OrderStopLoss(), 0, clrWhite))
            {
                Print("Closed Order #", OrderTicket(), " at Stop Loss Price: ", OrderStopLoss());
            }
        }
        else if (OrderTakeProfit() != 0.0 && ((OrderType() == OP_BUY && Bid >= OrderTakeProfit()) || (OrderType() == OP_SELL && Ask <= OrderTakeProfit())))
        {
            // Close the order when take profit is hit
            if (OrderClose(OrderTicket(), OrderLots(), OrderTakeProfit(), 0, clrWhite))
            {
                Print("Closed Order #", OrderTicket(), " at Take Profit Price: ", OrderTakeProfit());
            }
        }
    }
}

//+------------------------------------------------------------------+
