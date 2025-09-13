/*
Rapid Doji EA

This MQL5 Expert Advisor uses a simple strategy on a daily chart.

For step-by-step video tutorials (with transcripts) on how this EA
was built, go to the link below.
*/
#property copyright		"Copyright 2020, Anthony J. Garot"
#property link				"https://www.garot.com/trading/"
#property version			"1.01"
#property description	""
#property description	"Rapid Doji EA"
#property description	""
#property description	"Entry: Two pending orders around each Daily Doji bar."
#property description	"Risk adjustment: Fixed trailing SL."
#property strict

#include <Trade\Trade.mqh>
#include <Trade\AccountInfo.mqh>

#define NUM_RATES 3

//--- globals
CTrade			g_trade;					// trade object
double			g_pointSize = 0.;		// initialize to ludicrous value
//--- handles
int				g_handleAtr = 0;
//--- arrays
MqlRates			g_rates[];
double			g_atr[];

//--- input parameters
input double	SL_ATR_FACTOR = 1.75;			// ATR Factor to determine distance to place SL
input int		AR_FIXED_TRAILING_PTS = 2700;	// Max points to keep SL from prices while trailing
input int		InpAtrPeriod = 14;				// ATR Period

//--- constants
const long		MAGIC_NUMBER = 5678900;
const double	TRADE_VOLUME = 0.10;				// For strategy development, fixed lot size of 0.10 is good
const int		CHART_ID = 0;						// Current chart
const int		SUB_WINDOW = 0;					// Main Window

//+------------------------------------------------------------------+
//| Initialization of the EA                                         |
//+------------------------------------------------------------------+
int OnInit(void)
  {
   g_trade.SetExpertMagicNumber(MAGIC_NUMBER);

   g_pointSize = SymbolInfoDouble(_Symbol, SYMBOL_POINT);
   PrintFormat("g_pointSize is [%.5f]", g_pointSize);

//--- ATR
   g_handleAtr = iATR(_Symbol,			// symbol
                      _Period,			// period
                      InpAtrPeriod);	// atr_period
   if(g_handleAtr == INVALID_HANDLE)
     {
      PrintFormat("%s(): Error creating iATR indicator handle for [%s] [%d]", _Symbol, _Period);
      return INIT_FAILED;
     }

   ArrayResize(g_rates, NUM_RATES);
   ArraySetAsSeries(g_rates, true);
   ArrayResize(g_atr, NUM_RATES);
   ArraySetAsSeries(g_atr, true);

//--- ok
   Print("INIT_SUCCEEDED for EA!");
   return(INIT_SUCCEEDED);
  }

//+------------------------------------------------------------------+
//| runs every tick                                                  |
//+------------------------------------------------------------------+
void OnTick(void)
  {
//--- Bar level functionality
   if(IsNewBar(_Symbol, _Period))
     {
      //--- Need data or we've got nothing
      if(! RefreshData())
        {
         Print("Couldn't refresh data");
         return;
        }

      //--- See if a position is already open
      if(SelectPosition(_Symbol, MAGIC_NUMBER))
        {
         // Position is open, so adjust risk as necessary
         FixedTrailingStop();
        }
      else
        {
         //--- No position open, so look for signal
         CheckForOpenSignal();
        }

      //--- Draw every Doji whether we place a trade or not
      if(IsDoji(g_rates[1]))
        {
         DrawArrow(g_rates[1].time, g_rates[1].low - .333 * g_atr[1], ARROW_UP, Yellow, "Doji");
        }
     }
  }

//+------------------------------------------------------------------+
//| Runs on the TradeTransaction event (broker tells us things)      |
//+------------------------------------------------------------------+
void OnTradeTransaction(
   const MqlTradeTransaction&	trans,			// trade transaction structure
   const MqlTradeRequest&			request,			// request structure
   const MqlTradeResult&			result			// result structure
)
  {
   if(trans.deal > 0)
     {
      HistoryDealSelect(trans.deal);

      // Detect when an order is converted to a position, then delete any other pending order
      if((ENUM_DEAL_ENTRY)HistoryDealGetInteger(trans.deal, DEAL_ENTRY) == DEAL_ENTRY_IN)
        {
         if(! OrderDeletePending(_Symbol, MAGIC_NUMBER))
           {
            Print("Print! Could not delete pending orders");
            ExpertRemove();
           }
        }
     }
  }

//+------------------------------------------------------------------+
//| OPEN LOGIC                                                       |
//| Returns true if a signal was found (not if position opened).     |
//+------------------------------------------------------------------+
bool CheckForOpenSignal()
  {
//--- These vars are passed and filled in by the signal functions
   double			price = 0.;
   double			sl = 0.;			// initialize to no SL
   double			tp = 0.;			// initialize to no TP

//--- BUY STOP
   if(IsPendingBuySignal(price, sl, tp))
     {
      //--- additional checking
      if(Bars(_Symbol, _Period) > 100)
        {
         bool opened = OrderStop(g_trade,
                                 _Symbol,
                                 ORDER_TYPE_BUY_STOP,
                                 TRADE_VOLUME,
                                 price,
                                 sl,
                                 tp);

         //--- Draw some lines
         if(opened)
           {
            DrawLine(sl, Brown, STYLE_DOT);
            DrawLine(price, LightBlue, STYLE_DOT);
           }
         else
           {
            Print("Something bad happened");
           }
        }
     }

//--- SELL STOP
   if(IsPendingSellSignal(price, sl, tp))
     {
      //--- additional checking
      if(Bars(_Symbol, _Period) > 100)
        {
         bool opened = OrderStop(g_trade,
                                 _Symbol,
                                 ORDER_TYPE_SELL_STOP,
                                 TRADE_VOLUME,
                                 price,
                                 sl,
                                 tp);

         //--- Draw some lines
         if(opened)
           {
            DrawLine(sl, Brown, STYLE_DOT);
            DrawLine(price, LightBlue, STYLE_DOT);
           }
         else
           {
            Print("Something bad happened");
           }
        }
     }

   return false;
  }

//+------------------------------------------------------------------+
//| Checks STOPS and FREEZE levels                                   |
//+------------------------------------------------------------------+
bool CheckStopsLevel(const ENUM_ORDER_TYPE order_type,
                          const string symbol,
                          const double price,
                          const double sl)
  {
//--- shorthand constants make code more readable
   const double pointSize = SymbolInfoDouble(symbol, SYMBOL_POINT);
   const double minStopsLevel = SymbolInfoInteger(symbol, SYMBOL_TRADE_STOPS_LEVEL) * pointSize;
   const double ask = SymbolInfoDouble(symbol, SYMBOL_ASK);
   const double bid = SymbolInfoDouble(symbol, SYMBOL_BID);
   double distancePts = -1.;

   switch(order_type)
     {
      case ORDER_TYPE_BUY:
        {
         if(sl != NULL)
           {
            // SL distancePts to price.
            distancePts = bid - sl;
            if(distancePts <= minStopsLevel)
              {
               Print(StringFormat("%s(): INFO: For BUY, distancePts [%.5f] <= minStopsLevel [%.5f]", __FUNCTION__, distancePts, minStopsLevel));
               return false;
              }
           }
        }
      break;

      case ORDER_TYPE_SELL:
        {
         if(sl != NULL)
           {
            // SL distancePts to price.
            distancePts = (sl - ask);
            if(distancePts <= minStopsLevel)
              {
               Print(StringFormat("%s(): INFO: For SELL, distancePts [%.5f] <= minStopsLevel [%.5f]", __FUNCTION__, distancePts, minStopsLevel));
               return false;
              }
           }
        }
      break;

      case ORDER_TYPE_BUY_STOP:
        {
         if(price < ask)
           {
            Print(StringFormat("%s(): INFO: BUY_STOP price [%.5f] < ask [%.5f]", __FUNCTION__, price, ask));
            return false;
           }

         if(sl != NULL)
           {
            // SL distancePts to price.
            distancePts = (price - sl);
            if(distancePts < minStopsLevel)
              {
               Print(StringFormat("%s(): INFO: For BUY_STOP, distancePts [%.5f] <= minStopsLevel [%.5f]", __FUNCTION__, distancePts, minStopsLevel));
               return false;
              }
           }
        }
      break;

      case ORDER_TYPE_SELL_STOP:
        {
         if(price > bid)
           {
            Print(StringFormat("%s(): INFO: SELL_STOP price [%.5f] > bid [%.5f]", __FUNCTION__, price, bid));
            return false;
           }

         if(sl != NULL)
           {
            // SL distancePts to price.
            distancePts = (sl - price);
            if(distancePts < minStopsLevel)
              {
               Print(StringFormat("%s(): INFO: For SELL_STOP, distancePts [%.5f] <= minStopsLevel [%.5f]", __FUNCTION__, distancePts, minStopsLevel));
               return false;
              }
           }
        }
      break;

      default:
         Print(StringFormat("%s(): Unknown order_type [%s]", __FUNCTION__, EnumToString(order_type)));
         return false;
     }

   return true;
  }

const uchar ARROW_UP = 233;
const uchar ARROW_DOWN = 234;
void DrawArrow(const datetime time, double price, const uchar code, const color clr, string comment)
  {

   static int s_id = 0;		// to make unique string
   const string name = StringFormat("Arrow_%2d %s", s_id++, comment);

   if(!ObjectCreate(CHART_ID, name, OBJ_ARROW, SUB_WINDOW, time, price))
     {
      PrintFormat("%s(): Failed to create OBJ_TEXT. Error code: [%d]", __FUNCTION__, GetLastError());
      return;
     }

   ObjectSetInteger(CHART_ID, name, OBJPROP_COLOR, clr);
   ObjectSetInteger(CHART_ID, name, OBJPROP_ARROWCODE, code);
   ObjectSetInteger(CHART_ID, name, OBJPROP_WIDTH, 3);
  }

//+------------------------------------------------------------------+
//| Draws a line on the chart for easy viewing                       |
//+------------------------------------------------------------------+
void DrawLine(const double price, const color clr = Plum, const ENUM_LINE_STYLE lineStyle = STYLE_SOLID)
  {
   static int s_id = 0;			// to make unique string
   const string name = StringFormat("Line_%2d Price [%.5f]", s_id++, price);

   if(!ObjectCreate(CHART_ID, name, OBJ_TREND, SUB_WINDOW, g_rates[2].time, price, g_rates[0].time, price))
     {
      PrintFormat("%s(): Failed to create OBJ_TREND. Error code: [%d]", __FUNCTION__, GetLastError());
      return;
     }

   ObjectSetInteger(CHART_ID, name, OBJPROP_COLOR, clr);
   ObjectSetInteger(CHART_ID, name, OBJPROP_STYLE, lineStyle);
  }

//+------------------------------------------------------------------+
//| Trailing stop                                                    |
//| Risk handled at the bar level.                                   |
//+------------------------------------------------------------------+
bool FixedTrailingStop(void)
  {
   CPositionInfo	g_positionInfo;			// MQL standard library for positions
   double			slDPts = 0.;			// SL distance points
   double			plDPts = 0.;			// Profit/Loss distance points

//--- Get the step amount.
//--- This sort of assumes that the original SL was set at N*ATR in the first place;
//--- otherwise, you will get a big jump the first time, or it won't move at all.
   const double fixedDPts = AR_FIXED_TRAILING_PTS * g_pointSize;

   double price = 0.;							// Current price (ASK or BID)
   const double open = g_positionInfo.PriceOpen();
   const double sl = g_positionInfo.StopLoss();
   const ENUM_POSITION_TYPE positionType = g_positionInfo.PositionType();

   switch(positionType)
     {
      case POSITION_TYPE_BUY:
         price = SymbolInfoDouble(_Symbol, SYMBOL_BID);
         slDPts = price - sl;
         plDPts = price - open;		// Profit Loss is determined from the open
         break;
      case POSITION_TYPE_SELL:
         price = SymbolInfoDouble(_Symbol, SYMBOL_ASK);
         slDPts = sl - price;
         plDPts = open - price;
         break;
      default:
         PrintFormat("Unknown position type [%s]", EnumToString(positionType));
         ExpertRemove();
     }

//--- Is the distance from slDPts (price to SL) greater than the trailing ATR distance?
   if(slDPts > fixedDPts)
     {
      //--- Never move SL when unprofitable, e.g. lower volatility.
      const double spread = (double) SymbolInfoInteger(_Symbol, SYMBOL_SPREAD) * g_pointSize;
      if(plDPts < spread)
        {
         // not profitable
         return false;
        }

      double newSL = -1.;		// Start with ridiculous value
      switch(positionType)
        {
         case POSITION_TYPE_BUY:
            newSL = g_rates[1].close - fixedDPts;
            break;
         case POSITION_TYPE_SELL:
            newSL = g_rates[1].close + fixedDPts;
            break;
         default:
            PrintFormat("Unknown position type [%s]", EnumToString(positionType));
            ExpertRemove();
        }

      //--- Don't request a move if the new SL is essentially the same as the old one.
      if(MathAbs(newSL - sl) < g_pointSize)
        {
         return false;
        }

      //--- Move SL absolute
      bool modified = PositionModify(g_trade, g_positionInfo, _Symbol, newSL, NULL);
      if(modified)
        {
         DrawLine(newSL, Red);
        }
      return modified;
     }
   return false;
  }

//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double FixLotSize(const double lotSize, const string symbol)
  {
// Lot STEP
   double lotStep = SymbolInfoDouble(symbol, SYMBOL_VOLUME_STEP);

   double normalizedLotSize = MathFloor(lotSize / lotStep) * lotStep;

   double minLot = SymbolInfoDouble(symbol, SYMBOL_VOLUME_MIN);
   double maxLot = SymbolInfoDouble(symbol, SYMBOL_VOLUME_MAX);
   if(normalizedLotSize < minLot)
      normalizedLotSize = minLot;
   if(normalizedLotSize > maxLot)
      normalizedLotSize = maxLot;

   return normalizedLotSize;
  }

//+------------------------------------------------------------------+
//| Body <= 3% of Bar                                                |
//| Does not consider position of body, e.g. gravestone or dragonfly |
//+------------------------------------------------------------------+
bool IsDoji(MqlRates& rate)
  {
   const double DOJI_PCT = 0.03;		// Assume 3% or less body size for Doji

   const double sizeBody = MathAbs(rate.open - rate.close);
   const double sizeBar = (rate.high - rate.low);
   return (sizeBody <= DOJI_PCT * sizeBar);
  }

//--- Detects when a "new bar" occurs, which is the same as when the previous bar has completed.
bool IsNewBar(const string symbol, const ENUM_TIMEFRAMES period)
  {
   bool isNewBar = false;
   static datetime priorBarOpenTime = NULL;

//--- SERIES_LASTBAR_DATE == Open time of the last bar of the symbol-period
   const datetime currentBarOpenTime = (datetime) SeriesInfoInteger(symbol, period, SERIES_LASTBAR_DATE);

   if(priorBarOpenTime != currentBarOpenTime)
     {
      //--- Don't want new bar just because EA started
      isNewBar = (priorBarOpenTime == NULL) ? false : true;	// priorBarOpenTime is only NULL once

      //--- Regardless of new bar, update the held bar time
      priorBarOpenTime = currentBarOpenTime;
     }

   return isNewBar;
  }

//+------------------------------------------------------------------+
//| Strategy Signal for Buy                                          |
//+------------------------------------------------------------------+
bool IsPendingBuySignal(double &price, double &sl, double &tp)
  {
   if(! IsDoji(g_rates[1]))
      return false;

   price = g_rates[1].high;
   sl = g_rates[1].low - SL_ATR_FACTOR * g_atr[1];

   return true;
  }

//+------------------------------------------------------------------+
//| Strategy Signal for Sell                                         |
//+------------------------------------------------------------------+
bool IsPendingSellSignal(double &price, double &sl, double &tp)
  {
   if(! IsDoji(g_rates[1]))
      return false;

   price = g_rates[1].low;
   sl = g_rates[1].high + SL_ATR_FACTOR * g_atr[1];

   return true;
  }

//+------------------------------------------------------------------+
//| Delete all pending orders                                        |
//+------------------------------------------------------------------+
bool OrderDeletePending(const string symbol, const long magicNumber)
  {
   COrderInfo order;

   int numOrders = OrdersTotal();
   for(int i = numOrders - 1; i >= 0; i--)
     {
      if(order.SelectByIndex(i))
        {
         // It appears that ORDER_MAGIC doesn't exist until the order is closed. ???
         if(order.Symbol() == symbol && order.Magic() == magicNumber)
           {
            switch(order.OrderType())
              {
               // For any pending order type
               case ORDER_TYPE_BUY_STOP:
               case ORDER_TYPE_SELL_STOP:
               case ORDER_TYPE_BUY_LIMIT:
               case ORDER_TYPE_SELL_LIMIT:
                  PrintFormat("	%s(): INFO:About to delete PND order, ticket [%d] type [%s]",
                              __FUNCTION__,
                              order.Ticket(),
                              EnumToString(order.OrderType()));

                  if(! g_trade.OrderDelete(order.Ticket()))
                    {
                     return false;
                    }
                  break;
               default:
                  PrintFormat("Unknown order type [%s]", EnumToString(order.OrderType()));
                  //ExpertRemove();
              }
           }
        }
     }

   return true;
  }

//+------------------------------------------------------------------+
//| Place a STOP order                                               |
//+------------------------------------------------------------------+
bool OrderStop(CTrade& trade,
               const string symbol,
               const ENUM_ORDER_TYPE order_type,
               double volume,
               const double price,
               const double sl = 0.,
               const double tp = 0.)
  {

//--- additional checking
   if(! TerminalInfoInteger(TERMINAL_TRADE_ALLOWED))
     {
      PrintFormat("%s(%s): Error [TERMINAL_TRADE_ALLOWED]", __FUNCTION__, symbol);
      return false;
     }

//--- ensure volume is legit, even though it's a constant.
//--- This is a good idea anyway; but I put it in this code specifically to pass the MQL5.com validator.
   volume = FixLotSize(volume, symbol);

//--- Ensure we have enough money for the trade
   CAccountInfo account;
   const ENUM_ORDER_TYPE direction = (order_type == ORDER_TYPE_BUY_STOP) ? ORDER_TYPE_BUY : ORDER_TYPE_SELL;
   const double freeMargin = account.FreeMarginCheck(symbol, direction, volume, price);
   if(freeMargin < 0.00)
     {
      Print("Error! Not enough money");
      return false;
     }

//--- Ensure TP and SL meet minimum server levels.
   if(! CheckStopsLevel(order_type, symbol, price, sl))
     {
      return false;
     }

//--- enum to string
   if(order_type == ORDER_TYPE_BUY_STOP)
     {
      if(!trade.BuyStop(volume, price, symbol, sl, tp, ORDER_TIME_GTC, NULL, NULL))
        {
         PrintFormat("%s(%s) Error: Retcode [%d] : '%s'", __FUNCTION__, symbol, trade.ResultRetcode(), trade.ResultComment());
         return false;
        }
     }
   else
      if(order_type == ORDER_TYPE_SELL_STOP)
        {
         if(!trade.SellStop(volume, price, symbol, sl, tp, ORDER_TIME_GTC, NULL, NULL))
           {
            PrintFormat("%s(%s) Error: Retcode [%d] : '%s'", __FUNCTION__, symbol, trade.ResultRetcode(), trade.ResultComment());
            return false;
           }
        }
      else
        {
         PrintFormat("%s(): Print! Unknown order type [%s]", __FUNCTION__, EnumToString(order_type));
        }

   return true;
  }

//+------------------------------------------------------------------+
//| Modify existing position                                         |
//+------------------------------------------------------------------+
bool PositionModify(CTrade& trade,
                    CPositionInfo& position,
                    const string symbol,
                    const double sl,
                    double tp = NULL)
  {

//--- additional checking
   if(! TerminalInfoInteger(TERMINAL_TRADE_ALLOWED))
     {
      PrintFormat("%s(%s): Error [TERMINAL_TRADE_ALLOWED]", __FUNCTION__, symbol);
      return false;
     }

   const ENUM_POSITION_TYPE positionType = position.PositionType();
   const double price = position.PriceOpen();
   if(tp == NULL)
     {
      tp = position.TakeProfit();
     }

//--- Ensure TP and SL meet minimum server levels.
   const ENUM_ORDER_TYPE orderType = (positionType == POSITION_TYPE_BUY) ? ORDER_TYPE_BUY : ORDER_TYPE_SELL;
   if(! CheckStopsLevel(orderType, symbol, price, sl))
     {
      return false;
     }

//--- Do a broker modify
   bool modified = trade.PositionModify(symbol, sl, tp);
   if(!modified)
     {
      PrintFormat("%s(%s) Error: Retcode [%d] : '%s'", __FUNCTION__, symbol, trade.ResultRetcode(), trade.ResultComment());
      return false;
     }

   return true;
  }

//+------------------------------------------------------------------+
//| Gets necessary rates and indicator data                          |
//+------------------------------------------------------------------+
bool RefreshData(void)
  {
//--- go trading only for first ticks of new bar
   if(CopyRates(_Symbol, _Period, 0, NUM_RATES, g_rates) != NUM_RATES)
     {
      Print("CopyRates of ", _Symbol, " failed, no history");
      return false;
     }

//--- get current ATR
   if(g_handleAtr != INVALID_HANDLE)
     {
      if(CopyBuffer(g_handleAtr, 0, 0, NUM_RATES, g_atr) != NUM_RATES)
        {
         Print("CopyBuffer from iATR failed, no data");
         return false;
        }
     }

   return true;
  }

// Determines if we have an open position or not
bool SelectPosition(const string symbol, const ulong magicNumber)
  {
//--- Do we have an open position?
   if(!PositionSelect(symbol))
     {
      //--- No position is open for this symbol
      return false;
     }
   else
     {
      //--- We found a position for this symbol, but is it one of ours?
      return(PositionGetInteger(POSITION_MAGIC) == magicNumber);
     }

   return false;
  }
//+------------------------------------------------------------------+
