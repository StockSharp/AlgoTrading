
#include <Trade\Trade.mqh> 

// Define signal states
#define SIGNAL_BUY    1
#define SIGNAL_NOT    0
#define SIGNAL_SELL   -1

// Input parameters
input double MaxRiskPercent = 0.2;             // Risk percentage for each trade
input int OptimizationInterval = 3600;         // Interval (in seconds) to optimize parameters (1 hour)
input long InpMagicNumber = 121300;            // Magic number for EA trades
input int MaxSimultaneousTrades = 1;           // Maximum number of trades allowed at the same time
input int TrendPeriod = 20;                    // Trend period for 5-minute charts
input double BreakEvenMultiplier = 1.5;        // Move stop loss to break even after this ATR multiplier is reached
input double PartialTakeProfitPercent = 0.5;   // Percent of position to close at the first take-profit level
input int PerformanceCheckInterval = 14400;    // Interval to check and adjust parameters (4 hours)
input double MaxSpreadPoints = 20;             // Maximum spread allowed for opening trades
input int NewsAvoidanceMinutes = 30;           // Time in minutes to avoid trading around high-impact news

// Multi-timeframe settings
input ENUM_TIMEFRAMES HigherTimeframe = PERIOD_H1; // Higher timeframe for trend confirmation (1 hour)
input int HigherTimeframeTrendPeriod = 50;        // Trend period on the higher timeframe

// RSI and ATR Optimization input parameters
input int RSI_Min_Period = 8;
input int RSI_Max_Period = 14;
input double ATR_Min_Multiplier = 1.0;
input double ATR_Max_Multiplier = 2.0;
input double TrailingStop_Min_Multiplier = 1.0;
input double TrailingStop_Max_Multiplier = 2.0;
input double TrailingTP_Min_Multiplier = 1.0;
input double TrailingTP_Max_Multiplier = 2.0;

// Structure to hold parameter sets
struct ParameterSet {
    int periodRSI;
    double atrMultiplier;
    double trailingStopMultiplier;
    double trailingTakeProfitMultiplier;
    double performanceScore;
};

// Structure to track trade states per symbol
struct SymbolTradeState {
    string symbol;
    bool tradeActive;
    bool breakEvenApplied;
    datetime lastTradeTime;
    int rsiHandle;
    int atrHandle;
    int trendHandle;
    int higherTimeframeTrendHandle;
    int winCount;
    int lossCount;
};

// Global variables
ParameterSet currentBestSet;
CTrade ExtTrade;
double currentRiskPercent; // Adjustable risk percentage
datetime lastOptimization = 0;
datetime lastPerformanceCheck = 0;
SymbolTradeState symbolTradeStates[100];  // Adjust the size if tracking more symbols
int symbolCount = 0;

// Function declarations
void OptimizeParameters();
void AdjustParametersBasedOnPerformance(int symbolIndex);
void CheckTradeSignals();
void ApplyTrailingStop();
double iATRValue(int handle);
double iRSIValue(int handle);
double iTrendValue(int handle);
double BacktestWithParameters(int rsiPeriod, double atrMultiplier, double trailingStopMultiplier, double trailingTPMultiplier);
bool IsSpreadAcceptable(string symbol);
bool IsHighImpactNewsUpcoming(int minutesAhead);

// Function to log information
void LogInfo(string message) {
    Print(TimeToString(TimeCurrent(), TIME_DATE | TIME_MINUTES | TIME_SECONDS) + ": " + message);
}

// Function to find a symbol in the trade state array
int FindSymbolStateIndex(string symbol) {
    for (int i = 0; i < symbolCount; i++) {
        if (symbolTradeStates[i].symbol == symbol) {
            return i;
        }
    }
    return -1;  // Symbol not found
}

// Function to add a new symbol to the trade state array
void AddSymbolState(string symbol) {
    if (symbolCount < 100) {
        if (SymbolInfoDouble(symbol, SYMBOL_TRADE_TICK_VALUE) > 0) {  // Validate if the symbol is available
            symbolTradeStates[symbolCount].symbol = symbol;
            symbolTradeStates[symbolCount].tradeActive = false;
            symbolTradeStates[symbolCount].breakEvenApplied = false;
            symbolTradeStates[symbolCount].lastTradeTime = 0;
            symbolTradeStates[symbolCount].winCount = 0;
            symbolTradeStates[symbolCount].lossCount = 0;
            symbolTradeStates[symbolCount].rsiHandle = iRSI(symbol, PERIOD_M5, 14, PRICE_CLOSE);
            symbolTradeStates[symbolCount].atrHandle = iATR(symbol, PERIOD_M5, 14);
            symbolTradeStates[symbolCount].trendHandle = iMA(symbol, PERIOD_M5, TrendPeriod, 0, MODE_SMA, PRICE_CLOSE);
            symbolTradeStates[symbolCount].higherTimeframeTrendHandle = iMA(symbol, HigherTimeframe, HigherTimeframeTrendPeriod, 0, MODE_SMA, PRICE_CLOSE);
            
            if (symbolTradeStates[symbolCount].rsiHandle == INVALID_HANDLE || 
                symbolTradeStates[symbolCount].atrHandle == INVALID_HANDLE || 
                symbolTradeStates[symbolCount].trendHandle == INVALID_HANDLE ||
                symbolTradeStates[symbolCount].higherTimeframeTrendHandle == INVALID_HANDLE) {
                LogInfo("Error: Failed to initialize indicators for symbol: " + symbol);
            }
            symbolCount++;
        } else {
            LogInfo("Error: Invalid symbol - " + symbol);
        }
    } else {
        LogInfo("Error: Maximum symbol limit reached. Cannot add symbol: " + symbol);
    }
}

// Function to calculate lot size based on optimized parameters
double CalculateLotSize(int symbolIndex) {
    double accountBalance = AccountInfoDouble(ACCOUNT_BALANCE);
    double atrValue = iATRValue(symbolTradeStates[symbolIndex].atrHandle);
    if (atrValue <= 0) {
        LogInfo("Error: Invalid ATR value. Unable to calculate lot size.");
        return 0.0;
    }

    double riskAmount = accountBalance * currentRiskPercent / 100.0;
    double stopLossPoints = atrValue * currentBestSet.atrMultiplier;
    double lotSize = riskAmount / (stopLossPoints * _Point);

    // Declare variables to hold symbol information
    double minVolume, maxVolume, volumeStep;

    // Retrieve symbol information using the reference form of SymbolInfoDouble
    if (!SymbolInfoDouble(symbolTradeStates[symbolIndex].symbol, SYMBOL_VOLUME_MIN, minVolume) ||
        !SymbolInfoDouble(symbolTradeStates[symbolIndex].symbol, SYMBOL_VOLUME_MAX, maxVolume) ||
        !SymbolInfoDouble(symbolTradeStates[symbolIndex].symbol, SYMBOL_VOLUME_STEP, volumeStep)) {
        LogInfo("Error: Failed to retrieve volume information for symbol: " + symbolTradeStates[symbolIndex].symbol);
        return 0.0;
    }

    // Calculate the lot size within the symbol's volume constraints
    lotSize = MathFloor(lotSize / volumeStep) * volumeStep;
    if (lotSize < minVolume) lotSize = minVolume;
    else if (lotSize > maxVolume) lotSize = maxVolume;

    return NormalizeDouble(lotSize, 2);
}

// Function to retrieve the latest trend value (moving average)
double iTrendValue(int handle) {
    double trendValues[];
    int copied = CopyBuffer(handle, 0, 0, 1, trendValues);
    if (copied > 0 && ArraySize(trendValues) > 0) {
        return trendValues[0];
    }
    LogInfo("Error: Failed to retrieve trend value.");
    return 0.0;
}

// Function to retrieve the latest ATR value
double iATRValue(int handle) {
    double atrValues[];
    int copied = CopyBuffer(handle, 0, 0, 1, atrValues);
    if (copied > 0 && ArraySize(atrValues) > 0) {
        return atrValues[0];
    }
    LogInfo("Error: Failed to retrieve ATR value.");
    return 0.0;
}

// Function to retrieve the latest RSI value
double iRSIValue(int handle) {
    double rsiValues[];
    int copied = CopyBuffer(handle, 0, 0, 1, rsiValues);
    if (copied > 0 && ArraySize(rsiValues) > 0) {
        return rsiValues[0];
    }
    LogInfo("Error: Failed to retrieve RSI value.");
    return 0.0;
}

// Function to check if the current spread is acceptable
bool IsSpreadAcceptable(string symbol) {
    double spread = (SymbolInfoDouble(symbol, SYMBOL_ASK) - SymbolInfoDouble(symbol, SYMBOL_BID)) / _Point;
    return spread <= MaxSpreadPoints;
}

// Placeholder function to check for high-impact news
bool IsHighImpactNewsUpcoming(int minutesAhead) {
    // Placeholder: Implement actual high-impact news checking logic here
    return false; // Assume no high-impact news is upcoming
}

// Main function to check trading signals
void CheckTradeSignals() {
    for (int i = 0; i < symbolCount; i++) {
        if (symbolTradeStates[i].tradeActive) continue;  // Ensure only one trade per symbol

        // Avoid trading during high-impact news or if the spread is too high
        if (IsHighImpactNewsUpcoming(NewsAvoidanceMinutes) || !IsSpreadAcceptable(symbolTradeStates[i].symbol)) {
            continue;
        }

        double rsiValue = iRSIValue(symbolTradeStates[i].rsiHandle);
        double atrValue = iATRValue(symbolTradeStates[i].atrHandle);
        double trendValue = iTrendValue(symbolTradeStates[i].trendHandle);
        double higherTimeframeTrendValue = iTrendValue(symbolTradeStates[i].higherTimeframeTrendHandle);
        double price = SymbolInfoDouble(symbolTradeStates[i].symbol, SYMBOL_BID);
        double lotSize = CalculateLotSize(i); // Pass the symbol index 'i' to the function

        if (lotSize <= 0) {
            LogInfo("Invalid lot size. Cannot execute trade for symbol: " + symbolTradeStates[i].symbol);
            continue;
        }

        double stopLoss = atrValue * currentBestSet.atrMultiplier;
        double takeProfit = atrValue * currentBestSet.trailingTakeProfitMultiplier;

        // Buy Condition
        if (rsiValue < 30 && price > trendValue && price > higherTimeframeTrendValue) {
            if (ExtTrade.Buy(lotSize, symbolTradeStates[i].symbol, price, price - stopLoss, price + takeProfit, "Buy Signal")) {
                symbolTradeStates[i].tradeActive = true;
                symbolTradeStates[i].lastTradeTime = TimeCurrent();
                LogInfo("Buy trade executed for symbol: " + symbolTradeStates[i].symbol);
            } else {
                LogInfo("Buy trade failed: " + IntegerToString(GetLastError()));
            }
        }
        // Sell Condition
        else if (rsiValue > 70 && price < trendValue && price < higherTimeframeTrendValue) {
            if (ExtTrade.Sell(lotSize, symbolTradeStates[i].symbol, price, price + stopLoss, price - takeProfit, "Sell Signal")) {
                symbolTradeStates[i].tradeActive = true;
                symbolTradeStates[i].lastTradeTime = TimeCurrent();
                LogInfo("Sell trade executed for symbol: " + symbolTradeStates[i].symbol);
            } else {
                LogInfo("Sell trade failed: " + IntegerToString(GetLastError()));
            }
        }
    }
}

// Function to perform backtesting with given parameters
double BacktestWithParameters(int rsiPeriod, double atrMultiplier, double trailingStopMultiplier, double trailingTPMultiplier) {
    // Placeholder for backtesting logic
    double simulatedProfit = 0.0;
    
    // Implement real backtesting logic using historical data (this is a placeholder)
    return simulatedProfit;
}

// Function to optimize parameters using brute-force optimization
void OptimizeParameters() {
    LogInfo("Starting parameter optimization...");
    double bestPerformance = -DBL_MAX;
    ParameterSet bestSet;

    for (int rsiPeriod = RSI_Min_Period; rsiPeriod <= RSI_Max_Period; rsiPeriod++) {
        for (double atrMultiplier = ATR_Min_Multiplier; atrMultiplier <= ATR_Max_Multiplier; atrMultiplier += 0.5) {
            for (double trailingStopMultiplier = TrailingStop_Min_Multiplier; trailingStopMultiplier <= TrailingStop_Max_Multiplier; trailingStopMultiplier += 0.5) {
                for (double trailingTPMultiplier = TrailingTP_Min_Multiplier; trailingTPMultiplier <= TrailingTP_Max_Multiplier; trailingTPMultiplier += 0.5) {
                    double profit = BacktestWithParameters(rsiPeriod, atrMultiplier, trailingStopMultiplier, trailingTPMultiplier);
                    if (profit > bestPerformance) {
                        bestPerformance = profit;
                        bestSet.periodRSI = rsiPeriod;
                        bestSet.atrMultiplier = atrMultiplier;
                        bestSet.trailingStopMultiplier = trailingStopMultiplier;
                        bestSet.trailingTakeProfitMultiplier = trailingTPMultiplier;
                    }
                }
            }
        }
    }

    currentBestSet = bestSet;
    LogInfo("Optimization complete. Best RSI: " + IntegerToString(currentBestSet.periodRSI) +
            ", ATR Multiplier: " + DoubleToString(currentBestSet.atrMultiplier) + 
            ", Trailing Stop Multiplier: " + DoubleToString(currentBestSet.trailingStopMultiplier) +
            ", Trailing TP Multiplier: " + DoubleToString(currentBestSet.trailingTakeProfitMultiplier));
}

// Function to apply trailing stop
void ApplyTrailingStop() {
    for (int i = 0; i < symbolCount; i++) {
        if (!symbolTradeStates[i].tradeActive) continue;

        double atrValue = iATRValue(symbolTradeStates[i].atrHandle);
        double price = 0.0;

        if (PositionGetInteger(POSITION_TYPE) == POSITION_TYPE_BUY) {
            price = SymbolInfoDouble(symbolTradeStates[i].symbol, SYMBOL_BID);
        } else if (PositionGetInteger(POSITION_TYPE) == POSITION_TYPE_SELL) {
            price = SymbolInfoDouble(symbolTradeStates[i].symbol, SYMBOL_ASK);
        }

        if (price <= 0.0) {
            LogInfo("Error: Failed to retrieve current price for symbol: " + symbolTradeStates[i].symbol);
            continue;
        }

        double openPrice = PositionGetDouble(POSITION_PRICE_OPEN);
        double profitLevel = MathAbs(price - openPrice);

        double newStopLoss;
        if (profitLevel > 2.0 * atrValue) {
            newStopLoss = (PositionGetInteger(POSITION_TYPE) == POSITION_TYPE_BUY) ? price - 1.5 * atrValue : price + 1.5 * atrValue;
        } else if (profitLevel > 1.0 * atrValue) {
            newStopLoss = (PositionGetInteger(POSITION_TYPE) == POSITION_TYPE_BUY) ? price - 1.0 * atrValue : price + 1.0 * atrValue;
        } else {
            newStopLoss = (PositionGetInteger(POSITION_TYPE) == POSITION_TYPE_BUY) ? price - 0.5 * atrValue : price + 0.5 * atrValue;
        }

        if (ExtTrade.PositionModify(PositionGetInteger(POSITION_TICKET), newStopLoss, 0)) {
            LogInfo("Trailing stop updated for symbol: " + symbolTradeStates[i].symbol);
        }
    }
}

// Function to adjust parameters based on performance
void AdjustParametersBasedOnPerformance(int symbolIndex) {
    int wins = symbolTradeStates[symbolIndex].winCount;
    int losses = symbolTradeStates[symbolIndex].lossCount;
    double winRate = (wins + losses > 0) ? (double)wins / (wins + losses) : 0.0;

    if (winRate < 0.4) {
        currentRiskPercent = MathMax(currentRiskPercent - 0.05, 0.05);
        LogInfo("Risk reduced due to low win rate for symbol: " + symbolTradeStates[symbolIndex].symbol);
    } else if (winRate > 0.6) {
        currentRiskPercent = MathMin(currentRiskPercent + 0.05, MaxRiskPercent);
        LogInfo("Risk increased due to high win rate for symbol: " + symbolTradeStates[symbolIndex].symbol);
    }
}

// Main OnTick function
void OnTick() {
    datetime currentTime = TimeCurrent();
    LogInfo("OnTick called.");

    if (currentTime - lastOptimization > OptimizationInterval) {
        OptimizeParameters();
        lastOptimization = currentTime;
    }

    if (currentTime - lastPerformanceCheck > PerformanceCheckInterval) {
        for (int i = 0; i < symbolCount; i++) {
            AdjustParametersBasedOnPerformance(i);
        }
        lastPerformanceCheck = currentTime;
    }

    CheckTradeSignals();
    ApplyTrailingStop();
}

// Initialization function
int OnInit() {
    LogInfo("Initializing EA...");
    currentRiskPercent = MaxRiskPercent;
    AddSymbolState(_Symbol); // Add the current symbol to the symbol state
    OptimizeParameters();    // Perform initial parameter optimization
    return INIT_SUCCEEDED;   // Return success
}

// Deinitialization function
void OnDeinit(const int reason) {
    LogInfo("EA Deinitialized.");
    for (int i = 0; i < symbolCount; i++) {
        if (symbolTradeStates[i].rsiHandle != INVALID_HANDLE) {
            IndicatorRelease(symbolTradeStates[i].rsiHandle);
        }
        if (symbolTradeStates[i].atrHandle != INVALID_HANDLE) {
            IndicatorRelease(symbolTradeStates[i].atrHandle);
        }
        if (symbolTradeStates[i].trendHandle != INVALID_HANDLE) {
            IndicatorRelease(symbolTradeStates[i].trendHandle);
        }
        if (symbolTradeStates[i].higherTimeframeTrendHandle != INVALID_HANDLE) {
            IndicatorRelease(symbolTradeStates[i].higherTimeframeTrendHandle);
        }
    }
}
