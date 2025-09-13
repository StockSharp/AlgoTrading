
#property copyright "Mueller Peter"
#property link      "https://www.mql5.com/en/users/mullerp04/seller"
#property version   "1.00"

// Include external functions
#include <ImportantFunctions.mqh>

// Inputs for the Expert Advisor
input int MAPeriod = 50;               // Moving Average period
input double LotSize = 0.01;           // Lot size for trades
input int TPPoints = 150;              // Take profit points
input int SLPoints = 150;              // Stop loss points

CTrade Trade; // Object for managing trades

// Initialization function
int OnInit()
{
   return(INIT_SUCCEEDED); // Initialization succeeded
}

//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
{
   // Nothing to clean up for now
}

//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
{
   // Handle for the moving average indicator and arrays to store its values
   static int MAhandle = iMA(_Symbol, PERIOD_CURRENT, MAPeriod, 0, MODE_SMA, PRICE_CLOSE);
   static double MAArray[2];
   
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
      
      // If the moving average crosses above the price, sell
      if(MAArray[0] < PrevClose && MAArray[1] > CurrentClose)
      {
         double bid = SymbolInfoDouble(_Symbol, SYMBOL_BID);
         if(CheckVolumeValue(LotSize)) // Checking wether the volume is valid
            Trade.Sell(RoundtoLots(LotSize), _Symbol, 0, Round(bid + SLPoints * _Point, _Digits), Round(bid - TPPoints * _Point, _Digits));
         
      }
      
      // If the moving average crosses below the price, buy
      if(MAArray[0] > PrevClose && MAArray[1] < CurrentClose)
      {
         double ask = SymbolInfoDouble(_Symbol, SYMBOL_ASK);
         if(CheckVolumeValue(LotSize)) // Checking wether the volume is valid
            Trade.Buy(RoundtoLots(LotSize), _Symbol, 0, Round(ask - SLPoints * _Point, _Digits), Round(ask + TPPoints * _Point, _Digits));
      }
   }
}
