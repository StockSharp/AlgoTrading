//+------------------------------------------------------------------+
//|                                               TrendCollector.mq4 |
//|                                         Copyright 2014, jimbulux |
//|                                 http://www.jimbulux.blogspot.com |
//|                                                                  |
//|  TODO: Clean and refactor the code.                              |
//|        Add the MACD cond. for the trend reversal - changing slope|
//|        Check for same trades opening and allow oposite trades    |
//+------------------------------------------------------------------+
#property copyright "Copyright 2014, jimbulux"
#property link      "http://www.jimbulux.blogspot.com"
#property version   "1.01"
#property strict

//------------------------------------------ Includes ------------------------------------------------
#include <winuser32.mqh>

//--------------------------------------- Constant values --------------------------------------------
#define DEF_FREE_MARGIN_LIMIT -30                  // Minumal free margin limit to take a trade 
#define DEF_TRADE_MAGIC_NUM_OFFSET 23              // Trade magic number offset

#define DEF_ATR_VOLATILITY_LIM 0.0003              // Volatility limit
#define DEF_ATR_VOLATILITY_TRH 0.0005              // Volatility treshold
#define DEF_SPREAD_LIMIT 3                         // Maximum currency spread to take the trade

#define DEF_MAX_OPEN_SAME_TRADES 1                 // Maximum numbe of opened trades in the same direction
#define DEF_MAX_OPEN_TRADES      1                 // Maximum of opened trades for short and long. 10 Short, 10 Long
#define DEF_TP_PIPS              5                 // Take profit pips for normal more volatile trading. ATR > 0.0005
#define DEF_TP_PIPS_LV           2                 // Take profit pips for lower volatility
#define DEF_SL_PIPS              30                // Stop loss pips              
#define DEF_STOCH_UPPER_LIMIT    80.0              // Stochastic signal uppert limit to take the trade
#define DEF_STOCH_LOWER_LIMIT    20.0              // Stochastic signal lower limit to takse the trade
#define DEF_LOT_SIZE             0.01              // Default lot size
#define DEF_MAX_SLIPPAGE         2                 // Maximum slippage
#define DEF_TIMEFRAME            PERIOD_M5         // Default timeframe
#define DEF_MIN_PAUSE_BTW_TRDS   15                // Minimal pause between trades in minutes
#define DEF_TRD_ENTER_TIMEOUT    30                // Trade enter timeout after stochastic detection in minutes
#define DEF_SLOW_MA_DISTANCE     0                 // Distance from 204MA
#define DEF_FAST_MA_SPEED_TH     3                 // Difference of 4MA against the major trend. With small difference enter the trade

// ------------------------------------ Trading hours -----------------------------------------
#define DEF_TRADE_TIME_START     5                 // Beginning of the trading
#define DEF_TRADE_TIME_END       24                // End of the trading time

#define DEF_NEWS_HUNTER          true              // Enable / disable market news detector algorithm, that stops the main algorithm and switches to the news hunter algorithm
#define DEF_NEWS_HUNTER_URL      "http://www.forexfactory.com/ffcal_week_this.xml"
#define DEF_NEWS_HUNTER_TIMESPAN 2700              // For how many minutes we should not trade 45min = 2700s

const string DEF_AUTOTRADE_COMMENT="jOrder_";    // Autotrader order comment

//---------------------------------------- Input parameters ------------------------------------------
input int EA_FAST_MA_TH=DEF_FAST_MA_SPEED_TH;
input int EA_SLOW_MA_DISTANCE=DEF_SLOW_MA_DISTANCE;
input ENUM_MA_METHOD EA_MA_METHOD=MODE_EMA;
input int EA_MAX_OPEN_TRADES=DEF_MAX_OPEN_TRADES;
input int EA_MAX_OPEN_SAME_TRADES=DEF_MAX_OPEN_SAME_TRADES;
input int EA_SL_PIPS = DEF_SL_PIPS;
input int EA_TP_PIPS = DEF_TP_PIPS;
input int EA_TP_PIPS_LV=DEF_TP_PIPS_LV;
input int EA_TRADING_START_HOUR=DEF_TRADE_TIME_START;
input int EA_TRADING_END_HOUR=DEF_TRADE_TIME_END;
input double EA_ATR_VOLATILITY_LIM = DEF_ATR_VOLATILITY_LIM;
input double EA_ATR_VOLATILITY_TRH = DEF_ATR_VOLATILITY_TRH;
input double EA_TRADE_LOT_SIZE=DEF_LOT_SIZE;
input int EA_SPREAD_LIMIT=DEF_SPREAD_LIMIT;
input int EA_TRADE_MAX_SLIPPAGE=DEF_MAX_SLIPPAGE;
input double EA_FREE_MARGIN_LIMIT = DEF_FREE_MARGIN_LIMIT;
input double EA_STOCH_UPPER_LIMIT = DEF_STOCH_UPPER_LIMIT;
input double EA_STOCH_LOWER_LIMIT = DEF_STOCH_LOWER_LIMIT;
input int EA_TIME_FRAME=DEF_TIMEFRAME;
input int EA_MIN_PAUSE_BTW_TRDS=DEF_MIN_PAUSE_BTW_TRDS;

// RSS feed news hunter
input bool EA_NEWS_HUNTER=DEF_NEWS_HUNTER;
input long EA_NEWS_HUNTER_TIMESPAN=DEF_NEWS_HUNTER_TIMESPAN;
//---------------------------------------------- Enums -----------------------------------------------
enum TradeDirection 
  {
   TRADE_SHORT,
   TRADE_LONG,
   TRADE_NONE
  };

//------------------------------------------ Global variables ----------------------------------------
int gLostTradesCount=0,gWonTradesCount=0;
int gOrderCounter=0;
int gCurrentTimePeriod=EA_TIME_FRAME;
// ------------------------------------------- Classes -----------------------------------------------
/**
 * Trader account info class
 */
class TradeAccountInfo
  {
private:
   double            balance;
   double            margin;
   double            equity;
public:
                     TradeAccountInfo(void);
   double            GetBalance(void);
   double            GetMargin(void);
   double            GetEquity(void);
  };
TradeAccountInfo::TradeAccountInfo(void){}
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double TradeAccountInfo::GetBalance(void) 
  {
   return(AccountBalance());
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double TradeAccountInfo::GetMargin(void) 
  {
   return(AccountFreeMargin());
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
/**
 * Trader order class
 */
class TradeOrder
  {
private:
   string            tradeSymbol;
   int               tradeTicket;
   bool              tradeOpen;
   double            tradeOpenPrice;
   datetime          tradeOpenTime;
   double            currentSpread;
   double            currentStopLevel;
   double            currentTickSize;
   void              updateMarketInfo(void);
   int               tradeMagicNumber;
   int               tradeTimeFrame;
   TradeDirection    tradeOpenDirection;
public:
   TradeDirection    GetOpenTradeDirection();
   int               OpenLong(void);
   int               OpenShort(void);
   bool              IsOpen(void);
   bool              CloseTrade(void);
   double            GetOpenPrice(void);
   datetime          GetOpenTime(void);
   double            GetNormalizedStopLoss(void);
   double            GetNormalizedTakeProfit(void);
   void              CheckOpenTradeTpSl(void);
   int               GetTradeTimeFrame(void);
   void              SetTradingSymbol(string pSymbol);
   int               GetGrossProfitPips(void);
   int               GetNetProfitPips(void);
   double            GetGrossProfit(void);
   int               GetOpenTimeDurationMinutes(void);
   int               GetCurrentSpread(void);
  };
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int TradeOrder::GetCurrentSpread(void) 
  {
   updateMarketInfo();
   return currentSpread;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int TradeOrder::GetOpenTimeDurationMinutes(void) 
  {
   int retVal=-1;
   if(tradeOpen) 
     {
      retVal=(TimeHour(TimeCurrent()-tradeOpenTime)*60)+TimeMinute(TimeCurrent()-tradeOpenTime);
     }

   return retVal;
  }
// Profit in pips with commisions and swap
int TradeOrder::GetGrossProfitPips(void) 
  {
   int retVal=0;
   double currPrice=MarketInfo(tradeSymbol,MODE_ASK);
   if(tradeOpenDirection==TRADE_LONG) 
     {
      retVal=(int)((currPrice-tradeOpenPrice)/currentTickSize);
        } else if(tradeOpenDirection==TRADE_SHORT) {
      retVal=(int)((tradeOpenPrice-currPrice)/currentTickSize);
     }

   return retVal;
  }
// Profit in pips without commisions
int TradeOrder::GetNetProfitPips(void) 
  {
   int retVal=0;
   double currPrice=MarketInfo(tradeSymbol,MODE_ASK);
   if(tradeOpenDirection==TRADE_LONG) 
     {
      retVal=(int)((currPrice-tradeOpenPrice+currentSpread)/currentTickSize);
        } else if(tradeOpenDirection==TRADE_SHORT) {
      retVal=(int)((tradeOpenPrice-currPrice+currentSpread)/currentTickSize);
     }

   return retVal;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double TradeOrder::GetGrossProfit(void) 
  {
   double retVal=0.0;
   if(OrderSelect(tradeTicket,SELECT_BY_TICKET)) 
     {
      retVal=OrderProfit()-OrderSwap()-OrderCommission();
     }

   return retVal;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
TradeOrder::SetTradingSymbol(string pSymbol) 
  {
   tradeSymbol=pSymbol;
   tradeOpen=false;
   updateMarketInfo();
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int TradeOrder::OpenLong(void) 
  {
   if(!tradeOpen) 
     {
      updateMarketInfo();
      tradeOpenPrice=MarketInfo(tradeSymbol,MODE_ASK);
      tradeMagicNumber=DEF_TRADE_MAGIC_NUM_OFFSET+gOrderCounter;
      tradeOpenDirection=TRADE_LONG;

      tradeTicket=OrderSend(tradeSymbol,OP_BUY,EA_TRADE_LOT_SIZE,tradeOpenPrice,EA_TRADE_MAX_SLIPPAGE,0,0,
                            DEF_AUTOTRADE_COMMENT+IntegerToString(gOrderCounter++),tradeMagicNumber,0,clrGreen);

      if(tradeTicket>0) 
        {
         // For ECN Broker compatibility set stoploss after opening ticket
         if(OrderSelect(tradeTicket,SELECT_BY_TICKET)) 
           {
            if(!OrderModify(tradeTicket,tradeOpenPrice,GetNormalizedStopLoss(),GetNormalizedTakeProfit(),0,clrGreen))
               Print("Error modifying long order with ticket:",tradeTicket);
           }
         tradeOpen=true;
         tradeOpenTime=TimeCurrent();
           } else {
         tradeOpen=false;
         tradeOpenDirection=TRADE_NONE;
        }
     }

   return(tradeTicket);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int TradeOrder::OpenShort(void) 
  {
   if(!tradeOpen) 
     {
      updateMarketInfo();
      tradeOpenPrice=MarketInfo(tradeSymbol,MODE_BID);
      tradeMagicNumber=DEF_TRADE_MAGIC_NUM_OFFSET+gOrderCounter;
      tradeOpenDirection=TRADE_SHORT;
      tradeTicket=OrderSend(tradeSymbol,OP_SELL,EA_TRADE_LOT_SIZE,tradeOpenPrice,EA_TRADE_MAX_SLIPPAGE,0,0,
                            DEF_AUTOTRADE_COMMENT+IntegerToString(gOrderCounter++),tradeMagicNumber,0,clrRed);

      if(tradeTicket>0) 
        {
         // For ECN Broker compatibility set stoploss after opening ticket
         if(OrderSelect(tradeTicket,SELECT_BY_TICKET)) 
           {
            if(!OrderModify(tradeTicket,tradeOpenPrice,GetNormalizedStopLoss(),GetNormalizedTakeProfit(),0,clrRed))
               Print("Error modifying short order with ticket:",tradeTicket);
           }
         tradeOpen=true;
         tradeOpenTime=TimeCurrent();
           } else {
         tradeOpen=false;
         tradeOpenDirection=TRADE_NONE;
        }
     }

   return(tradeTicket);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool TradeOrder::IsOpen(void) 
  {
   return(tradeOpen);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool TradeOrder::CloseTrade(void) 
  {
   double askPrice=MarketInfo(tradeSymbol,MODE_ASK);

   if(OrderClose(tradeTicket,EA_TRADE_LOT_SIZE,askPrice,EA_TRADE_MAX_SLIPPAGE,clrYellow)) 
     {
      tradeOpen=false;
      tradeOpenDirection=TRADE_NONE;

      if(OrderSelect(tradeTicket,SELECT_BY_TICKET)==true) 
        {
         Print("Trade closed at: ",askPrice,"with profit: ",OrderProfit());
        }
     }
   return(!tradeOpen);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double TradeOrder::GetNormalizedStopLoss() 
  {
   if(tradeOpenDirection==TRADE_LONG) 
     {
      return(NormalizeDouble(tradeOpenPrice - ((EA_SL_PIPS) * currentTickSize), Digits));
        } else if(tradeOpenDirection==TRADE_SHORT) {
      return(NormalizeDouble(tradeOpenPrice + ((EA_SL_PIPS) * currentTickSize), Digits));
     }
   return(0.0);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double TradeOrder::GetNormalizedTakeProfit() 
  {
   double currAtr=iATR(tradeSymbol,EA_TIME_FRAME,14,0);    // ATR
   if(tradeOpenDirection==TRADE_LONG) 
     {
      return(NormalizeDouble(tradeOpenPrice + (((currAtr < EA_ATR_VOLATILITY_TRH) ? EA_TP_PIPS_LV : EA_TP_PIPS) * currentTickSize), Digits));
        } else if(tradeOpenDirection==TRADE_SHORT) {
      return(NormalizeDouble(tradeOpenPrice - (((currAtr < EA_ATR_VOLATILITY_TRH) ? EA_TP_PIPS_LV : EA_TP_PIPS) * currentTickSize), Digits));
     }
   return(0.0);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
TradeDirection TradeOrder::GetOpenTradeDirection() 
  {
   return(tradeOpenDirection);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double TradeOrder::GetOpenPrice(void) 
  {
   return(tradeOpenPrice);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
datetime TradeOrder::GetOpenTime() 
  {
   return(tradeOpenTime);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void TradeOrder::CheckOpenTradeTpSl(void) 
  {
   if(OrderSelect(tradeTicket,SELECT_BY_TICKET)==true) 
     {
      if(OrderCloseTime()!=0) 
        { // Order was closed by SL/TP
         tradeOpen=false;
         tradeOpenDirection=TRADE_NONE;
        }
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int TradeOrder::GetTradeTimeFrame(void) 
  {
   return(tradeTimeFrame);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
TradeOrder::updateMarketInfo(void) 
  {
   currentSpread = MarketInfo(tradeSymbol,MODE_SPREAD);        // Save current spread
   currentStopLevel = MarketInfo(tradeSymbol,MODE_STOPLEVEL);  // Save current stop level
   currentTickSize = MarketInfo(tradeSymbol, MODE_TICKSIZE);   // Save current tick size
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
/**
 * Trade expert class
 */
class TradeExpert
  {
private:
   TradeDirection    marketMaDirection;
   TradeAccountInfo *accountInfo;
   TradeOrder       *orderLong[];
   TradeOrder       *orderShort[];
   int               currTimeHour;
   double            lastAtr;
   double            marketBidPrice;
   double            marketAskPrice;
   double            marketTickSize;
   double            marketSpread;
   int               currentTimeFrame;
   bool              tradeEnterWithTrend;
   int               sameTradesCnt;
   string            tradeSymbol;
   int               lastTradeEnterMinute;
   int               lastStochMarkedShortTradeTimeStamp;
   int               lastStochMarkedLongTradeTimeStamp;
   void              checkForOpenTrades(void);
   double            doubleNormalize(double pToNormalize);
   void              initialize(void);
   bool              checkCrossing(TradeDirection pCrossDir,double pFirstPrevPar,double pSecPrevPar,double pFirstCurrPar,double pSecCurrPar);
   bool              checkLevelCrossing(TradeDirection pCrossDir,double pCrossLevel,double pPrevVal,double pCurrVal);
public:
                     TradeExpert(void);
                     TradeExpert(string pCurrencySymbol);
   int               GetNumberOfOpenTrades(TradeDirection pTradeDirection);
   void              CheckTrade(void);
   void              OnMarketTick(void);
   void              OnDispose(void);
  };
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
TradeExpert::TradeExpert(void) 
  {
   tradeSymbol=Symbol();
   initialize();
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
TradeExpert::TradeExpert(string pCurrencySymbol) 
  {
   tradeSymbol=pCurrencySymbol;
   initialize();
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void TradeExpert::OnDispose(void) 
  {
   delete accountInfo;
   for(int i=0; i<EA_MAX_OPEN_TRADES; i++) 
     {
      delete orderLong[i];
      delete orderShort[i];
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void TradeExpert::CheckTrade() 
  {
// Check the market direction using AO (Awsome oscilator)
//marketMaDirection = GetMarketDirection(-1);               

/** 
    * Immediate close check section without new bar forming wait
    * Silence is used for the exit optimization. If the stochastic register for an exit but silence is above 99.9 we wait for the 
    * silence to go back to lower values below 99.9. TODO: This should be also the best point for the oposite trade start.
    * Silencer and oposite trade positioning is used only when the above condition is met and stochastic's signal value is above STOCH_UPPER_LIMIT
    * MA's are used at closing to delay the close when the trade is strong in the opened direction
    */
//double silencePrev = iCustom(tradeSymbol, currentTimeFrame, "Silence2_02", 11, 96, 1, 1); 
//double silenceCurr = iCustom(tradeSymbol, currentTimeFrame, "Silence2_02", 11, 96, 1, 0); 
// Maybe use silencer to detect market activity and trade only in active hours
   marketTickSize=MarketInfo(tradeSymbol,MODE_TICKSIZE);   // Save current tick size
   marketAskPrice = MarketInfo(tradeSymbol, MODE_ASK);
   marketBidPrice = MarketInfo(tradeSymbol, MODE_BID);

   double stochMainPrev = iStochastic(tradeSymbol, EA_TIME_FRAME, 13, 3, 3, MODE_SMA, 0, MODE_MAIN, 1);
   double stochMainCurr = iStochastic(tradeSymbol, EA_TIME_FRAME, 13, 3, 3, MODE_SMA, 0, MODE_MAIN, 0);
   double stochSigPrev = iStochastic(tradeSymbol, EA_TIME_FRAME, 13, 3, 3, MODE_SMA, 0, MODE_SIGNAL, 1);
   double stochSigCurr = iStochastic(tradeSymbol, EA_TIME_FRAME, 13, 3, 3, MODE_SMA, 0, MODE_SIGNAL, 0);

   double maPrev = iMA(tradeSymbol, EA_TIME_FRAME, 204, 0,  EA_MA_METHOD, PRICE_CLOSE, 2);
   double maCurr = iMA(tradeSymbol, EA_TIME_FRAME, 204, 0,  EA_MA_METHOD, PRICE_CLOSE, 0);

   double maPrevFast = iMA(tradeSymbol, PERIOD_M1, 4, 0,  EA_MA_METHOD, PRICE_CLOSE, 2);
   double maCurrFast = iMA(tradeSymbol, PERIOD_M1, 4, 0,  EA_MA_METHOD, PRICE_CLOSE, 0);

   lastAtr=iATR(tradeSymbol,currentTimeFrame,4,0);    // ATR

/* Logic for closing open trades */
   for(int i=0; i<EA_MAX_OPEN_TRADES; i++) 
     {
      if((orderLong[i].IsOpen()) && 
         ((lastAtr < EA_ATR_VOLATILITY_TRH) && (orderLong[i].GetGrossProfitPips() >= EA_TP_PIPS_LV)) &&
         ((lastAtr > EA_ATR_VOLATILITY_TRH) && (orderLong[i].GetGrossProfitPips() >= EA_TP_PIPS)))
        {
         orderLong[i].CloseTrade();
        }
      if((orderShort[i].IsOpen()) && 
         ((lastAtr < EA_ATR_VOLATILITY_TRH) && (orderShort[i].GetGrossProfitPips() >= EA_TP_PIPS_LV)) &&
         ((lastAtr > EA_ATR_VOLATILITY_TRH) && (orderShort[i].GetGrossProfitPips() >= EA_TP_PIPS)))
        {
         orderShort[i].CloseTrade();
        }
     }

/*
    * Using DI+ DI- to avoid entering trades upon stochastic signal in the wrong direction. 
    * This rule can only override CCI channel index with values over 120.
    */
   if((GetNumberOfOpenTrades(TRADE_NONE)<EA_MAX_OPEN_TRADES)
      && (MathAbs(lastTradeEnterMinute-TimeMinute(TimeCurrent()))>=EA_MIN_PAUSE_BTW_TRDS)
      && (!gNewsFeed.IsNewsWithinTimeSpan(FXNEWS_IMPACT_HIGH,tradeSymbol,EA_NEWS_HUNTER_TIMESPAN))) 
     {
      currTimeHour=TimeHour(TimeCurrent());                  // Current time in hours to control when to enter the market

                                                             // Check for the spread to enter the trade
      if((marketSpread<=EA_SPREAD_LIMIT) && (lastAtr>=EA_ATR_VOLATILITY_LIM)
         && (currTimeHour>=EA_TRADING_START_HOUR) && (currTimeHour<=EA_TRADING_END_HOUR)) 
        {
         int slowMaDistanceNormalized=(int)((maCurr-marketBidPrice)/marketTickSize);
         int fastMaSpeed=(int)(((maCurrFast-maPrevFast)/marketTickSize)*10);

         if((maPrev>maCurr) && (stochMainCurr>EA_STOCH_UPPER_LIMIT)
            && (stochSigCurr>EA_STOCH_UPPER_LIMIT)
            && (checkCrossing(TRADE_SHORT,stochMainPrev,stochSigPrev,stochMainCurr,stochSigCurr))
            && (slowMaDistanceNormalized>EA_SLOW_MA_DISTANCE))
           {
            lastStochMarkedShortTradeTimeStamp=TimeMinute(TimeCurrent());   // Mark the Stochastic crossing time
           }

         if((fastMaSpeed<(-1)*EA_FAST_MA_TH)
            && (lastStochMarkedShortTradeTimeStamp!=-1)
            && (slowMaDistanceNormalized>EA_SLOW_MA_DISTANCE)
            && (stochSigCurr>EA_STOCH_UPPER_LIMIT)
            && (GetNumberOfOpenTrades(TRADE_SHORT)<EA_MAX_OPEN_SAME_TRADES)
            && (MathAbs(lastStochMarkedShortTradeTimeStamp-TimeMinute(TimeCurrent()))<=DEF_TRD_ENTER_TIMEOUT))
           {
            for(int i=0; i<EA_MAX_OPEN_TRADES; i++) 
              {
               if(!orderShort[i].IsOpen()) 
                 {
                  lastTradeEnterMinute=TimeMinute(TimeCurrent());  // Mark last opened trade time
                  lastStochMarkedShortTradeTimeStamp=-1;
                  orderShort[i].OpenShort(); // Open short trade
                 }
              }
           }
         if((maPrev<maCurr) && (stochMainCurr<EA_STOCH_LOWER_LIMIT)
            && (stochSigCurr<EA_STOCH_LOWER_LIMIT)
            && (checkCrossing(TRADE_LONG,stochMainPrev,stochSigPrev,stochMainCurr,stochSigCurr))
            && (slowMaDistanceNormalized<EA_SLOW_MA_DISTANCE))
           {
            lastStochMarkedLongTradeTimeStamp=TimeMinute(TimeCurrent()); // Mark the Stochastic crossing time
           }

         if((fastMaSpeed>EA_FAST_MA_TH)
            && (lastStochMarkedLongTradeTimeStamp!=-1)
            && (slowMaDistanceNormalized<EA_SLOW_MA_DISTANCE)
            && (stochSigCurr<EA_STOCH_LOWER_LIMIT)
            && (GetNumberOfOpenTrades(TRADE_LONG)<EA_MAX_OPEN_SAME_TRADES)
            && (MathAbs(lastStochMarkedLongTradeTimeStamp-TimeMinute(TimeCurrent()))<=DEF_TRD_ENTER_TIMEOUT))
           {
            for(int i=0; i<EA_MAX_OPEN_TRADES; i++) 
              {
               if(!orderLong[i].IsOpen()) 
                 {
                  lastTradeEnterMinute=TimeMinute(TimeCurrent());  // Mark last opened trade time
                  lastStochMarkedLongTradeTimeStamp=-1;
                  orderLong[i].OpenLong(); // Open long trade
                 }
              }
           }
        }
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void TradeExpert::OnMarketTick(void) 
  {
// Check for automatic TP/SL and update the status accordingly
   for(int i=0; i<EA_MAX_OPEN_TRADES; i++) 
     {
      orderLong[i].CheckOpenTradeTpSl();
      orderShort[i].CheckOpenTradeTpSl();
     }

   CheckTrade();  // Perform expert trade check and trade
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
/**
 * Checks for open trades from previuous session
 */
TradeExpert::checkForOpenTrades(void) 
  {
   for(int i=0; i<OrdersTotal(); i++) 
     {
      OrderSelect(i,SELECT_BY_POS);
      // Contains some open autotrader trades
      if((OrderSymbol()==tradeSymbol) && (StringFind(OrderComment(),DEF_AUTOTRADE_COMMENT)!=-1)) 
        {
         if(OrderProfit()>0) 
           {
            Print("Automatically closing old trade",OrderComment());
            OrderClose(OrderTicket(),OrderLots(),OrderClosePrice(),EA_TRADE_MAX_SLIPPAGE);
              } else {
            Alert("Automatically created old order with negative profit!!! Order ticket: ",OrderTicket(),", Order profit: ",OrderProfit());
           }
        }
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int TradeExpert::GetNumberOfOpenTrades(TradeDirection pTradeDirection) 
  {
   int ordersCount=0;

   for(int i=0; i<OrdersTotal(); i++) 
     {
      OrderSelect(i,SELECT_BY_POS);
      // Contains some open autotrader trades
      if((OrderSymbol()==tradeSymbol) && (StringFind(OrderComment(),DEF_AUTOTRADE_COMMENT)!=-1)) 
        {
         if(((pTradeDirection==TRADE_LONG) && (OrderType()==OP_BUY))
            || ((pTradeDirection==TRADE_SHORT) && (OrderType()==OP_SELL))
            || (pTradeDirection==TRADE_NONE)) 
           {
            ordersCount=ordersCount+1;
           }
        }
     }
   return ordersCount;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double TradeExpert::doubleNormalize(double pToNormalize) 
  {
   return(NormalizeDouble(pToNormalize, Digits));
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void TradeExpert::initialize(void) 
  {
   accountInfo=new TradeAccountInfo();
   currentTimeFrame=EA_TIME_FRAME;
   checkForOpenTrades();
   sameTradesCnt=0;
   marketTickSize=MarketInfo(tradeSymbol,MODE_TICKSIZE);   // Save current tick size  

   ArrayResize(orderLong,EA_MAX_OPEN_TRADES);
   ArrayResize(orderShort,EA_MAX_OPEN_TRADES);
   for(int i=0; i<EA_MAX_OPEN_TRADES; i++) 
     {
      orderLong[i]=new TradeOrder();
      orderShort[i]=new TradeOrder();
      orderLong[i].SetTradingSymbol(tradeSymbol);
      orderShort[i].SetTradingSymbol(tradeSymbol);
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool TradeExpert::checkCrossing(TradeDirection pCrossDir,double pFirstPrevPar,double pSecPrevPar,double pFirstCurrPar,double pSecCurrPar) 
  {
   if(pCrossDir==TRADE_LONG) 
     {
      if((pFirstPrevPar<pSecPrevPar) && (pFirstCurrPar>pSecCurrPar)) 
        {
         return(true);
        }
     }
   else if(pCrossDir==TRADE_SHORT) 
     {
      if((pFirstPrevPar>pSecPrevPar) && (pFirstCurrPar<pSecCurrPar)) 
        {
         return(true);
        }
     }
   return(false);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool TradeExpert::checkLevelCrossing(TradeDirection pCrossDir,double pCrossLevel,double pPrevVal,double pCurrVal) 
  {
   if(pCrossDir==TRADE_LONG) 
     {
      if((pPrevVal<=pCrossLevel) && (pCurrVal>=pCrossLevel)) 
        {
         return(true);
        }
     }
   else if(pCrossDir==TRADE_SHORT) 
     {
      if((pPrevVal>=pCrossLevel) && (pCurrVal<=pCrossLevel)) 
        {
         return(true);
        }
     }
   return(false);
  }

//+------------------------------------------------------------------+
//| FxNewsEvent class                                                |
//+------------------------------------------------------------------+
int FXNEWS_IMPACT_UKNOWN=-1;
int FXNEWS_IMPACT_LOW=1;
int FXNEWS_IMPACT_MEDIUM=2;
int FXNEWS_IMPACT_HIGH=3;
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
class FxNewsEvent
  {
private:
   string            country;
   string            title;
   int               impact;
   datetime          timestamp;
public:
                     FxNewsEvent();
   string            GetCountry(void);
   int               GetImpact(void);
   datetime          GetTimestamp(void);
   string            GetTitle(void);
   void              SetCountry(string pCurr);
   void              SetImpact(int pImpact);
   void              SetTimestamp(datetime pTimestamp);
   void              SetTitle(string pTitle);

  };
FxNewsEvent::FxNewsEvent() {}
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
string FxNewsEvent::GetCountry(void)
  {
   return country;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int FxNewsEvent::GetImpact(void) 
  {
   return impact;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
datetime FxNewsEvent::GetTimestamp(void) 
  {
   return timestamp;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void FxNewsEvent::SetCountry(string pCountry) 
  {
   country=pCountry;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void FxNewsEvent::SetImpact(int pImpact) 
  {
   impact=pImpact;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void FxNewsEvent::SetTimestamp(datetime pTimestamp) 
  {
   timestamp=pTimestamp;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
string FxNewsEvent::GetTitle(void) 
  {
   return title;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void FxNewsEvent::SetTitle(string pTitle) 
  {
   title=pTitle;
  }
//+------------------------------------------------------------------+
//| RSS Forex News Feed class                                        |
//+------------------------------------------------------------------+
class RssFxNewsFeed
  {
private:
   string            rssFeedContent[];
   string            feedUrl;
   string            lastNewsAvoidMessage;
   string            avoidMessage;
   void              parseRssFeed(string pUrl);
public:
                     RssFxNewsFeed(string pUrl);
   int               RssFxNewsFeed::GetRssFeeds(FxNewsEvent &fxNewsArray[]);
   bool              RssFxNewsFeed::IsNewsWithinTimeSpan(int pNewsLevel,string pTradeSymbol,int pTimeSpan);
  };
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
RssFxNewsFeed::RssFxNewsFeed(string pUrl) 
  {
   feedUrl=pUrl;
   parseRssFeed(feedUrl);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
RssFxNewsFeed::parseRssFeed(string pUrl) 
  {
   string cookie=NULL,headers;
   char result[],post[];
   int timeout=5000;
   string webResData[];

   int res=WebRequest("GET",pUrl,cookie,NULL,timeout,post,0,result,headers);
   if(res==-1) 
     {
      Print("Error code =",GetLastError());
      //--- maybe the URL is not added, show message to add it
      MessageBox("Add address '"+pUrl+"' in Expert Advisors tab of the Options window","Error",MB_ICONINFORMATION);
      return;
     }
   else 
     {
      Print("Fx news downloaded from: "+pUrl+". Parsing...");
     }

   string stringTags[];
   string webRes=CharArrayToString(result,0,WHOLE_ARRAY,CP_OEMCP);
   StringSplit(webRes,StringGetCharacter(">",0),stringTags);
   int tagsNumber=ArraySize(stringTags);

   int i=0;
   string text="";
   string currTag;

   string eventDate="",eventTime="";
   string tempVar[];
   int eventCounter=0;

   for(i=0; i<tagsNumber; i++)
     {
      currTag=StringTrimLeft(stringTags[i]+">");

      if(currTag=="<weeklyevents>")
        {
         Print("News block start;");
        }
      else if(currTag=="<event>")
        {
         eventCounter++;
         ArrayResize(gFxEvents,eventCounter);
         gFxEvents[eventCounter-1]=new FxNewsEvent();
        }
      else if(currTag=="<date>")
        {
         eventDate=StringSubstr(stringTags[i+1],9,StringLen(stringTags[i+1])-11);   // - 9 for ]]</date>

         int k=StringSplit(eventDate,StringGetCharacter("-",0),tempVar);
         eventDate=tempVar[2]+"."+tempVar[0]+"."+tempVar[1];
        }
      else if(currTag=="<time>")
        {
         eventTime=StringSubstr(stringTags[i+1],9,StringLen(stringTags[i+1])-11);   // - 9 for ]]</time>

         int k=StringSplit(eventTime,StringGetCharacter(":",0),tempVar);
         string ampm=StringSubstr(tempVar[1],2,2);
         if(ampm=="am") 
           {
            eventTime=tempVar[0]+":"+StringSubstr(tempVar[1],0,2);
              }else {
            eventTime=IntegerToString(12+StringToInteger(tempVar[0]))+":"+StringSubstr(tempVar[1],0,2);
           }

         gFxEvents[eventCounter-1].SetTimestamp(StrToTime(eventDate+" "+eventTime));
        }
      else if(currTag=="<title>")
        {
         gFxEvents[eventCounter-1].SetTitle(StringSubstr(stringTags[i+1],0,StringLen(stringTags[i+1])-7));
        }
      else if(currTag=="<impact>")
        {
         string impact=StringSubstr(stringTags[i+1],9,StringLen(stringTags[i+1])-11);
         int impactEnum=FXNEWS_IMPACT_UKNOWN;
         if(impact=="Low") 
           {
            gFxEvents[eventCounter-1].SetImpact(FXNEWS_IMPACT_LOW);
              } else if(impact=="Medium") {
            gFxEvents[eventCounter-1].SetImpact(FXNEWS_IMPACT_MEDIUM);
              } else if(impact=="High") {
            gFxEvents[eventCounter-1].SetImpact(FXNEWS_IMPACT_HIGH);
           }
        }
      else if(currTag=="<country>")
        {
         gFxEvents[eventCounter-1].SetCountry(StringSubstr(stringTags[i+1],0,StringLen(stringTags[i+1])-9));
        }
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool RssFxNewsFeed::IsNewsWithinTimeSpan(int pNewsImpact,string pTradeSymbol,int pTimeSpan)
  {
   bool newsWithinTimeSpan=false;
   int aSize = ArraySize(gFxEvents);
   for(int i = 0; i < aSize; i++)
     {
      int timeDiff =(TimeCurrent() - gFxEvents[i].GetTimestamp());
      if((timeDiff>= ((-1)*pTimeSpan))
         && (timeDiff<=pTimeSpan)) 
        {
         avoidMessage="Close news event: "+gFxEvents[i].GetTitle()+", at "+TimeToString(gFxEvents[i].GetTimestamp())
                      +", with impact level: "+gFxEvents[i].GetImpact()+", for country: "+gFxEvents[i].GetCountry();
         if(lastNewsAvoidMessage!=avoidMessage) 
           {
            Print(avoidMessage);
            //Alert(avoidMessage);      
            lastNewsAvoidMessage=avoidMessage;
           }
         if((StringFind(pTradeSymbol,gFxEvents[i].GetCountry())!=-1)
            && (gFxEvents[i].GetImpact()==pNewsImpact)) 
           {
            return true;
           }
        }
     }

   return newsWithinTimeSpan;
  }


// --------------------------------- Global variables --------------------------------
TradeAccountInfo *gAccountInfo;
TradeExpert *gTE;
RssFxNewsFeed *gNewsFeed;
FxNewsEvent *gFxEvents[];
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
//--- create timer
//EventSetTimer(10);

//---
// Instantiation of objects
   gAccountInfo=new TradeAccountInfo();
   gTE=new TradeExpert(Symbol());
   if(EA_NEWS_HUNTER)
     {
      gNewsFeed=new RssFxNewsFeed(DEF_NEWS_HUNTER_URL);
     }

   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
//--- destroy timer
//EventKillTimer();

// Deleting objects
   delete gAccountInfo;
   delete gNewsFeed;

   gTE.OnDispose();
   delete gTE;
  }
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
  {
//---
   if(gAccountInfo.GetMargin()>EA_FREE_MARGIN_LIMIT) 
     {
      gTE.OnMarketTick();
     }
  }
//+------------------------------------------------------------------+
//| Timer function                                                   |
//+------------------------------------------------------------------+
void OnTimer()
  {
//---
  }
//+------------------------------------------------------------------+
//| Tester function                                                  |
//+------------------------------------------------------------------+
double OnTester()
  {
//---
   double ret=0.0;
   Print("Won trades: ",gWonTradesCount);
   Print("Lost trades: ",gLostTradesCount);
//---

//---
   return(ret);
  }
//+------------------------------------------------------------------+
//| ChartEvent function                                              |
//+------------------------------------------------------------------+
void OnChartEvent(const int id,
                  const long &lparam,
                  const double &dparam,
                  const string &sparam)
  {
//---

  }
//+------------------------------------------------------------------+

void SetTimeFrame(int pTimeFrame) 
  {
   ChartSetSymbolPeriod(0, NULL, pTimeFrame);  // Set to the new timeframe
   gCurrentTimePeriod = pTimeFrame;            // Save the new timeframe
  }
//+------------------------------------------------------------------+
