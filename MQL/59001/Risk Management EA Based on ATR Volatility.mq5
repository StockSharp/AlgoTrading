//+------------------------------------------------------------------+
//| RiskManagementEA.mq5                                             |
//| Copyright 2025, Your Name                                        |
//| https://www.mql5.com                                             |
//+------------------------------------------------------------------+
#property copyright "Your Name"
#property link      "https://www.mql5.com"
#property version   "1.00"
#property strict

//--- Input parameters
input double RiskPercentage = 1.0; // Risk percentage per trade (e.g., 1% of account balance)
input int ATRPeriod = 14;          // ATR period for volatility measurement
input double ATRMultiplier = 2.0;  // ATR multiplier for stop-loss calculation
input bool UseATRStopLoss = true;  // Use ATR-based stop-loss (true) or fixed stop-loss (false)

//--- Global variables
double atrValue;       // Stores the ATR value
double positionSize;   // Calculated position size for the trade
double stopLossPrice;  // Calculated stop-loss price

//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
{
   // Check if the symbol is valid
   if(!SymbolSelect(_Symbol, true))
   {
      Print("Invalid symbol: ", _Symbol);
      return(INIT_FAILED);
   }
   Print("Risk Management EA initialized successfully.");
   return(INIT_SUCCEEDED);
}

//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
{
   Print("Risk Management EA deinitialized. Reason: ", reason);
}

//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
{
   // Calculate ATR value
   atrValue = iATR(_Symbol, PERIOD_CURRENT, ATRPeriod);
   if(atrValue == 0)
   {
      Print("Error: ATR calculation failed.");
      return;
   }
   
   // Calculate position size based on risk percentage and ATR
   double accountBalance = AccountInfoDouble(ACCOUNT_BALANCE);
   double riskAmount = accountBalance * (RiskPercentage / 100.0); // Risk amount in account currency
   double atrPips = atrValue / _Point;                           // Convert ATR to pips
   positionSize = riskAmount / (atrPips * ATRMultiplier * _Point); // Position size in lots
   
   // Adjust position size to comply with broker's minimum and maximum lot sizes
   double minLot = SymbolInfoDouble(_Symbol, SYMBOL_VOLUME_MIN);
   double maxLot = SymbolInfoDouble(_Symbol, SYMBOL_VOLUME_MAX);
   positionSize = NormalizeDouble(positionSize, 2);              // Round to 2 decimal places
   positionSize = MathMax(minLot, MathMin(maxLot, positionSize)); // Ensure within limits
   
   // Calculate stop-loss price
   double askPrice = SymbolInfoDouble(_Symbol, SYMBOL_ASK);
   if(UseATRStopLoss)
   {
      stopLossPrice = askPrice - (atrValue * ATRMultiplier);     // Dynamic ATR-based stop-loss
   }
   else
   {
      stopLossPrice = askPrice - 50 * _Point;                    // Fixed stop-loss (50 pips)
   }
   stopLossPrice = NormalizeDouble(stopLossPrice, _Digits);      // Normalize to symbol digits
   
   // Simple trading logic: Buy when fast MA crosses above slow MA
   double maFast = iMA(_Symbol, PERIOD_CURRENT, 10, 0, MODE_SMA, PRICE_CLOSE);
   double maSlow = iMA(_Symbol, PERIOD_CURRENT, 20, 0, MODE_SMA, PRICE_CLOSE);
   
   if(maFast > maSlow && PositionSelect(_Symbol) == false)       // No open position
   {
      // Open a buy trade with calculated position size and stop-loss
      if(trade.Buy(positionSize, _Symbol, askPrice, stopLossPrice, 0, "Risk Management EA"))
      {
         Print("Buy trade opened successfully. Lot size: ", positionSize, ", Stop-loss: ", stopLossPrice);
      }
      else
      {
         Print("Error opening buy trade: ", GetLastError());
      }
   }
}

//+------------------------------------------------------------------+
//| Trade function                                                   |
//+------------------------------------------------------------------+
#include <Trade\Trade.mqh>
CTrade trade;

//+------------------------------------------------------------------+
//| EA Description and Usage Instructions                            |
//+------------------------------------------------------------------+
// This Expert Advisor (EA) automates risk management by calculating the optimal
// position size for each trade based on a user-defined risk percentage and market
// volatility (measured by ATR). It ensures consistent risk exposure across trades.
//
// Usage Instructions:
// 1. Attach the EA to a chart in MetaTrader 5.
// 2. Configure the input parameters:
//    - RiskPercentage: Set the desired risk per trade (e.g., 1% of account balance).
//    - ATRPeriod: Set the period for ATR calculation (e.g., 14).
//    - ATRMultiplier: Set the multiplier for ATR-based stop-loss (e.g., 2.0).
//    - UseATRStopLoss: Choose true for ATR-based stop-loss or false for fixed stop-loss.
// 3. The EA will calculate the position size and open trades based on a simple
//    moving average crossover strategy.
//
// Benefits:
// - Maintains consistent risk across varying market conditions.
// - Protects against large losses during high volatility.
// - Customizable to suit different trading styles.