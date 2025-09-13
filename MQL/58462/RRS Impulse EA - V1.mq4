//+------------------------------------------------------------------+
//|                                                           RRS EA |
//|                               Copyright 2025, RRS Impulse EA |
//|                                             rajeeevrrs@gmail.com |
//+------------------------------------------------------------------+
#property copyright "RRS Impulse EA"
#property link      "https://t.me/rajeevrrs"
#property strict
#property version   "1.02"

//+------------------------------------------------------------------+
//| EA Inputs                                                        |
//+------------------------------------------------------------------+

extern string __TradingIndicator__ = "***Trading Indicator***";
enum TradingIndicator_enum {RSI, Stochastic, BollingerBands, RSI_Stochastic_BollingerBands};
extern TradingIndicator_enum Trading_Indicator = RSI;

enum TradeDirection_enum {Trend, CounterTrend};
extern TradeDirection_enum TradeDirection = CounterTrend;

enum SignalStrength_enum {NormalSignal, NormalMultiTimeFrame, StrongSignal, VeryStrongSignal};
extern SignalStrength_enum SignalStrength = NormalSignal;

extern string __LotSettings__ = "***Lot Settings***";
extern double minLot_Size = 0.01;
extern double maxLot_Size = 0.50;

extern string __OrderSettings__ = "***Order Settings***";
extern int TakeProfit = 100;
extern int StopLoss = 200;

extern string __TrailingSettings__ = "***Trailing Settings***";
extern int Trailing_Start = 50;
extern int Trailing_Gap = 50;

extern string __RestricationSettings__ = "***Restrication Settings***";
extern int maxSpread = 100;
extern int Slippage = 3;
extern int MaxOpenTrade = 10;

extern string __RSI_Indicator__ = "***RSI Indicator Settings***";
extern ENUM_TIMEFRAMES RSI_TimeFrame = PERIOD_CURRENT;
extern ENUM_APPLIED_PRICE RSI_AppliedPrice = PRICE_CLOSE;
extern int RSI_Period = 14;
extern int RSI_shift = 0;
extern int RSI_UpLevel = 80;
extern int RSI_BelowLevel = 20;

extern string __Stochastic_Indicator__ = "***Stochastic Indicator Settings***";
extern ENUM_TIMEFRAMES Stochastic_TimeFrame = PERIOD_CURRENT;
extern ENUM_MA_METHOD Stochastic_Method = MODE_SMA;
extern int Stochastic_Kperiod = 10;
extern int Stochastic_Dperiod = 3;
extern int Stochastic_Slowing = 3;
extern int Stochastic_PriceField = 0;
extern int Stochastic_Shift = 0;
extern int Stochastic_UpLevel = 80;
extern int Stochastic_BelowLevel = 20;

extern string __BollingerBands_Indicator__ = "***Bollinger Bands Indicator Settings***";
extern ENUM_TIMEFRAMES BollingerBands_TimeFrame = PERIOD_CURRENT;
extern ENUM_APPLIED_PRICE BollingerBands_AppliedPrice = PRICE_CLOSE;
extern int BollingerBands_Period = 20;
extern double BollingerBands_deviation = 2.0;
extern int BollingerBands_BandShift = 0;
extern int BollingerBands_Shift = 0;

extern string __Risk_Management__ = "***Risk Management***";
enum RiskInMoneyMode_enum {FixedMoney, BalancePercentage};
extern RiskInMoneyMode_enum Risk_In_Money_Type = BalancePercentage;
extern double Money_In_Risk = 5.0;

extern string __Currencies__ = "***Assest Management***";
extern string Trade_Currencies = "USD,GBP,AUD,CAD,JPY,XAU,XAG,EUR,CHF,SDG,HKD,NZD,BTC";

extern string __ExpertAdvisor__ = "***EA Settings***";
extern int Magic = 1000;
extern string EA_Notes = "Note For Your Reference";

//Timezone
int localHours = (TimeLocal() - TimeGMT()) / 3600;
int localMinutes = ((TimeLocal() - TimeGMT()) % 3600) / 60;
int brokerHours = (TimeCurrent() - TimeGMT()) / 3600;
int brokerMinutes = ((TimeCurrent() - TimeGMT()) % 3600) / 60;


//+------------------------------------------------------------------+
//| RRS Defined                                                      |
//+------------------------------------------------------------------+
//Int
int gBuyMagic, gSellMagic;
int OrderCount_BuyMagicOPBUY, OrderCount_SellMagicOPSELL, OrderCount_Symbol_OPBUY, OrderCount_Symbol_OPSELL;
int BuySellRandomMath;
int gSymbolIndex = 0;
int gSymbolRandomIndex = 0;
int gTimeFrameIndex = 0;
int gTimeFrameIndexForSymbol = 0;

//String
string gTradeComment;
string gRandomSymbol;
string buyOpenTrade_Symbol, sellOpenTrade_Symbol;
string gSignalStatus;
string cTimeFrame, cTimeFramePrint, cRSITimeframe, cSTOTimeFrame, cBBTimeFrame, cRSIAppliedPrice, cSTOMethod, cSTOPriceField, cBBAppliedPrice, cTradeDirection, cTrading_Indicator, cSignalStrength;

//Double
double gSymbolEA_FloatingPL, gBuyFloatingPL, gSellFloatingPL;
double gTargeted_Revenue, gRisk_Money;
double OrderSend_StopLoss, OrderSend_TakeProfit;
double gRandomLotSize;

//+------------------------------------------------------------------+
//| OnInit                                                           |
//+------------------------------------------------------------------+
int OnInit()
  {
   gBuyMagic    = Magic + 1;
   gSellMagic   = Magic + 11;

//Trade Comments
   string tc_TradeDirection = TradeDirection == Trend ? "T" : "CT";
   string tc_Trading_Indicator = Trading_Indicator == RSI ? "RSI" : Trading_Indicator == BollingerBands ? "BB" : Trading_Indicator == Stochastic ? "STO" : "RSB";
   string tc_SignalStrength = SignalStrength == NormalSignal ? "NS" : SignalStrength == NormalMultiTimeFrame ? "NMTF" : SignalStrength == StrongSignal ? "SS" : "VSS";

   gTradeComment = tc_Trading_Indicator + "+" + tc_TradeDirection + "+" + tc_SignalStrength + "+RRS";

   cRSITimeframe =
      RSI_TimeFrame == PERIOD_M1  ? "1 Minute"  :
      RSI_TimeFrame == PERIOD_M5  ? "5 Minutes" :
      RSI_TimeFrame == PERIOD_M15 ? "15 Minutes" :
      RSI_TimeFrame == PERIOD_M30 ? "30 Minutes" :
      RSI_TimeFrame == PERIOD_H1  ? "1 Hour" :
      RSI_TimeFrame == PERIOD_H4  ? "4 Hours" :
      RSI_TimeFrame == PERIOD_D1  ? "Daily" :
      RSI_TimeFrame == PERIOD_W1  ? "Weekly" :
      RSI_TimeFrame == PERIOD_MN1 ? "Monthly" : "Current";

   cSTOTimeFrame =
      Stochastic_TimeFrame == PERIOD_M1  ? "1 Minute"  :
      Stochastic_TimeFrame == PERIOD_M5  ? "5 Minutes" :
      Stochastic_TimeFrame == PERIOD_M15 ? "15 Minutes" :
      Stochastic_TimeFrame == PERIOD_M30 ? "30 Minutes" :
      Stochastic_TimeFrame == PERIOD_H1  ? "1 Hour" :
      Stochastic_TimeFrame == PERIOD_H4  ? "4 Hours" :
      Stochastic_TimeFrame == PERIOD_D1  ? "Daily" :
      Stochastic_TimeFrame == PERIOD_W1  ? "Weekly" :
      Stochastic_TimeFrame == PERIOD_MN1 ? "Monthly" : "Current";

   cBBTimeFrame =
      BollingerBands_TimeFrame == PERIOD_M1  ? "1 Minute"  :
      BollingerBands_TimeFrame == PERIOD_M5  ? "5 Minutes" :
      BollingerBands_TimeFrame == PERIOD_M15 ? "15 Minutes" :
      BollingerBands_TimeFrame == PERIOD_M30 ? "30 Minutes" :
      BollingerBands_TimeFrame == PERIOD_H1  ? "1 Hour" :
      BollingerBands_TimeFrame == PERIOD_H4  ? "4 Hours" :
      BollingerBands_TimeFrame == PERIOD_D1  ? "Daily" :
      BollingerBands_TimeFrame == PERIOD_W1  ? "Weekly" :
      BollingerBands_TimeFrame == PERIOD_MN1 ? "Monthly" : "Current";

   cRSIAppliedPrice = RSI_AppliedPrice == PRICE_CLOSE  ? "Close Price" :
                      RSI_AppliedPrice == PRICE_OPEN   ? "Open Price" :
                      RSI_AppliedPrice == PRICE_HIGH   ? "High Price" :
                      RSI_AppliedPrice == PRICE_LOW    ? "Low Price" :
                      RSI_AppliedPrice == PRICE_MEDIAN ? "Median Price" :
                      RSI_AppliedPrice == PRICE_TYPICAL? "Typical Price" :
                      RSI_AppliedPrice == PRICE_WEIGHTED? "Weighted Price" : "Unknown Price Type";

   cBBAppliedPrice = BollingerBands_AppliedPrice == PRICE_CLOSE   ? "Close Price" :
                     BollingerBands_AppliedPrice == PRICE_OPEN    ? "Open Price" :
                     BollingerBands_AppliedPrice == PRICE_HIGH    ? "High Price" :
                     BollingerBands_AppliedPrice == PRICE_LOW     ? "Low Price" :
                     BollingerBands_AppliedPrice == PRICE_MEDIAN  ? "Median Price" :
                     BollingerBands_AppliedPrice == PRICE_TYPICAL ? "Typical Price" :
                     BollingerBands_AppliedPrice == PRICE_WEIGHTED? "Weighted Price" : "Unknown Price Type";


   cSTOMethod = Stochastic_Method == MODE_SMA  ? "Simple Moving Average (SMA)" :
                Stochastic_Method == MODE_EMA  ? "Exponential Moving Average (EMA)" :
                Stochastic_Method == MODE_SMMA ? "Smoothed Moving Average (SMMA)" :
                Stochastic_Method == MODE_LWMA ? "Linear Weighted Moving Average (LWMA)" : "Unknown MA Method";

   cSTOPriceField = Stochastic_PriceField == 0 ? "Low/High" :
                    Stochastic_PriceField == 1 ? "Close/Close" : "Unknown Mode";

   cTradeDirection = TradeDirection == Trend ? "Trend" : "CounterTrend";
   cTrading_Indicator = Trading_Indicator == RSI ? "RSI" : Trading_Indicator == BollingerBands ? "Bollinger Band" : Trading_Indicator == Stochastic ? "Stochastic" : "RSI+Stochastic+Bollinger Band";
   cSignalStrength = SignalStrength == NormalSignal ? "Normal Signal" : SignalStrength == NormalMultiTimeFrame ? "Normal MultiTime Frame" : SignalStrength == StrongSignal ? "Strong Signal" : "Very Strong Signal";


   return(INIT_SUCCEEDED);
  }


//+------------------------------------------------------------------+
//| On Deinit                                                        |
//+------------------------------------------------------------------+
int deinit()
  {
   ObjectsDeleteAll(0,"#",-1,-1);
   return (0);
  }

//+------------------------------------------------------------------+
//| OnTick                                                           |
//+------------------------------------------------------------------+
void OnTick()
  {
//Pre-defined OnTick Value
   MathSrand(GetTickCount());
   BuySellRandomMath = MathRand() % 4;
   gRandomSymbol = (SignalStrength != NormalMultiTimeFrame) ? randomsymbol() : (gTimeFrameIndexForSymbol == 0) ? randomsymbol() : gRandomSymbol;
   gRandomLotSize = RandomLotSize();
   OrderCount_BuyMagicOPBUY = trade_count_ordertype(OP_BUY, gBuyMagic);
   OrderCount_SellMagicOPSELL = trade_count_ordertype(OP_SELL, gSellMagic);
   OrderCount_Symbol_OPBUY = TradeCountBySymbol(OP_BUY, gBuyMagic, gRandomSymbol);
   OrderCount_Symbol_OPSELL = TradeCountBySymbol(OP_SELL, gSellMagic, gRandomSymbol);
   buyOpenTrade_Symbol = GetAllOpenTradeSymbols(gBuyMagic, OP_BUY);
   sellOpenTrade_Symbol = GetAllOpenTradeSymbols(gSellMagic, OP_SELL);
   TrendCounterSignal(); //Signal Function

//Trailing TP
   if(Trailing_Gap > 0 && Trailing_Start > 0)
     {
      if(OrderCount_BuyMagicOPBUY >= 1)
         TrailingStopLoss(gBuyMagic, buyOpenTrade_Symbol);
      if(OrderCount_SellMagicOPSELL >= 1)
         TrailingStopLoss(gSellMagic, sellOpenTrade_Symbol);
     }

//Order Placement
   if((OrderCount_BuyMagicOPBUY + OrderCount_SellMagicOPSELL) < MaxOpenTrade && MarketInfo(gRandomSymbol, MODE_SPREAD) < maxSpread)
      NewOrderSend();

//Financial Value
   gBuyFloatingPL = CalculateTradeFloating(gBuyMagic);
   gSellFloatingPL = CalculateTradeFloating(gSellMagic);
   gSymbolEA_FloatingPL = gBuyFloatingPL + gSellFloatingPL;

//Risk In Money
   if(Risk_In_Money_Type == BalancePercentage)
      gRisk_Money =(-1.0 * AccountBalance() * (Money_In_Risk * 0.01));
   else
      gRisk_Money = (-1.0 * Money_In_Risk);

   if(gSymbolEA_FloatingPL <= gRisk_Money)
     {
      CloseOpenAndPendingTrades(gBuyMagic);
      CloseOpenAndPendingTrades(gSellMagic);
      Print("Risk Management => Successfully Closed");
     }

   ChartComment(); //Chart Comment to show details
// --------- OnTick End ------------ //
  }

//+------------------------------------------------------------------+
//| Doube Side Buy And Sell Order                                    |
//+------------------------------------------------------------------+
void NewOrderSend()
  {
   double iPips = MarketInfo(gRandomSymbol, MODE_POINT);
   int iStopLevel = MarketInfo(gRandomSymbol, MODE_STOPLEVEL) < MarketInfo(gRandomSymbol, MODE_SPREAD) ? MarketInfo(gRandomSymbol, MODE_SPREAD) + 2 : MarketInfo(gRandomSymbol, MODE_STOPLEVEL) + 2;
   double iBID = MarketInfo(gRandomSymbol, MODE_BID);
   double iASK = MarketInfo(gRandomSymbol, MODE_ASK);

   if(OrderCount_Symbol_OPBUY == 0 && CheckMoneyForTrade(gRandomSymbol, gRandomLotSize, OP_BUY) && ((TradeDirection == Trend && (gSignalStatus == "RSI_UP" || gSignalStatus == "Stochastic_UP" || gSignalStatus == "BB_UP" || gSignalStatus == "RSB_UP")) || (TradeDirection == CounterTrend && (gSignalStatus == "RSI_BELOW" || gSignalStatus == "Stochastic_BELOW" || gSignalStatus == "BB_BELOW" || gSignalStatus == "RSB_BELOW"))))
     {
      OrderSend_StopLoss = (StopLoss > 0) ? iASK - MathMax(StopLoss, iStopLevel) * iPips : 0;
      OrderSend_TakeProfit = (TakeProfit > 0) ? iASK + MathMax(TakeProfit, iStopLevel) * iPips : 0;
      ResetLastError();
      if(OrderSend(gRandomSymbol, OP_BUY, gRandomLotSize, iASK, Slippage, OrderSend_StopLoss, OrderSend_TakeProfit, gTradeComment, gBuyMagic, 0, clrNONE) == -1)
         Print(gRandomSymbol + " >> Buy Order => Error Code : " + GetLastError());
     }

   if(OrderCount_Symbol_OPSELL == 0 && CheckMoneyForTrade(gRandomSymbol, gRandomLotSize, OP_SELL) && ((TradeDirection == CounterTrend && (gSignalStatus == "RSI_UP" || gSignalStatus == "Stochastic_UP" || gSignalStatus == "BB_UP" || gSignalStatus == "RSB_UP")) || (TradeDirection == Trend && (gSignalStatus == "RSI_BELOW" || gSignalStatus == "Stochastic_BELOW" || gSignalStatus == "BB_BELOW" || gSignalStatus == "RSB_BELOW"))))
     {
      OrderSend_StopLoss = (StopLoss > 0) ? iBID + MathMax(StopLoss, iStopLevel) * iPips : 0;
      OrderSend_TakeProfit = (TakeProfit > 0) ? iBID - MathMax(TakeProfit, iStopLevel) * iPips : 0;
      ResetLastError();
      if(OrderSend(gRandomSymbol, OP_SELL, gRandomLotSize, iBID, Slippage, OrderSend_StopLoss, OrderSend_TakeProfit, gTradeComment, gSellMagic, 0, clrNONE) == -1)
         Print(gRandomSymbol + " >> Sell Order => Error Code : " + GetLastError());
     }
  }

// Calculate Trade profits and Loss
double CalculateTradeFloating(int CalculateTradeFloating_Magic)
  {
   double CalculateTradeFloating_Value = 0;
   for(int CalculateTradeFloating_i = 0; CalculateTradeFloating_i < OrdersTotal(); CalculateTradeFloating_i++)
     {
      OrderSelect(CalculateTradeFloating_i, SELECT_BY_POS, MODE_TRADES);
      if(OrderType() != OP_BUY && OrderType() != OP_SELL)
         continue;
      if(CalculateTradeFloating_Magic == OrderMagicNumber())
         CalculateTradeFloating_Value += OrderProfit() + OrderSwap() + OrderCommission();
     }
   return CalculateTradeFloating_Value;
  }

// Trade closing based on Magic number
void CloseOpenAndPendingTrades(int trade_close_magic)
  {
   for(int pos_0 = OrdersTotal() - 1; pos_0 >= 0; pos_0--)
     {
      OrderSelect(pos_0, SELECT_BY_POS, MODE_TRADES);
      if(OrderMagicNumber() != trade_close_magic)
         continue;

      if(OrderType() != OP_BUY && OrderType() != OP_SELL)
        {
         ResetLastError();
         if(!OrderDelete(OrderTicket()))
            Print(__FUNCTION__ " => Pending Order failed to close, error code:", GetLastError());
        }
      else
        {
         ResetLastError();
         if(OrderType() == OP_BUY)
           {
            if(!OrderClose(OrderTicket(), OrderLots(), OrderClosePrice(), Slippage, clrNONE))
               Print(__FUNCTION__ " => Buy Order failed to close, error code:", GetLastError());
           }
         if(OrderType() == OP_SELL)
           {
            if(!OrderClose(OrderTicket(), OrderLots(), OrderClosePrice(), Slippage, clrNONE))
               Print(__FUNCTION__ " => Sell Order failed to close, error code:", GetLastError());
           }
        }
     }
  }


// Trade Counting based on Order Type and Magic Number
int trade_count_ordertype(int trade_count_ordertype_value, int trade_count_ordertype_magic)
  {
   int count_4 = 0;
   for(int pos_8 = 0; pos_8 < OrdersTotal(); pos_8++)
     {
      OrderSelect(pos_8, SELECT_BY_POS, MODE_TRADES);
      if(OrderMagicNumber() != trade_count_ordertype_magic)
         continue;
      if(trade_count_ordertype_value == OrderType())
         count_4++;
     }
   return count_4;
  }

//+------------------------------------------------------------------+
//|      Trade Count Symbol                                          |
//+------------------------------------------------------------------+
int TradeCountBySymbol(int trade_count_ordertype_value, int trade_count_ordertype_magic, string TradeOrderSymbolCount)
  {
   int count_4 = 0;
   for(int pos_8 = 0; pos_8 < OrdersTotal(); pos_8++)
     {
      OrderSelect(pos_8, SELECT_BY_POS, MODE_TRADES);
      if(OrderMagicNumber() != trade_count_ordertype_magic && OrderSymbol() != TradeOrderSymbolCount)
         continue;
      if(trade_count_ordertype_value == OrderType())
         count_4++;
     }
   return count_4;
  }

// Chart Comment Status
void ChartComment()
  {
   string c_Risk_Type, c_risk_t;

   if(Risk_In_Money_Type == BalancePercentage)
     {
      c_risk_t = "Balance Percentage";
      c_Risk_Type = "(Percentage : " + Money_In_Risk + ") => (Money In Risk : " + -1 * gRisk_Money + ")";
     }
   else
     {
      c_risk_t = "Fixed Money";
      c_Risk_Type = "(Money In Risk : " + -1 * gRisk_Money + ")";
     }


   Comment("                                               ---------------------------------------------"
           "\n                                             :: ===>RRS Impulse EA<==="
           "\n                                             ------------------------------------------------" +
           "\n                                             :: Currency Pair                  : (Checking : " + gRandomSymbol + ") |:| (Buy Operational : " + buyOpenTrade_Symbol + ") |:| (Sell Operational : " + sellOpenTrade_Symbol + ")" +
           "\n                                             :: " + gRandomSymbol + " Info                  : (Spread : " + MarketInfo(gRandomSymbol, MODE_SPREAD) + ") |:| (Stop Level : " + MarketInfo(gRandomSymbol, MODE_STOPLEVEL) + ") |:| (Freeze Level : " + MarketInfo(gRandomSymbol, MODE_FREEZELEVEL) + ")" +
           "\n                                             ------------------------------------------------" +
           "\n                                             :: Indicator                         : " + cTrading_Indicator +
           "\n                                             :: Trade Direction                : " + cTradeDirection +
           "\n                                             :: Signal Strength                : " + cSignalStrength +
           "\n                                             ------------------------------------------------" +

           "\n                                             :: Take Profit                      : " + TakeProfit +
           "\n                                             :: Stop Loss                      : " + StopLoss +
           "\n                                             :: Lot Size                         : (Min Lot : " + minLot_Size + ") |:| (Max Lot : " + maxLot_Size + ") |:| (Random Lot : " + gRandomLotSize + ")" +
           "\n                                             :: Trailing                          : (Start : " + Trailing_Start + ") |:| (Gap : " + Trailing_Gap + ")" +
           "\n                                             :: Restrication                   : (Maximum Open Trade : " + MaxOpenTrade + ") |:| (Maximum Spread : " + maxSpread + ") |:| (Slippage : " + Slippage + ")" +
           "\n                                             :: Risk Management          : (Risk Type : " + c_risk_t + ") |:| "  + c_Risk_Type  +
           "\n                                             ------------------------------------------------" +
           cTimeFramePrint +
           "\n                                             :: RSI                             : (TimeFrame : " + cRSITimeframe + ") |:| (Applied Price : " + cRSIAppliedPrice + ") |:| (Period : " + RSI_Period + ") |:| (Shift : " + RSI_shift + ") |:| (UP Level : " + RSI_UpLevel + ") |:| (Below Level : " + RSI_BelowLevel + ")" +
           "\n                                             :: Stochastic                      : (TimeFrame : " + cSTOTimeFrame + ") |:| (Method : " + cSTOMethod + ") |:| (Kperiod : " + Stochastic_Kperiod + ") |:| (Dperiod : " + Stochastic_Dperiod + ") |:| (Slowing : " + Stochastic_Slowing + ") |:| (Price Field : " + cSTOPriceField + ") |:| (Shift : " + Stochastic_Shift + ") |:| (UP Level : " + Stochastic_UpLevel + ") |:| (Below Level : " + Stochastic_BelowLevel + ")" +
           "\n                                             :: Bollinger Bands                 : (TimeFrame : " + cBBTimeFrame + ") |:| (Applied Price : " + cBBAppliedPrice + ") |:| (Period : " + BollingerBands_Period + ") |:| (Deviation : " + BollingerBands_deviation + ") |:| (Band Shift : " + BollingerBands_BandShift + ") |:| (Shift : " + BollingerBands_Shift + ")" +
           "\n                                             ------------------------------------------------" +
           "\n                                             :: Magic Number               : " + Magic + " => (Buy Magic : " + gBuyMagic + ") |:| (Sell Magic : " + gSellMagic + ")" +
           "\n                                             :: Timezone                      : (Local PC : " + localHours + ":" + localMinutes + ")" + " |:| (Broker Timezone : " + brokerHours + ":" + brokerMinutes + ")" +
           "\n                                             :: Notes                           : " + EA_Notes +
           "\n                                             ------------------------------------------------" +
           "\n                                             :: Email                           : rajeeevrrs@gmail.com " +
           "\n                                             :: Telegram                     : @rajeevrrs " +
           "\n                                             :: Skype                         : rajeev-rrs " +
           "\n                                             ------------------------------------------------");
  }

//+--------------------------------------------------------------------+
// Trailing SL                                                         +
//+--------------------------------------------------------------------+
void TrailingStopLoss(int TrailingStopLoss_magic, string TrailingSymbol)
  {
// Loop through all open orders
   double TrailingStopLoss_entryPrice;
   for(int i = OrdersTotal() - 1; i >= 0; i--)
     {
      if(OrderSelect(i, SELECT_BY_POS, MODE_TRADES))
        {
         TrailingStopLoss_entryPrice = OrderOpenPrice();
         if(OrderMagicNumber() == TrailingStopLoss_magic && OrderType() == OP_BUY && OrderSymbol() == TrailingSymbol)
           {
            int BuyTrailingGap = MathMax(MathMax(MarketInfo(buyOpenTrade_Symbol, MODE_STOPLEVEL) + 2, MarketInfo(buyOpenTrade_Symbol, MODE_SPREAD) + 2), Trailing_Gap);
            double gPips_Buytrail = MarketInfo(buyOpenTrade_Symbol, MODE_POINT);
            if(MarketInfo(buyOpenTrade_Symbol, MODE_BID) - (TrailingStopLoss_entryPrice + Trailing_Start * gPips_Buytrail) > gPips_Buytrail * BuyTrailingGap)
              {
               if(OrderStopLoss() < MarketInfo(buyOpenTrade_Symbol, MODE_BID) - gPips_Buytrail * BuyTrailingGap || OrderStopLoss() == 0)
                 {
                  ResetLastError();
                  RefreshRates();
                  if(!OrderModify(OrderTicket(), OrderOpenPrice(), MarketInfo(buyOpenTrade_Symbol, MODE_BID) - gPips_Buytrail * BuyTrailingGap, OrderTakeProfit(), 0, clrNONE))
                     Print(__FUNCTION__ + " => " + buyOpenTrade_Symbol + " : Buy Order Error Code : " + GetLastError());
                 }
              }
           }

         if(OrderMagicNumber() == TrailingStopLoss_magic && OrderType() == OP_SELL && OrderSymbol() == TrailingSymbol)
           {
            int SellTrailingGap = MathMax(MathMax(MarketInfo(sellOpenTrade_Symbol, MODE_STOPLEVEL) + 2, MarketInfo(sellOpenTrade_Symbol, MODE_SPREAD) + 2), Trailing_Gap);
            double gPips_SellTrail = MarketInfo(sellOpenTrade_Symbol, MODE_POINT);
            if((TrailingStopLoss_entryPrice - Trailing_Start * gPips_SellTrail) - MarketInfo(sellOpenTrade_Symbol, MODE_ASK) > gPips_SellTrail * SellTrailingGap)
              {
               if(OrderStopLoss() > MarketInfo(sellOpenTrade_Symbol, MODE_ASK) + gPips_SellTrail * SellTrailingGap || OrderStopLoss() == 0)
                 {
                  ResetLastError();
                  RefreshRates();
                  if(!OrderModify(OrderTicket(), OrderOpenPrice(), MarketInfo(sellOpenTrade_Symbol, MODE_ASK) + gPips_SellTrail * SellTrailingGap, OrderTakeProfit(), 0, clrNONE))
                     Print(__FUNCTION__ + " => " + sellOpenTrade_Symbol + " : Sell Order Error Code : " + GetLastError());
                 }
              }
           }
        }
     }
  }


//+------------------------------------------------------------------+
//|    Check Balance and Margin                                      |
//+------------------------------------------------------------------+
bool CheckMoneyForTrade(string symb, double lots,int type)
  {
   double free_margin=AccountFreeMarginCheck(symb,type, lots);
//-- if there is not enough money
   if(free_margin<0)
     {
      Print("Not enough money to trade");
      return(false);
     }
//--- checking successful
   return(true);
  }
//+------------------------------------------------------------------+
//|  Random Symbol                                                   |
//+------------------------------------------------------------------+
string randomsymbol()
  {
   string randomsymbol_pairs[];
   int count = StringSplit(Trade_Currencies, ',', randomsymbol_pairs);

   int totalSymbols = SymbolsTotal(true);  // Include all Market Watch symbols
   string validSymbols[];

   for(int i = 0; i < totalSymbols; i++)
     {
      string symbolName = SymbolName(i, true);
      bool found = false;

      // Check if the symbol contains a valid pair
      for(int j = 0; j < count; j++)
        {
         for(int k = 0; k < count; k++)
           {
            if(j != k)  // Prevent matching same symbols (e.g., USDUSD)
              {
               if(StringFind(symbolName, randomsymbol_pairs[j]) != -1 &&
                  StringFind(symbolName, randomsymbol_pairs[k]) != -1)
                 {
                  found = true;
                  break;  // Break inner loop if a match is found
                 }
              }
           }
         if(found)
            break;  // Exit outer loop to avoid duplicate addition
        }

      // Add only if not already in the list
      if(found)
        {
         bool alreadyAdded = false;
         for(int m = 0; m < ArraySize(validSymbols); m++)
           {
            if(validSymbols[m] == symbolName)
              {
               alreadyAdded = true;
               break;
              }
           }

         if(!alreadyAdded)
           {
            ArrayResize(validSymbols, ArraySize(validSymbols) + 1);
            validSymbols[ArraySize(validSymbols) - 1] = symbolName;
           }
        }
     }

   if(ArraySize(validSymbols) > 0)
     {
      gSymbolRandomIndex = gSymbolRandomIndex < ArraySize(validSymbols) ? gSymbolRandomIndex : 0;
      string SelectedAssetSymbol = validSymbols[gSymbolRandomIndex];
      gSymbolRandomIndex++;
      return SelectedAssetSymbol;
     }
   return ""; // Return empty if no valid symbol found
  }


//+------------------------------------------------------------------+
//|   Get Symbol by Magic                                            |
//+------------------------------------------------------------------+
string GetAllOpenTradeSymbols(int OrderMagicNumberr, int OrderTypee)
  {
   string GetAllOpenTradeSymbols_symbols = "";
   string GetAllOpenTradeSymbols_symbolsArray[];
   for(int i = 0; i < OrdersTotal(); i++)
     {
      if(OrderSelect(i, SELECT_BY_POS, MODE_TRADES))  // Select order
        {
         if(OrderMagicNumber() == OrderMagicNumberr && OrderType() == OrderTypee)
           {
            string GetAllOpenTradeSymbols_symbol = OrderSymbol();
            if(StringFind(GetAllOpenTradeSymbols_symbols, GetAllOpenTradeSymbols_symbol) == -1)  // Check if symbol is already added
              {
               ArrayResize(GetAllOpenTradeSymbols_symbolsArray, ArraySize(GetAllOpenTradeSymbols_symbolsArray) + 1);
               GetAllOpenTradeSymbols_symbolsArray[ArraySize(GetAllOpenTradeSymbols_symbolsArray) - 1] = OrderSymbol();
              }
           }
        }
     }
// Return a random symbol from the valid list
   if(ArraySize(GetAllOpenTradeSymbols_symbolsArray) > 0)
     {
      gSymbolIndex = gSymbolIndex < ArraySize(GetAllOpenTradeSymbols_symbolsArray) ? gSymbolIndex : 0;
      string SelectedOpenSymbol = GetAllOpenTradeSymbols_symbolsArray[gSymbolIndex];
      gSymbolIndex++;
      return SelectedOpenSymbol;
     }
   return "";
  }

//+------------------------------------------------------------------+
//|        Random Lot size                                           |
//+------------------------------------------------------------------+
double RandomLotSize()
  {
// Retrieve lot constraints
   double minLot  = MarketInfo(gRandomSymbol, MODE_MINLOT);
   double maxLot  = MarketInfo(gRandomSymbol, MODE_MAXLOT);
   double lotStep = MarketInfo(gRandomSymbol, MODE_LOTSTEP);

// Ensure lotStep is valid
   if(lotStep <= 0)
      lotStep = 0.01; // Default to a small valid step

// Generate a random value within the specified range
   double LotrandomValue = minLot_Size + (maxLot_Size - minLot_Size) * MathRand() / 32767.0;

// Adjust to the nearest lot step
   LotrandomValue = minLot + lotStep * MathRound((LotrandomValue - minLot) / lotStep);

// Final check to ensure it remains within bounds
   LotrandomValue = MathMax(minLot, MathMin(LotrandomValue, maxLot));

// Normalize to 2 decimal places
   return NormalizeDouble(LotrandomValue, 2);
  }

//+------------------------------------------------------------------+
//|   Signal Function                                                |
//+------------------------------------------------------------------+
void TrendCounterSignal()
  {
   ENUM_TIMEFRAMES SelectedTimeFrame = PERIOD_D1;
   if(SignalStrength == NormalMultiTimeFrame)
     {
      ENUM_TIMEFRAMES TimeFrameArray[] = {PERIOD_M1, PERIOD_M5, PERIOD_M15, PERIOD_M30, PERIOD_H1, PERIOD_H4};
      gTimeFrameIndex = gTimeFrameIndex < ArraySize(TimeFrameArray) ? gTimeFrameIndex : 0;
      SelectedTimeFrame = TimeFrameArray[gTimeFrameIndex];
      gTimeFrameIndexForSymbol = gTimeFrameIndex;
      gTimeFrameIndex++;


      cTimeFrame =
         SelectedTimeFrame == PERIOD_M1  ? "1 Minute"  :
         SelectedTimeFrame == PERIOD_M5  ? "5 Minutes" :
         SelectedTimeFrame == PERIOD_M15 ? "15 Minutes" :
         SelectedTimeFrame == PERIOD_M30 ? "30 Minutes" :
         SelectedTimeFrame == PERIOD_H1  ? "1 Hour" :
         SelectedTimeFrame == PERIOD_H4  ? "4 Hours" : "Checking Multiple TimeFrame";
      cTimeFramePrint = "\n                                             :: TimeFrame                   : " + cTimeFrame;
     }

   gSignalStatus = "NoSignal";
   if(SignalStrength == NormalSignal) //Normal Signal
     {
      if(Trading_Indicator == RSI)
        {
         if(iRSI(gRandomSymbol,RSI_TimeFrame,RSI_Period,RSI_AppliedPrice,RSI_shift) > RSI_UpLevel)
            gSignalStatus = "RSI_UP";
         if(iRSI(gRandomSymbol,RSI_TimeFrame,RSI_Period,RSI_AppliedPrice,RSI_shift) < RSI_BelowLevel)
            gSignalStatus = "RSI_BELOW";
        }
      else
         if(Trading_Indicator == Stochastic)
           {
            if(iStochastic(gRandomSymbol,Stochastic_TimeFrame,Stochastic_Kperiod,Stochastic_Dperiod,Stochastic_Slowing,Stochastic_Method,Stochastic_PriceField,MODE_MAIN,Stochastic_Shift) > Stochastic_UpLevel && iStochastic(gRandomSymbol,Stochastic_TimeFrame,Stochastic_Kperiod,Stochastic_Dperiod,Stochastic_Slowing,Stochastic_Method,Stochastic_PriceField,MODE_SIGNAL,Stochastic_Shift) > Stochastic_UpLevel)
               gSignalStatus = "Stochastic_UP";
            if(iStochastic(gRandomSymbol,Stochastic_TimeFrame,Stochastic_Kperiod,Stochastic_Dperiod,Stochastic_Slowing,Stochastic_Method,Stochastic_PriceField,MODE_MAIN,Stochastic_Shift) < Stochastic_BelowLevel && iStochastic(gRandomSymbol,Stochastic_TimeFrame,Stochastic_Kperiod,Stochastic_Dperiod,Stochastic_Slowing,Stochastic_Method,Stochastic_PriceField,MODE_SIGNAL,Stochastic_Shift) < Stochastic_BelowLevel)
               gSignalStatus = "Stochastic_BELOW";
           }
         else
            if(Trading_Indicator == BollingerBands)
              {
               if(MarketInfo(gRandomSymbol, MODE_BID) > iBands(gRandomSymbol,BollingerBands_TimeFrame,BollingerBands_Period,BollingerBands_deviation,BollingerBands_BandShift,BollingerBands_AppliedPrice,MODE_UPPER,BollingerBands_Shift))
                  gSignalStatus = "BB_UP";
               if(MarketInfo(gRandomSymbol, MODE_ASK) < iBands(gRandomSymbol,BollingerBands_TimeFrame,BollingerBands_Period,BollingerBands_deviation,BollingerBands_BandShift,BollingerBands_AppliedPrice,MODE_LOWER,BollingerBands_Shift))
                  gSignalStatus = "BB_BELOW";
              }
            else
               if(Trading_Indicator == RSI_Stochastic_BollingerBands)
                 {
                  if(iRSI(gRandomSymbol,RSI_TimeFrame,RSI_Period,RSI_AppliedPrice,RSI_shift) > RSI_UpLevel &&
                     iStochastic(gRandomSymbol,Stochastic_TimeFrame,Stochastic_Kperiod,Stochastic_Dperiod,Stochastic_Slowing,Stochastic_Method,Stochastic_PriceField,MODE_MAIN,Stochastic_Shift) > Stochastic_UpLevel &&
                     iStochastic(gRandomSymbol,Stochastic_TimeFrame,Stochastic_Kperiod,Stochastic_Dperiod,Stochastic_Slowing,Stochastic_Method,Stochastic_PriceField,MODE_SIGNAL,Stochastic_Shift) > Stochastic_UpLevel &&
                     MarketInfo(gRandomSymbol, MODE_BID) > iBands(gRandomSymbol,BollingerBands_TimeFrame,BollingerBands_Period,BollingerBands_deviation,BollingerBands_BandShift,BollingerBands_AppliedPrice,MODE_UPPER,BollingerBands_Shift))
                     gSignalStatus = "RSB_UP";

                  if(iRSI(gRandomSymbol,RSI_TimeFrame,RSI_Period,RSI_AppliedPrice,RSI_shift) < RSI_BelowLevel &&
                     iStochastic(gRandomSymbol,Stochastic_TimeFrame,Stochastic_Kperiod,Stochastic_Dperiod,Stochastic_Slowing,Stochastic_Method,Stochastic_PriceField,MODE_MAIN,Stochastic_Shift) < Stochastic_BelowLevel &&
                     iStochastic(gRandomSymbol,Stochastic_TimeFrame,Stochastic_Kperiod,Stochastic_Dperiod,Stochastic_Slowing,Stochastic_Method,Stochastic_PriceField,MODE_SIGNAL,Stochastic_Shift) < Stochastic_BelowLevel &&
                     MarketInfo(gRandomSymbol, MODE_ASK) < iBands(gRandomSymbol,BollingerBands_TimeFrame,BollingerBands_Period,BollingerBands_deviation,BollingerBands_BandShift,BollingerBands_AppliedPrice,MODE_LOWER,BollingerBands_Shift))
                     gSignalStatus = "RSB_BELOW";
                 }
     }
   else
      if(SignalStrength == NormalMultiTimeFrame) //Normal MultiTimeFrame
        {
         if(Trading_Indicator == RSI)
           {
            if(iRSI(gRandomSymbol,SelectedTimeFrame,RSI_Period,RSI_AppliedPrice,RSI_shift) > RSI_UpLevel)
               gSignalStatus = "RSI_UP";
            if(iRSI(gRandomSymbol,SelectedTimeFrame,RSI_Period,RSI_AppliedPrice,RSI_shift) < RSI_BelowLevel)
               gSignalStatus = "RSI_BELOW";
           }
         else
            if(Trading_Indicator == Stochastic)
              {
               if(iStochastic(gRandomSymbol,SelectedTimeFrame,Stochastic_Kperiod,Stochastic_Dperiod,Stochastic_Slowing,Stochastic_Method,Stochastic_PriceField,MODE_MAIN,Stochastic_Shift) > Stochastic_UpLevel && iStochastic(gRandomSymbol,SelectedTimeFrame,Stochastic_Kperiod,Stochastic_Dperiod,Stochastic_Slowing,Stochastic_Method,Stochastic_PriceField,MODE_SIGNAL,Stochastic_Shift) > Stochastic_UpLevel)
                  gSignalStatus = "Stochastic_UP";
               if(iStochastic(gRandomSymbol,SelectedTimeFrame,Stochastic_Kperiod,Stochastic_Dperiod,Stochastic_Slowing,Stochastic_Method,Stochastic_PriceField,MODE_MAIN,Stochastic_Shift) < Stochastic_BelowLevel && iStochastic(gRandomSymbol,SelectedTimeFrame,Stochastic_Kperiod,Stochastic_Dperiod,Stochastic_Slowing,Stochastic_Method,Stochastic_PriceField,MODE_SIGNAL,Stochastic_Shift) < Stochastic_BelowLevel)
                  gSignalStatus = "Stochastic_BELOW";
              }
            else
               if(Trading_Indicator == BollingerBands)
                 {
                  if(MarketInfo(gRandomSymbol, MODE_BID) > iBands(gRandomSymbol,SelectedTimeFrame,BollingerBands_Period,BollingerBands_deviation,BollingerBands_BandShift,BollingerBands_AppliedPrice,MODE_UPPER,BollingerBands_Shift))
                     gSignalStatus = "BB_UP";
                  if(MarketInfo(gRandomSymbol, MODE_ASK) < iBands(gRandomSymbol,SelectedTimeFrame,BollingerBands_Period,BollingerBands_deviation,BollingerBands_BandShift,BollingerBands_AppliedPrice,MODE_LOWER,BollingerBands_Shift))
                     gSignalStatus = "BB_BELOW";
                 }
               else
                  if(Trading_Indicator == RSI_Stochastic_BollingerBands)
                    {
                     if(iRSI(gRandomSymbol,SelectedTimeFrame,RSI_Period,RSI_AppliedPrice,RSI_shift) > RSI_UpLevel &&
                        iStochastic(gRandomSymbol,SelectedTimeFrame,Stochastic_Kperiod,Stochastic_Dperiod,Stochastic_Slowing,Stochastic_Method,Stochastic_PriceField,MODE_MAIN,Stochastic_Shift) > Stochastic_UpLevel &&
                        iStochastic(gRandomSymbol,SelectedTimeFrame,Stochastic_Kperiod,Stochastic_Dperiod,Stochastic_Slowing,Stochastic_Method,Stochastic_PriceField,MODE_SIGNAL,Stochastic_Shift) > Stochastic_UpLevel &&
                        MarketInfo(gRandomSymbol, MODE_BID) > iBands(gRandomSymbol,SelectedTimeFrame,BollingerBands_Period,BollingerBands_deviation,BollingerBands_BandShift,BollingerBands_AppliedPrice,MODE_UPPER,BollingerBands_Shift))
                        gSignalStatus = "RSB_UP";

                     if(iRSI(gRandomSymbol,SelectedTimeFrame,RSI_Period,RSI_AppliedPrice,RSI_shift) < RSI_BelowLevel &&
                        iStochastic(gRandomSymbol,SelectedTimeFrame,Stochastic_Kperiod,Stochastic_Dperiod,Stochastic_Slowing,Stochastic_Method,Stochastic_PriceField,MODE_MAIN,Stochastic_Shift) < Stochastic_BelowLevel &&
                        iStochastic(gRandomSymbol,SelectedTimeFrame,Stochastic_Kperiod,Stochastic_Dperiod,Stochastic_Slowing,Stochastic_Method,Stochastic_PriceField,MODE_SIGNAL,Stochastic_Shift) < Stochastic_BelowLevel &&
                        MarketInfo(gRandomSymbol, MODE_ASK) < iBands(gRandomSymbol,SelectedTimeFrame,BollingerBands_Period,BollingerBands_deviation,BollingerBands_BandShift,BollingerBands_AppliedPrice,MODE_LOWER,BollingerBands_Shift))
                        gSignalStatus = "RSB_BELOW";
                    }
        }
      else
         if(SignalStrength == StrongSignal) //Strong Signal
           {
            if(Trading_Indicator == RSI)
              {
               if(iRSI(gRandomSymbol,PERIOD_M1,RSI_Period,RSI_AppliedPrice,RSI_shift) > RSI_UpLevel &&
                  iRSI(gRandomSymbol,PERIOD_M5,RSI_Period,RSI_AppliedPrice,RSI_shift) > RSI_UpLevel &&
                  iRSI(gRandomSymbol,PERIOD_M15,RSI_Period,RSI_AppliedPrice,RSI_shift) > RSI_UpLevel &&
                  iRSI(gRandomSymbol,PERIOD_M30,RSI_Period,RSI_AppliedPrice,RSI_shift) > RSI_UpLevel)
                  gSignalStatus = "RSI_UP";
               if(iRSI(gRandomSymbol,PERIOD_M1,RSI_Period,RSI_AppliedPrice,RSI_shift) < RSI_BelowLevel &&
                  iRSI(gRandomSymbol,PERIOD_M5,RSI_Period,RSI_AppliedPrice,RSI_shift) < RSI_BelowLevel &&
                  iRSI(gRandomSymbol,PERIOD_M15,RSI_Period,RSI_AppliedPrice,RSI_shift) < RSI_BelowLevel &&
                  iRSI(gRandomSymbol,PERIOD_M30,RSI_Period,RSI_AppliedPrice,RSI_shift) < RSI_BelowLevel)
                  gSignalStatus = "RSI_BELOW";
              }
            else
               if(Trading_Indicator == Stochastic)
                 {
                  if((iStochastic(gRandomSymbol,PERIOD_M1,Stochastic_Kperiod,Stochastic_Dperiod,Stochastic_Slowing,Stochastic_Method,Stochastic_PriceField,MODE_MAIN,Stochastic_Shift) > Stochastic_UpLevel && iStochastic(gRandomSymbol,PERIOD_M1,Stochastic_Kperiod,Stochastic_Dperiod,Stochastic_Slowing,Stochastic_Method,Stochastic_PriceField,MODE_SIGNAL,Stochastic_Shift) > Stochastic_UpLevel) &&
                     (iStochastic(gRandomSymbol,PERIOD_M5,Stochastic_Kperiod,Stochastic_Dperiod,Stochastic_Slowing,Stochastic_Method,Stochastic_PriceField,MODE_MAIN,Stochastic_Shift) > Stochastic_UpLevel && iStochastic(gRandomSymbol,PERIOD_M5,Stochastic_Kperiod,Stochastic_Dperiod,Stochastic_Slowing,Stochastic_Method,Stochastic_PriceField,MODE_SIGNAL,Stochastic_Shift) > Stochastic_UpLevel) &&
                     (iStochastic(gRandomSymbol,PERIOD_M15,Stochastic_Kperiod,Stochastic_Dperiod,Stochastic_Slowing,Stochastic_Method,Stochastic_PriceField,MODE_MAIN,Stochastic_Shift) > Stochastic_UpLevel && iStochastic(gRandomSymbol,PERIOD_M15,Stochastic_Kperiod,Stochastic_Dperiod,Stochastic_Slowing,Stochastic_Method,Stochastic_PriceField,MODE_SIGNAL,Stochastic_Shift) > Stochastic_UpLevel) &&
                     (iStochastic(gRandomSymbol,PERIOD_M30,Stochastic_Kperiod,Stochastic_Dperiod,Stochastic_Slowing,Stochastic_Method,Stochastic_PriceField,MODE_MAIN,Stochastic_Shift) > Stochastic_UpLevel && iStochastic(gRandomSymbol,PERIOD_M30,Stochastic_Kperiod,Stochastic_Dperiod,Stochastic_Slowing,Stochastic_Method,Stochastic_PriceField,MODE_SIGNAL,Stochastic_Shift) > Stochastic_UpLevel))
                     gSignalStatus = "Stochastic_UP";
                  if((iStochastic(gRandomSymbol,PERIOD_M1,Stochastic_Kperiod,Stochastic_Dperiod,Stochastic_Slowing,Stochastic_Method,Stochastic_PriceField,MODE_MAIN,Stochastic_Shift) < Stochastic_BelowLevel && iStochastic(gRandomSymbol,PERIOD_M1,Stochastic_Kperiod,Stochastic_Dperiod,Stochastic_Slowing,Stochastic_Method,Stochastic_PriceField,MODE_SIGNAL,Stochastic_Shift) < Stochastic_BelowLevel) &&
                     (iStochastic(gRandomSymbol,PERIOD_M5,Stochastic_Kperiod,Stochastic_Dperiod,Stochastic_Slowing,Stochastic_Method,Stochastic_PriceField,MODE_MAIN,Stochastic_Shift) < Stochastic_BelowLevel && iStochastic(gRandomSymbol,PERIOD_M5,Stochastic_Kperiod,Stochastic_Dperiod,Stochastic_Slowing,Stochastic_Method,Stochastic_PriceField,MODE_SIGNAL,Stochastic_Shift) < Stochastic_BelowLevel) &&
                     (iStochastic(gRandomSymbol,PERIOD_M15,Stochastic_Kperiod,Stochastic_Dperiod,Stochastic_Slowing,Stochastic_Method,Stochastic_PriceField,MODE_MAIN,Stochastic_Shift) < Stochastic_BelowLevel && iStochastic(gRandomSymbol,PERIOD_M15,Stochastic_Kperiod,Stochastic_Dperiod,Stochastic_Slowing,Stochastic_Method,Stochastic_PriceField,MODE_SIGNAL,Stochastic_Shift) < Stochastic_BelowLevel) &&
                     (iStochastic(gRandomSymbol,PERIOD_M30,Stochastic_Kperiod,Stochastic_Dperiod,Stochastic_Slowing,Stochastic_Method,Stochastic_PriceField,MODE_MAIN,Stochastic_Shift) < Stochastic_BelowLevel && iStochastic(gRandomSymbol,PERIOD_M30,Stochastic_Kperiod,Stochastic_Dperiod,Stochastic_Slowing,Stochastic_Method,Stochastic_PriceField,MODE_SIGNAL,Stochastic_Shift) < Stochastic_BelowLevel))
                     gSignalStatus = "Stochastic_BELOW";
                 }
               else
                  if(Trading_Indicator == BollingerBands)
                    {
                     if(MarketInfo(gRandomSymbol, MODE_BID) > iBands(gRandomSymbol,PERIOD_M1,BollingerBands_Period,BollingerBands_deviation,BollingerBands_BandShift,BollingerBands_AppliedPrice,MODE_UPPER,BollingerBands_Shift) &&
                        MarketInfo(gRandomSymbol, MODE_BID) > iBands(gRandomSymbol,PERIOD_M5,BollingerBands_Period,BollingerBands_deviation,BollingerBands_BandShift,BollingerBands_AppliedPrice,MODE_UPPER,BollingerBands_Shift) &&
                        MarketInfo(gRandomSymbol, MODE_BID) > iBands(gRandomSymbol,PERIOD_M15,BollingerBands_Period,BollingerBands_deviation,BollingerBands_BandShift,BollingerBands_AppliedPrice,MODE_UPPER,BollingerBands_Shift) &&
                        MarketInfo(gRandomSymbol, MODE_BID) > iBands(gRandomSymbol,PERIOD_M30,BollingerBands_Period,BollingerBands_deviation,BollingerBands_BandShift,BollingerBands_AppliedPrice,MODE_UPPER,BollingerBands_Shift))
                        gSignalStatus = "BB_UP";
                     if(MarketInfo(gRandomSymbol, MODE_ASK) < iBands(gRandomSymbol,PERIOD_M1,BollingerBands_Period,BollingerBands_deviation,BollingerBands_BandShift,BollingerBands_AppliedPrice,MODE_LOWER,BollingerBands_Shift) &&
                        MarketInfo(gRandomSymbol, MODE_ASK) < iBands(gRandomSymbol,PERIOD_M5,BollingerBands_Period,BollingerBands_deviation,BollingerBands_BandShift,BollingerBands_AppliedPrice,MODE_LOWER,BollingerBands_Shift) &&
                        MarketInfo(gRandomSymbol, MODE_ASK) < iBands(gRandomSymbol,PERIOD_M15,BollingerBands_Period,BollingerBands_deviation,BollingerBands_BandShift,BollingerBands_AppliedPrice,MODE_LOWER,BollingerBands_Shift) &&
                        MarketInfo(gRandomSymbol, MODE_ASK) < iBands(gRandomSymbol,PERIOD_M30,BollingerBands_Period,BollingerBands_deviation,BollingerBands_BandShift,BollingerBands_AppliedPrice,MODE_LOWER,BollingerBands_Shift))
                        gSignalStatus = "BB_BELOW";
                    }
                  else
                     if(Trading_Indicator == RSI_Stochastic_BollingerBands) //Very Strong Signal
                       {
                        if(
                           iRSI(gRandomSymbol,PERIOD_M1,RSI_Period,RSI_AppliedPrice,RSI_shift) > RSI_UpLevel &&
                           iRSI(gRandomSymbol,PERIOD_M5,RSI_Period,RSI_AppliedPrice,RSI_shift) > RSI_UpLevel &&
                           iRSI(gRandomSymbol,PERIOD_M15,RSI_Period,RSI_AppliedPrice,RSI_shift) > RSI_UpLevel &&
                           iRSI(gRandomSymbol,PERIOD_M30,RSI_Period,RSI_AppliedPrice,RSI_shift) > RSI_UpLevel &&

                           (iStochastic(gRandomSymbol,PERIOD_M1,Stochastic_Kperiod,Stochastic_Dperiod,Stochastic_Slowing,Stochastic_Method,Stochastic_PriceField,MODE_MAIN,Stochastic_Shift) > Stochastic_UpLevel && iStochastic(gRandomSymbol,PERIOD_M1,Stochastic_Kperiod,Stochastic_Dperiod,Stochastic_Slowing,Stochastic_Method,Stochastic_PriceField,MODE_SIGNAL,Stochastic_Shift) > Stochastic_UpLevel) &&
                           (iStochastic(gRandomSymbol,PERIOD_M5,Stochastic_Kperiod,Stochastic_Dperiod,Stochastic_Slowing,Stochastic_Method,Stochastic_PriceField,MODE_MAIN,Stochastic_Shift) > Stochastic_UpLevel && iStochastic(gRandomSymbol,PERIOD_M5,Stochastic_Kperiod,Stochastic_Dperiod,Stochastic_Slowing,Stochastic_Method,Stochastic_PriceField,MODE_SIGNAL,Stochastic_Shift) > Stochastic_UpLevel) &&
                           (iStochastic(gRandomSymbol,PERIOD_M15,Stochastic_Kperiod,Stochastic_Dperiod,Stochastic_Slowing,Stochastic_Method,Stochastic_PriceField,MODE_MAIN,Stochastic_Shift) > Stochastic_UpLevel && iStochastic(gRandomSymbol,PERIOD_M15,Stochastic_Kperiod,Stochastic_Dperiod,Stochastic_Slowing,Stochastic_Method,Stochastic_PriceField,MODE_SIGNAL,Stochastic_Shift) > Stochastic_UpLevel) &&
                           (iStochastic(gRandomSymbol,PERIOD_M30,Stochastic_Kperiod,Stochastic_Dperiod,Stochastic_Slowing,Stochastic_Method,Stochastic_PriceField,MODE_MAIN,Stochastic_Shift) > Stochastic_UpLevel && iStochastic(gRandomSymbol,PERIOD_M30,Stochastic_Kperiod,Stochastic_Dperiod,Stochastic_Slowing,Stochastic_Method,Stochastic_PriceField,MODE_SIGNAL,Stochastic_Shift) > Stochastic_UpLevel) &&

                           MarketInfo(gRandomSymbol, MODE_BID) > iBands(gRandomSymbol,PERIOD_M1,BollingerBands_Period,BollingerBands_deviation,BollingerBands_BandShift,BollingerBands_AppliedPrice,MODE_UPPER,BollingerBands_Shift) &&
                           MarketInfo(gRandomSymbol, MODE_BID) > iBands(gRandomSymbol,PERIOD_M5,BollingerBands_Period,BollingerBands_deviation,BollingerBands_BandShift,BollingerBands_AppliedPrice,MODE_UPPER,BollingerBands_Shift) &&
                           MarketInfo(gRandomSymbol, MODE_BID) > iBands(gRandomSymbol,PERIOD_M15,BollingerBands_Period,BollingerBands_deviation,BollingerBands_BandShift,BollingerBands_AppliedPrice,MODE_UPPER,BollingerBands_Shift) &&
                           MarketInfo(gRandomSymbol, MODE_BID) > iBands(gRandomSymbol,PERIOD_M30,BollingerBands_Period,BollingerBands_deviation,BollingerBands_BandShift,BollingerBands_AppliedPrice,MODE_UPPER,BollingerBands_Shift)
                        )
                           gSignalStatus = "RSB_UP";

                        if(
                           iRSI(gRandomSymbol,PERIOD_M1,RSI_Period,RSI_AppliedPrice,RSI_shift) < RSI_BelowLevel &&
                           iRSI(gRandomSymbol,PERIOD_M5,RSI_Period,RSI_AppliedPrice,RSI_shift) < RSI_BelowLevel &&
                           iRSI(gRandomSymbol,PERIOD_M15,RSI_Period,RSI_AppliedPrice,RSI_shift) < RSI_BelowLevel &&
                           iRSI(gRandomSymbol,PERIOD_M30,RSI_Period,RSI_AppliedPrice,RSI_shift) < RSI_BelowLevel &&

                           (iStochastic(gRandomSymbol,PERIOD_M1,Stochastic_Kperiod,Stochastic_Dperiod,Stochastic_Slowing,Stochastic_Method,Stochastic_PriceField,MODE_MAIN,Stochastic_Shift) < Stochastic_BelowLevel && iStochastic(gRandomSymbol,PERIOD_M1,Stochastic_Kperiod,Stochastic_Dperiod,Stochastic_Slowing,Stochastic_Method,Stochastic_PriceField,MODE_SIGNAL,Stochastic_Shift) < Stochastic_BelowLevel) &&
                           (iStochastic(gRandomSymbol,PERIOD_M5,Stochastic_Kperiod,Stochastic_Dperiod,Stochastic_Slowing,Stochastic_Method,Stochastic_PriceField,MODE_MAIN,Stochastic_Shift) < Stochastic_BelowLevel && iStochastic(gRandomSymbol,PERIOD_M5,Stochastic_Kperiod,Stochastic_Dperiod,Stochastic_Slowing,Stochastic_Method,Stochastic_PriceField,MODE_SIGNAL,Stochastic_Shift) < Stochastic_BelowLevel) &&
                           (iStochastic(gRandomSymbol,PERIOD_M15,Stochastic_Kperiod,Stochastic_Dperiod,Stochastic_Slowing,Stochastic_Method,Stochastic_PriceField,MODE_MAIN,Stochastic_Shift) < Stochastic_BelowLevel && iStochastic(gRandomSymbol,PERIOD_M15,Stochastic_Kperiod,Stochastic_Dperiod,Stochastic_Slowing,Stochastic_Method,Stochastic_PriceField,MODE_SIGNAL,Stochastic_Shift) < Stochastic_BelowLevel) &&
                           (iStochastic(gRandomSymbol,PERIOD_M30,Stochastic_Kperiod,Stochastic_Dperiod,Stochastic_Slowing,Stochastic_Method,Stochastic_PriceField,MODE_MAIN,Stochastic_Shift) < Stochastic_BelowLevel && iStochastic(gRandomSymbol,PERIOD_M30,Stochastic_Kperiod,Stochastic_Dperiod,Stochastic_Slowing,Stochastic_Method,Stochastic_PriceField,MODE_SIGNAL,Stochastic_Shift) < Stochastic_BelowLevel) &&

                           MarketInfo(gRandomSymbol, MODE_ASK) < iBands(gRandomSymbol,PERIOD_M1,BollingerBands_Period,BollingerBands_deviation,BollingerBands_BandShift,BollingerBands_AppliedPrice,MODE_LOWER,BollingerBands_Shift) &&
                           MarketInfo(gRandomSymbol, MODE_ASK) < iBands(gRandomSymbol,PERIOD_M5,BollingerBands_Period,BollingerBands_deviation,BollingerBands_BandShift,BollingerBands_AppliedPrice,MODE_LOWER,BollingerBands_Shift) &&
                           MarketInfo(gRandomSymbol, MODE_ASK) < iBands(gRandomSymbol,PERIOD_M15,BollingerBands_Period,BollingerBands_deviation,BollingerBands_BandShift,BollingerBands_AppliedPrice,MODE_LOWER,BollingerBands_Shift) &&
                           MarketInfo(gRandomSymbol, MODE_ASK) < iBands(gRandomSymbol,PERIOD_M30,BollingerBands_Period,BollingerBands_deviation,BollingerBands_BandShift,BollingerBands_AppliedPrice,MODE_LOWER,BollingerBands_Shift)
                        )
                           gSignalStatus = "RSB_BELOW";
                       }
           }
         else
            if(SignalStrength == VeryStrongSignal)
              {
               if(Trading_Indicator == RSI)
                 {
                  if(iRSI(gRandomSymbol,PERIOD_M1,RSI_Period,RSI_AppliedPrice,RSI_shift) > RSI_UpLevel &&
                     iRSI(gRandomSymbol,PERIOD_M5,RSI_Period,RSI_AppliedPrice,RSI_shift) > RSI_UpLevel &&
                     iRSI(gRandomSymbol,PERIOD_M15,RSI_Period,RSI_AppliedPrice,RSI_shift) > RSI_UpLevel &&
                     iRSI(gRandomSymbol,PERIOD_M30,RSI_Period,RSI_AppliedPrice,RSI_shift) > RSI_UpLevel &&
                     iRSI(gRandomSymbol,PERIOD_H1,RSI_Period,RSI_AppliedPrice,RSI_shift) > RSI_UpLevel &&
                     iRSI(gRandomSymbol,PERIOD_H4,RSI_Period,RSI_AppliedPrice,RSI_shift) > RSI_UpLevel)
                     gSignalStatus = "RSI_UP";
                  if(iRSI(gRandomSymbol,PERIOD_M1,RSI_Period,RSI_AppliedPrice,RSI_shift) < RSI_BelowLevel &&
                     iRSI(gRandomSymbol,PERIOD_M5,RSI_Period,RSI_AppliedPrice,RSI_shift) < RSI_BelowLevel &&
                     iRSI(gRandomSymbol,PERIOD_M15,RSI_Period,RSI_AppliedPrice,RSI_shift) < RSI_BelowLevel &&
                     iRSI(gRandomSymbol,PERIOD_M30,RSI_Period,RSI_AppliedPrice,RSI_shift) < RSI_BelowLevel &&
                     iRSI(gRandomSymbol,PERIOD_H1,RSI_Period,RSI_AppliedPrice,RSI_shift) < RSI_BelowLevel &&
                     iRSI(gRandomSymbol,PERIOD_H4,RSI_Period,RSI_AppliedPrice,RSI_shift) < RSI_BelowLevel)
                     gSignalStatus = "RSI_BELOW";
                 }
               else
                  if(Trading_Indicator == Stochastic)
                    {
                     if((iStochastic(gRandomSymbol,PERIOD_M1,Stochastic_Kperiod,Stochastic_Dperiod,Stochastic_Slowing,Stochastic_Method,Stochastic_PriceField,MODE_MAIN,Stochastic_Shift) > Stochastic_UpLevel && iStochastic(gRandomSymbol,PERIOD_M1,Stochastic_Kperiod,Stochastic_Dperiod,Stochastic_Slowing,Stochastic_Method,Stochastic_PriceField,MODE_SIGNAL,Stochastic_Shift) > Stochastic_UpLevel) &&
                        (iStochastic(gRandomSymbol,PERIOD_M5,Stochastic_Kperiod,Stochastic_Dperiod,Stochastic_Slowing,Stochastic_Method,Stochastic_PriceField,MODE_MAIN,Stochastic_Shift) > Stochastic_UpLevel && iStochastic(gRandomSymbol,PERIOD_M5,Stochastic_Kperiod,Stochastic_Dperiod,Stochastic_Slowing,Stochastic_Method,Stochastic_PriceField,MODE_SIGNAL,Stochastic_Shift) > Stochastic_UpLevel) &&
                        (iStochastic(gRandomSymbol,PERIOD_M15,Stochastic_Kperiod,Stochastic_Dperiod,Stochastic_Slowing,Stochastic_Method,Stochastic_PriceField,MODE_MAIN,Stochastic_Shift) > Stochastic_UpLevel && iStochastic(gRandomSymbol,PERIOD_M15,Stochastic_Kperiod,Stochastic_Dperiod,Stochastic_Slowing,Stochastic_Method,Stochastic_PriceField,MODE_SIGNAL,Stochastic_Shift) > Stochastic_UpLevel) &&
                        (iStochastic(gRandomSymbol,PERIOD_M30,Stochastic_Kperiod,Stochastic_Dperiod,Stochastic_Slowing,Stochastic_Method,Stochastic_PriceField,MODE_MAIN,Stochastic_Shift) > Stochastic_UpLevel && iStochastic(gRandomSymbol,PERIOD_M30,Stochastic_Kperiod,Stochastic_Dperiod,Stochastic_Slowing,Stochastic_Method,Stochastic_PriceField,MODE_SIGNAL,Stochastic_Shift) > Stochastic_UpLevel) &&
                        (iStochastic(gRandomSymbol,PERIOD_H1,Stochastic_Kperiod,Stochastic_Dperiod,Stochastic_Slowing,Stochastic_Method,Stochastic_PriceField,MODE_MAIN,Stochastic_Shift) > Stochastic_UpLevel && iStochastic(gRandomSymbol,PERIOD_H1,Stochastic_Kperiod,Stochastic_Dperiod,Stochastic_Slowing,Stochastic_Method,Stochastic_PriceField,MODE_SIGNAL,Stochastic_Shift) > Stochastic_UpLevel) &&
                        (iStochastic(gRandomSymbol,PERIOD_H4,Stochastic_Kperiod,Stochastic_Dperiod,Stochastic_Slowing,Stochastic_Method,Stochastic_PriceField,MODE_MAIN,Stochastic_Shift) > Stochastic_UpLevel && iStochastic(gRandomSymbol,PERIOD_H4,Stochastic_Kperiod,Stochastic_Dperiod,Stochastic_Slowing,Stochastic_Method,Stochastic_PriceField,MODE_SIGNAL,Stochastic_Shift) > Stochastic_UpLevel))
                        gSignalStatus = "Stochastic_UP";
                     if((iStochastic(gRandomSymbol,PERIOD_M1,Stochastic_Kperiod,Stochastic_Dperiod,Stochastic_Slowing,Stochastic_Method,Stochastic_PriceField,MODE_MAIN,Stochastic_Shift) < Stochastic_BelowLevel && iStochastic(gRandomSymbol,PERIOD_M1,Stochastic_Kperiod,Stochastic_Dperiod,Stochastic_Slowing,Stochastic_Method,Stochastic_PriceField,MODE_SIGNAL,Stochastic_Shift) < Stochastic_BelowLevel) &&
                        (iStochastic(gRandomSymbol,PERIOD_M5,Stochastic_Kperiod,Stochastic_Dperiod,Stochastic_Slowing,Stochastic_Method,Stochastic_PriceField,MODE_MAIN,Stochastic_Shift) < Stochastic_BelowLevel && iStochastic(gRandomSymbol,PERIOD_M5,Stochastic_Kperiod,Stochastic_Dperiod,Stochastic_Slowing,Stochastic_Method,Stochastic_PriceField,MODE_SIGNAL,Stochastic_Shift) < Stochastic_BelowLevel) &&
                        (iStochastic(gRandomSymbol,PERIOD_M15,Stochastic_Kperiod,Stochastic_Dperiod,Stochastic_Slowing,Stochastic_Method,Stochastic_PriceField,MODE_MAIN,Stochastic_Shift) < Stochastic_BelowLevel && iStochastic(gRandomSymbol,PERIOD_M15,Stochastic_Kperiod,Stochastic_Dperiod,Stochastic_Slowing,Stochastic_Method,Stochastic_PriceField,MODE_SIGNAL,Stochastic_Shift) < Stochastic_BelowLevel) &&
                        (iStochastic(gRandomSymbol,PERIOD_M30,Stochastic_Kperiod,Stochastic_Dperiod,Stochastic_Slowing,Stochastic_Method,Stochastic_PriceField,MODE_MAIN,Stochastic_Shift) < Stochastic_BelowLevel && iStochastic(gRandomSymbol,PERIOD_M30,Stochastic_Kperiod,Stochastic_Dperiod,Stochastic_Slowing,Stochastic_Method,Stochastic_PriceField,MODE_SIGNAL,Stochastic_Shift) < Stochastic_BelowLevel) &&
                        (iStochastic(gRandomSymbol,PERIOD_H1,Stochastic_Kperiod,Stochastic_Dperiod,Stochastic_Slowing,Stochastic_Method,Stochastic_PriceField,MODE_MAIN,Stochastic_Shift) < Stochastic_BelowLevel && iStochastic(gRandomSymbol,PERIOD_H1,Stochastic_Kperiod,Stochastic_Dperiod,Stochastic_Slowing,Stochastic_Method,Stochastic_PriceField,MODE_SIGNAL,Stochastic_Shift) < Stochastic_BelowLevel) &&
                        (iStochastic(gRandomSymbol,PERIOD_H4,Stochastic_Kperiod,Stochastic_Dperiod,Stochastic_Slowing,Stochastic_Method,Stochastic_PriceField,MODE_MAIN,Stochastic_Shift) < Stochastic_BelowLevel && iStochastic(gRandomSymbol,PERIOD_H4,Stochastic_Kperiod,Stochastic_Dperiod,Stochastic_Slowing,Stochastic_Method,Stochastic_PriceField,MODE_SIGNAL,Stochastic_Shift) < Stochastic_BelowLevel))
                        gSignalStatus = "Stochastic_BELOW";
                    }
                  else
                     if(Trading_Indicator == BollingerBands)
                       {
                        if(MarketInfo(gRandomSymbol, MODE_BID) > iBands(gRandomSymbol,PERIOD_M1,BollingerBands_Period,BollingerBands_deviation,BollingerBands_BandShift,BollingerBands_AppliedPrice,MODE_UPPER,BollingerBands_Shift) &&
                           MarketInfo(gRandomSymbol, MODE_BID) > iBands(gRandomSymbol,PERIOD_M5,BollingerBands_Period,BollingerBands_deviation,BollingerBands_BandShift,BollingerBands_AppliedPrice,MODE_UPPER,BollingerBands_Shift) &&
                           MarketInfo(gRandomSymbol, MODE_BID) > iBands(gRandomSymbol,PERIOD_M15,BollingerBands_Period,BollingerBands_deviation,BollingerBands_BandShift,BollingerBands_AppliedPrice,MODE_UPPER,BollingerBands_Shift) &&
                           MarketInfo(gRandomSymbol, MODE_BID) > iBands(gRandomSymbol,PERIOD_M30,BollingerBands_Period,BollingerBands_deviation,BollingerBands_BandShift,BollingerBands_AppliedPrice,MODE_UPPER,BollingerBands_Shift) &&
                           MarketInfo(gRandomSymbol, MODE_BID) > iBands(gRandomSymbol,PERIOD_H1,BollingerBands_Period,BollingerBands_deviation,BollingerBands_BandShift,BollingerBands_AppliedPrice,MODE_UPPER,BollingerBands_Shift) &&
                           MarketInfo(gRandomSymbol, MODE_BID) > iBands(gRandomSymbol,PERIOD_H4,BollingerBands_Period,BollingerBands_deviation,BollingerBands_BandShift,BollingerBands_AppliedPrice,MODE_UPPER,BollingerBands_Shift))
                           gSignalStatus = "BB_UP";
                        if(MarketInfo(gRandomSymbol, MODE_ASK) < iBands(gRandomSymbol,PERIOD_M1,BollingerBands_Period,BollingerBands_deviation,BollingerBands_BandShift,BollingerBands_AppliedPrice,MODE_LOWER,BollingerBands_Shift) &&
                           MarketInfo(gRandomSymbol, MODE_ASK) < iBands(gRandomSymbol,PERIOD_M5,BollingerBands_Period,BollingerBands_deviation,BollingerBands_BandShift,BollingerBands_AppliedPrice,MODE_LOWER,BollingerBands_Shift) &&
                           MarketInfo(gRandomSymbol, MODE_ASK) < iBands(gRandomSymbol,PERIOD_M15,BollingerBands_Period,BollingerBands_deviation,BollingerBands_BandShift,BollingerBands_AppliedPrice,MODE_LOWER,BollingerBands_Shift) &&
                           MarketInfo(gRandomSymbol, MODE_ASK) < iBands(gRandomSymbol,PERIOD_M30,BollingerBands_Period,BollingerBands_deviation,BollingerBands_BandShift,BollingerBands_AppliedPrice,MODE_LOWER,BollingerBands_Shift) &&
                           MarketInfo(gRandomSymbol, MODE_ASK) < iBands(gRandomSymbol,PERIOD_H1,BollingerBands_Period,BollingerBands_deviation,BollingerBands_BandShift,BollingerBands_AppliedPrice,MODE_LOWER,BollingerBands_Shift) &&
                           MarketInfo(gRandomSymbol, MODE_ASK) < iBands(gRandomSymbol,PERIOD_H4,BollingerBands_Period,BollingerBands_deviation,BollingerBands_BandShift,BollingerBands_AppliedPrice,MODE_LOWER,BollingerBands_Shift))
                           gSignalStatus = "BB_BELOW";
                       }
                     else
                        if(Trading_Indicator == RSI_Stochastic_BollingerBands)
                          {
                           if(
                              iRSI(gRandomSymbol,PERIOD_M1,RSI_Period,RSI_AppliedPrice,RSI_shift) > RSI_UpLevel &&
                              iRSI(gRandomSymbol,PERIOD_M5,RSI_Period,RSI_AppliedPrice,RSI_shift) > RSI_UpLevel &&
                              iRSI(gRandomSymbol,PERIOD_M15,RSI_Period,RSI_AppliedPrice,RSI_shift) > RSI_UpLevel &&
                              iRSI(gRandomSymbol,PERIOD_M30,RSI_Period,RSI_AppliedPrice,RSI_shift) > RSI_UpLevel &&
                              iRSI(gRandomSymbol,PERIOD_H1,RSI_Period,RSI_AppliedPrice,RSI_shift) > RSI_UpLevel &&
                              iRSI(gRandomSymbol,PERIOD_H4,RSI_Period,RSI_AppliedPrice,RSI_shift) > RSI_UpLevel &&

                              (iStochastic(gRandomSymbol,PERIOD_M1,Stochastic_Kperiod,Stochastic_Dperiod,Stochastic_Slowing,Stochastic_Method,Stochastic_PriceField,MODE_MAIN,Stochastic_Shift) > Stochastic_UpLevel && iStochastic(gRandomSymbol,PERIOD_M1,Stochastic_Kperiod,Stochastic_Dperiod,Stochastic_Slowing,Stochastic_Method,Stochastic_PriceField,MODE_SIGNAL,Stochastic_Shift) > Stochastic_UpLevel) &&
                              (iStochastic(gRandomSymbol,PERIOD_M5,Stochastic_Kperiod,Stochastic_Dperiod,Stochastic_Slowing,Stochastic_Method,Stochastic_PriceField,MODE_MAIN,Stochastic_Shift) > Stochastic_UpLevel && iStochastic(gRandomSymbol,PERIOD_M5,Stochastic_Kperiod,Stochastic_Dperiod,Stochastic_Slowing,Stochastic_Method,Stochastic_PriceField,MODE_SIGNAL,Stochastic_Shift) > Stochastic_UpLevel) &&
                              (iStochastic(gRandomSymbol,PERIOD_M15,Stochastic_Kperiod,Stochastic_Dperiod,Stochastic_Slowing,Stochastic_Method,Stochastic_PriceField,MODE_MAIN,Stochastic_Shift) > Stochastic_UpLevel && iStochastic(gRandomSymbol,PERIOD_M15,Stochastic_Kperiod,Stochastic_Dperiod,Stochastic_Slowing,Stochastic_Method,Stochastic_PriceField,MODE_SIGNAL,Stochastic_Shift) > Stochastic_UpLevel) &&
                              (iStochastic(gRandomSymbol,PERIOD_M30,Stochastic_Kperiod,Stochastic_Dperiod,Stochastic_Slowing,Stochastic_Method,Stochastic_PriceField,MODE_MAIN,Stochastic_Shift) > Stochastic_UpLevel && iStochastic(gRandomSymbol,PERIOD_M30,Stochastic_Kperiod,Stochastic_Dperiod,Stochastic_Slowing,Stochastic_Method,Stochastic_PriceField,MODE_SIGNAL,Stochastic_Shift) > Stochastic_UpLevel) &&
                              (iStochastic(gRandomSymbol,PERIOD_H1,Stochastic_Kperiod,Stochastic_Dperiod,Stochastic_Slowing,Stochastic_Method,Stochastic_PriceField,MODE_MAIN,Stochastic_Shift) > Stochastic_UpLevel && iStochastic(gRandomSymbol,PERIOD_H1,Stochastic_Kperiod,Stochastic_Dperiod,Stochastic_Slowing,Stochastic_Method,Stochastic_PriceField,MODE_SIGNAL,Stochastic_Shift) > Stochastic_UpLevel) &&
                              (iStochastic(gRandomSymbol,PERIOD_H4,Stochastic_Kperiod,Stochastic_Dperiod,Stochastic_Slowing,Stochastic_Method,Stochastic_PriceField,MODE_MAIN,Stochastic_Shift) > Stochastic_UpLevel && iStochastic(gRandomSymbol,PERIOD_H4,Stochastic_Kperiod,Stochastic_Dperiod,Stochastic_Slowing,Stochastic_Method,Stochastic_PriceField,MODE_SIGNAL,Stochastic_Shift) > Stochastic_UpLevel) &&

                              MarketInfo(gRandomSymbol, MODE_BID) > iBands(gRandomSymbol,PERIOD_M1,BollingerBands_Period,BollingerBands_deviation,BollingerBands_BandShift,BollingerBands_AppliedPrice,MODE_UPPER,BollingerBands_Shift) &&
                              MarketInfo(gRandomSymbol, MODE_BID) > iBands(gRandomSymbol,PERIOD_M5,BollingerBands_Period,BollingerBands_deviation,BollingerBands_BandShift,BollingerBands_AppliedPrice,MODE_UPPER,BollingerBands_Shift) &&
                              MarketInfo(gRandomSymbol, MODE_BID) > iBands(gRandomSymbol,PERIOD_M15,BollingerBands_Period,BollingerBands_deviation,BollingerBands_BandShift,BollingerBands_AppliedPrice,MODE_UPPER,BollingerBands_Shift) &&
                              MarketInfo(gRandomSymbol, MODE_BID) > iBands(gRandomSymbol,PERIOD_M30,BollingerBands_Period,BollingerBands_deviation,BollingerBands_BandShift,BollingerBands_AppliedPrice,MODE_UPPER,BollingerBands_Shift) &&
                              MarketInfo(gRandomSymbol, MODE_BID) > iBands(gRandomSymbol,PERIOD_H1,BollingerBands_Period,BollingerBands_deviation,BollingerBands_BandShift,BollingerBands_AppliedPrice,MODE_UPPER,BollingerBands_Shift) &&
                              MarketInfo(gRandomSymbol, MODE_BID) > iBands(gRandomSymbol,PERIOD_H4,BollingerBands_Period,BollingerBands_deviation,BollingerBands_BandShift,BollingerBands_AppliedPrice,MODE_UPPER,BollingerBands_Shift)
                           )
                              gSignalStatus = "RSB_UP";

                           if(
                              iRSI(gRandomSymbol,PERIOD_M1,RSI_Period,RSI_AppliedPrice,RSI_shift) < RSI_BelowLevel &&
                              iRSI(gRandomSymbol,PERIOD_M5,RSI_Period,RSI_AppliedPrice,RSI_shift) < RSI_BelowLevel &&
                              iRSI(gRandomSymbol,PERIOD_M15,RSI_Period,RSI_AppliedPrice,RSI_shift) < RSI_BelowLevel &&
                              iRSI(gRandomSymbol,PERIOD_M30,RSI_Period,RSI_AppliedPrice,RSI_shift) < RSI_BelowLevel &&
                              iRSI(gRandomSymbol,PERIOD_H1,RSI_Period,RSI_AppliedPrice,RSI_shift) < RSI_BelowLevel &&
                              iRSI(gRandomSymbol,PERIOD_H4,RSI_Period,RSI_AppliedPrice,RSI_shift) < RSI_BelowLevel &&

                              (iStochastic(gRandomSymbol,PERIOD_M1,Stochastic_Kperiod,Stochastic_Dperiod,Stochastic_Slowing,Stochastic_Method,Stochastic_PriceField,MODE_MAIN,Stochastic_Shift) < Stochastic_BelowLevel && iStochastic(gRandomSymbol,PERIOD_M1,Stochastic_Kperiod,Stochastic_Dperiod,Stochastic_Slowing,Stochastic_Method,Stochastic_PriceField,MODE_SIGNAL,Stochastic_Shift) < Stochastic_BelowLevel) &&
                              (iStochastic(gRandomSymbol,PERIOD_M5,Stochastic_Kperiod,Stochastic_Dperiod,Stochastic_Slowing,Stochastic_Method,Stochastic_PriceField,MODE_MAIN,Stochastic_Shift) < Stochastic_BelowLevel && iStochastic(gRandomSymbol,PERIOD_M5,Stochastic_Kperiod,Stochastic_Dperiod,Stochastic_Slowing,Stochastic_Method,Stochastic_PriceField,MODE_SIGNAL,Stochastic_Shift) < Stochastic_BelowLevel) &&
                              (iStochastic(gRandomSymbol,PERIOD_M15,Stochastic_Kperiod,Stochastic_Dperiod,Stochastic_Slowing,Stochastic_Method,Stochastic_PriceField,MODE_MAIN,Stochastic_Shift) < Stochastic_BelowLevel && iStochastic(gRandomSymbol,PERIOD_M15,Stochastic_Kperiod,Stochastic_Dperiod,Stochastic_Slowing,Stochastic_Method,Stochastic_PriceField,MODE_SIGNAL,Stochastic_Shift) < Stochastic_BelowLevel) &&
                              (iStochastic(gRandomSymbol,PERIOD_M30,Stochastic_Kperiod,Stochastic_Dperiod,Stochastic_Slowing,Stochastic_Method,Stochastic_PriceField,MODE_MAIN,Stochastic_Shift) < Stochastic_BelowLevel && iStochastic(gRandomSymbol,PERIOD_M30,Stochastic_Kperiod,Stochastic_Dperiod,Stochastic_Slowing,Stochastic_Method,Stochastic_PriceField,MODE_SIGNAL,Stochastic_Shift) < Stochastic_BelowLevel) &&
                              (iStochastic(gRandomSymbol,PERIOD_H1,Stochastic_Kperiod,Stochastic_Dperiod,Stochastic_Slowing,Stochastic_Method,Stochastic_PriceField,MODE_MAIN,Stochastic_Shift) < Stochastic_BelowLevel && iStochastic(gRandomSymbol,PERIOD_H1,Stochastic_Kperiod,Stochastic_Dperiod,Stochastic_Slowing,Stochastic_Method,Stochastic_PriceField,MODE_SIGNAL,Stochastic_Shift) < Stochastic_BelowLevel) &&
                              (iStochastic(gRandomSymbol,PERIOD_H4,Stochastic_Kperiod,Stochastic_Dperiod,Stochastic_Slowing,Stochastic_Method,Stochastic_PriceField,MODE_MAIN,Stochastic_Shift) < Stochastic_BelowLevel && iStochastic(gRandomSymbol,PERIOD_H4,Stochastic_Kperiod,Stochastic_Dperiod,Stochastic_Slowing,Stochastic_Method,Stochastic_PriceField,MODE_SIGNAL,Stochastic_Shift) < Stochastic_BelowLevel) &&

                              MarketInfo(gRandomSymbol, MODE_ASK) < iBands(gRandomSymbol,PERIOD_M1,BollingerBands_Period,BollingerBands_deviation,BollingerBands_BandShift,BollingerBands_AppliedPrice,MODE_LOWER,BollingerBands_Shift) &&
                              MarketInfo(gRandomSymbol, MODE_ASK) < iBands(gRandomSymbol,PERIOD_M5,BollingerBands_Period,BollingerBands_deviation,BollingerBands_BandShift,BollingerBands_AppliedPrice,MODE_LOWER,BollingerBands_Shift) &&
                              MarketInfo(gRandomSymbol, MODE_ASK) < iBands(gRandomSymbol,PERIOD_M15,BollingerBands_Period,BollingerBands_deviation,BollingerBands_BandShift,BollingerBands_AppliedPrice,MODE_LOWER,BollingerBands_Shift) &&
                              MarketInfo(gRandomSymbol, MODE_ASK) < iBands(gRandomSymbol,PERIOD_M30,BollingerBands_Period,BollingerBands_deviation,BollingerBands_BandShift,BollingerBands_AppliedPrice,MODE_LOWER,BollingerBands_Shift) &&
                              MarketInfo(gRandomSymbol, MODE_ASK) < iBands(gRandomSymbol,PERIOD_H1,BollingerBands_Period,BollingerBands_deviation,BollingerBands_BandShift,BollingerBands_AppliedPrice,MODE_LOWER,BollingerBands_Shift) &&
                              MarketInfo(gRandomSymbol, MODE_ASK) < iBands(gRandomSymbol,PERIOD_H4,BollingerBands_Period,BollingerBands_deviation,BollingerBands_BandShift,BollingerBands_AppliedPrice,MODE_LOWER,BollingerBands_Shift)
                           )
                              gSignalStatus = "RSB_BELOW";
                          }
              }

  }

//+------------------------------------------------------------------+

//+------------------------------------------------------------------+
