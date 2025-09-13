//+------------------------------------------------------------------+
//|                                                              HAN |
//|                                  Copyright © 2013, EarnForex.com |
//|                                        http://www.earnforex.com/ |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2013, EarnForex"
#property link      "http://www.earnforex.com"
#property version "1.00"
#property strict
/*
Uses Heiken Ashi candles.
Sells on bullish HA candle, its body is longer than previous body, previous also bullish, and current candle has no lower wick.
Buys on bearish HA candle, its body is longer than previous body, previous also bearish, and current candle has no upper wick.
Exit shorts on bearish HA candle and current candle has no upper wick, previous also bearish.
Exit longs on bullish HA candle and current candle has no lower wick, previous also bullish.
*/
//--- Money management
extern double Lots=0.1;       // Basic lot size
extern bool MM=false;     // If true - ATR-based position sizing
extern int ATR_Period=20;
extern double ATR_Multiplier=1;
extern double Risk=2; // Risk tolerance in percentage points
extern double FixedBalance=0; // If greater than 0, position size calculator will use it instead of actual account balance.
extern double MoneyRisk=0; // Risk tolerance in base currency
extern bool UseMoneyInsteadOfPercentage=false;
extern bool UseEquityInsteadOfBalance=false;
extern int LotDigits=2; // How many digits after dot supported in lot size. For example, 2 for 0.01, 1 for 0.1, 3 for 0.001, etc.
//--- Miscellaneous
extern string OrderCommentary="HAN";
extern int Slippage=100;    // Tolerated slippage in brokers' pips
extern int Magic=1520122013;    // Order magic number
//--- Global variables
//--- Common
int LastBars=0;
bool HaveLongPosition;
bool HaveShortPosition;
double StopLoss; // Not actual stop-loss - just a potential loss of MM estimation.
//+------------------------------------------------------------------+
//| Initialization                                                   |
//+------------------------------------------------------------------+
int init()
  {
   return(0);
  }
//+------------------------------------------------------------------+
//| Deinitialization                                                 |
//+------------------------------------------------------------------+
int deinit()
  {
   return(0);
  }
//+------------------------------------------------------------------+
//| Each tick                                                        |
//+------------------------------------------------------------------+
int start()
  {
   if((!IsTradeAllowed()) || (IsTradeContextBusy()) || (!IsConnected()) || ((!MarketInfo(Symbol(), MODE_TRADEALLOWED)) && (!IsTesting()))) return(0);
//--- Trade only if new bar has arrived
   if(LastBars!=Bars) LastBars=Bars;
   else return(0);

   if(MM)
     {
      //--- Getting the potential loss value based on current ATR.
      StopLoss=iATR(NULL,0,ATR_Period,1)*ATR_Multiplier;
     }
//--- Close conditions   
   bool BearishClose = false;
   bool BullishClose = false;
//--- Signals
   bool Bullish = false;
   bool Bearish = false;
//--- Heiken Ashi indicator values
   double HAOpenLatest,HAOpenPrevious,HACloseLatest,HAClosePrevious,HAHighLatest,HALowLatest;
//---
   HAOpenLatest=iCustom(NULL,0,"Heiken Ashi",2,1);
   HAOpenPrevious= iCustom(NULL,0,"Heiken Ashi",2,2);
   HACloseLatest = iCustom(NULL,0,"Heiken Ashi",3,1);
   HAClosePrevious = iCustom(NULL,0,"Heiken Ashi",3,2);
   if(HAOpenLatest>= HACloseLatest) HAHighLatest = iCustom(NULL,0,"Heiken Ashi",0,1);
   else HAHighLatest= iCustom(NULL,0,"Heiken Ashi",1,1);
   if(HAOpenLatest >= HACloseLatest) HALowLatest = iCustom(NULL, 0, "Heiken Ashi", 1, 1);
   else HALowLatest = iCustom(NULL, 0, "Heiken Ashi", 0, 1);
//--- REVERSED!!!
//--- Close signals
//--- Bullish HA candle, current has no lower wick, previous also bullish
   if((HAOpenLatest<HACloseLatest) && (HALowLatest==HAOpenLatest) && (HAOpenPrevious<HAClosePrevious))
     {
      BullishClose=true;
     }
//--- Bearish HA candle, current has no upper wick, previous also bearish
   else if((HAOpenLatest>HACloseLatest) && (HAHighLatest==HAOpenLatest) && (HAOpenPrevious>HAClosePrevious))
     {
      BearishClose=true;
     }

//--- Sell entry condition
//--- Bullish HA candle, and body is longer than previous body, previous also bullish, current has no lower wick
   if((HAOpenLatest<HACloseLatest) && (HACloseLatest-HAOpenLatest>MathAbs(HAClosePrevious-HAOpenPrevious)) && (HAOpenPrevious<HAClosePrevious) && (HALowLatest==HAOpenLatest))
     {
      Bullish = false;
      Bearish = true;
     }
//--- Buy entry condition
//--- Bearish HA candle, and body is longer than previous body, previous also bearish, current has no upper wick
   else if((HAOpenLatest>HACloseLatest) && (HAOpenLatest-HACloseLatest>MathAbs(HAClosePrevious-HAOpenPrevious)) && (HAOpenPrevious>HAClosePrevious) && (HAHighLatest==HAOpenLatest))
     {
      Bullish = true;
      Bearish = false;
     }
   else
     {
      Bullish = false;
      Bearish = false;
     }
//---
   GetPositionStates();
//---
   if((HaveShortPosition) && (BearishClose)) ClosePrevious();
   if((HaveLongPosition) && (BullishClose)) ClosePrevious();
//---
   if(Bullish)
     {
      if(!HaveLongPosition) fBuy();
     }
   else if(Bearish)
     {
      if(!HaveShortPosition) fSell();
     }
   return(0);
  }
//+------------------------------------------------------------------+
//| Check what position is currently open										|
//+------------------------------------------------------------------+
void GetPositionStates()
  {
   int total=OrdersTotal();
   for(int cnt=0; cnt<total; cnt++)
     {
      if(OrderSelect(cnt,SELECT_BY_POS,MODE_TRADES)==false) continue;
      if(OrderMagicNumber()!=Magic) continue;
      if(OrderSymbol()!=Symbol()) continue;
      //---
      if(OrderType()==OP_BUY)
        {
         HaveLongPosition=true;
         HaveShortPosition=false;
         return;
        }
      else if(OrderType()==OP_SELL)
        {
         HaveLongPosition=false;
         HaveShortPosition=true;
         return;
        }
     }
   HaveLongPosition=false;
   HaveShortPosition=false;
  }
//+------------------------------------------------------------------+
//| Buy                                                              |
//+------------------------------------------------------------------+
void fBuy()
  {
   RefreshRates();
   int result= OrderSend(Symbol(),OP_BUY,LotsOptimized(),Ask,Slippage,0,0,OrderCommentary,Magic);
   if(result == -1)
     {
      int e=GetLastError();
      Print("OrderSend Error: ",e);
     }
  }
//+------------------------------------------------------------------+
//| Sell                                                             |
//+------------------------------------------------------------------+
void fSell()
  {
   RefreshRates();
   int result= OrderSend(Symbol(),OP_SELL,LotsOptimized(),Bid,Slippage,0,0,OrderCommentary,Magic);
   if(result == -1)
     {
      int e=GetLastError();
      Print("OrderSend Error: ",e);
     }
  }
//+------------------------------------------------------------------+
//| Calculate position size depending on money management parameters.|
//+------------------------------------------------------------------+
double LotsOptimized()
  {
   if(!MM) return (Lots);
//---
   double Size,RiskMoney,PositionSize=0;
//---
   if(AccountCurrency() == "") return(0);
//---
   if(FixedBalance>0)
     {
      Size=FixedBalance;
     }
   else if(UseEquityInsteadOfBalance)
     {
      Size=AccountEquity();
     }
   else
     {
      Size=AccountBalance();
     }
//---
   if(!UseMoneyInsteadOfPercentage) RiskMoney=Size*Risk/100;
   else RiskMoney=MoneyRisk;
//---
   double UnitCost = MarketInfo(Symbol(), MODE_TICKVALUE);
   double TickSize = MarketInfo(Symbol(), MODE_TICKSIZE);
//---
   if((StopLoss!=0) && (UnitCost!=0) && (TickSize!=0)) PositionSize=NormalizeDouble(RiskMoney/(StopLoss*UnitCost/TickSize),LotDigits);
//---
   if(PositionSize<MarketInfo(Symbol(),MODE_MINLOT)) PositionSize=MarketInfo(Symbol(),MODE_MINLOT);
   else if(PositionSize>MarketInfo(Symbol(),MODE_MAXLOT)) PositionSize=MarketInfo(Symbol(),MODE_MAXLOT);
//---
   return(PositionSize);
  }
//+------------------------------------------------------------------+
//| Close previous position                                          |
//+------------------------------------------------------------------+
void ClosePrevious()
  {
   int total = OrdersTotal();
   for(int i = 0; i < total; i++)
     {
      if(OrderSelect(i,SELECT_BY_POS)==false) continue;
      if((OrderSymbol()==Symbol()) && (OrderMagicNumber()==Magic))
        {
         if(OrderType()==OP_BUY)
           {
            RefreshRates();
            if(OrderClose(OrderTicket(),OrderLots(),Bid,Slippage)){};
           }
         else if(OrderType()==OP_SELL)
           {
            RefreshRates();
            if(OrderClose(OrderTicket(),OrderLots(),Ask,Slippage)){};
           }
        }
     }
  }
//+------------------------------------------------------------------+
