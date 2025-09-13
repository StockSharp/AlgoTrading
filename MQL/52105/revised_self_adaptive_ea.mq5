
#include <Trade\Trade.mqh>
#include <Trade\SymbolInfo.mqh>

// Define signal states
#define SIGNAL_BUY    1
#define SIGNAL_NOT    0
#define SIGNAL_SELL  -1

// Input parameters
input int  InpAverBodyPeriod   = 3;
input int  InpMAPeriod         = 2;
input int  InpPeriodRSI        = 6;
input ENUM_APPLIED_PRICE InpPrice = PRICE_CLOSE;
input double InpLot            = 0.05;
input double MaxRiskPercent    = 1.0;     // Maximum risk percentage per trade
input double MaxSpread         = 20.0;    // Maximum allowed spread in points
input bool UseTrailingStop     = true;
input long InpMagicNumber      = 121300;

// Arrow settings for indicator signals
#define BUY_ARROW   233       // Arrow code for a buy signal (can be customized)
#define SELL_ARROW  234       // Arrow code for a sell signal (can be customized)
#define ARROW_COLOR_BUY  clrLime
#define ARROW_COLOR_SELL clrRed
#define ARROW_SIZE  1

// Variables for buy/sell signals
int ExtSignalOpen = SIGNAL_NOT;

CTrade ExtTrade;
CSymbolInfo ExtSymbolInfo;

int    ExtIndicatorHandle = INVALID_HANDLE;
int    ExtTrendMAHandle   = INVALID_HANDLE;
int    ExtATRHandle       = INVALID_HANDLE;

double dynamicStopLoss = 0.0;
double dynamicTakeProfit = 0.0;
double dynamicTrailingStop = 0.0;
double marketVolatility = 0.0;

// Initialization function
int OnInit() {
    Print("Initializing EA...");

    ExtTrade.SetExpertMagicNumber(InpMagicNumber);

    // Initialize RSI
    ExtIndicatorHandle = iRSI(_Symbol, _Period, InpPeriodRSI, InpPrice);
    if (ExtIndicatorHandle == INVALID_HANDLE) {
        Print("Error initializing RSI");
        return(INIT_FAILED);
    }

    // Initialize Moving Average
    ExtTrendMAHandle = iMA(_Symbol, _Period, InpMAPeriod, 0, MODE_SMA, PRICE_CLOSE);
    if (ExtTrendMAHandle == INVALID_HANDLE) {
        Print("Error initializing MA");
        return(INIT_FAILED);
    }

    // Initialize ATR for volatility-based SL/TP
    ExtATRHandle = iATR(_Symbol, _Period, 14);
    if (ExtATRHandle == INVALID_HANDLE) {
        Print("Error initializing ATR");
        return(INIT_FAILED);
    }

    ObjectDelete(0, "MarketInfoLabelEA");

    return(INIT_SUCCEEDED);
}

// Deinitialize function
void OnDeinit(const int reason) {
    IndicatorRelease(ExtIndicatorHandle);
    IndicatorRelease(ExtTrendMAHandle);
    IndicatorRelease(ExtATRHandle);
    ObjectDelete(0, "MarketInfoLabelEA");
}

// Function to detect trade signals and draw arrows for indicators
void CheckTradeSignals() {
    ExtSignalOpen = SIGNAL_NOT;  // Reset the signal
    double rsiValue = iRSI(_Symbol, _Period, InpPeriodRSI, InpPrice);  // Get current RSI value

    // Check for Bullish Engulfing pattern (buy signal)
    if (iClose(_Symbol, _Period, 1) > iOpen(_Symbol, _Period, 1) &&     // Current candle bullish
        iOpen(_Symbol, _Period, 1) < iClose(_Symbol, _Period, 2) &&     // Previous candle bearish
        rsiValue < 30) {                                                // RSI below 30 (oversold)
        ExtSignalOpen = SIGNAL_BUY;
        DrawSignalArrow(SIGNAL_BUY, iTime(_Symbol, _Period, 1), iLow(_Symbol, _Period, 1)); // Draw buy arrow
        Print("Bullish Engulfing pattern detected - Buy signal.");
    }

    // Check for Bearish Engulfing pattern (sell signal)
    if (iClose(_Symbol, _Period, 1) < iOpen(_Symbol, _Period, 1) &&     // Current candle bearish
        iOpen(_Symbol, _Period, 1) > iClose(_Symbol, _Period, 2) &&     // Previous candle bullish
        rsiValue > 70) {                                                // RSI above 70 (overbought)
        ExtSignalOpen = SIGNAL_SELL;
        DrawSignalArrow(SIGNAL_SELL, iTime(_Symbol, _Period, 1), iHigh(_Symbol, _Period, 1)); // Draw sell arrow
        Print("Bearish Engulfing pattern detected - Sell signal.");
    }
}

// Draws a signal arrow on the chart
void DrawSignalArrow(int signalType, datetime time, double price) {
    string arrowName = (signalType == SIGNAL_BUY ? "BuyArrow" : "SellArrow") + IntegerToString(TimeCurrent());
    int arrowCode = (signalType == SIGNAL_BUY) ? BUY_ARROW : SELL_ARROW;
    color arrowColor = (signalType == SIGNAL_BUY) ? ARROW_COLOR_BUY : ARROW_COLOR_SELL;

    if (!ObjectCreate(0, arrowName, OBJ_ARROW, 0, time, price)) {
        Print("Error creating arrow object: ", arrowName);
        return;
    }

    ObjectSetInteger(0, arrowName, OBJPROP_COLOR, arrowColor);
    ObjectSetInteger(0, arrowName, OBJPROP_WIDTH, ARROW_SIZE);
    ObjectSetInteger(0, arrowName, OBJPROP_ARROWCODE, arrowCode);
}

// Main OnTick function
void OnTick() {
    // Check for trade signals
    CheckTradeSignals();

    // Other EA logic remains unchanged...
}
