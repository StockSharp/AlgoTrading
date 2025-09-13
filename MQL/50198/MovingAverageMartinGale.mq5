
#property copyright "Mueller Peter"
#property link      "https://www.mql5.com/en/users/mullerp04/seller"
#property version   "1.00"
#include <ImportantFunctions.mqh>

// Inputs for the Expert Advisor
input int MAPeriod = 50;               // Moving Average period
input double StartingLot = 0.01;       // Starting lot size for trades
input double MaxLot = 0.5;             // Maximum lot size allowed
input int TPPoints = 100;              // Take profit points
input int SLPoints = 300;              // Stop loss points
input double LotMultiplier = 2;        // Multiplier for increasing lot size after a loss
input double TPMultiplier = 2;         // Multiplier for adjusting TP and SL after a loss

CTrade Trade; // Object for managing trades

// Initialization function
int OnInit()
{
   return(INIT_SUCCEEDED); // Initialization succeeded
}

// Deinitialization function
void OnDeinit(const int reason)
{
   // Nothing to clean up for now
}

// Main function called on every tick
void OnTick()
{
   // Handle for the moving average indicator and arrays to store its values
   static int MAhandle = iMA(_Symbol, PERIOD_CURRENT, MAPeriod, 0, MODE_SMA, PRICE_CLOSE);
   static double MAArray[2];
   static double Vol = StartingLot;    // Current lot size
   static double TP = TPPoints*_Point; // Current take profit in points
   static double SL = SLPoints*_Point; // Current stop loss in points
   
   // Check if the market is open
   if(!MarketOpen())
      return;
   
   // If there are open positions, exit the function
   if(PositionsTotal()) 
      return;
   
   // If a new candle has formed
   if(IsNewCandle())
   {
      // Copy the moving average values into the array
      CopyBuffer(MAhandle, 0, 1, 2, MAArray);
      double PrevClose = iClose(_Symbol, PERIOD_CURRENT, 2); // Close price of the previous candle
      double CurrentClose = iClose(_Symbol, PERIOD_CURRENT, 1); // Close price of the current candle
      
      // Check for crossover conditions
      if((MAArray[0] > PrevClose && MAArray[1] < CurrentClose) || (MAArray[0] < PrevClose && MAArray[1] > CurrentClose))
      {
         // If the last trade was a loss and we can increase the lot size
         if(HistoryLastProfit() < 0 && Vol * LotMultiplier < MaxLot)
         {
            Vol *= LotMultiplier;       // Increase lot size
            TP *= TPMultiplier;         // Adjust take profit
            SL *= TPMultiplier;         // Adjust stop loss
         }
         
         // If the last trade was a profit
         if(HistoryLastProfit() > 0)
         {
            Vol = StartingLot;           // Reset lot size
            TP = TPPoints * _Point;      // Reset take profit
            SL = SLPoints * _Point;      // Reset stop loss
         }
      }
      
      // If the moving average crosses above the price, sell
      if(MAArray[0] < PrevClose && MAArray[1] > CurrentClose)
      {
         double bid = SymbolInfoDouble(_Symbol, SYMBOL_BID);
         if(CheckVolumeValue(RoundtoLots(Vol))) //Checking wether the volume is valid
            Trade.Sell(RoundtoLots(Vol), _Symbol, 0, Round(bid + SL, _Digits), Round(bid - TP, _Digits));
      }
      
      // If the moving average crosses below the price, buy
      if(MAArray[0] > PrevClose && MAArray[1] < CurrentClose)
      {
         double ask = SymbolInfoDouble(_Symbol, SYMBOL_ASK);
         if(CheckVolumeValue(RoundtoLots(Vol))) //Checking wether the volume is valid
            Trade.Buy(RoundtoLots(Vol), _Symbol, 0, Round(ask - SL, _Digits), Round(ask + TP, _Digits));
      }
   }
}