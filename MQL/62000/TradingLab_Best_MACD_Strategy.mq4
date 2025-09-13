//+------------------------------------------------------------------+
//|           TradingLab_Best_MACD_Strategy.mq5                      |
//|           Author: Mueller Peter                                  |
//|           https://www.mql5.com/en/users/mullerp04/seller         |
//+------------------------------------------------------------------+
#property copyright "Mueller Peter"
#property link      "https://www.mql5.com/en/users/mullerp04/seller"
#property version   "1.00"

#include <ExpertFunctions.mqh>  // Custom functions (e.g., CheckVolumeValue)

// Input parameters
input int SignalValidity = 7;                // How many candles a signal remains valid
input double Lotsize = 1;                    // Fixed lot size for each trade
input int SLPointDistanceFromMA = 50;        // Distance of Stop Loss from Moving Average in points

//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
   // No initialization needed in this version
   return(INIT_SUCCEEDED);
  }

//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
   // No deinitialization needed
  }

//+------------------------------------------------------------------+
//| Expert tick function: runs on every new tick                     |
//+------------------------------------------------------------------+
void OnTick()
 {
   // Only check conditions once per candle
   if(!IsNewCandle())
      return;

   // Get current ask and bid prices
   double ask = SymbolInfoDouble(_Symbol,SYMBOL_BID);
   double bid = SymbolInfoDouble(_Symbol,SYMBOL_BID);

   // Signal memory variables
   static int ResistanceTouch = 0;
   static int SupportTouch = 0;
   static int MACDDownSig = 0;
   static int MACDUpSig = 0;

   // Decrease counters over time
   if(ResistanceTouch) ResistanceTouch--;
   if(SupportTouch)    SupportTouch--;
   if(MACDDownSig)     MACDDownSig--;
   if(MACDUpSig)       MACDUpSig--;

   // Get support and resistance levels using custom indicator "Box"
   double ResistanceValue = iCustom(_Symbol,PERIOD_CURRENT,"Box",20,10,0,1);   
   double SupportValue    = iCustom(_Symbol,PERIOD_CURRENT,"Box",20,10,1,1);

   // Check if resistance was touched
   if(ResistanceValue != EMPTY_VALUE && iHigh(_Symbol,PERIOD_CURRENT,1) > ResistanceValue)
   {
      ResistanceTouch = SignalValidity;
      PrintFormat("Resistance touch. resistance level: %lf, high before: %lf", ResistanceValue, iHigh(_Symbol,PERIOD_CURRENT,1));
      Print(TimeLocal());
   }

   // Check if support was touched
   if(SupportValue != EMPTY_VALUE && iLow(_Symbol,PERIOD_CURRENT,1) < SupportValue)
   {
      SupportTouch = SignalValidity;
      PrintFormat("Support touch. support level: %lf, low before: %lf", SupportValue, iLow(_Symbol,PERIOD_CURRENT,1));
      Print(TimeLocal());
   }

   // MACD crossover UP signal
   if(iMACD(_Symbol,PERIOD_CURRENT,12,26,9,PRICE_CLOSE,MODE_MAIN,1) >
      iMACD(_Symbol,PERIOD_CURRENT,12,26,9,PRICE_CLOSE,MODE_SIGNAL,1) &&
      iMACD(_Symbol,PERIOD_CURRENT,12,26,9,PRICE_CLOSE,MODE_MAIN,2) <
      iMACD(_Symbol,PERIOD_CURRENT,12,26,9,PRICE_CLOSE,MODE_SIGNAL,2) &&
      iMACD(_Symbol,PERIOD_CURRENT,12,26,9,PRICE_CLOSE,MODE_MAIN,1) < 0)
      MACDUpSig = SignalValidity;

   // MACD crossover DOWN signal
   if(iMACD(_Symbol,PERIOD_CURRENT,12,26,9,PRICE_CLOSE,MODE_MAIN,1) <
      iMACD(_Symbol,PERIOD_CURRENT,12,26,9,PRICE_CLOSE,MODE_SIGNAL,1) &&
      iMACD(_Symbol,PERIOD_CURRENT,12,26,9,PRICE_CLOSE,MODE_MAIN,2) >
      iMACD(_Symbol,PERIOD_CURRENT,12,26,9,PRICE_CLOSE,MODE_SIGNAL,2) &&
      iMACD(_Symbol,PERIOD_CURRENT,12,26,9,PRICE_CLOSE,MODE_MAIN,1) > 0)
      MACDDownSig = SignalValidity;

   // --- BUY Condition ---
   if(MACDUpSig && SupportTouch &&
      iMA(_Symbol,PERIOD_CURRENT,200,0,MODE_SMA,PRICE_CLOSE,0) < ask &&
      (MACDUpSig == SignalValidity || SupportTouch == SignalValidity))
   {
      if(!CheckVolumeValue(Lotsize))
         return;

      double Sl = iMA(_Symbol,PERIOD_CURRENT,200,0,MODE_SMA,PRICE_CLOSE,0) - SLPointDistanceFromMA * _Point;
      double Tp = ask + (ask - iMA(_Symbol,PERIOD_CURRENT,200,0,MODE_SMA,PRICE_CLOSE,0) + SLPointDistanceFromMA * _Point) * 1.5;
      OrderSend(_Symbol, OP_BUY, Lotsize, Ask, 500, Sl, Tp);
   }

   // --- SELL Condition ---
   if(MACDDownSig && ResistanceTouch &&
      iMA(_Symbol,PERIOD_CURRENT,200,0,MODE_SMA,PRICE_CLOSE,0) > ask &&
      (MACDDownSig == SignalValidity || SupportTouch == SignalValidity))
   {
      if(!CheckVolumeValue(Lotsize))
         return;

      double Sl = iMA(_Symbol,PERIOD_CURRENT,200,0,MODE_SMA,PRICE_CLOSE,0) + SLPointDistanceFromMA * _Point;
      double Tp = bid - (iMA(_Symbol,PERIOD_CURRENT,200,0,MODE_SMA,PRICE_CLOSE,0) - bid + SLPointDistanceFromMA * _Point) * 1.5;
      OrderSend(_Symbol, OP_SELL, Lotsize, Bid, 500, Sl, Tp);
   }
}
