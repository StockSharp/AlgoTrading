#property copyright "Copyright 2023, MetaQuotes Ltd."
#property link      "https://www.mql5.com"
#property version   "1.00"
#include <Trade\Trade.mqh> // Include the Trade library for trading operations

CTrade Trade; // Create a trading object for executing trades

// Input variables to customize the EA's behavior
input double Lot_Size_Per_Thousand = 0.01; // Lot size per thousand dollars of account balance
input double Total_Commission_for_Lot_Traded = 7.0; // Total commission per lot traded
input bool Plot_Max_Difference = false; // Flag to control plotting of maximum price difference

// Initialization function called when the EA is loaded
int OnInit()
{
   return INIT_SUCCEEDED; // Return initialization succeeded signal
}

// Deinitialization function called when the EA is removed or the chart is closed
void OnDeinit(const int reason)
{
   // Clean-up code would go here
}

// Main function called on every new tick of price data
void OnTick()
{
   // Calculate price ratios for arbitrage calculations
   double EURUSDaskGBPUSDbid = SymbolInfoDouble("EURUSD", SYMBOL_ASK) / SymbolInfoDouble("GBPUSD", SYMBOL_BID);
   double EURGBPask = SymbolInfoDouble("EURGBP", SYMBOL_ASK);
   double EURUSDbidGBPUSDask = SymbolInfoDouble("EURUSD", SYMBOL_BID) / SymbolInfoDouble("GBPUSD", SYMBOL_ASK);
   double EURGBPbid = SymbolInfoDouble("EURGBP", SYMBOL_BID);

   // Track and log the largest price difference if plotting is enabled
   static double biggest = 0.0;
   if(MathAbs(EURUSDaskGBPUSDbid - EURGBPask) > biggest && Plot_Max_Difference)
   {
      biggest = MathAbs(EURUSDaskGBPUSDbid - EURGBPask);
      PrintFormat("Biggest Difference in points: %.5lf", biggest);
      PrintFormat("Needed: %.5lf", _Point + Round(3*Total_Commission_for_Lot_Traded*_Point + SymbolInfoInteger("EURUSD", SYMBOL_SPREAD)*_Point + SymbolInfoInteger("GBPUSD", SYMBOL_SPREAD)*_Point + SymbolInfoInteger("EURGBP", SYMBOL_SPREAD)*_Point, _Digits));
   }

   // Check for an arbitrage opportunity to sell EURGBP
   if(EURUSDaskGBPUSDbid - EURGBPask > _Point + Round(3*Total_Commission_for_Lot_Traded*_Point + SymbolInfoInteger("EURUSD", SYMBOL_SPREAD)*_Point + SymbolInfoInteger("GBPUSD", SYMBOL_SPREAD)*_Point + SymbolInfoInteger("EURGBP", SYMBOL_SPREAD)*_Point, _Digits))
   {
      CloseNegSide(); // Close negative side of trades
      if(!PositionsTotal()) // Check if there are no open positions
      {
         // calculate Volume
         double Vol = MathMin(RoundtoLots(AccountInfoDouble(ACCOUNT_BALANCE) / 1000 * Lot_Size_Per_Thousand), SymbolInfoDouble("EURGBP", SYMBOL_VOLUME_MAX));
         Trade.Sell(Vol, "EURUSD");
         Trade.Buy(Vol, "GBPUSD");
         Trade.Buy(Vol, "EURGBP");
      }
   }

   // Check for an arbitrage opportunity to buy EURGBP
   if(EURUSDbidGBPUSDask - EURGBPbid < -_Point + Round(-3*Total_Commission_for_Lot_Traded*_Point - SymbolInfoInteger("EURUSD", SYMBOL_SPREAD)*_Point - SymbolInfoInteger("GBPUSD", SYMBOL_SPREAD)*_Point - SymbolInfoInteger("EURGBP", SYMBOL_SPREAD)*_Point, _Digits))
   {
      ClosePosSide(); // Close positive side of trades
      if(!PositionsTotal()) // Check if there are no open positions
      {
         // calculate Volume
         double Vol = MathMin(RoundtoLots(AccountInfoDouble(ACCOUNT_BALANCE) / 1000 * Lot_Size_Per_Thousand), SymbolInfoDouble("EURGBP", SYMBOL_VOLUME_MAX));
         Trade.Buy(Vol, "EURUSD");
         Trade.Sell(Vol, "GBPUSD");
         Trade.Sell(Vol, "EURGBP");
      }
   }
}


// Function to close the positive side: EURGBP GBPUSD Buy and EURUSD Sell positions
void ClosePosSide()
{
   // Iterate over all open positions
   for(int i = 0; i < PositionsTotal(); i++)
   {
      // Select each position by its ticket number
      if(PositionSelectByTicket(PositionGetTicket(i)))
      {
         // Get the type of the position (Buy/Sell)
         ENUM_POSITION_TYPE Type = (ENUM_POSITION_TYPE) PositionGetInteger(POSITION_TYPE);
         // Get the symbol of the position
         string symb = PositionGetString(POSITION_SYMBOL);
         // Close buy positions for GBPUSD or EURGBP
         if(Type == POSITION_TYPE_BUY && (symb == "GBPUSD" || symb == "EURGBP"))
            Trade.PositionClose(PositionGetTicket(i));
         // Close sell positions for EURUSD
         if(Type == POSITION_TYPE_SELL && symb == "EURUSD")
            Trade.PositionClose(PositionGetTicket(i));
      }
   }
}

// Function to close the negative side: EURGBP GBPUSD Sell and EURUSD Buy positions
void CloseNegSide()
{
   // Iterate over all open positions
   for(int i = 0; i < PositionsTotal(); i++)
   {
      // Select each position by its ticket number
      if(PositionSelectByTicket(PositionGetTicket(i)))
      {
         // Get the type of the position (Buy/Sell)
         ENUM_POSITION_TYPE Type = (ENUM_POSITION_TYPE) PositionGetInteger(POSITION_TYPE);
         // Get the symbol of the position
         string symb = PositionGetString(POSITION_SYMBOL);
         // Close sell positions for GBPUSD or EURGBP
         if(Type == POSITION_TYPE_SELL && (symb == "GBPUSD" || symb == "EURGBP"))
            Trade.PositionClose(PositionGetTicket(i));
         // Close buy positions for EURUSD
         if(Type == POSITION_TYPE_BUY && symb == "EURUSD")
            Trade.PositionClose(PositionGetTicket(i));
      }
   }
}

// Function to round a value to valid lot size
double RoundtoLots(double Val, bool down = false)
{  
   // Round up by default
   if(!down)
   {
      // Round according to the lot step size of the symbol
      if(SymbolInfoDouble(_Symbol, SYMBOL_VOLUME_STEP) == 0.01)
         return Round(Val, 2);
      if(SymbolInfoDouble(_Symbol, SYMBOL_VOLUME_STEP) == 0.1)
         return Round(Val, 1);
      if(SymbolInfoDouble(_Symbol, SYMBOL_VOLUME_STEP) == 0.001)
         return Round(Val, 3);
      return Round(Val, 0);
   }
   // Round down when specified
   else
   {
      if(SymbolInfoDouble(_Symbol, SYMBOL_VOLUME_STEP) == 0.01)
         return RoundDown(Round(Val, 4), 2);
      if(SymbolInfoDouble(_Symbol, SYMBOL_VOLUME_STEP) == 0.1)
         return RoundDown(Round(Val, 3), 1);
      if(SymbolInfoDouble(_Symbol, SYMBOL_VOLUME_STEP) == 0.001)
         return RoundDown(Round(Val, 5), 3);
      return RoundDown(Round(Val, 2), 0);
   }
}

// Generic function to round a value to a specified number of decimal places
double Round(double value, int decimals)
{     
   // Validate input
   if (decimals < 0) 
   {  
      Print("Wrong decimals input parameter, parameter can't be below 0");
      return 0;
   }
   double timesten = value * MathPow(10, decimals);
   timesten = MathRound(timesten);
   double truevalue = timesten / MathPow(10, decimals);
   return truevalue;      
}

// Function to round a value up to a specified number of decimal places
double RoundUp(double val, int decim)
{
   if(Round(val, decim) < val) 
      return Round(val, decim) + MathPow(10, -decim);
   else 
      return Round(val, decim);
}

// Function to round a value down to a specified number of decimal places
double RoundDown(double val, int decim)
{
   if(Round(val, decim) > val) 
      return Round(val, decim) - MathPow(10, -decim);
   else 
      return Round(val, decim); 
}