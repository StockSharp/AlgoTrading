
#property strict

enum TradeSizeTypeEnum { FixedSize, BalancePercent, EquityPercent };
input TradeSizeTypeEnum TradeSizeType = FixedSize; // Type of trade size calculation
input double FixedLotSize = 0.01;                  // Fixed lot size
input double TradeSizePercent = 1.0;               // Percentage of balance or equity for trade size
input int    MagicNumber  = 123456;                // Unique identifier for EA's trades
input double MA_Period    = 52;                    // Moving Average period
input double TP_Multiplier = 1400.0;               // Take Profit multiplier
input double SL_Multiplier = 900.0;                // Stop Loss multiplier
input int    MinTradeInterval = 60;                // Minimum interval between trades in minutes
input int    RSI_Period = 13;                      // RSI period
input int    RSI_Overbought = 85;                  // RSI overbought level
input int    RSI_Oversold = 35;                    // RSI oversold level
input int    MACD_FastEMA = 8;                     // MACD fast EMA period
input int    MACD_SlowEMA = 24;                    // MACD slow EMA period
input int    MACD_SignalSMA = 13;                  // MACD signal SMA period
input int    BollingerBands_Period = 25;           // Bollinger Bands period
input double BollingerBands_Deviation = 2.5;       // Bollinger Bands deviation
input int    Stochastic_K = 10;                    // Stochastic %K period
input int    Stochastic_D = 2;                     // Stochastic %D period
input int    Stochastic_Slowing = 2;               // Stochastic slowing

// Input parameters to toggle each indicator
input bool UseMA = true;                           // Toggle MA on/off
input bool UseRSI = true;                          // Toggle RSI on/off
input bool UseMACD = true;                         // Toggle MACD on/off
input bool UseBollingerBands = false;              // Toggle Bollinger Bands on/off
input bool UseStochastic = true;                   // Toggle Stochastic on/off

datetime lastTradeTime = 0;

// Function to Check the correctness of the order volume  
                      
bool CheckVolumeValue(double volume,string &description)
  {
// minimal allowed volume for trade operations
   double min_volume=SymbolInfoDouble(Symbol(),SYMBOL_VOLUME_MIN);
   if(volume<min_volume)
     {
      description=StringFormat("Volume is less than the minimal allowed SYMBOL_VOLUME_MIN=%.2f",min_volume);
      return(false);
     }

// maximal allowed volume of trade operations
   double max_volume=SymbolInfoDouble(Symbol(),SYMBOL_VOLUME_MAX);
   if(volume>max_volume)
     {
      description=StringFormat("Volume is greater than the maximal allowed SYMBOL_VOLUME_MAX=%.2f",max_volume);
      return(false);
     }

//get minimal step of volume changing
   double volume_step=SymbolInfoDouble(Symbol(),SYMBOL_VOLUME_STEP);

   int ratio=(int)MathRound(volume/volume_step);
   if(MathAbs(ratio*volume_step-volume)>0.0000001)
     {
      description=StringFormat("Volume is not a multiple of the minimal step SYMBOL_VOLUME_STEP=%.2f, the closest correct volume is %.2f",
                               volume_step,ratio*volume_step);
      return(false);
     }
   description="Correct volume value";
   return(true);
  }

// Function to calculate lot size based on selected method
double CalculateLotSize()
  {
   double lotSize = FixedLotSize;

   if(TradeSizeType == BalancePercent)
     {
      lotSize = AccountBalance() * TradeSizePercent / 100.0 / MarketInfo(Symbol(), MODE_MARGINREQUIRED);
     }
   else
      if(TradeSizeType == EquityPercent)
        {
         lotSize = AccountEquity() * TradeSizePercent / 100.0 / MarketInfo(Symbol(), MODE_MARGINREQUIRED);
        }

   lotSize = NormalizeDouble(lotSize, 2); // Normalize lot size to 2 decimal places
   return lotSize;
  }

// Function to detect buy signal
bool IsBuySignal()
  {
   bool maCondition = true, rsiCondition = true, macdCondition = true, bbCondition = true, stochasticCondition = true;

   if(UseMA)
     {
      double MA_Short = iMA(NULL, 0, 20, 0, MODE_SMA, PRICE_CLOSE, 0);
      double MA_Long = iMA(NULL, 0, (int)MA_Period, 0, MODE_SMA, PRICE_CLOSE, 0);
      maCondition = MA_Short > MA_Long;
     }

   if(UseRSI)
     {
      double RSI = iRSI(NULL, 0, RSI_Period, PRICE_CLOSE, 0);
      rsiCondition = RSI < 50; // Relaxed condition from RSI_Oversold to 50
     }

   if(UseMACD)
     {
      double MACD_Line = iMACD(NULL, 0, MACD_FastEMA, MACD_SlowEMA, MACD_SignalSMA, PRICE_CLOSE, MODE_MAIN, 0);
      double MACD_Signal = iMACD(NULL, 0, MACD_FastEMA, MACD_SlowEMA, MACD_SignalSMA, PRICE_CLOSE, MODE_SIGNAL, 0);
      macdCondition = MACD_Line > MACD_Signal;
     }

   if(UseBollingerBands)
     {
      double LowerBand = iBands(NULL, 0, BollingerBands_Period, BollingerBands_Deviation, 0, PRICE_CLOSE, MODE_LOWER, 0);
      bbCondition = Close[0] < LowerBand;
     }

   if(UseStochastic)
     {
      double K = iStochastic(NULL, 0, Stochastic_K, Stochastic_D, Stochastic_Slowing, MODE_SMA, 0, MODE_MAIN, 0);
      double D = iStochastic(NULL, 0, Stochastic_K, Stochastic_D, Stochastic_Slowing, MODE_SMA, 0, MODE_SIGNAL, 0);
      stochasticCondition = K < 50 && D < 50; // Relaxed condition from 20 to 50
     }

// Log the indicator values for debugging
   Print("Buy Check - MA Condition: ", maCondition, " RSI Condition: ", rsiCondition, " MACD Condition: ", macdCondition, " BB Condition: ", bbCondition, " Stochastic Condition: ", stochasticCondition);

   return maCondition && rsiCondition && macdCondition && bbCondition && stochasticCondition;
  }

// Function to detect sell signal
bool IsSellSignal()
  {
   bool maCondition = true, rsiCondition = true, macdCondition = true, bbCondition = true, stochasticCondition = true;

   if(UseMA)
     {
      double MA_Short = iMA(NULL, 0, 20, 0, MODE_SMA, PRICE_CLOSE, 0);
      double MA_Long = iMA(NULL, 0, (int)MA_Period, 0, MODE_SMA, PRICE_CLOSE, 0);
      maCondition = MA_Short < MA_Long;
     }

   if(UseRSI)
     {
      double RSI = iRSI(NULL, 0, RSI_Period, PRICE_CLOSE, 0);
      rsiCondition = RSI > 50; // Relaxed condition from RSI_Overbought to 50
     }

   if(UseMACD)
     {
      double MACD_Line = iMACD(NULL, 0, MACD_FastEMA, MACD_SlowEMA, MACD_SignalSMA, PRICE_CLOSE, MODE_MAIN, 0);
      double MACD_Signal = iMACD(NULL, 0, MACD_FastEMA, MACD_SlowEMA, MACD_SignalSMA, PRICE_CLOSE, MODE_SIGNAL, 0);
      macdCondition = MACD_Line < MACD_Signal;
     }

   if(UseBollingerBands)
     {
      double UpperBand = iBands(NULL, 0, BollingerBands_Period, BollingerBands_Deviation, 0, PRICE_CLOSE, MODE_UPPER, 0);
      bbCondition = Close[0] > UpperBand;
     }

   if(UseStochastic)
     {
      double K = iStochastic(NULL, 0, Stochastic_K, Stochastic_D, Stochastic_Slowing, MODE_SMA, 0, MODE_MAIN, 0);
      double D = iStochastic(NULL, 0, Stochastic_K, Stochastic_D, Stochastic_Slowing, MODE_SMA, 0, MODE_SIGNAL, 0);
      stochasticCondition = K > 50 && D > 50; // Relaxed condition from 80 to 50
     }

// Log the indicator values for debugging
   Print("Sell Check - MA Condition: ", maCondition, " RSI Condition: ", rsiCondition, " MACD Condition: ", macdCondition, " BB Condition: ", bbCondition, " Stochastic Condition: ", stochasticCondition);

   return maCondition && rsiCondition && macdCondition && bbCondition && stochasticCondition;
  }

// Main function to handle new tick events
void OnTick()
{
   static datetime lastTime = 0;

   if(Time[0] != lastTime)
   {
      lastTime = Time[0];

      // Check if the minimum trade interval has passed
      if(TimeCurrent() - lastTradeTime < MinTradeInterval * 60)
      {
         Print("Trade interval not met. Waiting for ", MinTradeInterval, " minutes.");
         return;
      }

      double stopLoss = 0.0;
      double takeProfit = 0.0;
      int ticket = 0;

      double lotSize = CalculateLotSize();
      string description;

      // Check volume validity
      if(!CheckVolumeValue(lotSize, description))
      {
         Print("Invalid lot size: ", description);
         return;
      }

      if(IsBuySignal())
      {
         stopLoss = NormalizeDouble(Bid - (SL_Multiplier * Point), Digits);
         takeProfit = NormalizeDouble(Bid + (TP_Multiplier * Point), Digits);
         Print("Buy Signal: Lot Size = ", DoubleToStr(lotSize, 2), " StopLoss = ", DoubleToStr(stopLoss, Digits), " TakeProfit = ", DoubleToStr(takeProfit, Digits));

         ticket = OrderSend(Symbol(), OP_BUY, lotSize, Ask, 3, stopLoss, takeProfit, "Buy Order", MagicNumber, 0, clrGreen);
         if(ticket > 0)
         {
            lastTradeTime = TimeCurrent();
            Print("Buy order placed successfully. Ticket: ", ticket);
         }
         else
         {
            Print("Error placing buy order. Error code: ", GetLastError());
         }
      }
      else if(IsSellSignal())
      {
         stopLoss = NormalizeDouble(Ask + (SL_Multiplier * Point), Digits);
         takeProfit = NormalizeDouble(Ask - (TP_Multiplier * Point), Digits);
         Print("Sell Signal: Lot Size = ", DoubleToStr(lotSize, 2), " StopLoss = ", DoubleToStr(stopLoss, Digits), " TakeProfit = ", DoubleToStr(takeProfit, Digits));

         ticket = OrderSend(Symbol(), OP_SELL, lotSize, Bid, 3, stopLoss, takeProfit, "Sell Order", MagicNumber, 0, clrRed);
         if(ticket > 0)
         {
            lastTradeTime = TimeCurrent();
            Print("Sell order placed successfully. Ticket: ", ticket);
         }
         else
         {
            Print("Error placing sell order. Error code: ", GetLastError());
         }
      }
   }
}


//+------------------------------------------------------------------+
