//+------------------------------------------------------------------+
//| MultiOrders.mq5: A MetaTrader 5 Expert Advisor for Opening        |
//|                  Multiple Buy and Sell Orders Based on User Input |
//| Copyright 2023, MetaQuotes Software Corp.                        |
//| https://www.mql5.com                                             |
//+------------------------------------------------------------------+
#property copyright "Copyright 2023, MetaQuotes Software Corp."
#property link      "https://www.mql5.com"
#property version   "1.00"
#property indicator_chart_window
#property indicator_buffers 0
#property indicator_plots   0

#include <ChartObjects\ChartObjectsTxtControls.mqh>
#include <Trade\Trade.mqh>

input int       Num_of_Buy = 5;             // Number of Buy orders to open
input int       Num_of_Sell = 5;            // Number of Sell orders to open
input double    RiskPercentage = 1.0;       // Risk percentage per trade
input int       StopLoss = 200;             // Stop Loss (in points)
input int       TakeProfit = 400;           // Take Profit (in points)
input int       Slippage = 3;               // Slippage

CChartObjectButton buyButton, sellButton;
CTrade trade;

//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
{
   // Create Buy button
   if (!buyButton.Create(0, "BuyButton", 0, 50, 50, 100, 30))
   {
      Print("Failed to create Buy button");
      return (INIT_FAILED);
   }
   buyButton.Description("Open Buy Orders");
   buyButton.State(0);

   // Create Sell button
   if (!sellButton.Create(0, "SellButton", 0, 50, 100, 100, 30))
   {
      Print("Failed to create Sell button");
      return (INIT_FAILED);
   }
   sellButton.Description("Open Sell Orders");
   sellButton.State(0);

   return (INIT_SUCCEEDED);
}

//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
{
   // Get the current bid and ask prices
   double bid = SymbolInfoDouble(_Symbol, SYMBOL_BID);
   double ask = SymbolInfoDouble(_Symbol, SYMBOL_ASK);

   // Calculate the spread
   double spread = ask - bid;

   // Calculate the average price
   double avgPrice = (bid + ask) / 2.0;

   // Check if the spread is less than the configured slippage
   if (spread <= Slippage * _Point) {
      // Calculate the lot size based on the risk percentage
      double lotSize = CalculateLotSize();

      // Check if the average price is above the current ask price
      if (avgPrice > ask) {
         // Place a buy order
         trade.Buy(lotSize, _Symbol, ask, ask - StopLoss * _Point, ask + TakeProfit * _Point, "Buy order");
      }
      // Check if the average price is below the current bid price
      else if (avgPrice < bid) {
         // Place a sell order
         trade.Sell(lotSize, _Symbol, bid, bid + StopLoss * _Point, bid - TakeProfit * _Point, "Sell order");
      }
   }
}
//+------------------------------------------------------------------+
//| ChartEvent function |
//+------------------------------------------------------------------+
void OnChartEvent(const int id,
const long &lparam,
const double &dparam,
const string &sparam)
{
if (id == CHARTEVENT_OBJECT_CLICK)
{
if (sparam == "BuyButton")
{
OpenMultipleBuyOrders();
}
else if (sparam == "SellButton")
{
OpenMultipleSellOrders();
}
}
}
//+------------------------------------------------------------------+
//| Calculate the appropriate lot size based on risk percentage |
//+------------------------------------------------------------------+
double CalculateLotSize()
{
   double riskAmount = AccountInfoDouble(ACCOUNT_BALANCE) * RiskPercentage / 100;
   double lotSize = riskAmount / (StopLoss * _Point * (AccountInfoInteger(ACCOUNT_LEVERAGE) / 100));
   return NormalizeDouble(lotSize, 2);
}

//+------------------------------------------------------------------+
//| Open multiple Buy orders |
//+------------------------------------------------------------------+
void OpenMultipleBuyOrders()
{
double calculatedLotSize = CalculateLotSize();

for (int i = 0; i < Num_of_Buy; i++)
{
string comment = "Buy Order #" + IntegerToString(i + 1);
trade.Buy(calculatedLotSize, _Symbol, SymbolInfoDouble(_Symbol, SYMBOL_ASK), StopLoss, TakeProfit, comment);
}
}

//+------------------------------------------------------------------+
//| Open multiple Sell orders |
//+------------------------------------------------------------------+
void OpenMultipleSellOrders()
{
double calculatedLotSize = CalculateLotSize();

for (int i = 0; i < Num_of_Sell; i++)
{
string comment = "Sell Order #" + IntegerToString(i + 1);
trade.Sell(calculatedLotSize, _Symbol, SymbolInfoDouble(_Symbol, SYMBOL_BID), StopLoss, TakeProfit, comment);
}
}