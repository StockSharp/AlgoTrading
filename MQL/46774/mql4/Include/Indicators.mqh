//+------------------------------------------------------------------+
//|                                                   Indicators.mqh |
//|                                  Copyright 2023, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright "Copyright 2023, MetaQuotes Ltd."
#property link      "https://www.mql5.com"
#property strict
//+------------------------------------------------------------------+
//| defines                                                          |
//+------------------------------------------------------------------+
// #define MacrosHello   "Hello, world!"
// #define MacrosYear    2010
//+------------------------------------------------------------------+
//| enums                                                            |
//+------------------------------------------------------------------+
enum BarColor { GREEN_BAR, RED_BAR };
enum TrendDirection { BULLISH, BEARISH, LATERAL_UNCERTAIN };

//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
string GetTrandName(TrendDirection trendDirectionCode)
  {
   switch(trendDirectionCode)
     {
      case BULLISH:
         return "BULLISH";
      case BEARISH:
         return "BEARISH";
      default:
         return "LATERAL_UNCERTAIN";
     }
  }
//+------------------------------------------------------------------+
//| DLL imports                                                      |
//+------------------------------------------------------------------+
// #import "user32.dll"
//   int      SendMessageA(int hWnd,int Msg,int wParam,int lParam);
// #import "my_expert.dll"
//   int      ExpertRecalculate(int wParam,int lParam);
// #import
//+------------------------------------------------------------------+
//| EX5 imports                                                      |
//+------------------------------------------------------------------+
// #import "stdlib.ex5"
//   string ErrorDescription(int error_code);
// #import
//+------------------------------------------------------------------+

//+------------------------------------------------------------------+
double GetDistanceFromEMA(double currentPrice, double emaSlow)
  {
   return MathAbs(currentPrice - emaSlow);
  }

//+------------------------------------------------------------------+
bool IsDistanceLowerThanNeighboringBars(double currentDistance, double& distances[])
  {
   for(int i = 0; i < 3; i++)
     {
      if(currentDistance >= distances[i])
         return false;
     }
   return true;
  }

//+------------------------------------------------------------------+
bool IsDistanceLowerThanBarSize(double distanceFromEMAInPips, double barSizeInPips)
  {
   return distanceFromEMAInPips < barSizeInPips;
  }

//+------------------------------------------------------------------+
bool IsBreakEMAFastCondition(BarColor barColor, double openPrice, double closePrice, double emaFast)
  {
   if(barColor == GREEN_BAR)
     {
      return openPrice < emaFast && closePrice > emaFast;
     }
   else
      if(barColor == RED_BAR)
        {
         return openPrice > emaFast && closePrice < emaFast;
        }
   return false;
  }

//+------------------------------------------------------------------+
bool CrossesSignalFromAbove(double mainLine, double signalLine, double prevMainLine, double prevSignalLine)
  {
   if(prevMainLine >= 80 && mainLine <= 80)
     {
      return true;
     }
   return false;
  }
//+------------------------------------------------------------------+
bool CrossesSignalFromBelow(double mainLine, double signalLine, double prevMainLine, double prevSignalLine)
  {
   if(prevMainLine <= 20 && mainLine >= 20)
     {
      return true;
     }
   return false;
  }
//+------------------------------------------------------------------+
bool CrossesEMAFromAbove(double ema, int shift)
  {
   return((iOpen(Symbol(),PERIOD_CURRENT,shift) >= ema || iLow(Symbol(),PERIOD_CURRENT,shift) >= ema) && (iClose(Symbol(),PERIOD_CURRENT,shift) <= ema || iHigh(Symbol(),PERIOD_CURRENT,shift) <= ema));
  }
//+------------------------------------------------------------------+
bool CrossesEMAFromBelow(double ema, int shift)
  {
   return((iOpen(Symbol(),PERIOD_CURRENT,shift) <= ema || iLow(Symbol(),PERIOD_CURRENT,shift) <= ema) && (iClose(Symbol(),PERIOD_CURRENT,shift) >= ema || iHigh(Symbol(),PERIOD_CURRENT,shift) >= ema));
  }
//+------------------------------------------------------------------+
//returns true if there is a bullbreakout of level Donchian Channel
bool DonchianBullBreakout(string symbol, ENUM_TIMEFRAMES Timeframe, double donchianLevelValue, int bar = 1)
  {
   return(iOpen(symbol, Timeframe, bar) < donchianLevelValue && iClose(symbol, Timeframe, bar) > donchianLevelValue);
  }
//returns true if there is a bearbreakout of level Donchian Channel
bool DonchianBearBreakout(string symbol, ENUM_TIMEFRAMES Timeframe, double donchianLevelValue, int bar = 1)
  {
   return(iOpen(symbol, Timeframe, bar) > donchianLevelValue && iClose(symbol, Timeframe, bar) < donchianLevelValue);
  }
//+------------------------------------------------------------------+
//returns true if there is a bullbreakout
bool BullBreakout(string symbol, ENUM_TIMEFRAMES Timeframe, int bar = 1)
  {
   return(AbnormalCandle(symbol,Timeframe,bar)&& iClose(symbol,Timeframe,bar) > iOpen(symbol,Timeframe,bar) && iClose(symbol,Timeframe,bar) - iOpen(symbol,Timeframe,bar) > 0.5*(iHigh(symbol,Timeframe,bar)-iLow(symbol,Timeframe,bar)));
  }
//returns true if there is a bearbreakout.
bool BearBreakout(string symbol, ENUM_TIMEFRAMES Timeframe, int bar = 1)
  {
   return(AbnormalCandle(symbol,Timeframe,bar)&& iClose(symbol,Timeframe,bar) < iOpen(symbol,Timeframe,bar) && iOpen(symbol,Timeframe,bar) - iClose(symbol,Timeframe,bar) > 0.5*(iHigh(symbol,Timeframe,bar)-iLow(symbol,Timeframe,bar)));
  }
//returns true if the last candle is abnormally big compared to the ones before
bool AbnormalCandle(string symbol, ENUM_TIMEFRAMES Timeframe, int bar = 1)
  {
   double SavedChange = 0;

   for(int i = bar+1; i < bar+11; i ++)
     {
      SavedChange = SavedChange + (iHigh(symbol,Timeframe,i) - iLow(symbol,Timeframe,i));
     }
   double Averagechange = SavedChange/10;

   if((iHigh(symbol,Timeframe,bar) - iLow(symbol,Timeframe,bar)) > Averagechange * 3)
      return true;
   return false;
  }

//+------------------------------------------------------------------+
//| Indicators Logic                                                 |
//+------------------------------------------------------------------+

//+------------------------------------------------------------------+
void Andean(int timeframe, double& bull_buffer[], double& bear_buffer[], double& ma_buffer[], int bars = 100, int length = 50, bool showMA = true, int sig_length = 15)
  {
//---
   double up1[];
   double up2[];
   double dn1[];
   double dn2[];
//---
   ArrayResize(up1,         bars+sig_length);
   ArrayResize(up2,         bars+sig_length);
   ArrayResize(dn1,         bars+sig_length);
   ArrayResize(dn2,         bars+sig_length);
   ArrayResize(bull_buffer, bars+sig_length);
   ArrayResize(bear_buffer, bars+sig_length);
   ArrayResize(ma_buffer,   bars+sig_length);
//---
   ArrayFill(up1,        0, bars+sig_length, 0);
   ArrayFill(up2,        0, bars+sig_length, 0);
   ArrayFill(dn1,        0, bars+sig_length, 0);
   ArrayFill(dn2,        0, bars+sig_length, 0);
   ArrayFill(bull_buffer,0, bars+sig_length, 0);
   ArrayFill(bear_buffer,0, bars+sig_length, 0);
   ArrayFill(ma_buffer,  0, bars+sig_length, 0);
//----
   for(int i = bars-1; i >= 0; i--)
     {
      double alpha = 2.0 / (double(length +1));
      double C = iClose(_Symbol, timeframe, i);
      double O = iOpen(_Symbol, timeframe, i);

      if(up1[i+1] == 0 || up1[i+1] == EMPTY_VALUE)
         up1[i+1] = iClose(_Symbol, timeframe, i+1);
      if(up2[i+1] == 0 || up2[i+1] == EMPTY_VALUE)
         up2[i+1] = iClose(_Symbol, timeframe, i+1) * iClose(_Symbol, timeframe, i+1);
      up1[i] = MathMax(MathMax(C, O), up1[i+1] - (up1[i+1] - C) * alpha);
      up2[i] = MathMax(MathMax(C * C, O * O), up2[i+1] - (up2[i+1] - C * C) * alpha);
      if(up1[i] == 0)
         up1[i] = C;
      if(up2[i] == 0)
         up2[i] = C * C;

      if(dn1[i+1] == 0 || dn1[i+1] == EMPTY_VALUE)
         dn1[i+1] = iClose(_Symbol, timeframe, i+1);
      if(dn2[i+1] == 0 || dn2[i+1] == EMPTY_VALUE)
         dn2[i+1] = iClose(_Symbol, timeframe, i+1) * iClose(_Symbol, timeframe, i+1);
      dn1[i] = MathMin(MathMin(C, O), dn1[i+1] + (C - dn1[i+1]) * alpha);
      dn2[i] = MathMin(MathMin(C * C, O * O), dn2[i+1] + (C * C - dn2[i+1]) * alpha);
      if(dn1[i] == 0)
         dn1[i] = C;
      if(dn2[i] == 0)
         dn2[i] = C * C;

      //Components
      bull_buffer[i] = NormalizeDouble(MathSqrt(dn2[i] - dn1[i] * dn1[i]),Digits);
      bear_buffer[i] = NormalizeDouble(MathSqrt(up2[i] - up1[i] * up1[i]),Digits);

      if(showMA)
        {
         int _cnt = 0;
         double ma_ = 0;
         for(int j = 0; j < sig_length; j++)
           {
            if(i+j < bars)
              {
               ma_ += MathMax(bull_buffer[i+j],bear_buffer[i+j]);
               _cnt++;
              }
           }
         if(_cnt > 0)
            ma_buffer[i] = NormalizeDouble(ma_ / double(_cnt),Digits);
         else
            ma_buffer[i] = NormalizeDouble(ma_,Digits);
        }
     }
   return;
  }
//+------------------------------------------------------------------+
void DonchianChannels(int timeframe, double& bull_buffer[], double& bear_buffer[], double& ma_buffer[], int DonchianChannelsPeriods = 20, int DonchianChannelsExtremes = 1, int DonchianChannelsMargins = 1, int DonchianChannelsAdvance = 0, int DonchianChannelsBars = 100)
  {
//---
   ArrayResize(bull_buffer, DonchianChannelsBars+DonchianChannelsAdvance);
   ArrayResize(bear_buffer, DonchianChannelsBars+DonchianChannelsAdvance);
   ArrayResize(ma_buffer,   DonchianChannelsBars+DonchianChannelsAdvance);
//---
   ArrayFill(bull_buffer,0, DonchianChannelsBars+DonchianChannelsAdvance, 0);
   ArrayFill(bear_buffer,0, DonchianChannelsBars+DonchianChannelsAdvance, 0);
   ArrayFill(ma_buffer,  0, DonchianChannelsBars+DonchianChannelsAdvance, 0);
//----
//int shift=0;//, cnt(0), loopbegin(0);
   double smin=0, smax=0, SsMax=0, SsMin=0;
//Variables : bar(0), prevbars(0), start(0), cs(0), prevcs(0),commodt(0);

   for(int shift=0; shift<DonchianChannelsBars; shift++)
     {
      if(DonchianChannelsExtremes ==1)
        {
         SsMax = iHigh(_Symbol,timeframe,iHighest(_Symbol,timeframe,MODE_HIGH,DonchianChannelsPeriods,shift));
         SsMin = iLow(_Symbol,timeframe,iLowest(_Symbol,timeframe,MODE_LOW,DonchianChannelsPeriods,shift));
        }
      else
         if(DonchianChannelsExtremes == 3)
           {
            SsMax = (iOpen(_Symbol,timeframe,iHighest(_Symbol,timeframe,MODE_OPEN,DonchianChannelsPeriods,shift))+iHigh(_Symbol,timeframe,iHighest(_Symbol,timeframe,MODE_HIGH,DonchianChannelsPeriods,shift)))/2;
            SsMin = (iOpen(_Symbol,timeframe,iLowest(_Symbol,timeframe,MODE_OPEN,DonchianChannelsPeriods,shift))+iLow(_Symbol,timeframe,iLowest(_Symbol,timeframe,MODE_LOW,DonchianChannelsPeriods,shift)))/2;
           }
         else
           {
            SsMax = iOpen(_Symbol,timeframe,iHighest(_Symbol,timeframe,MODE_OPEN,DonchianChannelsPeriods,shift));
            SsMin = iOpen(_Symbol,timeframe,iLowest(_Symbol,timeframe,MODE_OPEN,DonchianChannelsPeriods,shift));
           }
      smin = SsMin+(SsMax-SsMin)*DonchianChannelsMargins/100;
      smax = SsMax-(SsMax-SsMin)*DonchianChannelsMargins/100;
      bear_buffer[shift-DonchianChannelsAdvance]=NormalizeDouble(smin,Digits);
      bull_buffer[shift-DonchianChannelsAdvance]=NormalizeDouble(smax,Digits);
      ma_buffer[shift-DonchianChannelsAdvance]=NormalizeDouble(smin+(smax-smin)/2,Digits);
     }
//----
  }
//+------------------------------------------------------------------+
void SuperScalperEMA(int timeframe, int bars, double& bull_buffer[], double& bear_buffer[], int periodEMAFast = 50, int periodEMASlow = 150, int stochasticK = 5, int stochasticD = 5, int stochasticSlowing = 5, double overboughtLevel = 80, double oversoldLevel = 20, double adxLevel = 37.0, int adxPeriods = 14)
  {
//---
   int sig_length = periodEMASlow;
   double barSizeBuffer[];
   double distanceFromEmaBuffer[];
   double retracementIndexBuffer[];
   double stochasticIndexBuffer[];
   double breakEMAFastIndexBuffer[];
//---
   ArrayResize(barSizeBuffer, bars+sig_length);
   ArrayResize(distanceFromEmaBuffer, bars+sig_length);
   ArrayResize(retracementIndexBuffer, bars+sig_length);
   ArrayResize(stochasticIndexBuffer, bars+sig_length);
   ArrayResize(breakEMAFastIndexBuffer, bars+sig_length);
   ArrayResize(bull_buffer, bars+sig_length);
   ArrayResize(bear_buffer, bars+sig_length);
//---
   ArrayFill(barSizeBuffer,0, bars+sig_length, 0);
   ArrayFill(distanceFromEmaBuffer,0, bars+sig_length, 0);
   ArrayFill(retracementIndexBuffer,0, bars+sig_length, 0);
   ArrayFill(stochasticIndexBuffer,0, bars+sig_length, 0);
   ArrayFill(breakEMAFastIndexBuffer,0, bars+sig_length, 0);
   ArrayFill(bull_buffer,0, bars+sig_length, 0);
   ArrayFill(bear_buffer,0, bars+sig_length, 0);
//----
   bool retracementCondition = false;
   bool breakEMAFastCondition = false;
   bool stochasticCondition = false;

   int lastBuySignalIndex = bars;
   int lastSellSignalIndex = bars;

   int retracementBarIndex = bars;
   int breakEMAFastBarIndex = bars;
   int stochasticBarIndex = bars;

   for(int i = bars - 1; i >= 0; i--)
     {
      // Determine the color of the current bar
      BarColor currentBarColor = (iClose(_Symbol,timeframe,i) > iOpen(_Symbol,timeframe,i)) ? GREEN_BAR : RED_BAR;

      // Calculate EMAs
      double emaFast = iMA(_Symbol,timeframe, periodEMAFast, 0, MODE_EMA, PRICE_CLOSE, i);
      double emaSlow = iMA(_Symbol,timeframe, periodEMASlow, 0, MODE_EMA, PRICE_CLOSE, i);
      double emaFastPrev = iMA(_Symbol,timeframe, periodEMAFast, 0, MODE_EMA, PRICE_CLOSE, i+1);

      // Calculate Stochastic
      double stochasticMain = iStochastic(_Symbol,timeframe, stochasticK, stochasticD, stochasticSlowing, MODE_EMA, 0, MODE_MAIN, i);
      double stochasticSignal = iStochastic(_Symbol,timeframe, stochasticK, stochasticD, stochasticSlowing, MODE_EMA, 0, MODE_SIGNAL, i);
      double stochasticPrevMain = iStochastic(_Symbol,timeframe, stochasticK, stochasticD, stochasticSlowing, MODE_EMA, 0, MODE_MAIN, i+1);
      double stochasticPrevSignal = iStochastic(_Symbol,timeframe, stochasticK, stochasticD, stochasticSlowing, MODE_EMA, 0, MODE_SIGNAL, i+1);

      // Calculate ADX
      double adx      = iADX(_Symbol,timeframe, adxPeriods, PRICE_CLOSE, MODE_MAIN, i);
      double adxPrev  = iADX(_Symbol,timeframe, adxPeriods, PRICE_CLOSE, MODE_MAIN, i+1);
      double adxPrev2 = iADX(_Symbol,timeframe, adxPeriods, PRICE_CLOSE, MODE_MAIN, i+2);

      // Calculate the distance from the Slow EMA
      double distanceFromEMAPrice = iClose(_Symbol,timeframe,i);
      if(iClose(_Symbol,timeframe,i) > emaSlow)
         distanceFromEMAPrice = iLow(_Symbol,timeframe,i);
      if(iClose(_Symbol,timeframe,i) < emaSlow)
         distanceFromEMAPrice = iHigh(_Symbol,timeframe,i);

      double distanceFromEMAInPips = GetDistanceFromEMA(distanceFromEMAPrice, emaSlow) / Point;

      // Calculate the bar size in pips
      double barSizeInPips = (iHigh(_Symbol,timeframe,i) - iLow(_Symbol,timeframe,i)) / Point;

      barSizeBuffer[i] = barSizeInPips;
      distanceFromEmaBuffer[i] = distanceFromEMAInPips;

      // Calculate distances of previous and next 3 bars
      double prevDistances[16];
      double nextDistances[16];
      if(i > 5)
        {
         for(int j = 1; j < 4; j++)
           {
            if(iClose(_Symbol,timeframe,i) > emaSlow)
              {
               prevDistances[j - 1] = GetDistanceFromEMA(iLow(_Symbol,timeframe,i - j - 1), emaSlow) / Point;
               nextDistances[j - 1] = GetDistanceFromEMA(iLow(_Symbol,timeframe,i + j + 1), emaSlow) / Point;
              }
            if(iClose(_Symbol,timeframe,i) < emaSlow)
              {
               prevDistances[j - 1] = GetDistanceFromEMA(iHigh(_Symbol,timeframe,i - j - 1), emaSlow) / Point;
               nextDistances[j - 1] = GetDistanceFromEMA(iHigh(_Symbol,timeframe,i + j + 1), emaSlow) / Point;
              }
           }
        }

      // Check if the current distance is lower than previous and next 3 bars
      bool isLowerThanNeighbors = IsDistanceLowerThanNeighboringBars(distanceFromEMAInPips, prevDistances) &&
                                  IsDistanceLowerThanNeighboringBars(distanceFromEMAInPips, nextDistances);

      // Check if the distanceFromEMA is lower than the bar size
      bool isLowerThanBarSize = IsDistanceLowerThanBarSize(distanceFromEMAInPips, barSizeInPips);

      if(isLowerThanNeighbors && isLowerThanBarSize)
        {
         retracementCondition = true;
         retracementBarIndex = i;
         retracementIndexBuffer[i] = i;
        }

      // BUY condition
      if(iOpen(_Symbol,timeframe,i) > emaSlow && iClose(_Symbol,timeframe,i) > emaSlow && emaFast > emaSlow)
        {

         // Check the break of EMA Fast condition
         breakEMAFastCondition = IsBreakEMAFastCondition(currentBarColor, iOpen(_Symbol,timeframe,i), iClose(_Symbol,timeframe,i), emaFast);
         if((currentBarColor == GREEN_BAR && breakEMAFastCondition) || (emaFastPrev < emaSlow && emaFast > emaSlow))
           {
            breakEMAFastBarIndex = i;
            breakEMAFastIndexBuffer[i] = i;
           }

         // Check Stochastic condition
         stochasticCondition = CrossesSignalFromBelow(stochasticMain, stochasticSignal, stochasticPrevMain, stochasticPrevSignal);
         if(stochasticCondition)
           {
            stochasticBarIndex = i;
            stochasticIndexBuffer[i] = i;
           }

         // Check retracement and break of Fast EMA
         if(
            adx < adxLevel &&
            ((lastBuySignalIndex - i) > 3 && (retracementBarIndex - i) <= 3 && (stochasticBarIndex - i) <= 3 && (breakEMAFastBarIndex - i) <= 3 && breakEMAFastBarIndex < retracementBarIndex)
         )
           {
            // Draw a green arrow for buy signal
            ObjectCreate("BuyArrow" + IntegerToString(i), OBJ_ARROW_UP, 0, iTime(_Symbol,timeframe,i), iLow(_Symbol,timeframe,i) - Point * 10);
            ObjectSetInteger(0, "BuyArrow" + IntegerToString(i), OBJPROP_COLOR, clrGreen);
            bull_buffer[i] = 1; // Place a buy signal on the chart
            lastBuySignalIndex = i;
           }
        }
      // SELL condition
      else
         if(iOpen(_Symbol,timeframe,i) < emaSlow && iClose(_Symbol,timeframe,i) < emaSlow && emaFast < emaSlow) // Use "else if" to avoid conflicting conditions
           {

            // Check the break of EMA Fast condition
            breakEMAFastCondition = IsBreakEMAFastCondition(currentBarColor, iOpen(_Symbol,timeframe,i), iClose(_Symbol,timeframe,i), emaFast);
            if((currentBarColor == RED_BAR && breakEMAFastCondition) || (emaFastPrev > emaSlow && emaFast < emaSlow))
              {
               breakEMAFastBarIndex = i;
               breakEMAFastIndexBuffer[i] = i;
              }

            // Check Stochastic condition
            stochasticCondition = CrossesSignalFromAbove(stochasticMain, stochasticSignal, stochasticPrevMain, stochasticPrevSignal);
            if(stochasticCondition)
              {
               stochasticBarIndex = i;
               stochasticIndexBuffer[i] = i;
              }

            if(
               adx < adxLevel &&
               ((lastSellSignalIndex - i) > 3 && (retracementBarIndex - i) <= 3 && (stochasticBarIndex - i) <= 3 && (breakEMAFastBarIndex - i) <= 3 && breakEMAFastBarIndex < retracementBarIndex)
            )
              {
               // Draw a red arrow for sell signal
               ObjectCreate("SellArrow" + IntegerToString(i), OBJ_ARROW_DOWN, 0, iTime(_Symbol,timeframe,i), iHigh(_Symbol,timeframe,i) + Point * 10);
               ObjectSetInteger(0, "SellArrow" + IntegerToString(i), OBJPROP_COLOR, clrRed);
               bear_buffer[i] = 1; // Place a sell signal on the chart
               lastSellSignalIndex = i;
              }
           }
     }
  }
//+------------------------------------------------------------------+
