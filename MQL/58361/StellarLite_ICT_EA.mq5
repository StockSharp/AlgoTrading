//+------------------------------------------------------------------+
//|                                    StellarLite_ICT_EA.mq5         |
//|                      Copyright 2025, Advanced ICT Trader          |
//|                                      https://www.example.com      |
//+------------------------------------------------------------------+
#property copyright "Copyright 2025, Advanced ICT Trader"
#property link      "https://www.example.com"
#property version   "1.12"
#property description "EA for Stellar Lite 5K Challenge using Silver Bullet and 2022 Model"
#property description "High win-rate ICT strategies with low risk, partial TPs, trailing SL"

#include <Trade\Trade.mqh>
#include <Trade\AccountInfo.mqh>
#include <Trade\PositionInfo.mqh>
#include <Trade\SymbolInfo.mqh>
#include <Trade\DealInfo.mqh>

//--- Input Parameters
input group             "Risk Management"
input double            RiskPercentPerTrade = 0.25;     // Risk 0.25% per trade
input double            MaxTotalDrawdownPercent = 10.0; // Stellar Lite max drawdown
input double            MaxDailyDrawdownPercent = 5.0;  // Stellar Lite daily limit
input double            InitialBalance = 5000.0;        // Starting balance

input group             "Trade Management"
input double            TP1_RR = 1.0;                   // TP1 = 1:1 Risk:Reward
input double            TP2_RR = 2.0;                   // TP2 = 2:1
input double            TP3_RR = 3.0;                   // TP3 = 3:1
input double            PartialClosePercentTP1 = 50.0;  // Close 50% at TP1
input double            PartialClosePercentTP2 = 25.0;  // Close 25% at TP2
input double            PartialClosePercentTP3 = 25.0;  // Close 25% at TP3
input bool              MoveSLToBE_AfterTP1 = true;     // Move SL to Break Even after TP1
input int               BE_PlusPips = 1;                // Pips above/below BE
input double            TrailingSL_Pips = 10.0;         // Trailing SL after TP2

input group             "Strategy Selection"
input bool              Use_SilverBullet = true;        // Enable Silver Bullet
input string            SB_StartTime = "10:00";         // NY AM Killzone Start
input string            SB_EndTime = "11:00";           // NY AM Killzone End
input bool              Use_2022Model = true;           // Enable 2022 Model
input bool              Use_OTE_Entry = true;           // Use Fibonacci OTE

input group             "Higher Timeframe Bias"
input ENUM_TIMEFRAMES   HTF = PERIOD_H1;                // HTF for Bias
input int               HTF_MA_Period = 200;            // MA period
input ENUM_MA_METHOD    HTF_MA_Method = MODE_SMA;       // MA method
input ENUM_APPLIED_PRICE HTF_MA_Price = PRICE_CLOSE;    // Applied price

input group             "Draw on Liquidity (DOL)"
input int               DOL_Lookback_Bars = 120;        // Lookback bars
input double            NDOG_NWOG_Threshold = 0.5;      // ATR multiplier for NDOG/NWOG

input group             "Fibonacci OTE"
input double            OTE_Lower_Level = 0.618;        // Lower Fib level
input double            OTE_Upper_Level = 0.786;        // Upper Fib level

input group             "Visuals"
input bool              ShowTradeLevels = true;         // Show Entry, SL, TP lines
input color             BuyLevelColor = clrDodgerBlue;
input color             SellLevelColor = clrRed;
input color             TPLevelColor = clrGreen;
input color             SLLevelColor = clrOrangeRed;

//--- Global Variables
CTrade          trade;
CAccountInfo    accountInfo;
CPositionInfo   positionInfo;
CSymbolInfo     symbolInfo;
long            magicNumber;
double          minLot, maxLot, lotStep, pointValue, tickSize;
int             digitsFactor;
datetime        dailyStartTime = 0;
double          dailyStartEquity = 0;
int             htfMAHandle;
int             atrHandle;

// Structure for Trade Setup
struct TradeSetup
{
   bool        isValid;
   double      entryPrice;
   double      stopLossPrice;
   double      tp1Price;
   double      tp2Price;
   double      tp3Price;
   double      tp4Price;
   ENUM_ORDER_TYPE orderType;
   string      strategyName;
   double      lotSize;
};

//+------------------------------------------------------------------+
//| Expert initialization function                                    |
//+------------------------------------------------------------------+
int OnInit()
{
   magicNumber = ChartID(); // Simplified magic number
   trade.SetExpertMagicNumber(magicNumber);

   if(!symbolInfo.Name(_Symbol)) // Verify symbol initialization
   {
      Print("Error initializing symbol info for ", _Symbol);
      return(INIT_FAILED);
   }
   minLot = symbolInfo.LotsMin();
   maxLot = symbolInfo.LotsMax();
   lotStep = symbolInfo.LotsStep();
   pointValue = symbolInfo.Point();
   tickSize = symbolInfo.TickSize();
   digitsFactor = (symbolInfo.Digits() == 5 || symbolInfo.Digits() == 3) ? 10 : 1;

   htfMAHandle = iMA(_Symbol, HTF, HTF_MA_Period, 0, HTF_MA_Method, HTF_MA_Price);
   if(htfMAHandle == INVALID_HANDLE)
   {
      Print("Error initializing HTF MA handle");
      return(INIT_FAILED);
   }

   if(RiskPercentPerTrade <= 0 || InitialBalance <= 0)
   {
      Print("Error: Invalid RiskPercentPerTrade or InitialBalance");
      return(INIT_FAILED);
   }

   dailyStartEquity = accountInfo.Equity();
   dailyStartTime = TimeCurrent();
   Print("StellarLite ICT EA Initialized. Magic: ", magicNumber, ", Symbol: ", _Symbol);
   return(INIT_SUCCEEDED);
}

//+------------------------------------------------------------------+
//| Expert deinitialization function                                  |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
{
   if(htfMAHandle != INVALID_HANDLE) IndicatorRelease(htfMAHandle);
   if(atrHandle != INVALID_HANDLE) IndicatorRelease(atrHandle);
   ObjectsDeleteAll(ChartID(), "SL_ICT_EA_");
   Print("StellarLite ICT EA Deinitialized. Reason: ", reason);
}

//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
{
   if(!IsTradingAllowed()) return;

   MqlDateTime currentTime;
   TimeToStruct(TimeCurrent(), currentTime);
   datetime currentDay = TimeCurrent() - (currentTime.hour * 3600 + currentTime.min * 60);
   if(currentDay > dailyStartTime)
   {
      dailyStartEquity = accountInfo.Equity();
      dailyStartTime = currentDay;
   }

   if(CheckDrawdownLimits())
   {
      Print("Drawdown limit reached. Stopping trading.");
      return;
   }

   static datetime lastBarTime = 0;
   datetime currentBarTime = (datetime)SeriesInfoInteger(_Symbol, _Period, SERIES_LASTBAR_DATE);

   if(currentBarTime > lastBarTime)
   {
      lastBarTime = currentBarTime;
      if(positionInfo.SelectByMagic(_Symbol, magicNumber))
         ManageOpenTrades();
      else
         CheckForEntrySignals();
   }
   else if(positionInfo.SelectByMagic(_Symbol, magicNumber))
   {
      ManageOpenTrades();
   }

   if(ShowTradeLevels && positionInfo.SelectByMagic(_Symbol, magicNumber))
   {
      DrawTradeInfo(positionInfo.PriceOpen(), positionInfo.StopLoss(), positionInfo.TakeProfit(),
                    positionInfo.PositionType() == POSITION_TYPE_BUY ? BuyLevelColor : SellLevelColor,
                    SLLevelColor, TPLevelColor);
   }
   else
   {
      ObjectsDeleteAll(ChartID(), "SL_ICT_EA_Level_");
   }
}

//+------------------------------------------------------------------+
//| Check if trading is allowed                                       |
//+------------------------------------------------------------------+
bool IsTradingAllowed()
{
   if(!accountInfo.TradeAllowed() || !MQLInfoInteger(MQL_TRADE_ALLOWED))
      return false;
   return true;
}

//+------------------------------------------------------------------+
//| Check Drawdown Limits                                            |
//+------------------------------------------------------------------+
bool CheckDrawdownLimits()
{
   double equity = accountInfo.Equity();
   double balance = InitialBalance;
   double currentDrawdown = ((balance - equity) / balance) * 100.0;

   double maxAllowedLoss = balance * (MaxTotalDrawdownPercent / 100.0);
   double stopTradingEquity = balance - maxAllowedLoss;
   if(equity <= stopTradingEquity || currentDrawdown >= 10.0)
   {
      Print("Total Drawdown Limit Reached: ", currentDrawdown, "%. Equity: ", equity);
      return true;
   }

   double dailyMaxLoss = balance * (MaxDailyDrawdownPercent / 100.0);
   double dailyStopEquity = dailyStartEquity - dailyMaxLoss;
   if(equity <= dailyStopEquity)
   {
      Print("Daily Drawdown Limit Reached. Equity: ", equity);
      return true;
   }
   return false;
}

//+------------------------------------------------------------------+
//| Check for Entry Signals                                          |
//+------------------------------------------------------------------+
void CheckForEntrySignals()
{
   TradeSetup setup = {false, 0, 0, 0, 0, 0, 0, ORDER_TYPE_BUY, "", 0};

   ENUM_ORDER_TYPE htfBias = DetermineHTFBias();
   if(htfBias == -1) return;

   MqlDateTime currentTime;
   TimeToStruct(TimeCurrent(), currentTime);
   string timeStr = StringFormat("%02d:%02d", currentTime.hour, currentTime.min);

   if(Use_SilverBullet && timeStr >= SB_StartTime && timeStr < SB_EndTime)
   {
      setup = CheckSilverBulletEntry(htfBias);
      if(setup.isValid)
      {
         OpenTrade(setup);
         return;
      }
   }

   if(!setup.isValid && Use_2022Model)
   {
      setup = Check2022ModelEntry(htfBias);
      if(setup.isValid)
      {
         OpenTrade(setup);
         return;
      }
   }
}

//+------------------------------------------------------------------+
//| Determine Higher Timeframe Bias                                  |
//+------------------------------------------------------------------+
ENUM_ORDER_TYPE DetermineHTFBias()
{
   double ma[];
   if(CopyBuffer(htfMAHandle, 0, 0, 3, ma) < 3)
      return -1;
   double currentPrice = symbolInfo.Ask();
   if(currentPrice > ma[1] && ma[1] > ma[2])
      return ORDER_TYPE_BUY;
   else if(currentPrice < ma[1] && ma[1] < ma[2])
      return ORDER_TYPE_SELL;
   return -1;
}

//+------------------------------------------------------------------+
//| Check Silver Bullet Entry                                        |
//+------------------------------------------------------------------+
TradeSetup CheckSilverBulletEntry(ENUM_ORDER_TYPE bias)
{
   TradeSetup setup = {false, 0, 0, 0, 0, 0, 0, ORDER_TYPE_BUY, "SilverBullet", 0};
   MqlRates rates[];
   if(CopyRates(_Symbol, _Period, 0, 20, rates) < 20) return setup;

   double liquidityLevel = FindNearestLiquidityLevel(bias == ORDER_TYPE_SELL);
   bool liquiditySwept = CheckLiquiditySweep(liquidityLevel, bias, rates);
   bool mssConfirmed = CheckMSS(bias, rates);
   double fvgHigh = 0, fvgLow = 0;
   FindFVG(rates, fvgHigh, fvgLow);
   bool isNDOG_NWOG = CheckNDOG_NWOG(rates);

   if(liquiditySwept && mssConfirmed && fvgHigh > 0 && fvgLow > 0 && isNDOG_NWOG)
   {
      double currentPrice = rates[0].close;
      if(currentPrice <= fvgHigh && currentPrice >= fvgLow)
      {
         setup.orderType = bias;
         setup.entryPrice = CalculateEntryPrice(fvgLow, fvgHigh, Use_OTE_Entry);
         setup.stopLossPrice = FindProtectiveStopLoss(rates, bias == ORDER_TYPE_BUY);
         setup.tp1Price = bias == ORDER_TYPE_BUY ? 
            setup.entryPrice + (setup.entryPrice - setup.stopLossPrice) * TP1_RR :
            setup.entryPrice - (setup.stopLossPrice - setup.entryPrice) * TP1_RR;
         setup.tp2Price = bias == ORDER_TYPE_BUY ? 
            setup.entryPrice + (setup.entryPrice - setup.stopLossPrice) * TP2_RR :
            setup.entryPrice - (setup.stopLossPrice - setup.entryPrice) * TP2_RR;
         setup.tp3Price = bias == ORDER_TYPE_BUY ? 
            setup.entryPrice + (setup.entryPrice - setup.stopLossPrice) * TP3_RR :
            setup.entryPrice - (setup.stopLossPrice - setup.entryPrice) * TP3_RR;
         setup.isValid = true;
      }
   }
   return setup;
}

//+------------------------------------------------------------------+
//| Check 2022 Model Entry                                           |
//+------------------------------------------------------------------+
TradeSetup Check2022ModelEntry(ENUM_ORDER_TYPE bias)
{
   TradeSetup setup = {false, 0, 0, 0, 0, 0, 0, ORDER_TYPE_BUY, "2022Model", 0};
   MqlRates rates[];
   if(CopyRates(_Symbol, _Period, 0, 20, rates) < 20) return setup;

   double liquidityLevel = FindNearestLiquidityLevel(bias == ORDER_TYPE_SELL);
   bool inducement = CheckLiquiditySweep(liquidityLevel, bias == ORDER_TYPE_SELL ? ORDER_TYPE_BUY : ORDER_TYPE_SELL, rates);
   bool mssConfirmed = CheckMSS(bias, rates);
   double fvgHigh = 0, fvgLow = 0;
   FindFVG(rates, fvgHigh, fvgLow);
   bool isNDOG_NWOG = CheckNDOG_NWOG(rates);

   if(inducement && mssConfirmed && fvgHigh > 0 && fvgLow > 0 && isNDOG_NWOG)
   {
      double currentPrice = rates[0].close;
      if(currentPrice <= fvgHigh && currentPrice >= fvgLow)
      {
         setup.orderType = bias;
         setup.entryPrice = CalculateEntryPrice(fvgLow, fvgHigh, Use_OTE_Entry);
         setup.stopLossPrice = FindProtectiveStopLoss(rates, bias == ORDER_TYPE_BUY);
         setup.tp1Price = bias == ORDER_TYPE_BUY ? 
            setup.entryPrice + (setup.entryPrice - setup.stopLossPrice) * TP1_RR :
            setup.entryPrice - (setup.stopLossPrice - setup.entryPrice) * TP1_RR;
         setup.tp2Price = bias == ORDER_TYPE_BUY ? 
            setup.entryPrice + (setup.entryPrice - setup.stopLossPrice) * TP2_RR :
            setup.entryPrice - (setup.stopLossPrice - setup.entryPrice) * TP2_RR;
         setup.tp3Price = bias == ORDER_TYPE_BUY ? 
            setup.entryPrice + (setup.entryPrice - setup.stopLossPrice) * TP3_RR :
            setup.entryPrice - (setup.stopLossPrice - setup.entryPrice) * TP3_RR;
         setup.isValid = true;
      }
   }
   return setup;
}

//+------------------------------------------------------------------+
//| Calculate Lot Size                                               |
//+------------------------------------------------------------------+
double CalculateLotSize(double stopLossPrice, double entryPrice, ENUM_ORDER_TYPE orderType)
{
   if(RiskPercentPerTrade <= 0) return minLot;
   double accountBalance = accountInfo.Balance();
   double riskAmount = accountBalance * (RiskPercentPerTrade / 100.0);
   double slDistancePoints = MathAbs(entryPrice - stopLossPrice) / pointValue;
   if(slDistancePoints <= 0) return minLot;
   double tickValue = symbolInfo.TickValue();
   double calculatedLot = riskAmount / (slDistancePoints * tickValue);
   calculatedLot = NormalizeDouble(MathFloor(calculatedLot / lotStep) * lotStep, 2);
   return MathMax(minLot, MathMin(maxLot, calculatedLot));
}

//+------------------------------------------------------------------+
//| Open Trade                                                       |
//+------------------------------------------------------------------+
bool OpenTrade(TradeSetup &setup)
{
   if(!setup.isValid || setup.lotSize < minLot) return false;
   MqlTradeRequest request;
   MqlTradeResult result;
   request.action = TRADE_ACTION_DEAL; // Explicitly set action
   request.symbol = _Symbol;
   request.volume = setup.lotSize;
   request.magic = magicNumber;
   request.comment = setup.strategyName + " Entry";
   request.sl = NormalizeDouble(setup.stopLossPrice, symbolInfo.Digits());
   request.tp = NormalizeDouble(setup.tp3Price, symbolInfo.Digits());
   request.deviation = 5;
   if(setup.orderType == ORDER_TYPE_BUY)
   {
      request.type = ORDER_TYPE_BUY;
      request.price = symbolInfo.Ask();
   }
   else if(setup.orderType == ORDER_TYPE_SELL)
   {
      request.type = ORDER_TYPE_SELL;
      request.price = symbolInfo.Bid();
   }
   else return false;
   // Hardcode filling mode as a workaround (0 = ORDER_FILLING_FOK)
   request.type_filling = 0; // Temporary fix due to missing ENUM_ORDER_FILLING
   request.type_time = ORDER_TIME_GTC;

   if(trade.OrderSend(request, result))
   {
      if(result.retcode == TRADE_RETCODE_DONE || result.retcode == TRADE_RETCODE_PLACED)
      {
         Print("Trade Opened: ", setup.orderType == ORDER_TYPE_BUY ? "BUY" : "SELL", " @ ", request.price);
         StorePartialTPLevels(result.order, setup);
         if(ShowTradeLevels)
         {
            DrawTradeInfo(request.price, request.sl, request.tp,
                          setup.orderType == ORDER_TYPE_BUY ? BuyLevelColor : SellLevelColor,
                          SLLevelColor, TPLevelColor);
            DrawPartialTPLevel(1, setup.tp1Price);
            DrawPartialTPLevel(2, setup.tp2Price);
            DrawPartialTPLevel(3, setup.tp3Price);
         }
         return true;
      }
      Print("OrderSend failed. Retcode: ", result.retcode);
   }
   return false;
}

//+------------------------------------------------------------------+
//| Manage Open Trades                                               |
//+------------------------------------------------------------------+
void ManageOpenTrades()
{
   if(!positionInfo.SelectByMagic(_Symbol, magicNumber)) return;
   ulong ticket = positionInfo.Ticket();
   double initialVolume = GlobalVariableGet("SL_ICT_EA_" + IntegerToString(magicNumber) + "_" + IntegerToString(ticket) + "_InitVol");
   if(initialVolume == 0) initialVolume = positionInfo.Volume();
   double currentVolume = positionInfo.Volume();
   double entryPrice = positionInfo.PriceOpen();
   double currentSL = positionInfo.StopLoss();
   double currentTP = positionInfo.TakeProfit();
   ENUM_POSITION_TYPE type = positionInfo.PositionType();
   double currentPrice = type == POSITION_TYPE_BUY ? symbolInfo.Bid() : symbolInfo.Ask();

   TradeSetup partialTPs = RetrievePartialTPLevels(ticket);
   if(!partialTPs.isValid) return;

   bool tp1Hit = HasTPLevelBeenHit(ticket, 1);
   bool tp2Hit = HasTPLevelBeenHit(ticket, 2);

   if(!tp1Hit && partialTPs.tp1Price > 0)
   {
      bool hit = (type == POSITION_TYPE_BUY && currentPrice >= partialTPs.tp1Price) ||
                 (type == POSITION_TYPE_SELL && currentPrice <= partialTPs.tp1Price);
      if(hit)
      {
         double volToClose = initialVolume * (PartialClosePercentTP1 / 100.0);
         volToClose = NormalizeDouble(MathFloor(volToClose / lotStep) * lotStep, 2);
         if(volToClose >= minLot && currentVolume >= volToClose)
         {
            if(trade.PositionClosePartial(ticket, volToClose))
            {
               Print("TP1 Hit for #", ticket, ". Closed ", volToClose, " lots.");
               MarkTPLevelAsHit(ticket, 1);
               if(MoveSLToBE_AfterTP1)
               {
                  double beLevel = entryPrice + (type == POSITION_TYPE_BUY ? BE_PlusPips : -BE_PlusPips) * pointValue;
                  beLevel = NormalizeDouble(beLevel, symbolInfo.Digits());
                  if((type == POSITION_TYPE_BUY && beLevel > currentSL) || (type == POSITION_TYPE_SELL && beLevel < currentSL))
                     trade.PositionModify(ticket, beLevel, currentTP);
               }
            }
         }
      }
   }

   if(tp1Hit && !tp2Hit && partialTPs.tp2Price > 0)
   {
      bool hit = (type == POSITION_TYPE_BUY && currentPrice >= partialTPs.tp2Price) ||
                 (type == POSITION_TYPE_SELL && currentPrice <= partialTPs.tp2Price);
      if(hit)
      {
         double volToClose = initialVolume * (PartialClosePercentTP2 / 100.0);
         volToClose = NormalizeDouble(MathFloor(volToClose / lotStep) * lotStep, 2);
         if(currentVolume >= volToClose)
         {
            if(trade.PositionClosePartial(ticket, volToClose))
            {
               Print("TP2 Hit for #", ticket, ". Closed ", volToClose, " lots.");
               MarkTPLevelAsHit(ticket, 2);
               double newSL = type == POSITION_TYPE_BUY ? currentPrice - TrailingSL_Pips * pointValue * digitsFactor :
                              currentPrice + TrailingSL_Pips * pointValue * digitsFactor;
               newSL = NormalizeDouble(newSL, symbolInfo.Digits());
               if((type == POSITION_TYPE_BUY && newSL > currentSL) || (type == POSITION_TYPE_SELL && newSL < currentSL))
                  trade.PositionModify(ticket, newSL, currentTP);
            }
         }
      }
   }
}

//+------------------------------------------------------------------+
//| Store Partial TP Levels                                          |
//+------------------------------------------------------------------+
void StorePartialTPLevels(ulong ticket, TradeSetup &setup)
{
   string prefix = "SL_ICT_EA_" + IntegerToString(magicNumber) + "_" + IntegerToString(ticket) + "_";
   GlobalVariableSet(prefix + "TP1", setup.tp1Price);
   GlobalVariableSet(prefix + "TP2", setup.tp2Price);
   GlobalVariableSet(prefix + "TP3", setup.tp3Price);
   GlobalVariableSet(prefix + "InitVol", setup.lotSize);
   GlobalVariableSet(prefix + "TP1Hit", 0);
   GlobalVariableSet(prefix + "TP2Hit", 0);
   GlobalVariableSet(prefix + "IsValid", 1);
}

//+------------------------------------------------------------------+
//| Retrieve Partial TP Levels                                       |
//+------------------------------------------------------------------+
TradeSetup RetrievePartialTPLevels(ulong ticket)
{
   TradeSetup setup = {false, 0, 0, 0, 0, 0, 0, ORDER_TYPE_BUY, "", 0};
   string prefix = "SL_ICT_EA_" + IntegerToString(magicNumber) + "_" + IntegerToString(ticket) + "_";
   if(GlobalVariableCheck(prefix + "IsValid") && GlobalVariableGet(prefix + "IsValid") == 1)
   {
      setup.tp1Price = GlobalVariableGet(prefix + "TP1");
      setup.tp2Price = GlobalVariableGet(prefix + "TP2");
      setup.tp3Price = GlobalVariableGet(prefix + "TP3");
      setup.lotSize = GlobalVariableGet(prefix + "InitVol");
      setup.isValid = true;
   }
   return setup;
}

//+------------------------------------------------------------------+
//| Mark TP Level as Hit                                             |
//+------------------------------------------------------------------+
void MarkTPLevelAsHit(ulong ticket, int tpLevel)
{
   string prefix = "SL_ICT_EA_" + IntegerToString(magicNumber) + "_" + IntegerToString(ticket) + "_";
   string varName = prefix + "TP" + IntegerToString(tpLevel) + "Hit";
   GlobalVariableSet(varName, 1);
}

//+------------------------------------------------------------------+
//| Check if TP Level Has Been Hit                                   |
//+------------------------------------------------------------------+
bool HasTPLevelBeenHit(ulong ticket, int tpLevel)
{
   string prefix = "SL_ICT_EA_" + IntegerToString(magicNumber) + "_" + IntegerToString(ticket) + "_";
   string varName = prefix + "TP" + IntegerToString(tpLevel) + "Hit";
   return GlobalVariableCheck(varName) && GlobalVariableGet(varName) == 1;
}

//+------------------------------------------------------------------+
//| Draw Trade Info Lines                                            |
//+------------------------------------------------------------------+
void DrawTradeInfo(double entry, double sl, double tp, color entryClr, color slClr, color tpClr)
{
   string entryLine = "SL_ICT_EA_Level_Entry_" + IntegerToString(magicNumber);
   string slLine = "SL_ICT_EA_Level_SL_" + IntegerToString(magicNumber);
   string tpLine = "SL_ICT_EA_Level_TP_" + IntegerToString(magicNumber);

   ObjectDelete(ChartID(), entryLine);
   ObjectDelete(ChartID(), slLine);
   ObjectDelete(ChartID(), tpLine);

   if(entry > 0)
   {
      ObjectCreate(ChartID(), entryLine, OBJ_HLINE, 0, 0, entry);
      ObjectSetInteger(ChartID(), entryLine, OBJPROP_COLOR, entryClr);
      ObjectSetInteger(ChartID(), entryLine, OBJPROP_STYLE, STYLE_DOT);
      ObjectSetString(ChartID(), entryLine, OBJPROP_TEXT, "Entry");
   }
   if(sl > 0)
   {
      ObjectCreate(ChartID(), slLine, OBJ_HLINE, 0, 0, sl);
      ObjectSetInteger(ChartID(), slLine, OBJPROP_COLOR, slClr);
      ObjectSetInteger(ChartID(), slLine, OBJPROP_STYLE, STYLE_DOT);
      ObjectSetString(ChartID(), slLine, OBJPROP_TEXT, "SL");
   }
   if(tp > 0)
   {
      ObjectCreate(ChartID(), tpLine, OBJ_HLINE, 0, 0, tp);
      ObjectSetInteger(ChartID(), tpLine, OBJPROP_COLOR, tpClr);
      ObjectSetInteger(ChartID(), tpLine, OBJPROP_STYLE, STYLE_DOT);
      ObjectSetString(ChartID(), tpLine, OBJPROP_TEXT, "TP3");
   }
   ChartRedraw();
}

//+------------------------------------------------------------------+
//| Draw Partial TP Level Lines                                      |
//+------------------------------------------------------------------+
void DrawPartialTPLevel(int tpNum, double price)
{
   if(price <= 0 || !ShowTradeLevels) return;
   string lineName = "SL_ICT_EA_Level_TP" + IntegerToString(tpNum) + "_" + IntegerToString(magicNumber);
   ObjectDelete(ChartID(), lineName);
   ObjectCreate(ChartID(), lineName, OBJ_HLINE, 0, 0, price);
   ObjectSetInteger(ChartID(), lineName, OBJPROP_COLOR, TPLevelColor);
   ObjectSetInteger(ChartID(), lineName, OBJPROP_STYLE, STYLE_DASH);
   ObjectSetString(ChartID(), lineName, OBJPROP_TEXT, "TP" + IntegerToString(tpNum));
   ChartRedraw();
}

//+------------------------------------------------------------------+
//| ICT Helper Functions                                             |
//+------------------------------------------------------------------+
double FindNearestLiquidityLevel(bool findHigh)
{
   MqlRates rates[];
   if(CopyRates(_Symbol, _Period, 0, DOL_Lookback_Bars, rates) < DOL_Lookback_Bars) return 0;
   double level = findHigh ? rates[0].high : rates[0].low;
   for(int i = 1; i < DOL_Lookback_Bars && i < ArraySize(rates); i++)
      if(findHigh && rates[i].high > level) level = rates[i].high;
      else if(!findHigh && rates[i].low < level) level = rates[i].low;
   return level;
}

bool CheckLiquiditySweep(double liquidityLevel, ENUM_ORDER_TYPE bias, MqlRates &rates[])
{
   if(liquidityLevel == 0 || ArraySize(rates) < 2) return false;
   double currentPrice = rates[0].close;
   if(bias == ORDER_TYPE_BUY && currentPrice < liquidityLevel && rates[1].low <= liquidityLevel)
      return true;
   if(bias == ORDER_TYPE_SELL && currentPrice > liquidityLevel && rates[1].high >= liquidityLevel)
      return true;
   return false;
}

bool CheckMSS(ENUM_ORDER_TYPE bias, MqlRates &rates[])
{
   if(ArraySize(rates) < 2) return false;
   if(bias == ORDER_TYPE_BUY) return rates[0].close > rates[1].high && rates[1].close < rates[2].open;
   if(bias == ORDER_TYPE_SELL) return rates[0].close < rates[1].low && rates[1].close > rates[2].open;
   return false;
}

void FindFVG(MqlRates &rates[], double &fvgHigh, double &fvgLow)
{
   fvgHigh = 0; fvgLow = 0;
   for(int i = 2; i < ArraySize(rates) - 1 && i < 10; i++)
   {
      if(rates[i].high < rates[i-2].low && rates[i-1].close > rates[i].high)
      { fvgHigh = rates[i-2].low; fvgLow = rates[i].high; break; }
      if(rates[i].low > rates[i-2].high && rates[i-1].close < rates[i].low)
      { fvgHigh = rates[i].low; fvgLow = rates[i-2].high; break; }
   }
}

bool CheckNDOG_NWOG(MqlRates &rates[])
{
   atrHandle = iATR(_Symbol, _Period, 14);
   double atr[];
   if(CopyBuffer(atrHandle, 0, 0, 1, atr) < 1)
   {
      IndicatorRelease(atrHandle);
      return false;
   }
   IndicatorRelease(atrHandle);
   double threshold = atr[0] * NDOG_NWOG_Threshold;
   return (rates[0].high - rates[0].low) <= threshold;
}

double CalculateEntryPrice(double level1, double level2, bool useOTE)
{
   double range = MathAbs(level2 - level1);
   if(!useOTE) return (level1 + level2) / 2.0;
   return level1 < level2 ? level1 + range * OTE_Lower_Level : level1 - range * OTE_Lower_Level;
}

double FindProtectiveStopLoss(MqlRates &rates[], bool isBuy)
{
   if(ArraySize(rates) < 1) return 0;
   double level = isBuy ? rates[0].low : rates[0].high;
   for(int i = 1; i < 10 && i < ArraySize(rates); i++)
      if(isBuy && rates[i].low < level) level = rates[i].low;
      else if(!isBuy && rates[i].high > level) level = rates[i].high;
   return isBuy ? level - pointValue * digitsFactor : level + pointValue * digitsFactor;
}
//+------------------------------------------------------------------+