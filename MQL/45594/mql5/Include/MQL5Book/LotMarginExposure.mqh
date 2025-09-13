//+------------------------------------------------------------------+
//|                                            LotMarginExposure.mqh |
//|                               Copyright (c) 2021-2022, Marketeer |
//|                          https://www.mql5.com/en/users/marketeer |
//+------------------------------------------------------------------+

//+------------------------------------------------------------------+
//| Lot Exposure Margin Level Risk                                   |
//+------------------------------------------------------------------+
namespace LEMLR
{
   // Container for all calculated margin characteristics per symbol
   struct SymbolLotExposureRisk
   {
      double lot;                       // requested lot
      int atrPointsNormalized;          // normalized by tick size
      double atrValue;                  // money value for 1 lot
      double lotFromExposureRaw;        // non-normalized (can be less than mininal lot)
      double lotFromExposure;           // normalized
      double lotFromRiskOfStopLossRaw;  // non-normalized (can be less than mininal lot)
      double lotFromRiskOfStopLoss;     // normalized
      double exposureFromLot;           // per 'lot'
      double marginLevelFromLot;        // per 'lot'
      int lotDigits;                    // digits in normalized lots
   };
   
   bool IsOfKindBuy(const ENUM_ORDER_TYPE type)
   {
      return (type & 1) == 0;
   }
   
   bool IsOfKindSell(const ENUM_ORDER_TYPE type)
   {
      return (type & 1) == 1;
   }
   
   double GetCurrentPrice(const string symbol, const ENUM_ORDER_TYPE type)
   {
      return SymbolInfoDouble(symbol, IsOfKindBuy(type) ? SYMBOL_ASK : SYMBOL_BID);
   }
   
   // Main function to calculate all marginal characteristics for given symbol and money
   bool Estimate(const ENUM_ORDER_TYPE type, const string symbol, const double lot,
      const double price, const double exposure,
      const double riskLevel, const int riskPoints, const ENUM_TIMEFRAMES riskPeriod,
      double money, SymbolLotExposureRisk &r)
   {
      double lot1margin;
      if(!OrderCalcMargin(type, symbol, 1.0,
         price == 0 ? GetCurrentPrice(symbol, type) : price,
         lot1margin))
      {
         Print("OrderCalcMargin ", symbol, " failed: ", _LastError);
         return false;
      }
      if(lot1margin == 0)
      {
         Print("Margin ", symbol, " is zero, ", _LastError);
         return false;
      }
   
      const double tickValue = SymbolInfoDouble(symbol, SYMBOL_TRADE_TICK_VALUE);
      const int pointsInTick = (int)(SymbolInfoDouble(symbol, SYMBOL_TRADE_TICK_SIZE) / SymbolInfoDouble(symbol, SYMBOL_POINT));
      const double pointValue = tickValue / pointsInTick;
      if(pointValue == 0)
      {
         Print(symbol, " is not available (probably yet), ", _LastError);
         return false;
      }
      const int atrPoints = (riskPoints > 0) ? riskPoints :
         (int)(((MathMax(iHigh(symbol, riskPeriod, 1), iHigh(symbol, riskPeriod, 0))
         -  MathMin(iLow(symbol, riskPeriod, 1), iLow(symbol, riskPeriod, 0)))
         / SymbolInfoDouble(symbol, SYMBOL_POINT)));
      r.atrPointsNormalized = atrPoints / pointsInTick * pointsInTick; // rounding
     
      if(r.atrPointsNormalized == 0)
      {
         Print(symbol, " ATR on " , EnumToString(riskPeriod), " is not available (probably yet), ", _LastError);
         return false;
      }
     
      r.atrValue = r.atrPointsNormalized * pointValue;
     
      double usedMargin = 0;
      if(money == 0)
      {
         money = AccountInfoDouble(ACCOUNT_MARGIN_FREE);
         usedMargin = AccountInfoDouble(ACCOUNT_MARGIN);
      }
   
      r.lotFromExposureRaw = money * exposure / 100.0 / lot1margin;
      r.lotFromExposure = NormalizeLot(symbol, r.lotFromExposureRaw);
     
      r.lotFromRiskOfStopLossRaw = money * riskLevel / 100.0 / (pointValue * r.atrPointsNormalized);
      r.lotFromRiskOfStopLoss = NormalizeLot(symbol, r.lotFromRiskOfStopLossRaw);
   
      r.lot = lot <= 0 ? SymbolInfoDouble(symbol, SYMBOL_VOLUME_MIN) : lot;
      double margin = r.lot * lot1margin;

      #ifdef DebugLogging
      {
         Print((float)r.lot, " ", symbol, "=", margin, " ", lot1margin, " ",
            EnumToString((ENUM_SYMBOL_CALC_MODE)SymbolInfoInteger(symbol, SYMBOL_TRADE_CALC_MODE)),
            " ", (float)pointValue);
      }
      #endif
     
      if(lot < 0)
      {
         margin = r.lotFromRiskOfStopLossRaw * lot1margin;
      }
     
      r.exposureFromLot = (margin + usedMargin) / money * 100.0;
      r.marginLevelFromLot = margin > 0 ? money / (margin + usedMargin) * 100.0 : 0;
      r.lotDigits = (int)MathLog10(1.0 / SymbolInfoDouble(symbol, SYMBOL_VOLUME_MIN));
     
      return true;
   }
   
   // Normalize lot in double according to symbol specs
   double NormalizeLot(const string symbol, const double lot)
   {
      const double stepLot = SymbolInfoDouble(symbol, SYMBOL_VOLUME_STEP);
      if(stepLot == 0)
      {
         Print("LOTSTEP IS ZERO");
         return 0;
      }
     
      const double newLotsRounded = MathFloor(lot / stepLot) * stepLot;
   
      const double minLot = SymbolInfoDouble(symbol, SYMBOL_VOLUME_MIN);
      if(minLot == 0)
      {
         Print("MINLOT IS ZERO ", symbol);
         return 0;
      }
   
      if(newLotsRounded < minLot)
      {
         return 0;
      }
     
      const double maxLot = SymbolInfoDouble(symbol, SYMBOL_VOLUME_MAX);
      if(newLotsRounded > maxLot) return maxLot;
     
      return newLotsRounded;
   }
};
//+------------------------------------------------------------------+
