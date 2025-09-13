//+-----------------------------------------------------------------------------+
//|Simple continuous trend-following strategy  BUY/SELL for chart symbol.mq4    |
//|                                   Copyright 2024, MetaQuotes Software Corp. |
//|                                                         http://www.mql4.com |
//+-----------------------------------------------------------------------------+
#property copyright     "Copyright 2024, MetaQuotes Ltd."
#property link          "https://www.mql5.com"
#property version       "1.01"
#property description   "persinaru@gmail.com"
#property description   "IP 2024 - free open source"
#property description   "Simple continuous trend-following strategy"
#property description   ""
#property description   "WARNING: Use this software at your own risk."
#property description   "The creator of this script cannot be held responsible for any damage or loss."
#property description   ""
#property strict
#property show_inputs
#property script_show_inputs

// Input parameters
extern double lotSize = 0.1;
extern double stopLossPips = 100; // Stop loss in pips
extern double takeProfitPips = 200; // Take profit in pips
extern bool OpenBuyPosition = false;
extern bool OpenSellPosition = false;

// Global variables to track open positions
bool hasOpenBuyPosition = false;
bool hasOpenSellPosition = false;

//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
{
    // Initializing
    Print("EA initialized successfully.");
    return(INIT_SUCCEEDED);
}

//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
{
    // Deinitializing
    Print("EA deinitialized.");
}

//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
{
    // Reset position tracking variables
    hasOpenBuyPosition = false;
    hasOpenSellPosition = false;

    // Loop through all open orders
    for (int i = 0; i < OrdersTotal(); i++)
    {
        if (OrderSelect(i, SELECT_BY_POS))
        {
            if (OrderSymbol() == Symbol())
            {
                if (OrderType() == OP_BUY)
                {
                    hasOpenBuyPosition = true;
                }
                else if (OrderType() == OP_SELL)
                {
                    hasOpenSellPosition = true;
                }
            }
        }
    }

    // Check if there's no open buy position
    if (!hasOpenBuyPosition && OpenBuyPosition)
    {
        // Open a buy position
        double price = SymbolInfoDouble(_Symbol, SYMBOL_BID);
        int ticket = OrderSend(Symbol(), OP_BUY, lotSize, price, 2, 0, 0, "Buy order", 0, 0, clrNONE);
        if (ticket > 0)
        {
            // Order opened successfully, set stop loss and take profit
            if (!OrderModify(ticket, price, price - stopLossPips * Point, price + takeProfitPips * Point, 0, clrNONE))
            {
                Print("Error modifying buy order ", GetLastError());
            }
            else
            {
                Print("Buy order opened successfully with ticket ", ticket);
            }
        }
        else
        {
            Print("Error opening buy order ", GetLastError());
        }
    }

    // Check if there's no open sell position
    if (!hasOpenSellPosition && OpenSellPosition)
    {
        // Open a sell position
        double price = SymbolInfoDouble(_Symbol, SYMBOL_ASK);
        int ticket = OrderSend(Symbol(), OP_SELL, lotSize, price, 2, 0, 0, "Sell order", 0, 0, clrNONE);
        if (ticket > 0)
        {
            // Order opened successfully, set stop loss and take profit
            if (!OrderModify(ticket, price, price + stopLossPips * Point, price - takeProfitPips * Point, 0, clrNONE))
            {
                Print("Error modifying sell order ", GetLastError());
            }
            else
            {
                Print("Sell order opened successfully with ticket ", ticket);
            }
        }
        else
        {
            Print("Error opening sell order ", GetLastError());
        }
    }

    // Check for closed orders
    for (int j = OrdersTotal() - 1; j >= 0; j--)
    {
        if (OrderSelect(j, SELECT_BY_POS))
        {
            // Check if the order is for the current symbol
            if (OrderSymbol() == Symbol())
            {
                // Check if the order is closed and its type
                if (OrderCloseTime() > 0)
                {
                    if (OrderType() == OP_BUY)
                    {
                        // If buy order closed with profit, reset martingale level for buy orders
                        if (OrderProfit() > 0)
                        {
                            hasOpenBuyPosition = false; // Resetting the buy position flag
                        }
                    }
                    else if (OrderType() == OP_SELL)
                    {
                        // If sell order closed with profit, reset martingale level for sell orders
                        if (OrderProfit() > 0)
                        {
                            hasOpenSellPosition = false; // Resetting the sell position flag
                        }
                    }
                }
            }
        }
    }
}
