//+------------------------------------------------------------------+
//| EA RM Stochastic Band            |
//| Optimized for Better Trade Execution                            |
//| By Ronny Maheza                                                      |
//+------------------------------------------------------------------+

#property copyright "Copyright 2025, Ronny Maheza"
#property version   "1.02"
#property strict

// **Input Parameters**
input int Slippage = 3;
input double LotSize = 0.1;
input int MaxTradesPerTrend = 1;

input int StochPeriodK = 5;
input int StochPeriodD = 3;
input int BollingerPeriod = 20;
input double BollingerDeviation = 2.0;

input double SLMultiplier = 1.5;
input double TPMultiplier = 3.0;
input int BreakEvenPips = 10;
input double TrailingMultiplier = 1.5;

input int MaxSpreadStandard = 3;
input int MaxSpreadCent = 10;
input int MinStochOversold = 20;
input int MaxStochOverbought = 80;
input double MinMargin = 100; // Minimum free margin required

double atr, stochK[3], stochD[3], bollUpper[3], bollLower[3], spread;
int timeframes[3] = { PERIOD_M1, PERIOD_M5, PERIOD_M15 };

//+------------------------------------------------------------------+
//| Get Multi-Timeframe Indicators                                  |
//+------------------------------------------------------------------+
void GetIndicators() {
    for (int i = 0; i < 3; i++) {
        stochK[i] = iStochastic(NULL, timeframes[i], StochPeriodK, StochPeriodD, 3, MODE_SMA, STO_LOWHIGH, MODE_MAIN, 0);
        stochD[i] = iStochastic(NULL, timeframes[i], StochPeriodK, StochPeriodD, 3, MODE_SMA, STO_LOWHIGH, MODE_SIGNAL, 0);
        bollUpper[i] = iBands(NULL, timeframes[i], BollingerPeriod, BollingerDeviation, 0, PRICE_CLOSE, MODE_UPPER, 0);
        bollLower[i] = iBands(NULL, timeframes[i], BollingerPeriod, BollingerDeviation, 0, PRICE_CLOSE, MODE_LOWER, 0);
    }

    atr = iATR(NULL, PERIOD_M15, 14, 0);
    spread = MarketInfo(Symbol(), MODE_SPREAD);
}

//+------------------------------------------------------------------+
//| Get Max Spread Limit                                            |
//+------------------------------------------------------------------+
int GetMaxSpreadLimit() {
    return spread > MaxSpreadStandard ? MaxSpreadCent : MaxSpreadStandard;
}

//+------------------------------------------------------------------+
//| Validate Trade Execution Conditions                             |
//+------------------------------------------------------------------+
bool CanTrade() {
    if (AccountFreeMargin() < MinMargin) {
        Print("Insufficient free margin! Trade execution blocked.");
        return false;
    }
    if (spread > GetMaxSpreadLimit()) {
        Print("Spread too high, skipping trade.");
        return false;
    }
    return true;
}

//+------------------------------------------------------------------+
//| Count Open Trades                                               |
//+------------------------------------------------------------------+
int GetTradeCount() {
    int count = 0;
    for (int i = 0; i < OrdersTotal(); i++) {
        if (OrderSelect(i, SELECT_BY_POS) && OrderSymbol() == Symbol()) {
            count++;
        }
    }
    return count;
}

//+------------------------------------------------------------------+
//| Open Buy Order                                                  |
//+------------------------------------------------------------------+
void OpenBuy() {
    if (!CanTrade()) return;

    double price = NormalizeDouble(Ask, Digits);
    double SL = NormalizeDouble(price - (atr * SLMultiplier), Digits);
    double TP = NormalizeDouble(price + (atr * TPMultiplier), Digits);

    int ticket = OrderSend(Symbol(), OP_BUY, LotSize, price, Slippage, SL, TP, "Buy Order", 0, 0, clrGreen);
    if (ticket > 0) {
        Print("Buy Order placed successfully. Ticket: ", ticket);
    } else {
        Print("Buy Order failed. Error: ", GetLastError());
    }
}

//+------------------------------------------------------------------+
//| Open Sell Order                                                 |
//+------------------------------------------------------------------+
void OpenSell() {
    if (!CanTrade()) return;

    double price = NormalizeDouble(Bid, Digits);
    double SL = NormalizeDouble(price + (atr * SLMultiplier), Digits);
    double TP = NormalizeDouble(price - (atr * TPMultiplier), Digits);

    int ticket = OrderSend(Symbol(), OP_SELL, LotSize, price, Slippage, SL, TP, "Sell Order", 0, 0, clrRed);
    if (ticket > 0) {
        Print("Sell Order placed successfully. Ticket: ", ticket);
    } else {
        Print("Sell Order failed. Error: ", GetLastError());
    }
}

//+------------------------------------------------------------------+
//| OnTick Function                                                 |
//+------------------------------------------------------------------+
void OnTick() {
    GetIndicators();

    if (OrdersTotal() == 0) {
        if (stochK[0] < MinStochOversold && stochK[1] < MinStochOversold && stochK[2] < MinStochOversold) {
            OpenBuy();
        }
        else if (stochK[0] > MaxStochOverbought && stochK[1] > MaxStochOverbought && stochK[2] > MaxStochOverbought) {
            OpenSell();
        }
    }
}
